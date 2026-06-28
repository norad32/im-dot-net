using Spectre.Console;
using Spectre.Console.Cli;
using ImDotNet.Core.Logging;

namespace ImDotNet.Cli.Commands;

public sealed class CheckCommand : Command<GlobalSettings>
{
    private const string OkMessage = "I'm .NET OK";

    public override int Execute(CommandContext context, GlobalSettings settings)
    {
        Logger.Get(typeof(CheckCommand)).Information(OkMessage);
        AnsiConsole.MarkupLine($"[green]{OkMessage}[/]");
        return 0;
    }
}