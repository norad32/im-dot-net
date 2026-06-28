using ImDotNet.Core.Logging;
using Spectre.Console.Cli;
using ImDotNet.Gui;

namespace ImDotNet.Cli.Commands;

public sealed class GuiCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings)
    {
        var log = Logger.Get(typeof(GuiCommand));

        try
        {
            log.Information("Launching GUI...");
            Logger.SetupGui();
            using var window = new AppWindow();
            window.Run();
            return 0;
        }
        catch (Exception ex)
        {
            log.Error(ex, "GUI terminated unexpectedly");
            return 1;
        }
    }
}