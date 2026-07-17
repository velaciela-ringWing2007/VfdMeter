using System.Globalization;

namespace VfdMeter;

internal static class MetricFormatter
{
    private const double MaximumScaledValue = 999.9;

    public static string FormatNetworkSpeed(double bytesPerSecond)
    {
        var value = double.IsFinite(bytesPerSecond) ? Math.Max(0, bytesPerSecond) : 0;
        var (scaledValue, unit) = value switch
        {
            >= 1_000_000_000 => (value / 1_000_000_000, 'G'),
            >= 1_000_000 => (value / 1_000_000, 'M'),
            >= 1_000 => (value / 1_000, 'K'),
            _ => (value, 'B')
        };

        var roundedValue = Math.Round(scaledValue, 1, MidpointRounding.AwayFromZero);
        var displayValue = Math.Min(MaximumScaledValue, roundedValue);
        return displayValue.ToString("000.0", CultureInfo.InvariantCulture) + unit;
    }
}
