using Spectre.Console;
using Spectre.Console.Cli;
using ImDotNet.Core.Logging;

namespace ImDotNet.Cli.Commands;

public class Check : Command<Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var level = ImDotNet.Core.Logging.LevelParser.Resolve(settings.LogLevelText, fallback: "ERROR");
        ImDotNet.Core.Logging.Logger.Setup(level);

        ImDotNet.Core.Logging.Logger.Get(typeof(Check)).Info("I'm .NET OK");
        AnsiConsole.MarkupLine("I'm .NET OK");
        return 0;
    }
}
