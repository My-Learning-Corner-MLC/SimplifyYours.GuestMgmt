using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Invitations;
using GuestManagementService.Application.Invitations.GetInvitationSettings;
using GuestManagementService.Application.Invitations.IssuePreviewToken;
using GuestManagementService.Application.Invitations.RotatePublicToken;
using GuestManagementService.Application.Invitations.SaveInvitationSettings;
using GuestManagementService.Contracts.Invitations;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

/// <summary>
/// The organiser's view of the invitation they are composing. Authenticated throughout — the guest
/// -facing surface is <see cref="InvitationEndpoints"/>.
/// </summary>
/// <remarks>
/// Nested under <c>/invitations/settings/events/{eventId}</c>. "settings" is the one reserved
/// first-segment literal under the shared <c>/invitations</c> prefix (see
/// <see cref="InvitationEndpoints"/>'s remarks) — it can never collide with an anonymous invitation
/// token, so the anonymous surface and this authenticated one can share one prefix safely.
/// </remarks>
public static class InvitationSettingsEndpoints
{
    public static IEndpointRouteBuilder MapInvitationSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/invitations/settings/events/{eventId:guid}")
            .WithTags("InvitationSettings");

        group
            .MapGet("", GetAsync)
            .WithName("GetInvitationSettings")
            .RequireAuthorization(Permissions.EventsView);

        // Saving also carries the public-link enable/disable action — see
        // SaveInvitationSettingsCommandHandler.ApplyPublicLinkChange. There is deliberately no
        // separate enable/disable endpoint: composing content and turning on the link it's shared
        // through are one organiser action, not two.
        group
            .MapPut("", SaveAsync)
            .WithName("SaveInvitationSettings")
            .RequireAuthorization(Permissions.EventsUpdate);

        // Rotating is narrower than enable/disable: "give me a fresh link" while staying enabled,
        // not a visibility toggle — so it stays its own action rather than folding into the save.
        group
            .MapPost("public-token", RotatePublicTokenAsync)
            .WithName("RotateInvitationPublicToken")
            .RequireAuthorization(Permissions.EventsUpdate);

        group
            .MapPost("preview-token", IssuePreviewTokenAsync)
            .WithName("IssueInvitationPreviewToken")
            .RequireAuthorization(Permissions.EventsUpdate);

        return endpoints;
    }

    internal static async Task<IResult> GetAsync(
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new GetInvitationSettingsQuery(eventId), cancellationToken);

            if (result.Status != GetInvitationSettingsStatus.Found)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new InvitationSettingsResponse(
                eventId,
                result.EventType,
                result.TemplateId,
                result.FieldValues ?? new Dictionary<string, string?>(),
                result.IsConfigured,
                InvitationFieldSchema.RequiredFor(result.EventType),
                result.PublicLinkEnabled,
                result.PublicEventToken));
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    internal static async Task<IResult> SaveAsync(
        Guid eventId,
        SaveInvitationSettingsRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new SaveInvitationSettingsCommand(
                    eventId,
                    request?.TemplateId,
                    request?.FieldValues,
                    request?.PublicLinkEnabled),
                cancellationToken);

            if (result.Status != SaveInvitationSettingsStatus.Saved)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new InvitationSettingsResponse(
                eventId,
                result.EventType,
                result.TemplateId,
                result.FieldValues ?? new Dictionary<string, string?>(),
                IsConfigured: true,
                InvitationFieldSchema.RequiredFor(result.EventType),
                result.PublicLinkEnabled,
                result.PublicEventToken));
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    internal static async Task<IResult> RotatePublicTokenAsync(
        Guid eventId,
        SetPublicTokenRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request?.Action, "rotate", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrorResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["action"] = ["'action' must be \"rotate\"."],
                },
                httpContext);
        }

        try
        {
            var result = await sender.Send(new RotatePublicTokenCommand(eventId), cancellationToken);

            if (result.Status != RotatePublicTokenStatus.Rotated)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new PublicTokenResponse(Enabled: true, result.PublicEventToken));
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    internal static async Task<IResult> IssuePreviewTokenAsync(
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new IssuePreviewTokenCommand(eventId), cancellationToken);

            if (result.Status != IssuePreviewTokenStatus.Issued)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new PreviewTokenResponse(result.Token!, result.ExpiresAt!.Value));
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
}
