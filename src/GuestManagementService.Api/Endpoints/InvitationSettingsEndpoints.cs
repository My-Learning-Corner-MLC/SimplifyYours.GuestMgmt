using FluentValidation;
using GuestManagementService.Api.Responses;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Invitations;
using GuestManagementService.Application.Invitations.GetInvitationSettings;
using GuestManagementService.Application.Invitations.IssuePreviewToken;
using GuestManagementService.Application.Invitations.RevokePublicLink;
using GuestManagementService.Application.Invitations.SaveInvitationSettings;
using GuestManagementService.Application.Invitations.SetPublicLink;
using GuestManagementService.Contracts.Invitations;
using MediatR;

namespace GuestManagementService.Api.Endpoints;

/// <summary>
/// The organiser's view of the invitation they are composing. Authenticated throughout — the guest
/// -facing surface is <see cref="InvitationEndpoints"/>.
/// </summary>
public static class InvitationSettingsEndpoints
{
    public static IEndpointRouteBuilder MapInvitationSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/events/{eventId:guid}/invitation-settings")
            .WithTags("InvitationSettings");

        group
            .MapGet("", GetAsync)
            .WithName("GetInvitationSettings")
            .RequireAuthorization(Permissions.GuestsView);

        group
            .MapPut("", SaveAsync)
            .WithName("SaveInvitationSettings")
            .RequireAuthorization(Permissions.EventsUpdate);

        // Public link and preview issuance are composing-the-invitation actions, same as saving the
        // template and content, so they share EventsUpdate rather than introducing a new permission.
        group
            .MapPut("public-link", SetPublicLinkAsync)
            .WithName("SetInvitationPublicLink")
            .RequireAuthorization(Permissions.EventsUpdate);

        group
            .MapPost("public-link/revoke", RevokePublicLinkAsync)
            .WithName("RevokeInvitationPublicLink")
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
                InvitationFieldSchema.RequiredFor(result.EventType)));
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
                new SaveInvitationSettingsCommand(eventId, request?.TemplateId, request?.FieldValues),
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
                InvitationFieldSchema.RequiredFor(result.EventType)));
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    internal static async Task<IResult> SetPublicLinkAsync(
        Guid eventId,
        SetPublicLinkRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new SetPublicLinkCommand(eventId, request.Enabled), cancellationToken);

            if (result.Status != SetPublicLinkStatus.Updated)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new PublicLinkResponse(result.Enabled, result.PublicEventToken));
        }
        catch (ValidationException exception)
        {
            return ApiErrorResults.ValidationProblem(ToValidationErrors(exception), httpContext);
        }
    }

    internal static async Task<IResult> RevokePublicLinkAsync(
        Guid eventId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new RevokePublicLinkCommand(eventId), cancellationToken);

            if (result.Status != RevokePublicLinkStatus.Revoked)
            {
                return ApiErrorResults.NotFound(
                    "The event was not found. It may have been deleted or the id may be incorrect.",
                    httpContext);
            }

            return Results.Ok(new PublicLinkResponse(Enabled: true, result.PublicEventToken));
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
