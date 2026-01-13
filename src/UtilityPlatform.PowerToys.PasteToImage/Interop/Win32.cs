using System.Runtime.InteropServices;
using System.Text;

namespace UtilityPlatform.PowerToys.PasteToImage.Interop;

internal static class Win32
{
    private const int _whKeyboardLl = 13;

    private const int _wmKeyDown = 0x0100;
    private const int _wmSysKeyDown = 0x0104;

    private const int _vkControl = 0x11;

    private const uint _cfBitmap = 2;
    private const uint _cfDib = 8;
    private const uint _cfDibV5 = 17;

    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct Kbdllhookstruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nint DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    public static bool IsKeyDownMessage(nint wParam)
        => wParam == _wmKeyDown || wParam == _wmSysKeyDown;

    public static bool IsControlPressed()
        => (GetKeyState(_vkControl) & 0x8000) != 0;

    public static string GetWindowClassName(nint hWnd)
    {
        var builder = new StringBuilder(256);
        _ = GetClassName(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    public static nint InstallKeyboardHook(LowLevelKeyboardProc proc)
    {
        var moduleHandle = GetModuleHandle(null);
        return SetWindowsHookEx(_whKeyboardLl, proc, moduleHandle, 0);
    }

    public static bool ContainsClipboardImage()
        => IsClipboardFormatAvailable(_cfDib)
           || IsClipboardFormatAvailable(_cfDibV5)
           || IsClipboardFormatAvailable(_cfBitmap);
}

