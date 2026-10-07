using System.Globalization;
using ImDotNet.Cli.Commands;
using ImDotNet.Core;
using ImDotNet.Core.Logging;
using ImDotNet.Gui;
using ImDotNet.Gui.Panels;
using Serilog.Events;
using Serilog.Parsing;

namespace ImDotNet.Tests;

public sealed class AboutTests
{
    [Fact]
    public void Main_Should_ThrowArgumentNullException_If_ArgumentsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => ImDotNet.Cli.Program.Main(null!));
    }

    [Fact]
    public void Description_Should_IncludeAppNameAndVersion()
    {
        string description = About.Description();

        Assert.Contains(About.AppName, description, StringComparison.Ordinal);
        Assert.Contains(About.Version, description, StringComparison.Ordinal);
    }
}

public sealed class LevelParserTests
{
    [Theory]
    [InlineData("FATAL", Level.FATAL)]
    [InlineData("fatal", Level.FATAL)]
    [InlineData("ERROR", Level.ERROR)]
    [InlineData("warning", Level.WARNING)]
    [InlineData(" INFO ", Level.INFO)]
    [InlineData("debug", Level.DEBUG)]
    [InlineData("notset", Level.NOTSET)]
    public void Parse_Should_ReturnMatchingLevel_If_NameIsValid(string text, Level expected)
    {
        Assert.Equal(expected, LevelParser.Parse(text));
    }

    [Theory]
    [InlineData("50", Level.FATAL)]
    [InlineData("100", Level.FATAL)]
    [InlineData("49", Level.ERROR)]
    [InlineData("40", Level.ERROR)]
    [InlineData("39", Level.WARNING)]
    [InlineData("30", Level.WARNING)]
    [InlineData("29", Level.INFO)]
    [InlineData("20", Level.INFO)]
    [InlineData("19", Level.DEBUG)]
    [InlineData("10", Level.DEBUG)]
    [InlineData("9", Level.NOTSET)]
    [InlineData("0", Level.NOTSET)]
    [InlineData("-1", Level.NOTSET)]
    public void Parse_Should_ReturnSeverityBucket_If_TextIsAnInteger(string text, Level expected)
    {
        Assert.Equal(expected, LevelParser.Parse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_Should_UseFallback_If_TextIsNullOrWhitespace(string? text)
    {
        Assert.Equal(Level.WARNING, LevelParser.Resolve(text, "WARNING"));
    }

    [Fact]
    public void Resolve_Should_ParseText_If_TextIsProvided()
    {
        Assert.Equal(Level.DEBUG, LevelParser.Resolve("DEBUG", "ERROR"));
    }

    [Fact]
    public void Parse_Should_ThrowArgumentNullException_If_TextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => LevelParser.Parse(null!));
    }

    [Theory]
    [InlineData("CRITICAL")]
    [InlineData("TRACE")]
    [InlineData("abc")]
    public void Parse_Should_ThrowInvalidOperationException_If_NameIsUnknown(string text)
    {
        Assert.Throws<InvalidOperationException>(() => LevelParser.Parse(text));
    }

    [Fact]
    public void Resolve_Should_ThrowInvalidOperationException_If_FallbackIsInvalid()
    {
        Assert.Throws<InvalidOperationException>(() => LevelParser.Resolve(null, "INVALID"));
    }
}

public sealed class GuiSinkTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void Emit_Should_ThrowArgumentNullException_If_LogEventIsNull()
    {
        var sink = new GuiSink();

        Assert.Throws<ArgumentNullException>(() => sink.Emit(null!));
    }

    [Fact]
    public void Drain_Should_ReturnEmptyList_If_NoEntriesWereEmitted()
    {
        var sink = new GuiSink();

        Assert.Empty(sink.Drain());
    }

    [Fact]
    public void Drain_Should_ReturnEntriesInEmissionOrder_If_MultipleEntriesWereEmitted()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "First"));
        sink.Emit(CreateEvent(LogEventLevel.Warning, "Second"));

        var entries = sink.Drain();

        Assert.Collection(entries,
            first => Assert.Equal("First", first.Message),
            second => Assert.Equal("Second", second.Message));
    }

    [Fact]
    public void Drain_Should_ClearBufferedEntries_If_DrainedOnce()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message"));

        Assert.Single(sink.Drain());
        Assert.Empty(sink.Drain());
    }

    [Fact]
    public void Emit_Should_MapLevelToNotSet_If_EventLevelIsUnknown()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent((LogEventLevel)999, "Message"));

        Assert.Equal(Level.NOTSET, Assert.Single(sink.Drain()).Level);
    }

    [Fact]
    public void Clear_Should_RemoveBufferedEntries_If_EntriesWereEmitted()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message"));

        sink.Clear();

        Assert.Empty(sink.Drain());
    }

    [Theory]
    [InlineData(LogEventLevel.Fatal, Level.FATAL)]
    [InlineData(LogEventLevel.Error, Level.ERROR)]
    [InlineData(LogEventLevel.Warning, Level.WARNING)]
    [InlineData(LogEventLevel.Information, Level.INFO)]
    [InlineData(LogEventLevel.Debug, Level.DEBUG)]
    [InlineData(LogEventLevel.Verbose, Level.DEBUG)]
    public void Emit_Should_MapLevel_If_EventHasSupportedLevel(LogEventLevel eventLevel, Level expected)
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(eventLevel, "Message"));

        Assert.Equal(expected, Assert.Single(sink.Drain()).Level);
    }

    [Fact]
    public void Emit_Should_UseRootLogger_If_SourceContextIsMissing()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message"));

        Assert.Equal("root", Assert.Single(sink.Drain()).Logger);
    }

    [Fact]
    public void Emit_Should_UseSourceContext_If_PropertyIsPresent()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message",
            new LogEventProperty("SourceContext", new ScalarValue("MyLogger"))));

        Assert.Equal("MyLogger", Assert.Single(sink.Drain()).Logger);
    }

    [Fact]
    public void Emit_Should_IncludeLocation_If_FileAndPositiveLineArePresent()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message",
            new LogEventProperty("SourceFile", new ScalarValue("/src/file.cs")),
            new LogEventProperty("LineNumber", new ScalarValue(42))));

        var entry = Assert.Single(sink.Drain());
        Assert.Equal("/src/file.cs", entry.File);
        Assert.Equal(42, entry.Line);
        Assert.Contains("(file.cs:42)", entry.Formatted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("unknown", 42)]
    [InlineData("/src/file.cs", 0)]
    [InlineData("/src/file.cs", -1)]
    public void Emit_Should_OmitLocation_If_FileOrLineIsInvalid(string file, int line)
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message",
            new LogEventProperty("SourceFile", new ScalarValue(file)),
            new LogEventProperty("LineNumber", new ScalarValue(line))));

        var entry = Assert.Single(sink.Drain());
        Assert.Null(entry.File);
        Assert.Null(entry.Line);
        Assert.DoesNotContain(".cs:", entry.Formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_Should_PreserveThreadId_If_PropertyIsPresent()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message",
            new LogEventProperty("ThreadId", new ScalarValue(7))));

        Assert.Equal("7", Assert.Single(sink.Drain()).ThreadId);
    }

    [Fact]
    public void Emit_Should_OmitLocation_If_LineNumberIsMissing()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message",
            new LogEventProperty("SourceFile", new ScalarValue("/src/file.cs"))));

        var entry = Assert.Single(sink.Drain());
        Assert.Null(entry.File);
        Assert.Null(entry.Line);
    }

    [Fact]
    public void Emit_Should_RenderStructuredMessage_If_MessageHasProperties()
    {
        var sink = new GuiSink();
        sink.Emit(new LogEvent(Timestamp, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("Hello {Name:l}"),
            [new LogEventProperty("Name", new ScalarValue("Ada"))]));

        Assert.Equal("Hello Ada", Assert.Single(sink.Drain()).Message);
    }

    [Fact]
    public void Emit_Should_PreserveException_If_EventHasException()
    {
        var sink = new GuiSink();
        var exception = new InvalidOperationException("Failure");
        sink.Emit(CreateEventWithException(LogEventLevel.Error, "Failed", exception));

        Assert.Contains("Failure", Assert.Single(sink.Drain()).Exception, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_Should_PreserveTimestamp_If_EventIsEmitted()
    {
        var sink = new GuiSink();
        sink.Emit(CreateEvent(LogEventLevel.Information, "Message"));

        Assert.Equal(Timestamp.DateTime, Assert.Single(sink.Drain()).Timestamp);
    }

    private static LogEvent CreateEvent(LogEventLevel level, string message,
        params LogEventProperty[] properties)
    {
        return new LogEvent(Timestamp, level, null,
            new MessageTemplateParser().Parse(message), properties);
    }

    private static LogEvent CreateEventWithException(LogEventLevel level, string message,
        Exception exception)
    {
        return new LogEvent(Timestamp, level, exception,
            new MessageTemplateParser().Parse(message), []);
    }
}

public sealed class LoggingInterceptorTests
{
    [Fact]
    public void Intercept_Should_ConfigureLogger_If_SettingsContainLogLevel()
    {
        var interceptor = new LoggingInterceptor();
        var settings = new GlobalSettings { LogLevelText = "DEBUG" };

        try
        {
            interceptor.Intercept(null!, settings);

            Assert.Equal(Level.DEBUG, Logger.CurrentLevel);
        }
        finally
        {
            Logger.Shutdown();
        }
    }

    [Fact]
    public void Intercept_Should_UseErrorLevel_If_LogLevelIsMissing()
    {
        var interceptor = new LoggingInterceptor();
        var settings = new GlobalSettings();

        try
        {
            interceptor.Intercept(null!, settings);

            Assert.Equal(Level.ERROR, Logger.CurrentLevel);
        }
        finally
        {
            Logger.Shutdown();
        }
    }

    [Fact]
    public void Intercept_Should_ThrowInvalidOperationException_If_LogLevelIsInvalid()
    {
        var interceptor = new LoggingInterceptor();
        var settings = new GlobalSettings { LogLevelText = "INVALID" };

        Assert.Throws<InvalidOperationException>(() => interceptor.Intercept(null!, settings));
    }
}

public sealed class LogPanelStateTests
{
    [Fact]
    public void LogPanelState_Should_InitializeWithExpectedDefaults()
    {
        var state = new LogPanelState();

        Assert.False(state.Visible);
        Assert.True(state.AutoScroll);
    }

    [Fact]
    public void LogPanel_Should_ThrowArgumentNullException_If_StateIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new LogPanel(null!));
    }

    [Fact]
    public void Controller_Should_ThrowArgumentNullException_If_WindowIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Controller(null!, null!, null!));
    }

    [Fact]
    public void MainPanel_Should_Initialize_If_StateAndCallbackAreProvided()
    {
        var state = new LogPanelState();
        var panel = new MainPanel(state, () => { });

        Assert.NotNull(panel);
    }

}

public sealed class LoggerTests
{
    [Fact]
    public void Setup_Should_SetCurrentLevel_If_LevelIsProvided()
    {
        using var environment = new IsolatedLoggerEnvironment();

        Logger.Setup(Level.WARNING);

        Assert.Equal(Level.WARNING, Logger.CurrentLevel);
    }

    [Fact]
    public void SetLevel_Should_UpdateCurrentLevel_If_LevelChanges()
    {
        using var environment = new IsolatedLoggerEnvironment();
        Logger.Setup(Level.ERROR);

        Logger.SetLevel(Level.DEBUG);

        Assert.Equal(Level.DEBUG, Logger.CurrentLevel);
    }

    [Fact]
    public void SetupGui_Should_ThrowInvalidOperationException_If_SetupWasNotCalled()
    {
        using var environment = new IsolatedLoggerEnvironment();
        Logger.Shutdown();

        Assert.Throws<InvalidOperationException>(() => Logger.SetupGui());
    }

    [Fact]
    public void SetupGui_Should_AddGuiSink_If_LoggerWasConfigured()
    {
        using var environment = new IsolatedLoggerEnvironment();
        GuiSink.Instance.Clear();
        Logger.Setup(Level.INFO);
        Logger.SetupGui();

        Serilog.Log.Information("GUI message");

        Assert.Contains(GuiSink.Instance.Drain(), entry => entry.Message == "GUI message");
        GuiSink.Instance.Clear();
    }

    [Fact]
    public void Get_Should_ReturnLogger_If_TypeIsProvided()
    {
        Assert.NotNull(Logger.Get<LoggerTests>());
        Assert.NotNull(Logger.Get<LoggerTests>());
    }

    private sealed class IsolatedLoggerEnvironment : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        private readonly string _variable = OperatingSystem.IsWindows()
            ? "LOCALAPPDATA"
            : OperatingSystem.IsMacOS() ? "HOME" : "XDG_STATE_HOME";
        private readonly string? _previousValue;

        public IsolatedLoggerEnvironment()
        {
            _previousValue = Environment.GetEnvironmentVariable(_variable);
            Directory.CreateDirectory(_path);
            Environment.SetEnvironmentVariable(_variable, _path);
        }

        public void Dispose()
        {
            Logger.Shutdown();
            Environment.SetEnvironmentVariable(_variable, _previousValue);
            if (Directory.Exists(_path))
                Directory.Delete(_path, recursive: true);
        }
    }
}

public sealed class CheckCommandTests
{
    [Fact]
    public void Execute_Should_ReturnZero_If_CommandExecutesSuccessfully()
    {
        var command = new CheckCommand();

        int result = command.Execute(null!, new GlobalSettings());

        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_Should_ReturnZero_If_CheckCommandIsProvided()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string variable = OperatingSystem.IsWindows()
            ? "LOCALAPPDATA"
            : OperatingSystem.IsMacOS() ? "HOME" : "XDG_STATE_HOME";
        string? previousValue = Environment.GetEnvironmentVariable(variable);
        Directory.CreateDirectory(path);
        Environment.SetEnvironmentVariable(variable, path);

        try
        {
            Assert.Equal(0, ImDotNet.Cli.Program.Main(["check"]));
        }
        finally
        {
            Logger.Shutdown();
            Environment.SetEnvironmentVariable(variable, previousValue);
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}

public sealed class LogEntryTests
{
    [Fact]
    public void Record_Should_SupportValueEquality_If_AllPropertiesMatch()
    {
        var timestamp = new DateTime(2026, 1, 2, 3, 4, 5);
        var first = new LogEntry(timestamp, Level.INFO, "Logger", "Message", null, "1", null, null);
        var second = new LogEntry(timestamp, Level.INFO, "Logger", "Message", null, "1", null, null);

        Assert.Equal(first, second);
    }
}

public sealed class StateTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private string ConfigPath => Path.Combine(_directory, "gui_state.ini");

    [Fact]
    public void Load_Should_ReturnDefaults_If_ConfigFileDoesNotExist()
    {
        var state = State.Load(ConfigPath);

        Assert.False(state.ShowLogPanel);
        Assert.Equal(0.333f, state.LogPanelHeightFraction);
    }

    [Fact]
    public void Load_Should_ReadSavedValues_If_ConfigFileContainsValidSettings()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(ConfigPath,
        [
            "[gui]",
            "show_log_panel=True",
            "log_panel_height_fraction=0.75"
        ]);

        var state = State.Load(ConfigPath);

        Assert.True(state.ShowLogPanel);
        Assert.Equal(0.75f, state.LogPanelHeightFraction);
    }

    [Fact]
    public void Load_Should_IgnoreMalformedLines_If_ConfigFileContainsInvalidEntries()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(ConfigPath,
        [
            "not a setting",
            "show_log_panel=not-a-bool",
            "log_panel_height_fraction=not-a-float"
        ]);

        var state = State.Load(ConfigPath);

        Assert.False(state.ShowLogPanel);
        Assert.Equal(0.333f, state.LogPanelHeightFraction);
    }

    [Fact]
    public void Load_Should_UseLastValue_If_SettingAppearsMoreThanOnce()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(ConfigPath,
        [
            "show_log_panel=False",
            "show_log_panel=True",
            "log_panel_height_fraction=0.25",
            "log_panel_height_fraction=0.5"
        ]);

        var state = State.Load(ConfigPath);

        Assert.True(state.ShowLogPanel);
        Assert.Equal(0.5f, state.LogPanelHeightFraction);
    }

    [Fact]
    public void Save_Should_WriteSettings_If_StateHasValues()
    {
        var state = new State(ConfigPath)
        {
            ShowLogPanel = true,
            LogPanelHeightFraction = 0.625f
        };

        state.Save();

        string contents = File.ReadAllText(ConfigPath);
        Assert.Contains("show_log_panel=True", contents, StringComparison.Ordinal);
        Assert.Contains("log_panel_height_fraction=0.625", contents, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_Should_IgnoreUnknownSetting_If_ConfigFileContainsAnUnrecognizedKey()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(ConfigPath, "unknown=value");

        var state = State.Load(ConfigPath);

        Assert.False(state.ShowLogPanel);
        Assert.Equal(0.333f, state.LogPanelHeightFraction);
    }

    [Fact]
    public void Save_Should_CreateParentDirectory_If_ItDoesNotExist()
    {
        var state = new State(ConfigPath);

        state.Save();

        Assert.True(File.Exists(ConfigPath));
    }

    [Fact]
    public void Save_Should_WriteInvariantFloat_If_CurrentCultureUsesCommaDecimalSeparator()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var state = new State(ConfigPath) { LogPanelHeightFraction = 0.625f };

            state.Save();

            Assert.Contains("log_panel_height_fraction=0.625", File.ReadAllText(ConfigPath), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Save_Should_WriteFileDirectly_If_ConfigPathHasNoDirectory()
    {
        string path = Path.Combine(_directory, "state.ini");
        var state = new State(path);

        state.Save();

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SaveAndLoad_Should_RoundTripSettings_If_StateIsPersisted()
    {
        var saved = new State(ConfigPath)
        {
            ShowLogPanel = true,
            LogPanelHeightFraction = 0.875f
        };
        saved.Save();

        var loaded = State.Load(ConfigPath);

        Assert.True(loaded.ShowLogPanel);
        Assert.Equal(saved.LogPanelHeightFraction, loaded.LogPanelHeightFraction);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
