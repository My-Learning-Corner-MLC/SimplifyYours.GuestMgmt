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
    IReadOnlyCollection<string> RequiredFields,
    bool PublicLinkEnabled,
    /// <summary>Null unless the public link is enabled. Raw value the frontend needs to build the shareable URL.</summary>
    string? PublicEventToken);

public sealed record SaveInvitationSettingsRequest(
    string? TemplateId,
    IReadOnlyDictionary<string, string?>? FieldValues,
    /// <summary>
    /// Omitted (null) leaves the public link untouched. True enables it (minting a token if none
    /// exists); false disables it and revokes the token. Folded into this same save call rather than
    /// a separate endpoint — composing content and turning on the link it's shared through are one
    /// organiser action.
    /// </summary>
    bool? PublicLinkEnabled = null);

/// <summary>
/// Body for <c>POST /invitations/settings/events/{id}/public-token</c>. "rotate" is the only
/// supported action today — enable/disable live on the main settings save instead of here, because
/// rotating is a narrower, distinct action: "give me a fresh link", not "turn it on/off".
/// </summary>
public sealed record SetPublicTokenRequest(string Action);

/// <summary>Token is the raw value the frontend needs to build the shareable URL.</summary>
public sealed record PublicTokenResponse(bool Enabled, string? PublicEventToken);

public sealed record PreviewTokenResponse(string Token, DateTimeOffset ExpiresAt);
