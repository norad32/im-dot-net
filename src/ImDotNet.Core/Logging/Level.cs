using System.Globalization;

namespace ImDotNet.Core.Logging;

public enum Level
{
    NOTSET   = 0,
    DEBUG    = 10,
    INFO     = 20,
    WARNING  = 30,
    ERROR    = 40,
    CRITICAL = 50,
}

public static class LevelParser
{
    public static Level Resolve(string? text, string fallback)
        => Parse(string.IsNullOrWhiteSpace(text) ? fallback : text!);

    public static Level Parse(string text)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
        {
            return num switch
            {
                >= 50 => Level.CRITICAL,
                40    => Level.ERROR,
                30    => Level.WARNING,
                20    => Level.INFO,
                10    => Level.DEBUG,
                _     => Level.NOTSET
            };
        }

        return text.Trim().ToUpperInvariant() switch
        {
            "CRITICAL" => Level.CRITICAL,
            "ERROR"    => Level.ERROR,
            "WARNING"  => Level.WARNING,
            "INFO"     => Level.INFO,
            "DEBUG"    => Level.DEBUG,
            "NOTSET"   => Level.NOTSET,
            _          => throw new InvalidOperationException(
                $"Invalid log level '{text}'. Use CRITICAL, ERROR, WARNING, INFO, DEBUG, NOTSET or 50/40/30/20/10/0.")
        };
    }
}
