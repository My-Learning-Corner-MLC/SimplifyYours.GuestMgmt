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

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

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
