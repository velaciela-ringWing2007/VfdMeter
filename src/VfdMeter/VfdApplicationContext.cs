namespace VfdMeter;

internal sealed class VfdApplicationContext : ApplicationContext
{
    private readonly SystemMonitor _monitor;
    private readonly NetworkMonitor _networkMonitor;
    private readonly DiskMonitor _diskMonitor;
    private readonly ForegroundWindowMonitor _foregroundWindowMonitor;
    private readonly OverlayForm _overlay;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _notifyIconImage;
    private readonly ToolStripMenuItem _showMenuItem;
    private readonly ToolStripMenuItem _hideMenuItem;
    private readonly ToolStripMenuItem _resetPositionMenuItem;
    private readonly ToolStripMenuItem _alwaysOnTopMenuItem;
    private bool _disposed;

    public VfdApplicationContext()
    {
        _monitor = new SystemMonitor();
        _networkMonitor = new NetworkMonitor();
        _diskMonitor = new DiskMonitor();

        _menu = new ContextMenuStrip();
        _showMenuItem = new ToolStripMenuItem("表示", null, (_, _) => ShowOverlay());
        _hideMenuItem = new ToolStripMenuItem("非表示", null, (_, _) => HideOverlay());
        _alwaysOnTopMenuItem = new ToolStripMenuItem(
            "常に最前面",
            null,
            (_, _) => ToggleAlwaysOnTop());
        _resetPositionMenuItem = new ToolStripMenuItem(
            "タスクバー位置へ戻す",
            null,
            (_, _) => ResetOverlayPosition());
        _menu.Items.AddRange([
            _showMenuItem,
            _hideMenuItem,
            _alwaysOnTopMenuItem,
            _resetPositionMenuItem
        ]);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("終了", null, (_, _) => ExitThread());
        _menu.Opening += (_, _) => UpdateMenuState();

        _overlay = new OverlayForm(_menu);

        _notifyIconImage = (Icon)SystemIcons.Application.Clone();
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _notifyIconImage,
            Text = "VfdMeter",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowOverlay();

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => UpdateUsage();

        UpdateUsage();
        _overlay.Show();
        _foregroundWindowMonitor = new ForegroundWindowMonitor(RestoreOverlayTopmost);
        _timer.Start();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _timer.Dispose();
            _foregroundWindowMonitor.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip = null;
            _notifyIcon.Dispose();
            _notifyIconImage.Dispose();
            _overlay.Dispose();
            _menu.Dispose();
            _networkMonitor.Dispose();
            _diskMonitor.Dispose();
            _monitor.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateUsage()
    {
        var networkSpeed = _networkMonitor.GetSpeed();
        _overlay.SetUsage(
            _monitor.GetCpuUsage(),
            _monitor.GetMemoryUsage(),
            networkSpeed.ReceivedBytesPerSecond,
            networkSpeed.SentBytesPerSecond,
            _diskMonitor.GetActivePercentage());
    }

    private void ShowOverlay()
    {
        _overlay.Show();
        _overlay.ApplyTopmost();
    }

    private void HideOverlay() => _overlay.Hide();

    private void ToggleAlwaysOnTop() =>
        _overlay.SetAlwaysOnTop(!_overlay.AlwaysOnTopEnabled);

    private void ResetOverlayPosition()
    {
        _overlay.ResetToTaskbarPosition();
        _overlay.Show();
        _overlay.ApplyTopmost();
    }

    private void UpdateMenuState()
    {
        var isVisible = _overlay.Visible;
        _showMenuItem.Enabled = !isVisible;
        _hideMenuItem.Enabled = isVisible;
        _resetPositionMenuItem.Enabled = isVisible;
        _alwaysOnTopMenuItem.Checked = _overlay.AlwaysOnTopEnabled;
    }

    private void RestoreOverlayTopmost()
    {
        if (_disposed || !_overlay.Visible || !_overlay.AlwaysOnTopEnabled)
        {
            return;
        }

        if (_overlay.InvokeRequired)
        {
            try
            {
                _overlay.BeginInvoke(RestoreOverlayTopmost);
            }
            catch (InvalidOperationException)
            {
                // The form is shutting down.
            }

            return;
        }

        _overlay.ApplyTopmost();
    }
}
