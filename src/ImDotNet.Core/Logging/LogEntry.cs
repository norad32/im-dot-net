using System;

namespace ImDotNet.Core.Logging;

public sealed record LogEntry(
    DateTime Timestamp,
    Level Level,
    string Logger,
    string Message,
    string? Exception,
    string? ThreadId,
    string? File,
    int? Line
);
