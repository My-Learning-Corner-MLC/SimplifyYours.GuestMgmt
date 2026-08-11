using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Invitations;
using GuestManagementService.Application.Invitations.GetInvitation;
using GuestManagementService.Application.Invitations.SubmitRsvp;
using GuestManagementService.Contracts.Invitations;
using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

/// <summary>
/// The public, unauthenticated invitation surface.
/// </summary>
/// <remarks>
/// These endpoints are anonymous by design — the invitation token is the only credential. They sit
/// under <c>/guests</c> so the gateway's existing <c>/api/v1/guests/{**remainder}</c> route already
/// forwards them, and rely on <c>InvitationRateLimits</c> for throttling.
/// <para>
/// Authorization in this service is opt-in per endpoint, so anything added here is public unless
/// it explicitly says otherwise. Treat that as a standing review item.
/// </para>
/// </remarks>
public static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guests/invitations").WithTags("Invitations");

        group
            .MapGet("{token}", GetInvitationAsync)
            .WithName("GetInvitation")
            .AllowAnonymous();

        group
            .MapGet("{token}/render", RenderInvitationAsync)
            .WithName("RenderInvitation")
            .AllowAnonymous();

        // POST only. Email link-scanners pre-fetch every URL they see, so a GET that recorded an
        // answer would manufacture phantom RSVPs from bots.
        group
            .MapPost("{token}/rsvp", SubmitRsvpAsync)
            .WithName("SubmitRsvp")
            .AllowAnonymous();

        return endpoints;
    }

    internal static async Task<IResult> GetInvitationAsync(
        string token,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvitationQuery(token), cancellationToken);

        if (result.Status != GetInvitationStatus.Found)
        {
            // One message for every failure — unknown token, malformed token, deleted event.
            // Distinguishing them would let someone probe which tokens are real.
            return ApiErrorResults.NotFound("This invitation could not be found.", httpContext);
        }

        ApplyPublicHeaders(httpContext);

        return Results.Ok(ToResponse(result.Guest!, result.Event!, result.Deadline, result.IsOpen));
    }

    internal static async Task<IResult> RenderInvitationAsync(
        string token,
        HttpContext httpContext,
        ISender sender,
        IInvitationRenderer renderer,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvitationQuery(token), cancellationToken);

        if (result.Status != GetInvitationStatus.Found)
        {
            return ApiErrorResults.NotFound("This invitation could not be found.", httpContext);
        }

        ApplyPublicHeaders(httpContext);

        // The app must be able to frame this, so frame-ancestors names the SPA origin rather than
        // denying framing outright.
        var spaOrigin = configuration["Invitations:PublicBaseUrl"]?.TrimEnd('/') ?? "'self'";
        httpContext.Response.Headers.ContentSecurityPolicy = $"frame-ancestors {spaOrigin}";

        var html = renderer.Render(
            ToRenderModel(result.Guest!, result.Event!),
            result.Event!.EventType);

        return Results.Content(html, "text/html; charset=utf-8");
    }

    internal static async Task<IResult> SubmitRsvpAsync(
        string token,
        SubmitRsvpRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new SubmitRsvpCommand(
                    token,
                    request?.RsvpStatus,
                    request?.PlusOnesConfirmed,
                    request?.DietaryNotes),
                cancellationToken);

            ApplyPublicHeaders(httpContext);

            return result.Status switch
            {
                SubmitRsvpStatus.Accepted => Results.Ok(
                    ToResponse(result.Guest!, result.Event!, result.Deadline, isOpen: true)),
                SubmitRsvpStatus.Closed => ApiErrorResults.Conflict(
                    "Responses for this event are closed.",
                    httpContext),
                _ => ApiErrorResults.NotFound("This invitation could not be found.", httpContext),
            };
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    private static Dictionary<string, string[]> ToValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
    }

    public static InvitationRenderModel ToRenderModel(Guest guest, EventReference eventReference)
    {
        return new InvitationRenderModel(
            guest.FirstName,
            eventReference.EventName,
            eventReference.EventDate?.ToString("D") ?? string.Empty,
            eventReference.EventStartTime?.ToString("t") ?? string.Empty,
            eventReference.VenueName ?? string.Empty,
            eventReference.VenueAddress ?? string.Empty,
            eventReference.VenueNotes ?? string.Empty);
    }

    /// <summary>
    /// Response carries a named person, so it must never be cached by a shared cache or indexed by
    /// a search engine — that would put guests' names and an event's address into search results.
    /// </summary>
    public static void ApplyPublicHeaders(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    public static GetInvitationResponse ToResponse(
        Guest guest,
        EventReference eventReference,
        DateTimeOffset? deadline,
        bool isOpen)
    {
        var venue = eventReference.VenueName is null
                    && eventReference.VenueAddress is null
                    && eventReference.VenueNotes is null
            ? null
            : new InvitationVenueResponse(
                eventReference.VenueName,
                eventReference.VenueAddress,
                eventReference.VenueNotes);

        return new GetInvitationResponse(
            // First name only. The page greets the guest; it does not need to prove who they are.
            guest.FirstName,
            new InvitationEventResponse(
                eventReference.EventName,
                eventReference.EventDate,
                eventReference.EventStartTime,
                eventReference.EventEndTime,
                eventReference.TimeZoneId,
                eventReference.EventDescription,
                venue),
            new InvitationRsvpResponse(
                guest.RsvpStatus.ToString(),
                InvitationMetadata.ReadPlusOnesAllowed(guest.Metadata),
                InvitationMetadata.ReadPlusOnesConfirmed(guest.Metadata),
                InvitationMetadata.ReadDietaryNotes(guest.Metadata),
                deadline,
                isOpen));
    }
}
