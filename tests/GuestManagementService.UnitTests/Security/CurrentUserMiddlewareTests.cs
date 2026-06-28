using System.Security.Claims;
using GuestManagementService.Api.Middleware;
using GuestManagementService.Api.Security;
using GuestManagementService.Application.Authorization;
using Microsoft.AspNetCore.Http;

namespace GuestManagementService.UnitTests.Security;

public sealed class CurrentUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUnauthenticated_PassesThroughWithoutSettingUser()
    {
        var accessor = new CurrentUserAccessor();
        var nextCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

        await middleware.InvokeAsync(context, accessor);

        Assert.True(nextCalled);
        Assert.Null(accessor.User);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubjectClaimIsMissing_Returns401AndDoesNotCallNext()
    {
        var accessor = new CurrentUserAccessor();
        var nextCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(subject: null, tenantId: Guid.NewGuid().ToString());

        await middleware.InvokeAsync(context, accessor);

        Assert.False(nextCalled);
        Assert.Null(accessor.User);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantClaimIsMissing_Returns401AndDoesNotCallNext()
    {
        var accessor = new CurrentUserAccessor();
        var nextCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(subject: Guid.NewGuid().ToString(), tenantId: null);

        await middleware.InvokeAsync(context, accessor);

        Assert.False(nextCalled);
        Assert.Null(accessor.User);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenSubjectClaimIsNotGuid_Returns401()
    {
        var accessor = new CurrentUserAccessor();
        var middleware = new CurrentUserMiddleware(_ => Task.CompletedTask);
        var context = CreateAuthenticatedContext(subject: "not-a-guid", tenantId: Guid.NewGuid().ToString());

        await middleware.InvokeAsync(context, accessor);

        Assert.Null(accessor.User);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantClaimIsNotGuid_Returns401()
    {
        var accessor = new CurrentUserAccessor();
        var middleware = new CurrentUserMiddleware(_ => Task.CompletedTask);
        var context = CreateAuthenticatedContext(subject: Guid.NewGuid().ToString(), tenantId: "not-a-guid");

        await middleware.InvokeAsync(context, accessor);

        Assert.Null(accessor.User);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenClaimsAreValid_PopulatesAccessorAndCallsNext()
    {
        var accessor = new CurrentUserAccessor();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var nextCalled = false;
        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(subject: userId.ToString(), tenantId: tenantId.ToString());

        await middleware.InvokeAsync(context, accessor);

        Assert.True(nextCalled);
        Assert.NotNull(accessor.User);
        Assert.Equal(userId, accessor.User!.UserId);
        Assert.Equal(tenantId, accessor.User.TenantId);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string? subject, string? tenantId)
    {
        var claims = new List<Claim>();
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject));
        }

        if (tenantId is not null)
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }
}
