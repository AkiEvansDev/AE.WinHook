using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace AE.WinHook.Hook;

public abstract class BaseHook
{
    #region Windows API Code

    [StructLayout(LayoutKind.Sequential)]
    protected class POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected class MouseHookStruct
    {
        public POINT pt;
        public int hwnd;
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected class MouseLLHookStruct
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public int dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected class KeyboardHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public int dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    protected static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    protected static extern int UnhookWindowsHookEx(int idHook);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    protected static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    protected static extern short GetKeyState(int vKey);

    protected delegate int HookProc(int nCode, int wParam, IntPtr lParam);

    protected const int WH_MOUSE_LL = 14;
    protected const int WH_KEYBOARD_LL = 13;

    protected const int WH_MOUSE = 7;
    protected const int WH_KEYBOARD = 2;
    protected const int WM_MOUSEMOVE = 0x200;
    protected const int WM_LBUTTONDOWN = 0x201;
    protected const int WM_RBUTTONDOWN = 0x204;
    protected const int WM_MBUTTONDOWN = 0x207;
    protected const int WM_LBUTTONUP = 0x202;
    protected const int WM_RBUTTONUP = 0x205;
    protected const int WM_MBUTTONUP = 0x208;
    protected const int WM_LBUTTONDBLCLK = 0x203;
    protected const int WM_RBUTTONDBLCLK = 0x206;
    protected const int WM_MBUTTONDBLCLK = 0x209;
    protected const int WM_MOUSEWHEEL = 0x020A;
    protected const int WM_KEYDOWN = 0x100;
    protected const int WM_KEYUP = 0x101;
    protected const int WM_SYSKEYDOWN = 0x104;
    protected const int WM_SYSKEYUP = 0x105;

    protected const byte VK_SHIFT = 0x10;
    protected const byte VK_CAPITAL = 0x14;
    protected const byte VK_NUMLOCK = 0x90;

    protected const byte VK_LSHIFT = 0xA0;
    protected const byte VK_RSHIFT = 0xA1;
    protected const byte VK_LCONTROL = 0xA2;
    protected const byte VK_RCONTROL = 0x3;
    protected const byte VK_LALT = 0xA4;
    protected const byte VK_RALT = 0xA5;
    protected const byte VK_LWIN = 0x5B;
    protected const byte VK_RWIN = 0x5C;

    protected const byte LLKHF_ALTDOWN = 0x20;

    #endregion

    protected int HookType;
    protected int HandleToHook;
    protected HookProc HookCallback;

    public bool IsStarted { get; private set; }

    public BaseHook()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.Exit += (s, e) => Stop();
        });
    }

    public void Start()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsStarted && HookType != 0)
            {
                HookCallback = new HookProc(HookCallbackProcedure);

                HandleToHook = SetWindowsHookEx(
                    HookType,
                    HookCallback,
                    Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
                    0
                );

                if (HandleToHook != 0)
                    IsStarted = true;
            }
        });
    }

    public void Stop()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (IsStarted)
            {
                _ = UnhookWindowsHookEx(HandleToHook);
                IsStarted = false;
            }
        });
    }

    protected virtual int HookCallbackProcedure(int nCode, int wParam, IntPtr lParam)
    {
        return 0;
    }
}
