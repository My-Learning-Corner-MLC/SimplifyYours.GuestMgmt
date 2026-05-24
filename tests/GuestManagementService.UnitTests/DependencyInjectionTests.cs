using GuestManagementService.Application;
using GuestManagementService.Application.Ping;
using GuestManagementService.Contracts.Ping;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GuestManagementService.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersApplicationServices()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        var handler = provider.GetRequiredService<IRequestHandler<GetPingStatusQuery, PingStatusResponse>>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var pipelineBehavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, string>>();

        Assert.NotNull(sender);
        Assert.IsType<GetPingStatusQueryHandler>(handler);
        Assert.Same(TimeProvider.System, timeProvider);
        Assert.NotNull(pipelineBehavior);
    }

    private sealed record TestRequest(string Name) : IRequest<string>;
}
