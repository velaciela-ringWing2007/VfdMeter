namespace VfdMeter;

internal sealed class OverlayForm : Form
{
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmMouseActivate = 0x0021;
    private const int WmContextMenu = 0x007B;
    private const int WmNcRightButtonUp = 0x00A5;
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
    private const int RightPadding = 10;
    private const int RightSafetyMargin = 12;
    private const string MaximumDisplayText = "CPU 100% MEM 100% NET ↓999.9G ↑999.9G DSK 100%";

    private static readonly Color NormalColor = Color.FromArgb(35, 235, 210);
    private static readonly Color WarningColor = Color.FromArgb(255, 165, 45);
    private static readonly Color CriticalColor = Color.FromArgb(255, 65, 65);

    private int _cpuUsage;
    private int _memoryUsage;
    private double _receivedBytesPerSecond;
    private double _sentBytesPerSecond;
    private int? _diskUsage;
    private bool _initialPlacementCompleted;
    private bool _userMoved;
    private bool _isAdjustingBounds;
    private readonly uint _taskbarCreatedMessage;
    private readonly ContextMenuStrip _sharedContextMenu;

    public OverlayForm(ContextMenuStrip sharedContextMenu)
    {
        _sharedContextMenu = sharedContextMenu ?? throw new ArgumentNullException(nameof(sharedContextMenu));
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
        double sentBytesPerSecond,
        int? diskUsage)
    {
        _cpuUsage = cpuUsage;
        _memoryUsage = memoryUsage;
        _receivedBytesPerSecond = receivedBytesPerSecond;
        _sentBytesPerSecond = sentBytesPerSecond;
        _diskUsage = diskUsage;
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
        DrawPart(e.Graphics, $"↓{MetricFormatter.FormatNetworkSpeed(_receivedBytesPerSecond)}", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, $" ↑{MetricFormatter.FormatNetworkSpeed(_sentBytesPerSecond)}", NormalColor, font, flags, ref x, y);
        DrawPart(e.Graphics, " DSK ", NormalColor, font, flags, ref x, y);
        DrawPart(
            e.Graphics,
            _diskUsage is int diskUsage ? $"{diskUsage:000}%" : "---",
            _diskUsage is int usage ? GetUsageColor(usage) : NormalColor,
            font,
            flags,
            ref x,
            y);
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
        if (message.Msg is WmContextMenu or WmNcRightButtonUp)
        {
            ShowContextMenu(message.LParam);
            return;
        }

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
        ClientSize = new Size(CalculateRequiredWidth(), ClientSize.Height);
    }

    private int CalculateRequiredWidth()
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
        var horizontalPadding = ScaleLogicalPixels(LeftPadding) + ScaleLogicalPixels(RightPadding);
        var safetyMargin = ScaleLogicalPixels(RightSafetyMargin);
        return contentWidth + horizontalPadding + safetyMargin;
    }

    private void ShowContextMenu(nint coordinates)
    {
        Point screenLocation;
        if (coordinates == -1)
        {
            screenLocation = PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
        }
        else
        {
            var packedCoordinates = (long)coordinates;
            screenLocation = new Point(
                unchecked((short)(packedCoordinates & 0xFFFF)),
                unchecked((short)((packedCoordinates >> 16) & 0xFFFF)));
        }

        _sharedContextMenu.Show(screenLocation);
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
