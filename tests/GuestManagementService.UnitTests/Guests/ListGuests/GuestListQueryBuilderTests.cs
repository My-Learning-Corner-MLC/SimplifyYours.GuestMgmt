using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Guests.ListGuests;
using GuestManagementService.Domain.Guests;

namespace GuestManagementService.UnitTests.Guests.ListGuests;

public sealed class GuestListQueryBuilderTests
{
    private static readonly Guid TestTenantId = Guid.Parse("8f10c8b2-12a4-4d6f-9301-7c52e84b7d20");
    private static readonly Guid TestEventId = Guid.Parse("1c2a3b4c-5d6e-4f70-8a91-2b3c4d5e6f70");
    private readonly DateTimeOffset _now = new(2026, 5, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ApplyFilters_ExcludesGuestsFromOtherEvents()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Bea", "Other", "bea@example.com", Guid.NewGuid(), TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplyFilters(guests.AsQueryable(), CreateOptions())
            .ToArray();

        var item = Assert.Single(result);
        Assert.Equal("Ada", item.FirstName);
    }

    [Fact]
    public void ApplyFilters_ExcludesGuestsFromOtherTenants()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Cleo", "Other", "cleo@example.com", TestEventId, Guid.NewGuid(), _now)
        };

        var result = GuestListQueryBuilder
            .ApplyFilters(guests.AsQueryable(), CreateOptions())
            .ToArray();

        var item = Assert.Single(result);
        Assert.Equal("Ada", item.FirstName);
    }

    [Fact]
    public void ApplyFilters_WhenSearchMatchesName_ReturnsMatchingGuests()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Bea", "Sample", "bea@example.com", TestEventId, TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplyFilters(guests.AsQueryable(), CreateOptions(search: "ada"))
            .ToArray();

        var item = Assert.Single(result);
        Assert.Equal("Ada", item.FirstName);
    }

    [Fact]
    public void ApplyFilters_WhenSearchMatchesEmail_ReturnsMatchingGuests()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Bea", "Sample", "bea@example.com", TestEventId, TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplyFilters(guests.AsQueryable(), CreateOptions(search: "bea@"))
            .ToArray();

        var item = Assert.Single(result);
        Assert.Equal("Bea", item.FirstName);
    }

    [Fact]
    public void ApplySorting_WhenNameAscending_ReturnsByLastNameThenFirstName()
    {
        var guests = new[]
        {
            CreateGuest("Zack", "Adams", "zack@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Ada", "Baker", "ada@example.com", TestEventId, TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplySorting(guests.AsQueryable(), GuestSortField.Name, SortDirection.Asc)
            .Select(guest => guest.FirstName)
            .ToArray();

        Assert.Equal(new[] { "Zack", "Ada" }, result);
    }

    [Fact]
    public void ApplySorting_WhenEmailDescending_ReturnsNewestEmailFirst()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now),
            CreateGuest("Bea", "Sample", "bea@example.com", TestEventId, TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplySorting(guests.AsQueryable(), GuestSortField.Email, SortDirection.Desc)
            .Select(guest => guest.EmailAddress)
            .ToArray();

        Assert.Equal(new[] { "bea@example.com", "ada@example.com" }, result);
    }

    [Fact]
    public void ApplySorting_WhenCreatedAtDescending_ReturnsNewestFirst()
    {
        var guests = new[]
        {
            CreateGuest("Ada", "Tester", "ada@example.com", TestEventId, TestTenantId, _now.AddDays(-1)),
            CreateGuest("Bea", "Sample", "bea@example.com", TestEventId, TestTenantId, _now)
        };

        var result = GuestListQueryBuilder
            .ApplySorting(guests.AsQueryable(), GuestSortField.CreatedAt, SortDirection.Desc)
            .Select(guest => guest.FirstName)
            .ToArray();

        Assert.Equal(new[] { "Bea", "Ada" }, result);
    }

    private GuestListQueryOptions CreateOptions(string? search = null)
    {
        return new GuestListQueryOptions(
            TestEventId,
            TestTenantId,
            1,
            20,
            search,
            GuestSortField.CreatedAt,
            SortDirection.Desc);
    }

    private static Guest CreateGuest(
        string firstName,
        string lastName,
        string email,
        Guid eventId,
        Guid tenantId,
        DateTimeOffset createdAt)
    {
        return Guest.Create(
            Guid.NewGuid(),
            eventId,
            tenantId,
            firstName,
            lastName,
            "+15551234567",
            "+15551234567",
            email,
            email,
            Gender.PreferNotToSay,
            null,
            null,
            createdAt);
    }
}
