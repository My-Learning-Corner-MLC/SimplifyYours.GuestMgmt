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

public class SeatingEndpointPolicyMappingTests
{
    [Fact]
    public void GetSeatingLayout_endpoint_requires_seating_view_policy()
    {
        var endpoints = MapSeatingEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, "GetSeatingLayout", StringComparison.Ordinal));

        Assert.NotNull(endpoint);

        var policies = endpoint!.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        Assert.Contains(Permissions.SeatingView, policies);
    }

    [Theory]
    [InlineData("CreateTables")]
    [InlineData("UpdateTable")]
    [InlineData("DeleteTable")]
    public void Mutation_endpoints_require_seating_manage_policy(string endpointName)
    {
        var endpoints = MapSeatingEndpointsForTest();

        var endpoint = endpoints.SingleOrDefault(e =>
            string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, endpointName, StringComparison.Ordinal));

        Assert.NotNull(endpoint);

        var policies = endpoint!.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => data.Policy)
            .Where(policy => policy is not null)
            .ToArray();

        Assert.Contains(Permissions.SeatingManage, policies);
    }

    // AddInfrastructure is intentionally omitted so the test does not attempt a
    // Postgres / Kafka / Redis connection, mirroring EndpointPolicyMappingTests.
    private static IReadOnlyList<Endpoint> MapSeatingEndpointsForTest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddPermissionPolicies();
        builder.Services.AddSingleton(Mock.Of<ISender>());

        var app = builder.Build();
        app.MapSeatingEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .ToList();
    }
}
