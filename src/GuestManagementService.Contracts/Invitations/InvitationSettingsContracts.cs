namespace GuestManagementService.Contracts.Invitations;

public sealed record InvitationSettingsResponse(
    Guid EventId,
    string EventType,
    string? TemplateId,
    IReadOnlyDictionary<string, string?> FieldValues,
    /// <summary>
    /// False when nothing has been saved and FieldValues are pre-fill defaults derived from the
    /// event. Lets the UI say "not yet configured" rather than implying the defaults were chosen.
    /// </summary>
    bool IsConfigured,
    IReadOnlyCollection<string> RequiredFields);

public sealed record SaveInvitationSettingsRequest(
    string? TemplateId,
    IReadOnlyDictionary<string, string?>? FieldValues);

public sealed record SetPublicLinkRequest(bool Enabled);

/// <summary>Token is the raw value the frontend needs to build the shareable URL.</summary>
public sealed record PublicLinkResponse(bool Enabled, string? PublicEventToken);

public sealed record PreviewTokenResponse(string Token, DateTimeOffset ExpiresAt);
