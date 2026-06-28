using ImGuiNET;
using Serilog;
using ImDotNet.Core.Logging;

namespace ImDotNet.Gui.Panels;

public sealed class MainPanel
{
    private readonly ILogger _log;
    private readonly LogPanelState _logPanelState;
    private readonly Action _onClose;

    public MainPanel(LogPanelState logPanelState, Action onClose)
    {
        _log = Logger.Get(typeof(MainPanel));
        _logPanelState = logPanelState;
        _onClose = onClose;
    }

    public void Draw()
    {
        ImGui.Text("Hello I'm .Net!");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("- Edit this GUI in src/ImDotNet.Gui/panels/MainPanel.cs");
        ImGui.Text("- Add panels, menus, docking, etc.");
        ImGui.Spacing();

        if (ImGui.Button("Show Logs (" + LogPanel.TOGGLE_KEY + ")"))
            _logPanelState.Visible = !_logPanelState.Visible;

        ImGui.SameLine();
        if (ImGui.Button("Generate test logs"))
        {
            GenerateTestLogs();
        }

        ImGui.Spacing();
        if (ImGui.Button("Close"))
        {
            _onClose();
        }
    }

    private void GenerateTestLogs()
    {
        _log.Debug("Debug log entry");
        _log.Information("Info log entry");
        _log.Warning("Warning log entry");
        _log.Error("Error log entry");
        _log.Fatal("Fatal log entry");
    }
}
