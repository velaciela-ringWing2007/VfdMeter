using System.Globalization;

namespace VfdMeter;

internal static class NetworkSpeedFormatter
{
    public static string Format(double bytesPerSecond)
    {
        var value = Math.Max(0, bytesPerSecond);

        return value switch
        {
            >= 1_000_000_000 => FormatScaled(value / 1_000_000_000, "G"),
            >= 1_000_000 => FormatScaled(value / 1_000_000, "M"),
            >= 1_000 => FormatScaled(value / 1_000, "K"),
            _ => $"{Math.Min(999, Math.Round(value)):0}B"
        };
    }

    private static string FormatScaled(double value, string suffix) =>
        value.ToString("0.#", CultureInfo.InvariantCulture) + suffix;
}
