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
            if (_entries.Count == 0) return [];
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
        var loggerName = logEvent.Properties.TryGetValue("SourceContext", out var sc)
            ? sc.ToString().Trim('"')
            : "root";

        var rawFile = logEvent.Properties.TryGetValue("SourceFile", out var sf)
            ? sf.ToString().Trim('"')
            : null;

        var rawLine = logEvent.Properties.TryGetValue("LineNumber", out var ln)
            && int.TryParse(ln.ToString(), out var lineNum)
            ? lineNum
            : (int?)null;

        bool hasLocation = !string.IsNullOrWhiteSpace(rawFile)
            && rawFile != "unknown"
            && rawLine is not null
            && rawLine > 0;

        var threadId = logEvent.Properties.TryGetValue("ThreadId", out var tid)
            ? tid.ToString()
            : null;

        var entry = new LogEntry(
            Timestamp: logEvent.Timestamp.DateTime,
            Level: MapLevel(logEvent.Level),
            Logger: loggerName,
            Message: logEvent.RenderMessage(),
            Exception: logEvent.Exception?.ToString(),
            ThreadId: threadId,
            File: hasLocation ? rawFile : null,
            Line: hasLocation ? rawLine : null
        );

        lock (_lock) { _entries.Add(entry with { Formatted = Format(entry) }); }
    }

    private static Level MapLevel(LogEventLevel l) => l switch
    {
        LogEventLevel.Fatal => Level.FATAL,
        LogEventLevel.Error => Level.ERROR,
        LogEventLevel.Warning => Level.WARNING,
        LogEventLevel.Information => Level.INFO,
        LogEventLevel.Debug => Level.DEBUG,
        LogEventLevel.Verbose => Level.DEBUG,
        _ => Level.NOTSET
    };

    private static string Format(LogEntry e)
    {
        var location = e.File is not null && e.Line is not null
            ? $" ({Path.GetFileName(e.File)}:{e.Line})"
            : string.Empty;

        return $"{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {e.Level,-8} | Thread:{e.ThreadId} | {e.Logger} | {e.Message}{location}";
    }
}
