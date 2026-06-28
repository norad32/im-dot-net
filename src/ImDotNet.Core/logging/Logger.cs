using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Enrichers.CallerInfo;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImDotNet.Core.Logging;

public static class Logger
{
    private static readonly object _lock = new();
    private static bool _configured;
    private static readonly string _appName = ResolveAppName();
    private static readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Error);

    private const long MaxLogFileSize = 1 * 1024 * 1024; // 1 MB
    private const int RetainedLogCount = 3;
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level,-8} | Thread:{ThreadId} | {SourceContext} | {Message:lj}{NewLine}{Exception}";

    private const string OutputTemplateWithLocation =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level,-8} | Thread:{ThreadId} | {SourceContext} | {Message:lj} ({SourceFile}:{LineNumber}){NewLine}{Exception}";

    public static Level CurrentLevel => FromSerilogLevel(_levelSwitch.MinimumLevel);

    /// <summary>
    /// Configures the logger with console and file sinks.
    /// Must be called before any other method.
    /// </summary>
    public static void Setup(Level level = Level.ERROR)
    {
        lock (_lock)
        {
            _levelSwitch.MinimumLevel = ToSerilogLevel(level);
            BuildAndApply(includeGui: false);
            _configured = true;
        }
    }

    /// <summary>
    /// Adds the GUI sink to the logger. Requires Setup() to have been called first.
    /// </summary>
    public static void SetupGui()
    {
        lock (_lock)
        {
            if (!_configured)
                throw new InvalidOperationException("Call Logger.Setup() before SetupGui().");

            BuildAndApply(includeGui: true);
        }
    }

    /// <summary>
    /// Changes the minimum log level at runtime without rebuilding the pipeline.
    /// Thread-safe via LoggingLevelSwitch's internal volatile field.
    /// </summary>
    public static void SetLevel(Level level) =>
        _levelSwitch.MinimumLevel = ToSerilogLevel(level);

    /// <summary>
    /// Returns a Serilog ILogger scoped to the given type.
    /// </summary>
    public static ILogger Get<T>() => Serilog.Log.Logger.ForContext<T>();

    /// <inheritdoc cref="Get{T}"/>
    public static ILogger Get(Type t) => Serilog.Log.Logger.ForContext(t);

    /// <summary>
    /// Flushes and closes all sinks. Call on application exit.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            Serilog.Log.CloseAndFlush();
            _configured = false;
        }
    }

    private static void BuildAndApply(bool includeGui)
    {
        var logPath = GetLogPath();
        var logDir = Path.GetDirectoryName(logPath)!;

        bool fileLoggingAvailable = true;
        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch (Exception ex)
        {
            fileLoggingAvailable = false;
            Console.Error.WriteLine(
                $"[Logger] Failed to create log directory '{logDir}': {ex.Message}. " +
                "File logging will be disabled.");
        }

#if DEBUG
        const string activeTemplate = OutputTemplateWithLocation;
#else
        const string activeTemplate = OutputTemplate;
#endif

        var config = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithCallerInfo(
                includeFileInfo: true,
                assemblyPrefix: "ImDotNet")
            .WriteTo.Console(outputTemplate: activeTemplate);

        if (fileLoggingAvailable)
        {
            config.WriteTo.File(
                path: logPath,
                outputTemplate: activeTemplate,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: MaxLogFileSize,
                retainedFileCountLimit: RetainedLogCount,
                encoding: System.Text.Encoding.UTF8,
                shared: true
            );
        }

        if (includeGui)
            config.WriteTo.Sink(GuiSink.Instance);

        Log.CloseAndFlush();
        Log.Logger = config.CreateLogger();
    }

    private static string GetLogPath() =>
        Path.Combine(GetUserLogDir(_appName), $"{_appName}.log");

    private static string GetUserLogDir(string app)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                app, "Logs");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library", "Logs", app);

        var xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "state");

        return Path.Combine(xdgState, app, "logs");
    }

    private static string ResolveAppName()
    {
        var proc = Process.GetCurrentProcess().ProcessName;
        var fromAsm = Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
        return string.IsNullOrWhiteSpace(proc) ? fromAsm : proc;
    }

    private static LogEventLevel ToSerilogLevel(Level level) => level switch
    {
        Level.FATAL => LogEventLevel.Fatal,
        Level.ERROR => LogEventLevel.Error,
        Level.WARNING => LogEventLevel.Warning,
        Level.INFO => LogEventLevel.Information,
        Level.DEBUG => LogEventLevel.Debug,
        _ => LogEventLevel.Verbose
    };

    private static Level FromSerilogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Fatal => Level.FATAL,
        LogEventLevel.Error => Level.ERROR,
        LogEventLevel.Warning => Level.WARNING,
        LogEventLevel.Information => Level.INFO,
        LogEventLevel.Debug => Level.DEBUG,
        _ => Level.NOTSET
    };
}
