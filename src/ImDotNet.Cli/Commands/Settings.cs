using System.ComponentModel;
using Spectre.Console.Cli;

namespace ImDotNet.Cli.Commands;

public class Settings : CommandSettings
{
    [CommandOption("-l|--log-level <LEVEL>")]
    [Description("Set log level (name or number): CRITICAL, ERROR, WARNING, INFO, DEBUG, NOTSET or 50/40/30/20/10/0. Subcommand value overrides global.")]
    public string? LogLevelText { get; set; }
}
