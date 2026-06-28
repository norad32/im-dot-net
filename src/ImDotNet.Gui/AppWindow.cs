using ImGuiNET;
using ImDotNet.Core;
using ImDotNet.Core.Logging;
using ImDotNet.Gui.Panels;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Text;
using Serilog;

namespace ImDotNet.Gui;

public sealed class AppWindow : IDisposable
{
    private readonly IWindow _window;
    private GL? _gl;
    private IInputContext? _input;
    private Controller? _controller;
    private readonly State _state;
    private readonly LogPanelState _logPanelState;
    private readonly MainPanel _mainPanel;
    private readonly LogPanel _logPanel;
    private readonly ILogger _log;
    private readonly string _configPath;

    public AppWindow()
    {
        _log = Logger.Get(typeof(AppWindow));
        _state = State.Load();
        _logPanelState = new LogPanelState();

        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImDotNet",
            "imgui.ini"
        );

        var opts = WindowOptions.Default with
        {
            Title = $"{About.AppName} {About.Version}",
            Size = new Vector2D<int>(1024, 768),
            API = GraphicsAPI.Default
        };
        _window = Window.Create(opts);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.Resize += OnResize;

        _mainPanel = new MainPanel(_logPanelState, () => _window.Close());
        _logPanel = new LogPanel(_logPanelState);

        LoadState(_configPath);
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();
        _input = _window.CreateInput();
        _controller = new Controller(_gl, _window, _input);

        Logger.SetupGui();

        foreach (var kb in _input.Keyboards)
            kb.KeyDown += (_, key, _) =>
            {
                if (key == Key.GraveAccent)
                {
                    _logPanelState.Visible = !_logPanelState.Visible;
                    SaveState(_configPath);
                }
            };

        _log.Information("GUI initialised");
    }

    private void OnUpdate(double dt)
    {
        _controller!.Update((float)dt);
    }

    private void OnRender(double dt)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewport = ImGui.GetMainViewport();
        var io = ImGui.GetIO();

        _logPanel.HandleToggleKey(io);

        // Full-screen host window - content goes here
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));

        var bgFlags = ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.NoSavedSettings;

        ImGui.Begin("##background", bgFlags);
        ImGui.PopStyleVar(3);

        _mainPanel.Draw();

        ImGui.End();

    s    _logPanel.Draw();
        _controller!.Render();
    }

    private void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(size);
        _controller?.WindowResized(size.X, size.Y);
    }

    private void OnClosing()
    {
        SaveState(_configPath);
        _state.Save();
        _log.Information("GUI closed");
    }

    private void LoadState(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                _log.Information($"No config found at {configPath}. Using defaults");
                return;
            }

            var lines = File.ReadAllLines(configPath);
            bool inStateSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed == "[State]")
                {
                    inStateSection = true;
                    continue;
                }

                if (trimmed.StartsWith("[") && inStateSection)
                    break;

                if (!inStateSection)
                    continue;

                if (trimmed.StartsWith("logs_panel_visible="))
                {
                    _logPanelState.Visible = trimmed.EndsWith("1");
                }
                else if (trimmed.StartsWith("logs_autoscroll="))
                {
                    _logPanelState.AutoScroll = trimmed.EndsWith("1");
                }
            }

            _log.Debug($"Loaded state from {configPath}");
        }
        catch (Exception ex)
        {
            _log.Warning("Could not read State from ini", ex);
        }
    }

    private void SaveState(string configPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            var sb = new StringBuilder();
            sb.AppendLine("[State]");
            sb.AppendLine($"logs_panel_visible={(_logPanelState.Visible ? 1 : 0)}");
            sb.AppendLine($"logs_autoscroll={(_logPanelState.AutoScroll ? 1 : 0)}");

            File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);
            _log.Debug($"Saved state to {configPath}");
        }
        catch (Exception ex)
        {
            _log.Warning("Could not save State to ini", ex);
        }
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _input?.Dispose();
        _gl?.Dispose();
        _window.Dispose();
    }
}