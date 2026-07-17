namespace VfdMeter;

internal sealed class OverlayForm : Form
{
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmNcHitTest = 0x0084;
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;
    private const int WmDpiChanged = 0x02E0;
    private const int SpiSetWorkArea = 0x002F;
    private const int HtClient = 1;
    private const int HtCaption = 2;

    private static readonly Color NormalColor = Color.FromArgb(35, 235, 210);
    private static readonly Color WarningColor = Color.FromArgb(255, 165, 45);
    private static readonly Color CriticalColor = Color.FromArgb(255, 65, 65);

    private int _cpuUsage;
    private int _memoryUsage;
    private double _receivedBytesPerSecond;
    private double _sentBytesPerSecond;
    private bool _initialPlacementCompleted;
    private bool _userMoved;
    private bool _isAdjustingBounds;

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
    }

    public Point CurrentPosition => Location;

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

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PlaceInitially();
        _initialPlacementCompleted = true;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (_initialPlacementCompleted && !_isAdjustingBounds)
        {
            KeepFullyInsideCurrentScreen();
        }
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);

        switch (message.Msg)
        {
            case WmNcHitTest when message.Result == (nint)HtClient:
                message.Result = (nint)HtCaption;
                break;

            case WmEnterSizeMove:
                _userMoved = true;
                break;

            case WmExitSizeMove:
                KeepPartiallyVisible();
                break;

            case WmDisplayChange:
            case WmDpiChanged:
                HandleEnvironmentChange();
                break;

            case WmSettingChange when message.WParam == (nint)SpiSetWorkArea:
                HandleEnvironmentChange();
                break;
        }
    }

    private void PlaceInitially()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        var dpiScale = DeviceDpi / 96d;
        SetBoundsSafely(WindowPlacement.GetInitialBounds(
            Size,
            workingArea,
            dpiScale));
    }

    private void HandleEnvironmentChange()
    {
        if (!_initialPlacementCompleted)
        {
            return;
        }

        if (_userMoved)
        {
            KeepFullyInsideCurrentScreen();
        }
        else
        {
            PlaceInitially();
        }
    }

    private void KeepFullyInsideCurrentScreen()
    {
        var workingArea = Screen.FromRectangle(Bounds).WorkingArea;
        SetBoundsSafely(WindowPlacement.ClampFullyVisible(Bounds, workingArea));
    }

    private void KeepPartiallyVisible()
    {
        var workingArea = Screen.FromRectangle(Bounds).WorkingArea;
        var minimumVisiblePixels = Math.Max(1, (int)Math.Round(
            WindowPlacement.MinimumVisibleLength * DeviceDpi / 96d));
        SetBoundsSafely(WindowPlacement.ClampPartiallyVisible(
            Bounds,
            workingArea,
            minimumVisiblePixels));
    }

    private void SetBoundsSafely(Rectangle bounds)
    {
        if (Bounds == bounds)
        {
            return;
        }

        _isAdjustingBounds = true;
        try
        {
            Bounds = bounds;
        }
        finally
        {
            _isAdjustingBounds = false;
        }
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
