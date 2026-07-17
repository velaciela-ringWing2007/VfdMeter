namespace VfdMeter;

internal sealed class VfdApplicationContext : ApplicationContext
{
    private readonly SystemMonitor _monitor;
    private readonly OverlayForm _overlay;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _notifyIconImage;
    private bool _disposed;

    public VfdApplicationContext()
    {
        _monitor = new SystemMonitor();
        _overlay = new OverlayForm();

        _menu = new ContextMenuStrip();
        _menu.Items.Add("表示", null, (_, _) => ShowOverlay());
        _menu.Items.Add("非表示", null, (_, _) => _overlay.Hide());
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
            _monitor.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateUsage() =>
        _overlay.SetUsage(_monitor.GetCpuUsage(), _monitor.GetMemoryUsage());

    private void ShowOverlay()
    {
        _overlay.Show();
        _overlay.BringToFront();
    }
}
