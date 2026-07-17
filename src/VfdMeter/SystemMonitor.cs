using System.Runtime.InteropServices;

namespace VfdMeter;

internal sealed class SystemMonitor : IDisposable
{
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;
    private bool _hasPreviousSample;
    private bool _disposed;

    public int GetCpuUsage()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var currentIdle = ToUInt64(idle);
        var currentKernel = ToUInt64(kernel);
        var currentUser = ToUInt64(user);

        if (!_hasPreviousSample)
        {
            _previousIdle = currentIdle;
            _previousKernel = currentKernel;
            _previousUser = currentUser;
            _hasPreviousSample = true;
            return 0;
        }

        var idleDelta = currentIdle - _previousIdle;
        var totalDelta = (currentKernel - _previousKernel) + (currentUser - _previousUser);

        _previousIdle = currentIdle;
        _previousKernel = currentKernel;
        _previousUser = currentUser;

        if (totalDelta == 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(100d * (totalDelta - idleDelta) / totalDelta), 0, 100);
    }

    public int GetMemoryUsage()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        return GlobalMemoryStatusEx(ref status) ? (int)status.MemoryLoad : 0;
    }

    public void Dispose() => _disposed = true;

    private static ulong ToUInt64(FileTime value) =>
        ((ulong)value.HighDateTime << 32) | value.LowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out FileTime idleTime,
        out FileTime kernelTime,
        out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
