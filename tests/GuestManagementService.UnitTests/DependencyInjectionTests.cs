using GuestManagementService.Application;
using GuestManagementService.Application.Ping;
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
        var pingService = provider.GetRequiredService<IPingService>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var pipelineBehavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, string>>();

        Assert.IsType<PingService>(pingService);
        Assert.Same(TimeProvider.System, timeProvider);
        Assert.NotNull(pipelineBehavior);
    }

    private sealed record TestRequest(string Name) : IRequest<string>;
}
