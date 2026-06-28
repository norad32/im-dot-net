using ImDotNet.Core.Logging;
using Spectre.Console.Cli;

namespace ImDotNet.Cli.Commands;

public sealed class LoggingInterceptor : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is GlobalSettings s)
        {
            var level = LevelParser.Resolve(s.LogLevelText, fallback: "ERROR");
            Logger.Setup(level);
        }
    }
}