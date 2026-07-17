namespace VfdMeter;

internal sealed class VfdApplicationContext : ApplicationContext
{
    private readonly SystemMonitor _monitor;
    private readonly NetworkMonitor _networkMonitor;
    private readonly OverlayForm _overlay;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _notifyIconImage;
    private bool _disposed;

    public VfdApplicationContext()
    {
        _monitor = new SystemMonitor();
        _networkMonitor = new NetworkMonitor();
        _overlay = new OverlayForm();

        _menu = new ContextMenuStrip();
        _menu.Items.Add("表示", null, (_, _) => ShowOverlay());
        _menu.Items.Add("非表示", null, (_, _) => _overlay.Hide());
        _menu.Items.Add("タスクバー位置へ戻す", null, (_, _) => ResetOverlayPosition());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("終了", null, (_, _) => ExitThread());

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
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIconImage.Dispose();
            _menu.Dispose();
            _overlay.Dispose();
            _networkMonitor.Dispose();
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
            networkSpeed.SentBytesPerSecond);
    }

    private void ShowOverlay()
    {
        _overlay.Show();
        _overlay.ApplyTopmost();
    }

    private void ResetOverlayPosition()
    {
        _overlay.ResetToTaskbarPosition();
        _overlay.Show();
        _overlay.ApplyTopmost();
    }
}
