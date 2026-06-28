using ImDotNet.Cli.Commands;
using ImDotNet.Core.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

public class Program
{
    public static int Main(string[] args)
    {
        Logger.Setup(Level.INFO);

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("imdotnet");
            config.SetApplicationVersion(GetVersion());
            config.SetInterceptor(new LoggingInterceptor());

            config.AddCommand<GuiCommand>("gui")
                  .WithDescription("Launch the GUI");

            config.AddCommand<CheckCommand>("check")
                  .WithDescription("Quick self-check and exit");
        });

        try
        {
            if (args.Length == 0)
            {
                args = ["gui"];
            }

            return app.Run(args);
        }
        catch (CommandParseException ex)
        {
            AnsiConsole.MarkupLine($"[red]Parse error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 2;
        }
    }

    private static string GetVersion() =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            is { Length: > 0 } info
            ? info
            : "0.0.0+local";
}
