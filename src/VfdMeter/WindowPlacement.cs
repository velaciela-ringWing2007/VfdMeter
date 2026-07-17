namespace VfdMeter;

internal static class WindowPlacement
{
    public const int RightMargin = 32;
    public const int VerticalMargin = 12;
    public const int MinimumVisibleLength = 32;

    public static Rectangle GetInitialBounds(
        Size windowSize,
        Rectangle workingArea,
        double dpiScale)
    {
        var rightMargin = ScaleForDpi(RightMargin, dpiScale);
        var verticalMargin = ScaleForDpi(VerticalMargin, dpiScale);
        var desiredBounds = new Rectangle(
            workingArea.Right - windowSize.Width - rightMargin,
            workingArea.Bottom - windowSize.Height - verticalMargin,
            windowSize.Width,
            windowSize.Height);

        return ClampFullyVisible(desiredBounds, workingArea);
    }

    public static Rectangle ClampFullyVisible(Rectangle bounds, Rectangle workingArea)
    {
        var width = Math.Min(bounds.Width, workingArea.Width);
        var height = Math.Min(bounds.Height, workingArea.Height);
        var x = Math.Clamp(bounds.X, workingArea.Left, workingArea.Right - width);
        var y = Math.Clamp(bounds.Y, workingArea.Top, workingArea.Bottom - height);

        return new Rectangle(x, y, width, height);
    }

    public static Rectangle ClampPartiallyVisible(
        Rectangle bounds,
        Rectangle workingArea,
        int minimumVisibleLength)
    {
        var visibleWidth = Math.Min(minimumVisibleLength, Math.Min(bounds.Width, workingArea.Width));
        var visibleHeight = Math.Min(minimumVisibleLength, Math.Min(bounds.Height, workingArea.Height));
        var x = Math.Clamp(
            bounds.X,
            workingArea.Left - bounds.Width + visibleWidth,
            workingArea.Right - visibleWidth);
        var y = Math.Clamp(
            bounds.Y,
            workingArea.Top - bounds.Height + visibleHeight,
            workingArea.Bottom - visibleHeight);

        return new Rectangle(x, y, bounds.Width, bounds.Height);
    }

    private static int ScaleForDpi(int logicalPixels, double dpiScale) =>
        Math.Max(0, (int)Math.Round(logicalPixels * dpiScale));
}
