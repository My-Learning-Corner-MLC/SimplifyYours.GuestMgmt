using GuestManagementService.Api.Endpoints;
using GuestManagementService.Api.Security;
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

        Assert.Equal(
            new[] { Permissions.GuestsAdd, Permissions.GuestsView }.OrderBy(p => p),
            policies.OrderBy(p => p));
    }

    [Fact]
    public void ListGuests_endpoint_requires_guests_view_policy()
    {
        var endpoints = MapGuestEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "ListGuests", StringComparison.Ordinal));

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
}
