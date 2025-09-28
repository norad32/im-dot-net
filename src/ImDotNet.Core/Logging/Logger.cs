using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImDotNet.Core.Logging;

public static class Logger
{
    private static readonly object _lock = new();
    private static bool _configured;
    private static string? _appName;
    private static Hierarchy? _repo;
    private static PatternLayout? _layout;
    private static Level _current = Level.ERROR;

    // preserved external sinks (e.g., GUI)
    private static readonly List<IAppender> _sinks = new();
    private static GuiAppender? _guiAppender;

    // ---------- public API ----------

    public static void Setup(Level level = Level.ERROR)
    {
        lock (_lock)
        {
            _current = level;
            _repo = (Hierarchy)LogManager.GetRepository(Assembly.GetExecutingAssembly());
            var root = _repo.Root;
            root.RemoveAllAppenders();

            _layout = BuildLayout();
            var console = BuildConsoleAppender(_layout, level);
            var rolling = BuildRollingFileAppender(_layout, level);

            var rebuilt = new List<IAppender> { console, rolling };

            foreach (var sink in _sinks.ToArray())
            {
                TrySetAppenderLevelAndLayout(sink, level, _layout);
                rebuilt.Add(sink);
            }

            // Truly async: forward to real appenders on a background worker thread
            var async = new log4net.Appender.BufferingForwardingAppender
            {
                Name = "BufferingAppender",
                Fix = FixFlags.All,
                // Optional tuning (defaults are fine):
                // BufferSize = 512,
                // Lossy = false,
                // LossyEvaluator = new log4net.Core.LevelEvaluator(ToLog4Level(Level.ERROR))
            };
            foreach (IAppender a in rebuilt)
                async.AddAppender(a);
            async.ActivateOptions();

            root.AddAppender(async);
            root.Level = ToLog4Level(level);
            _repo.Configured = true;
            _configured = true;
        }
    }

    public static GuiAppender SetupGui(Level level = Level.ERROR)
    {
        lock (_lock)
        {
            if (!_configured)
                throw new InvalidOperationException("Call AppLogger.Setup() before SetupGui().");

            _guiAppender ??= new GuiAppender { Name = "ImGui" };
            TrySetAppenderLevelAndLayout(_guiAppender, level, _layout ?? BuildLayout());

            _sinks.RemoveAll(s => s is GuiAppender);
            _sinks.Add(_guiAppender);

            // rebuild to include GUI sink
            Setup(_current);
            return _guiAppender;
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            _repo?.Shutdown();
            _configured = false;
        }
    }

    public static void SetLevel(Level level)
    {
        lock (_lock)
        {
            _current = level;
            if (_repo is null) return;

            _repo.Root.Level = ToLog4Level(level);
            foreach (IAppender appender in EnumerateAllAppenders())
                TrySetAppenderLevelAndLayout(appender, level, _layout ?? BuildLayout());
        }
    }

    // Get app logger or a child logger (category)
    public static ILog Get(Type t) => Get(t.FullName ?? t.Name);
    public static ILog Get(string? name = null)
    {
        if (!_configured)
            Setup(Level.ERROR);

        var cat = string.IsNullOrWhiteSpace(name) ? AppName : $"{AppName}.{name}";
        return LogManager.GetLogger(Assembly.GetExecutingAssembly(), cat);
    }

    // ---------- helpers ----------

    private static IEnumerable<IAppender> EnumerateAllAppenders()
    {
        if (_repo is null) yield break;
        foreach (var a in _repo.GetAppenders())
        {
            yield return a;

            // AsyncForwardingAppender derives from ForwardingAppender, so this covers both.
            if (a is ForwardingAppender fwd)
            {
                foreach (IAppender inner in fwd.Appenders)
                    yield return inner;
            }

            if (a is BufferingForwardingAppender bfwd)
            {
                foreach (IAppender inner in bfwd.Appenders)
                    yield return inner;
            }
        }
    }

    private static void TrySetAppenderLevelAndLayout(IAppender appender, Level level, PatternLayout layout)
    {
        if (appender is AppenderSkeleton sk)
        {
            sk.Threshold = ToLog4Level(level);
            sk.Layout = layout;
            sk.ActivateOptions();
        }
    }

    private static PatternLayout BuildLayout()
    {
        // Python: "{asctime}.{msecs:03} | {levelname:8s} | {name}:{lineno} | {message}"
        // Approx:
        return Activate(new PatternLayout("%date{yyyy-MM-dd HH:mm:ss.fff} | %-8level | %logger - %message%newline"));
    }

    private static ConsoleAppender BuildConsoleAppender(PatternLayout layout, Level level)
    {
        return Activate(new ConsoleAppender
        {
            Layout = layout,
            Target = "Console.Out",
            Name = "Console",
            Threshold = ToLog4Level(level),
        });
    }

    private static RollingFileAppender BuildRollingFileAppender(PatternLayout layout, Level level)
    {
        var filePath = GetLogPath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        return Activate(new RollingFileAppender
        {
            Name = "RollingFile",
            File = filePath,
            AppendToFile = true,
            RollingStyle = RollingFileAppender.RollingMode.Size,
            MaximumFileSize = "1MB",
            MaxSizeRollBackups = 3,
            StaticLogFileName = true,
            LockingModel = new FileAppender.MinimalLock(),
            Encoding = System.Text.Encoding.UTF8,
            Layout = layout,
            Threshold = ToLog4Level(level),
        });
    }

    private static T Activate<T>(T appender) where T : IOptionHandler
    {
        appender.ActivateOptions();
        return appender;
    }

    private static string GetLogPath()
    {
        var baseDir = GetUserLogDir(AppName);
        return Path.Combine(baseDir, $"{AppName}.log");
    }

    private static string GetUserLogDir(string app)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, app, "Logs");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs");
            return Path.Combine(root, app);
        }
        else
        {
            var root = Environment.GetEnvironmentVariable("XDG_STATE_HOME")
                       ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "state");
            return Path.Combine(root, app, "logs");
        }
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

    private static log4net.Core.Level ToLog4Level(Level level) => level switch
    {
        Level.CRITICAL => log4net.Core.Level.Fatal,
        Level.ERROR    => log4net.Core.Level.Error,
        Level.WARNING  => log4net.Core.Level.Warn,
        Level.INFO     => log4net.Core.Level.Info,
        Level.DEBUG    => log4net.Core.Level.Debug,
        _              => log4net.Core.Level.All,
    };
}
