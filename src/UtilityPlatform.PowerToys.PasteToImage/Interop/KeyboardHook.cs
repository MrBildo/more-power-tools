using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UtilityPlatform.PowerToys.PasteToImage.Interop;

public readonly record struct KeyboardHookEvent(Keys VirtualKey, bool ControlPressed, bool IsKeyDown);

public sealed class KeyboardHook : IDisposable
{
    private readonly Func<KeyboardHookEvent, bool> _onEvent;
    private readonly Win32.LowLevelKeyboardProc _proc;

    private nint _hookHandle;

    public KeyboardHook(Func<KeyboardHookEvent, bool> onEvent)
    {
        _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != nint.Zero)
        {
            return;
        }

        _hookHandle = Win32.InstallKeyboardHook(_proc);

        if (_hookHandle == nint.Zero)
        {
            throw new InvalidOperationException($"Unable to install keyboard hook (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }

    public void Dispose()
    {
        if (_hookHandle == nint.Zero)
        {
            return;
        }

        Win32.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = nint.Zero;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
        {
            return Win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var isKeyDown = Win32.IsKeyDownMessage(wParam);

        var keyboardData = Marshal.PtrToStructure<Win32.Kbdllhookstruct>(lParam);
        var key = (Keys)keyboardData.VkCode;

        var evt = new KeyboardHookEvent(key, Win32.IsControlPressed(), isKeyDown);
        var handled = _onEvent(evt);

        return handled ? 1 : Win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

