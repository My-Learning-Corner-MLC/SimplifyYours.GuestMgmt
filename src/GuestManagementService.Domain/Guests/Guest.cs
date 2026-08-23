namespace GuestManagementService.Domain.Guests;

public sealed class Guest
{
    private Guest()
    {
    }

    private Guest(
        Guid id,
        Guid eventId,
        Guid tenantId,
        string firstName,
        string lastName,
        string phoneNumber,
        string normalizedPhoneNumber,
        string? emailAddress,
        string? normalizedEmailAddress,
        Gender gender,
        string? metadata,
        string invitationToken,
        DateTimeOffset createdAt)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        EmailAddress = emailAddress;
        NormalizedEmailAddress = normalizedEmailAddress;
        Gender = gender;
        Metadata = metadata;
        InvitationToken = invitationToken;
        DeliveryStatus = DeliveryStatus.NotSent;
        RsvpStatus = RsvpStatus.NoResponse;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid TenantId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string NormalizedPhoneNumber { get; private set; } = string.Empty;

    public string? EmailAddress { get; private set; }

    public string? NormalizedEmailAddress { get; private set; }

    public Gender Gender { get; private set; } = Gender.PreferNotToSay;

    // Event-type-specific attributes serialized as JSON (jsonb column). Each event type
    // has its own metadata shape (e.g. wedding: relationship, side, plus-ones, dietary notes);
    // the domain keeps it opaque so new event types can add fields without a schema change.
    public string? Metadata { get; private set; }

    /// <summary>
    /// Opaque, unguessable identifier for this guest's invitation link. Minted at creation rather
    /// than at send time so the link exists whether or not an invitation is ever emailed —
    /// the organiser can copy it and share it themselves.
    /// </summary>
    public string InvitationToken { get; private set; } = string.Empty;

    public DeliveryStatus DeliveryStatus { get; private set; }

    public RsvpStatus RsvpStatus { get; private set; }

    /// <summary>When the guest first responded. Later edits to the answer do not move it.</summary>
    public DateTimeOffset? RespondedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Records the guest's own RSVP.
    /// </summary>
    /// <remarks>
    /// <paramref name="metadata"/> already carries the guest's answers merged over the organiser's
    /// fields — the caller builds it so that the organiser's plus-ones allowance survives untouched.
    /// <see cref="RespondedAt"/> marks the <em>first</em> response and is not moved by later edits,
    /// so "when did they reply" stays answerable after someone changes their mind.
    /// </remarks>
    public void RecordRsvp(RsvpStatus status, string? metadata, DateTimeOffset respondedAt)
    {
        if (status == RsvpStatus.NoResponse)
        {
            throw new ArgumentException("A recorded RSVP cannot be NoResponse.", nameof(status));
        }

        RsvpStatus = status;
        Metadata = NormalizeOptionalText(metadata);
        RespondedAt ??= respondedAt.ToUniversalTime();
        UpdatedAt = respondedAt.ToUniversalTime();
    }

    public static Guest Create(
        Guid id,
        Guid eventId,
        Guid tenantId,
        string firstName,
        string lastName,
        string phoneNumber,
        string normalizedPhoneNumber,
        string? emailAddress,
        string? normalizedEmailAddress,
        Gender gender,
        string? metadata,
        string invitationToken,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Guest id must not be empty.", nameof(id));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        var normalizedFirstName = NormalizeRequiredText(firstName, nameof(firstName));
        var normalizedLastName = NormalizeRequiredText(lastName, nameof(lastName));
        var normalizedPhone = NormalizeRequiredText(phoneNumber, nameof(phoneNumber));
        var comparablePhone = NormalizeRequiredText(normalizedPhoneNumber, nameof(normalizedPhoneNumber));
        var cleanEmail = NormalizeOptionalText(emailAddress);
        var comparableEmail = NormalizeOptionalText(normalizedEmailAddress);
        var cleanMetadata = NormalizeOptionalText(metadata);
        var cleanToken = NormalizeRequiredText(invitationToken, nameof(invitationToken));

        return new Guest(
            id,
            eventId,
            tenantId,
            normalizedFirstName,
            normalizedLastName,
            normalizedPhone,
            comparablePhone,
            cleanEmail,
            comparableEmail,
            gender,
            cleanMetadata,
            cleanToken,
            createdAt.ToUniversalTime());
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
