using System.Security.Claims;
using GuestManagementService.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GuestManagementService.UnitTests.Security;

public class AuthorizationPolicyRegistrationTests
{
    [Fact]
    public async Task AddPermissionPolicies_registers_guests_add_policy()
    {
        var provider = BuildPolicyProvider();

        var policy = await provider.GetPolicyAsync(Permissions.GuestsAdd);

        Assert.NotNull(policy);
        var claimRequirement = Assert.Single(policy!.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal(Permissions.ClaimType, claimRequirement.ClaimType);
        Assert.NotNull(claimRequirement.AllowedValues);
        Assert.Contains(Permissions.GuestsAdd, claimRequirement.AllowedValues!);
    }

    [Fact]
    public async Task AddPermissionPolicies_registers_guests_view_policy()
    {
        var provider = BuildPolicyProvider();

        var policy = await provider.GetPolicyAsync(Permissions.GuestsView);

        Assert.NotNull(policy);
        var claimRequirement = Assert.Single(policy!.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal(Permissions.ClaimType, claimRequirement.ClaimType);
        Assert.NotNull(claimRequirement.AllowedValues);
        Assert.Contains(Permissions.GuestsView, claimRequirement.AllowedValues!);
    }

    [Theory]
    [InlineData("events.create")]
    [InlineData("events.view")]
    [InlineData("tenant.manage_users")]
    public async Task AddPermissionPolicies_does_not_register_cross_service_or_unknown_permission(string permission)
    {
        var provider = BuildPolicyProvider();

        var policy = await provider.GetPolicyAsync(permission);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Authorize_succeeds_when_principal_has_multiple_permission_claims_and_one_matches()
    {
        var services = BuildServices();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(Permissions.ClaimType, "events.view"),
                new Claim(Permissions.ClaimType, Permissions.GuestsAdd)
            },
            authenticationType: "TestAuth"));

        var result = await authorizationService.AuthorizeAsync(principal, Permissions.GuestsAdd);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Authorize_fails_when_principal_has_permission_claims_but_none_match()
    {
        var services = BuildServices();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(Permissions.ClaimType, "events.view")
            },
            authenticationType: "TestAuth"));

        var result = await authorizationService.AuthorizeAsync(principal, Permissions.GuestsAdd);

        Assert.False(result.Succeeded);
    }

    private static IAuthorizationPolicyProvider BuildPolicyProvider()
        => BuildServices().GetRequiredService<IAuthorizationPolicyProvider>();

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddPermissionPolicies();
        return services.BuildServiceProvider();
    }
}
