using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImDotNet.Core.Logging;

public static class Logger
{
    private static readonly object _lock = new();
    private static bool _configured;
    private static string? _appName;

    // Serilog level switch — lets us change level at runtime without rebuilding
    private static readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Error);

    public static void Setup(Level level = Level.ERROR)
    {
        lock (_lock)
        {
            _levelSwitch.MinimumLevel = ToSerilogLevel(level);
            BuildAndApply(includeGui: false);
            _configured = true;
        }
    }

    public static GuiSink SetupGui(Level level = Level.ERROR)
    {
        lock (_lock)
        {
            if (!_configured)
                throw new InvalidOperationException("Call Logger.Setup() before SetupGui().");

            // Debug info (mirrors your original)
            Console.WriteLine($"[DEBUG] AppName: {AppName}");
            Console.WriteLine($"[DEBUG] LogPath: {GetLogPath()}");
            Console.WriteLine($"[DEBUG] LogDir:  {GetUserLogDir(AppName)}");

            _levelSwitch.MinimumLevel = ToSerilogLevel(level);
            BuildAndApply(includeGui: true);

            return GuiSink.Instance;
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            Serilog.Log.CloseAndFlush();
            _configured = false;
        }
    }

    public static void SetLevel(Level level)
    {
        lock (_lock)
        {
            // LoggingLevelSwitch updates all sinks live — no rebuild needed
            _levelSwitch.MinimumLevel = ToSerilogLevel(level);
        }
    }

    /// <summary>
    /// Returns a Serilog ILogger scoped to the given type.
    /// </summary>
    public static ILogger Get(Type t) => Get(t.FullName ?? t.Name);

    public static ILogger Get(string? name = null)
    {
        if (!_configured)
            Setup(Level.DEBUG);

        var cat = string.IsNullOrWhiteSpace(name) ? AppName : $"{AppName}.{name}";
        return Serilog.Log.Logger.ForContext("SourceContext", cat);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void BuildAndApply(bool includeGui)
    {
        var logPath = GetLogPath();
        var logDir = Path.GetDirectoryName(logPath)!;

        Console.WriteLine($"[DEBUG] Creating file sink");
        Console.WriteLine($"[DEBUG] FilePath: {logPath}");
        Console.WriteLine($"[DEBUG] DirPath:  {logDir}");

        try
        {
            Directory.CreateDirectory(logDir);
            Console.WriteLine($"[DEBUG] Directory created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Directory creation failed: {ex.Message}");
        }

        const string template =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level,-8} | {SourceContext} - {Message:lj}{NewLine}{Exception}";

        var config = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: template)
            .WriteTo.File(
                path: logPath,
                outputTemplate: template,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 1 * 1024 * 1024,   // 1 MB
                retainedFileCountLimit: 3,
                encoding: System.Text.Encoding.UTF8,
                shared: true                // MinimalLock equivalent
            );

        if (includeGui)
            config.WriteTo.Sink(GuiSink.Instance);

        // Close previous logger before replacing
        Serilog.Log.CloseAndFlush();
        Serilog.Log.Logger = config.CreateLogger();

        Console.WriteLine($"[DEBUG] Logger built successfully");
    }

    private static string GetLogPath() =>
        Path.Combine(GetUserLogDir(AppName), $"{AppName}.log");

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

        return Path.Combine(
            Environment.GetEnvironmentVariable("XDG_STATE_HOME")
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "state"),
            app, "logs");
    }

    private static string AppName
    {
        get
        {
            if (_appName is not null) return _appName;
            var proc = Process.GetCurrentProcess().ProcessName;
            var fromAsm = Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
            _appName = string.IsNullOrWhiteSpace(proc) ? fromAsm : proc;
            return _appName;
        }
    }

    private static LogEventLevel ToSerilogLevel(Level level) => level switch
    {
        Level.CRITICAL => LogEventLevel.Fatal,
        Level.ERROR => LogEventLevel.Error,
        Level.WARNING => LogEventLevel.Warning,
        Level.INFO => LogEventLevel.Information,
        Level.DEBUG => LogEventLevel.Debug,
        _ => LogEventLevel.Verbose
    };
}
