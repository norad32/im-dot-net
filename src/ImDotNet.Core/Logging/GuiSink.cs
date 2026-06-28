using Serilog.Core;
using Serilog.Events;

namespace ImDotNet.Core.Logging;

public sealed class GuiSink : ILogEventSink
{
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();

    public static GuiSink Instance { get; } = new();

    public List<LogEntry> Drain()
    {
        lock (_lock)
        {
            if (_entries.Count == 0) return new List<LogEntry>();
            var drained = new List<LogEntry>(_entries);
            _entries.Clear();
            return drained;
        }
    }

    public void Clear()
    {
        lock (_lock) { _entries.Clear(); }
    }

    public void Emit(LogEvent logEvent)
    {
        // Try to get source context (logger name)
        var loggerName = logEvent.Properties.TryGetValue("SourceContext", out var sc)
            ? sc.ToString().Trim('"')
            : "root";

        var entry = new LogEntry(
            Timestamp: logEvent.Timestamp.DateTime,
            Level: MapLevel(logEvent.Level),
            Logger: loggerName,
            Message: logEvent.RenderMessage(),
            Exception: logEvent.Exception?.ToString(),
            ThreadId: null, // Serilog doesn't capture thread by default
            File: null, // requires Serilog.Enrichers.* or caller info
            Line: null
        );

        lock (_lock) { _entries.Add(entry); }
    }

    private static Level MapLevel(LogEventLevel l) => l switch
    {
        LogEventLevel.Fatal => Level.CRITICAL,
        LogEventLevel.Error => Level.ERROR,
        LogEventLevel.Warning => Level.WARNING,
        LogEventLevel.Information => Level.INFO,
        LogEventLevel.Debug => Level.DEBUG,
        LogEventLevel.Verbose => Level.DEBUG,
        _ => Level.NOTSET
    };
}
