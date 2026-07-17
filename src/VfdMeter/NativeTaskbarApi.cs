using System.Runtime.InteropServices;

namespace VfdMeter;

internal enum TaskbarEdge : uint
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3
}

internal readonly record struct TaskbarPosition(Rectangle Bounds, TaskbarEdge Edge);

internal static class NativeTaskbarApi
{
    private const uint AbmGetTaskbarPos = 0x00000005;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly nint HwndTopmost = new(-1);

    public static bool TryGetTaskbarPosition(out TaskbarPosition position)
    {
        var data = new AppBarData
        {
            Size = (uint)Marshal.SizeOf<AppBarData>()
        };

        if (SHAppBarMessage(AbmGetTaskbarPos, ref data) == 0)
        {
            position = default;
            return false;
        }

        var bounds = Rectangle.FromLTRB(
            data.Rect.Left,
            data.Rect.Top,
            data.Rect.Right,
            data.Rect.Bottom);

        if (bounds.Width <= 0 || bounds.Height <= 0 || data.Edge > (uint)TaskbarEdge.Bottom)
        {
            position = default;
            return false;
        }

        position = new TaskbarPosition(bounds, (TaskbarEdge)data.Edge);
        return true;
    }

    public static uint RegisterTaskbarCreatedMessage() =>
        RegisterWindowMessage("TaskbarCreated");

    public static void ApplyTopmostWithoutActivation(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return;
        }

        _ = SetWindowPos(
            windowHandle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern nuint SHAppBarMessage(uint message, ref AppBarData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct AppBarData
    {
        public uint Size;
        public nint WindowHandle;
        public uint CallbackMessage;
        public uint Edge;
        public NativeRect Rect;
        public nint Parameter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
