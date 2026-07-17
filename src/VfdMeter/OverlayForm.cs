namespace VfdMeter;

internal sealed class OverlayForm : Form
{
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmMouseActivate = 0x0021;
    private const int WmNcHitTest = 0x0084;
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;
    private const int WmDpiChanged = 0x02E0;
    private const int SpiSetWorkArea = 0x002F;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int MaNoActivate = 3;
    private const string DisplayFontFamily = "Consolas";
    private const float DisplayFontSize = 15f;
    private const int LeftPadding = 10;
    private const int RightPadding = 12;
    private const string MaximumDisplayText = "CPU 100% MEM 100% NET ↓999.9G ↑999.9G";
    private const string AdditionalWidthText = "0000";

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
    private readonly uint _taskbarCreatedMessage;

    public OverlayForm()
    {
        _taskbarCreatedMessage = NativeTaskbarApi.RegisterTaskbarCreatedMessage();
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

    protected override bool ShowWithoutActivation => true;

    public void ResetToTaskbarPosition()
    {
        _userMoved = false;
        UpdateDisplayWidth();
        PlaceAtDefaultPosition();
        ApplyTopmost();
    }

    public void ApplyTopmost() =>
        NativeTaskbarApi.ApplyTopmostWithoutActivation(Handle);

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

        using var font = CreateDisplayFont();
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        var x = ScaleLogicalPixels(LeftPadding);
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
        UpdateDisplayWidth();
        PlaceAtDefaultPosition();
        _initialPlacementCompleted = true;
        ApplyTopmost();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (_initialPlacementCompleted && !_isAdjustingBounds)
        {
            if (_userMoved)
            {
                KeepFullyInsideCurrentScreen();
            }
            else
            {
                PlaceAtDefaultPosition();
            }
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmMouseActivate)
        {
            message.Result = (nint)MaNoActivate;
            return;
        }

        base.WndProc(ref message);

        if (_taskbarCreatedMessage != 0 && (uint)message.Msg == _taskbarCreatedMessage)
        {
            if (!_userMoved)
            {
                PlaceAtDefaultPosition();
            }

            ApplyTopmost();
            return;
        }

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
                UpdateDisplayWidth();
                HandleEnvironmentChange();
                break;

            case WmSettingChange when message.WParam == (nint)SpiSetWorkArea:
                HandleEnvironmentChange();
                break;
        }
    }

    private void PlaceAtDefaultPosition()
    {
        var dpiScale = DeviceDpi / 96d;
        if (NativeTaskbarApi.TryGetTaskbarPosition(out var taskbar) &&
            WindowPlacement.TryGetTaskbarBounds(Size, taskbar, dpiScale, out var taskbarBounds))
        {
            SetBoundsSafely(taskbarBounds);
        }
        else
        {
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
            SetBoundsSafely(WindowPlacement.GetInitialBounds(Size, workingArea, dpiScale));
        }
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
            PlaceAtDefaultPosition();
        }

        ApplyTopmost();
    }

    private void KeepFullyInsideCurrentScreen()
    {
        var screenBounds = Screen.FromRectangle(Bounds).Bounds;
        SetBoundsSafely(WindowPlacement.ClampFullyVisible(Bounds, screenBounds));
    }

    private void KeepPartiallyVisible()
    {
        var screenBounds = Screen.FromRectangle(Bounds).Bounds;
        var minimumVisiblePixels = Math.Max(1, (int)Math.Round(
            WindowPlacement.MinimumVisibleLength * DeviceDpi / 96d));
        SetBoundsSafely(WindowPlacement.ClampPartiallyVisible(
            Bounds,
            screenBounds,
            minimumVisiblePixels));
    }

    private void UpdateDisplayWidth()
    {
        using var font = CreateDisplayFont();
        using var graphics = CreateGraphics();
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        var contentWidth = TextRenderer.MeasureText(
            graphics,
            MaximumDisplayText,
            font,
            Size.Empty,
            flags).Width;
        var extraWidth = TextRenderer.MeasureText(
            graphics,
            AdditionalWidthText,
            font,
            Size.Empty,
            flags).Width;
        var horizontalPadding = ScaleLogicalPixels(LeftPadding) + ScaleLogicalPixels(RightPadding);
        ClientSize = new Size(contentWidth + extraWidth + horizontalPadding, ClientSize.Height);
    }

    private int ScaleLogicalPixels(int pixels) =>
        Math.Max(0, (int)Math.Round(pixels * DeviceDpi / 96d));

    private static Font CreateDisplayFont() =>
        new(DisplayFontFamily, DisplayFontSize, FontStyle.Bold, GraphicsUnit.Point);

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
