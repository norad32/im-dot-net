namespace ImDotNet.Core;

public static class About
{
    public const string AppName = "ImDotNet";
    public const string Version = "1.0.0";

    public static string Description() =>
        $"{AppName} - a .NET imgui-bundle template (ImGui.NET + Silk.NET). Version {Version}.";
}