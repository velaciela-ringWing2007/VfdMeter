namespace VfdMeter;

internal sealed class OverlayForm : Form
{
    private static readonly Color NormalColor = Color.FromArgb(35, 235, 210);
    private static readonly Color WarningColor = Color.FromArgb(255, 165, 45);
    private static readonly Color CriticalColor = Color.FromArgb(255, 65, 65);

    private int _cpuUsage;
    private int _memoryUsage;
    private double _receivedBytesPerSecond;
    private double _sentBytesPerSecond;

    public OverlayForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.Black;
        ClientSize = new Size(468, 44);
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Location = new Point(workingArea.Right - Width - 12, workingArea.Bottom - Height - 12);
    }

    public void SetUsage(
        int cpuUsage,
        int memoryUsage,
        double receivedBytesPerSecond,
        double sentBytesPerSecond)
    {
        _cpuUsage = cpuUsage;
        _memoryUsage = memoryUsage;
        _receivedBytesPerSecond = receivedBytesPerSecond;
        _sentBytesPerSecond = sentBytesPerSecond;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var font = new Font("Consolas", 15f, FontStyle.Bold, GraphicsUnit.Point);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        var x = 10;
        var y = (ClientSize.Height - TextRenderer.MeasureText("0", font, Size.Empty, flags).Height) / 2;

        DrawPart(e.Graphics, "CPU ", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, $"{_cpuUsage:000}%", GetUsageColor(_cpuUsage), font, flags, ref x, y);
        DrawPart(e.Graphics, " MEM ", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, $"{_memoryUsage:000}%", GetUsageColor(_memoryUsage), font, flags, ref x, y);
        DrawPart(e.Graphics, " NET ", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, $"↓{NetworkSpeedFormatter.Format(_receivedBytesPerSecond)}", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, $" ↑{NetworkSpeedFormatter.Format(_sentBytesPerSecond)}", NormalColor, font, flags, ref x, y);
    }

    private static void DrawPart(
        Graphics graphics,
        string text,
        Color color,
        Font font,
        TextFormatFlags flags,
        ref int x,
        int y)
    {
        TextRenderer.DrawText(graphics, text, font, new Point(x, y), color, flags);
        x += TextRenderer.MeasureText(text, font, Size.Empty, flags).Width;
    }

    private static Color GetUsageColor(int usage) => usage switch
    {
        >= 90 => CriticalColor,
        >= 70 => WarningColor,
        _ => NormalColor
    };
}
