using System.Diagnostics;

namespace VfdMeter;

internal sealed class DiskMonitor : IDisposable
{
    private PerformanceCounter? _counter;
    private bool _disposed;

    public DiskMonitor()
    {
        try
        {
            _counter = new PerformanceCounter(
                "PhysicalDisk",
                "% Disk Time",
                "_Total",
                readOnly: true);
            _ = _counter.NextValue();
        }
        catch
        {
            _counter?.Dispose();
            _counter = null;
        }
    }

    public int? GetActivePercentage()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_counter is null)
        {
            return null;
        }

        try
        {
            var value = _counter.NextValue();
            return float.IsFinite(value)
                ? Math.Clamp((int)Math.Round(value), 0, 100)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _counter?.Dispose();
        _counter = null;
    }
}
