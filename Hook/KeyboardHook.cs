using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace AE.WinHook.Hook;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    Win = 8,
}

public delegate bool OnHookKey(KeyModifiers keyModifiers, Key key);

public class KeyboardHook : BaseHook
{
    public OnHookKey KeyDown { get; set; }
    public OnHookKey KeyUp { get; set; }

    public KeyboardHook()
    {
        HookType = WH_KEYBOARD_LL;
    }

    protected override int HookCallbackProcedure(int nCode, int wParam, IntPtr lParam)
    {
        if (nCode > -1 && (KeyDown != null || KeyUp != null))
        {
            var keyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

            var keyModifiers = KeyModifiers.None;
            var key = KeyInterop.KeyFromVirtualKey(keyboardHookStruct.vkCode);

            // Is Control
            if ( ((GetKeyState(VK_LCONTROL) & 0x80) != 0) || ((GetKeyState(VK_RCONTROL) & 0x80) != 0) )
                keyModifiers |= KeyModifiers.Control;

            // Is Alt
            if ( ((GetKeyState(VK_LALT) & 0x80) != 0) || ((GetKeyState(VK_RALT) & 0x80) != 0) )
                keyModifiers |= KeyModifiers.Alt;

            // Is Shift
            if ( ((GetKeyState(VK_LSHIFT) & 0x80) != 0) || ((GetKeyState(VK_RSHIFT) & 0x80) != 0))
                keyModifiers |= KeyModifiers.Shift;

            // Is Win
            if ( ((GetKeyState(VK_LWIN) & 0x80) != 0) || ((GetKeyState(VK_LWIN - VK_RWIN) & 0x80) != 0) )
                keyModifiers |= KeyModifiers.Win;

            var handled = false;
            switch (wParam)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    handled = KeyDown?.Invoke(keyModifiers, key) == true;
                    break;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    handled = KeyUp?.Invoke(keyModifiers, key) == true;
                    break;
            }

            if (handled)
                return 1;
        }

        return CallNextHookEx(HandleToHook, nCode, wParam, lParam);
    }

    public static bool IsKeyModifiers(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin => true,
            _ => false,
        };
    }

    public static KeyModifiers GetKeyModifiers(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => KeyModifiers.Control,
            Key.LeftAlt or Key.RightAlt => KeyModifiers.Alt,
            Key.LeftShift or Key.RightShift => KeyModifiers.Shift,
            Key.LWin or Key.RWin => KeyModifiers.Win,
            _ => KeyModifiers.None,
        };
    }

    public static bool IsKeyDown(Key key)
    {
        var code = KeyInterop.VirtualKeyFromKey(key);
        return (GetKeyState(code) & 0x80) != 0;
    }
}
