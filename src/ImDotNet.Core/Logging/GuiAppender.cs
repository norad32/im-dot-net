using log4net.Appender;
using log4net.Core;
using System.Collections.Concurrent;
using System.IO;

namespace ImDotNet.Core.Logging;

public sealed class GuiAppender : AppenderSkeleton
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    protected override void Append(LoggingEvent loggingEvent)
    {
        var msg = RenderLoggingEvent(loggingEvent); // uses Layout if set, otherwise raw message
        var entry = new LogEntry(
            Timestamp: loggingEvent.TimeStamp,
            Level: MapLevel(loggingEvent.Level),
            Logger: loggingEvent.LoggerName ?? "root",
            Message: msg?.TrimEnd('\r','\n') ?? loggingEvent.RenderedMessage ?? string.Empty,
            Exception: loggingEvent.ExceptionObject?.ToString(),
            ThreadId: loggingEvent.ThreadName,
            File: loggingEvent.LocationInformation?.FileName,
            Line: int.TryParse(loggingEvent.LocationInformation?.LineNumber, out var line) ? line : null
        );
        Entries.Enqueue(entry);
    }

    private static Level MapLevel(log4net.Core.Level l) =>
        l == log4net.Core.Level.Fatal ? Level.CRITICAL :
        l == log4net.Core.Level.Error ? Level.ERROR :
        l == log4net.Core.Level.Warn  ? Level.WARNING :
        l == log4net.Core.Level.Info  ? Level.INFO :
        l == log4net.Core.Level.Debug ? Level.DEBUG : Level.NOTSET;
}
