using System.Runtime.InteropServices;

namespace VfdMeter;

internal sealed class ForegroundWindowMonitor : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0x0000;

    private readonly Action _foregroundChanged;
    private readonly WinEventDelegate _callback;
    private nint _hook;

    public ForegroundWindowMonitor(Action foregroundChanged)
    {
        _foregroundChanged = foregroundChanged ?? throw new ArgumentNullException(nameof(foregroundChanged));
        _callback = OnWinEvent;
        _hook = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            0,
            _callback,
            0,
            0,
            WineventOutOfContext);
    }

    public void Dispose()
    {
        if (_hook == 0)
        {
            return;
        }

        _ = UnhookWinEvent(_hook);
        _hook = 0;
    }

    private void OnWinEvent(
        nint hook,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime)
    {
        try
        {
            _foregroundChanged();
        }
        catch
        {
            // A shell event must never terminate the application.
        }
    }

    private delegate void WinEventDelegate(
        nint hook,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint eventHookModule,
        WinEventDelegate callback,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(nint hook);
}
