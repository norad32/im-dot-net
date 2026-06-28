using System.ComponentModel;
using Spectre.Console.Cli;

namespace ImDotNet.Cli.Commands;

public class GlobalSettings : CommandSettings
{
    [CommandOption("-l|--log-level <LEVEL>")]
    [Description("Set log level (name or number): CRITICAL, ERROR, WARNING, INFO, DEBUG, NOTSET or 50/40/30/20/10/0.")]
    public string? LogLevelText { get; set; }
}