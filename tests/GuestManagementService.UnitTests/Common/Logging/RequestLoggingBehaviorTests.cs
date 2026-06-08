using GuestManagementService.Application.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuestManagementService.UnitTests.Common.Logging;

public sealed class RequestLoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestSucceeds_LogsStartAndCompletion()
    {
        var logger = new CapturingLogger<RequestLoggingBehavior<TestRequest, string>>();
        var behavior = new RequestLoggingBehavior<TestRequest, string>(logger);

        var result = await behavior.Handle(
            new TestRequest(),
            () => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", result);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("started"));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("completed"));
    }

    [Fact]
    public async Task Handle_WhenRequestFails_LogsFailureAndRethrows()
    {
        var logger = new CapturingLogger<RequestLoggingBehavior<TestRequest, string>>();
        var behavior = new RequestLoggingBehavior<TestRequest, string>(logger);
        var exception = new InvalidOperationException("failure");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestRequest(),
            () => Task.FromException<string>(exception),
            CancellationToken.None));

        Assert.Same(exception, thrown);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Exception == exception);
    }

    private sealed record TestRequest : IRequest<string>;

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
