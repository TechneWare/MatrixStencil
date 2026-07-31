using System.Runtime.InteropServices;

namespace MatrixStencil.ConsoleHost;

internal static class AnsiSupport
{
    private const int StandardOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    public static bool TryEnable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        var handle = GetStdHandle(StandardOutputHandle);

        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            return false;
        }

        if (!GetConsoleMode(handle, out var mode))
        {
            return false;
        }

        return SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);
}
