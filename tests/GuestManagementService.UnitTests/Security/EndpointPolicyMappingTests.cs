using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Abstractions.Invitations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GuestManagementService.UnitTests.Security;

public class EndpointPolicyMappingTests
{
    [Fact]
    public void AddGuest_endpoint_requires_guests_add_policy()
    {
        var endpoints = MapGuestEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "AddGuest", StringComparison.Ordinal));

        Assert.NotNull(endpoint);

        var policies = endpoint!.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        Assert.Contains(Permissions.GuestsAdd, policies);
    }

    [Fact]
    public void Guest_endpoints_cover_exactly_the_expected_policy_set()
    {
        var endpoints = MapGuestEndpointsForTest();

        var policies = endpoints
            .SelectMany(e => e.Metadata.GetOrderedMetadata<IAuthorizeData>())
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        // One entry per protected endpoint: AddGuest, QueryGuests, GetGuestInvitationLink.
        // Deliberately a multiset rather than a distinct set — authorization here is opt-in per
        // endpoint, so a forgotten RequireAuthorization silently makes an endpoint public and this
        // assertion is what catches it.
        Assert.Equal(
            new[] { Permissions.GuestsAdd, Permissions.GuestsView, Permissions.GuestsView }.OrderBy(p => p),
            policies.OrderBy(p => p));
    }

    [Fact]
    public void GetInvitationLink_endpoint_requires_guests_view_policy()
    {
        var endpoints = MapGuestEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "GetGuestInvitationLink", StringComparison.Ordinal));

        Assert.NotNull(endpoint);

        var policies = endpoint!.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        // The invitation token is credential-like: whoever holds it can read a guest's personal
        // data without authenticating. This endpoint must never become anonymous.
        Assert.Contains(Permissions.GuestsView, policies);
    }

    [Fact]
    public void ListGuests_endpoint_requires_guests_view_policy()
    {
        var endpoints = MapGuestEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "QueryGuests", StringComparison.Ordinal));

        Assert.NotNull(endpoint);

        var policies = endpoint!.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        Assert.Contains(Permissions.GuestsView, policies);
    }

    // AddInfrastructure is intentionally omitted so the test does not attempt
    // a Postgres / Kafka / Redis connection. WebApplication.CreateBuilder() is
    // used without arguments per plan Task 8: the test stays infrastructure-free
    // by only registering the services that minimal-API endpoint construction
    // actually requires (routing, permission policies, ISender for handler
    // parameter binding).
    private static IReadOnlyList<Endpoint> MapGuestEndpointsForTest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddPermissionPolicies();
        builder.Services.AddSingleton(Mock.Of<ISender>());

        var app = builder.Build();
        app.MapGuestEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .ToList();
    }

    [Fact]
    public void Invitation_settings_endpoints_are_both_protected()
    {
        // Authorization is opt-in per endpoint in this service, so an endpoint added to this group
        // without a policy would be silently public. Reading is guests.view; writing reuses
        // events.update, because composing an invitation is editing the event's presentation.
        var endpoints = MapInvitationSettingsEndpointsForTest();

        var policies = endpoints
            .SelectMany(e => e.Metadata.GetOrderedMetadata<IAuthorizeData>())
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .Distinct()
            .OrderBy(policy => policy)
            .ToArray();

        // Reading is guests.view; every write action — saving, the public-link toggle, revoke, and
        // preview issuance — reuses events.update, because each is a way of composing the event's
        // presentation. No endpoint in this group may end up with no policy at all.
        Assert.Equal(new[] { Permissions.EventsUpdate, Permissions.GuestsView }.OrderBy(p => p), policies);
        Assert.All(endpoints, e => Assert.NotEmpty(e.Metadata.GetOrderedMetadata<IAuthorizeData>()));
    }

    [Fact]
    public void Invitation_settings_save_requires_events_update()
    {
        var endpoints = MapInvitationSettingsEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "SaveInvitationSettings", StringComparison.Ordinal));

        Assert.NotNull(endpoint);
        Assert.Contains(
            Permissions.EventsUpdate,
            endpoint!.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(d => d.Policy));
    }

    private static IReadOnlyList<Endpoint> MapInvitationSettingsEndpointsForTest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddPermissionPolicies();
        builder.Services.AddSingleton(Mock.Of<ISender>());

        var app = builder.Build();
        app.MapInvitationSettingsEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .ToList();
    }

    [Fact]
    public void Invitation_endpoints_are_all_anonymous()
    {
        // The mirror image of the settings test. These three carry a guest's name and an event's
        // address, and the invitation token is their only credential — but "anonymous" here has to
        // be deliberate, not accidental. A future RequireAuthorization() added to "harden" one of
        // them would lock every guest out of their own invitation, and nothing else would catch it.
        var endpoints = MapInvitationEndpointsForTest();

        Assert.Equal(3, endpoints.Count);

        foreach (var endpoint in endpoints)
        {
            Assert.Empty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
        }
    }

    private static IReadOnlyList<Endpoint> MapInvitationEndpointsForTest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddPermissionPolicies();
        builder.Services.AddSingleton(Mock.Of<ISender>());

        // Registered so the renderer resolves from services; minimal APIs otherwise infer an
        // unregistered interface as a request body, which a GET cannot have.
        builder.Services.AddSingleton(Mock.Of<IInvitationRenderer>());

        var app = builder.Build();
        app.MapInvitationEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .ToList();
    }
}
