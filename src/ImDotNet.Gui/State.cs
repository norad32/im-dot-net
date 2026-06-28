using System.Globalization;
using System.Runtime.InteropServices;

namespace ImDotNet.Gui;

public class State
{
    private static string ConfigPath()
    {
        string dir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImDotNet")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ImDotNet");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "gui_state.ini");
    }

    public bool ShowLogPanel { get; set; } = false;
    public float LogPanelHeightFraction { get; set; } = 0.333f;

    public void Save()
    {
        var lines = new[]
        {
            "[gui]",
            $"show_log_panel={ShowLogPanel}",
            $"log_panel_height_fraction={LogPanelHeightFraction.ToString(CultureInfo.InvariantCulture)}"
        };
        File.WriteAllLines(ConfigPath(), lines);
    }

    public static State Load()
    {
        var state = new State();
        string path = ConfigPath();
        if (!File.Exists(path)) return state;
        foreach (var line in File.ReadAllLines(path))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            switch (key)
            {
                case "show_log_panel":
                    if (bool.TryParse(val, out bool b)) state.ShowLogPanel = b; break;
                case "log_panel_height_fraction":
                    if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                        state.LogPanelHeightFraction = f; break;
            }
        }
        return state;
    }
}
