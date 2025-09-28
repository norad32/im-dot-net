using ImDotNet.Cli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

public sealed class Program
{
    public static int Main(string[] args)
    {
        if (args.Contains("--version") || args.Contains("-v"))
        {
            ImDotNet.Core.Logging.Logger.Setup(ImDotNet.Core.Logging.Level.ERROR); // minimal noise
            var version = GetVersion();
            ImDotNet.Core.Logging.Logger.Get(typeof(Program)).InfoFormat("Version: {Version}", version);
            AnsiConsole.WriteLine(version);
            return 0;
        }

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("Im .NET");

            // Commands
            // config.AddCommand<Gui>("gui")
            //       .WithDescription("Launch the GUI");

            config.AddCommand<Check>("check")
                  .WithDescription("Quick self-check and exit");

            // Default to 'gui' when no subcommand is provided
            // config.SetDefaultCommand<Gui>();
        });

        try
        {
            return app.Run(args);
        }
        catch (CommandParseException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return -1;
        }
    }

    private static string GetVersion()
    {
        // Prefer InformationalVersion (can carry "+local" suffix), fall back to FileVersion.
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
            return info!;
        var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        return string.IsNullOrWhiteSpace(file) ? "0.0.0+local" : file!;
    }
}
