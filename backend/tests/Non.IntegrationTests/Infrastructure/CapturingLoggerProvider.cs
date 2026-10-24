using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Non.IntegrationTests.Infrastructure;

public static class TestDiagnostics
{
    private static readonly ConcurrentQueue<string> Errors = new();

    public static void Add(string entry)
    {
        Errors.Enqueue(entry);

        while (Errors.Count > 5 && Errors.TryDequeue(out _))
        {
        }
    }

    public static string Dump() =>
        Errors.IsEmpty
            ? "(в логах API ошибок нет)"
            : string.Join(Environment.NewLine + "-----" + Environment.NewLine, Errors);
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly string _category;

        public CapturingLogger(string category)
        {
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            TestDiagnostics.Add($"{_category}: {formatter(state, exception)}{Environment.NewLine}{exception}");
        }
    }
}