using ImGuiNET;
using ImDotNet.Core.Logging;
using System.Numerics;
using Serilog;

namespace ImDotNet.Gui.Panels;

public sealed class LogPanelState
{
    public bool Visible = false;
    public bool AutoScroll = true;
}

public sealed class LogPanel
{
    public const string TOGGLE_KEY = "`";
    private const int MIN_HEIGHT = 148;
    private const int MAX_LOG_ENTRIES = 1024;
    private const float INITIAL_Y_FRACTION = 2f / 3f;  // 2/3 down from top
    private const float INITIAL_HEIGHT_FRACTION = 1f / 3f;  // 1/3 height
    private const float WIDTH_SNAP_THRESHOLD = 0.5f;

    private readonly LogPanelState _state;
    private readonly Queue<LogEntry> _entries = new();
    private bool _wasAtBottom = true;
    private bool _initialized = false;

    public LogPanel(LogPanelState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));

        if (string.IsNullOrEmpty(TOGGLE_KEY) || TOGGLE_KEY.Length != 1)
            throw new InvalidOperationException($"{nameof(TOGGLE_KEY)} must be a single character");
    }

    public void HandleToggleKey(ImGuiIOPtr io)
    {
        // Don't steal input if ImGui wants it
        if (io.WantTextInput || io.WantCaptureKeyboard)
            return;

        try
        {
            // Safely iterate input queue
            for (int i = 0; i < io.InputQueueCharacters.Size; i++)
            {
                char c = (char)io.InputQueueCharacters[i];
                if (c == TOGGLE_KEY[0])
                {
                    _state.Visible = !_state.Visible;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to handle toggle key: {Exception}", ex);
        }
    }

    public void Draw()
    {
        if (!_state.Visible)
            return;

        var sink = GuiSink.Instance;
        if (sink == null)
        {
            ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
            ImGui.Begin($"Logs ({TOGGLE_KEY})", ImGuiWindowFlags.NoSavedSettings);
            ImGui.TextDisabled("Logging sink not initialized");
            ImGui.End();
            return;
        }

        var drained = sink.Drain();
        if (drained != null && drained.Count > 0)
        {
            foreach (var entry in drained)
            {
                _entries.Enqueue(entry);
                if (_entries.Count > MAX_LOG_ENTRIES)
                    _entries.Dequeue();
            }
        }

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos;
        var workSize = viewport.WorkSize;

        ImGui.SetNextWindowViewport(viewport.ID);

        if (!_initialized)
        {
            ImGui.SetNextWindowPos(
                new Vector2(workPos.X, workPos.Y + INITIAL_Y_FRACTION * workSize.Y),
                ImGuiCond.FirstUseEver
            );
            ImGui.SetNextWindowSize(
                new Vector2(workSize.X, INITIAL_HEIGHT_FRACTION * workSize.Y),
                ImGuiCond.FirstUseEver
            );
            _initialized = true;
        }

        ImGui.SetNextWindowSizeConstraints(
            new Vector2(workSize.X, MIN_HEIGHT),
            new Vector2(workSize.X, workSize.Y)
        );

        var flags = ImGuiWindowFlags.NoMove
                  | ImGuiWindowFlags.NoDocking
                  | ImGuiWindowFlags.NoSavedSettings;

        bool expanded = ImGui.Begin($"Logs ({TOGGLE_KEY})", flags);

        if (!expanded)
        {
            ImGui.End();
            return;
        }

        // Lock width to viewport
        var currentSize = ImGui.GetWindowSize();
        if (Math.Abs(currentSize.X - workSize.X) > WIDTH_SNAP_THRESHOLD)
        {
            ImGui.SetWindowSize(new Vector2(workSize.X, currentSize.Y));
        }

        // Snap to bottom
        ImGui.SetWindowPos(new Vector2(
            workPos.X,
            workPos.Y + workSize.Y - ImGui.GetWindowSize().Y
        ));

        // Toolbar
        var autoScroll = _state.AutoScroll;
        if (ImGui.Checkbox("Autoscroll##logpanel", ref autoScroll))
            _state.AutoScroll = autoScroll;

        ImGui.SameLine();
        if (ImGui.Button("Clear##logpanel"))
        {
            _entries.Clear();
            sink.Clear();
            _wasAtBottom = true;
        }

        ImGui.Separator();
        ImGui.BeginChild("##logs");

        float scrollY = ImGui.GetScrollY();
        float scrollMaxY = ImGui.GetScrollMaxY();
        bool atBottom = scrollY >= scrollMaxY - 1f;

        foreach (var entry in _entries)
        {
            ImGui.TextColored(
                LevelToColor(entry.Level),
                entry.Formatted
            );
        }

        if (_state.AutoScroll && (_wasAtBottom || drained?.Count > 0))
            ImGui.SetScrollHereY(1f);

        _wasAtBottom = atBottom;

        ImGui.EndChild();
        ImGui.End();
    }

    private static Vector4 LevelToColor(Level level) => level switch
    {
        Level.DEBUG => LogColorScheme.Debug,
        Level.INFO => LogColorScheme.Info,
        Level.WARNING => LogColorScheme.Warning,
        Level.ERROR => LogColorScheme.Error,
        Level.FATAL => LogColorScheme.Fatal,
        _ => LogColorScheme.Default
    };
}

/// <summary>
/// Centralized color scheme for log levels. Easily customizable.
/// </summary>
internal static class LogColorScheme
{
    public static readonly Vector4 Debug = new(0.6f, 0.6f, 0.6f, 1f);
    public static readonly Vector4 Info = new(1.0f, 1.0f, 1.0f, 1f);
    public static readonly Vector4 Warning = new(1.0f, 0.8f, 0.2f, 1f);
    public static readonly Vector4 Error = new(1.0f, 0.3f, 0.3f, 1f);
    public static readonly Vector4 Fatal = new(1.0f, 0.0f, 0.0f, 1f);
    public static readonly Vector4 Default = new(0.8f, 0.8f, 0.8f, 1f);
}
