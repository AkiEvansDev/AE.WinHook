using System;
using System.Runtime.InteropServices;

using AE.Dal;

namespace AE.WinHook.Hook;

public delegate bool OnHookKey(KeyModifiers keyModifiers, Keys key);

public class KeyboardHook : BaseHook
{
	#region Windows API Code

	[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
	private static extern short GetKeyState(int vKey);

	[StructLayout(LayoutKind.Sequential)]
	private class KeyboardHookStruct
	{
		public int vkCode;
		public int scanCode;
		public int flags;
		public int time;
		public int dwExtraInfo;
	}

	#endregion

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
			var key = (Keys)keyboardHookStruct.vkCode;

			// Is Control
			if (((GetKeyState(VK_LCONTROL) & 0x80) != 0) || ((GetKeyState(VK_RCONTROL) & 0x80) != 0))
				keyModifiers |= KeyModifiers.Control;

			// Is Alt
			if (((GetKeyState(VK_LALT) & 0x80) != 0) || ((GetKeyState(VK_RALT) & 0x80) != 0))
				keyModifiers |= KeyModifiers.Alt;

			// Is Shift
			if (((GetKeyState(VK_LSHIFT) & 0x80) != 0) || ((GetKeyState(VK_RSHIFT) & 0x80) != 0))
				keyModifiers |= KeyModifiers.Shift;

			// Is Win
			if (((GetKeyState(VK_LWIN) & 0x80) != 0) || ((GetKeyState(VK_LWIN - VK_RWIN) & 0x80) != 0))
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

		return base.HookCallbackProcedure(nCode, wParam, lParam);
	}

	public static bool IsKeyModifiers(Keys key)
	{
		return key switch
		{
			Keys.LeftControl or Keys.RightControl or Keys.LeftAlt or Keys.RightAlt or Keys.LeftShift or Keys.RightShift or Keys.LeftWin or Keys.RightWin => true,
			_ => false,
		};
	}

	public static KeyModifiers GetKeyModifiers(Keys key)
	{
		return key switch
		{
			Keys.LeftControl or Keys.RightControl => KeyModifiers.Control,
			Keys.LeftAlt or Keys.RightAlt => KeyModifiers.Alt,
			Keys.LeftShift or Keys.RightShift => KeyModifiers.Shift,
			Keys.LeftWin or Keys.RightWin => KeyModifiers.Win,
			_ => KeyModifiers.None,
		};
	}

	public static bool IsKeyDown(Keys key)
	{
		return (GetKeyState((int)key) & 0x80) != 0;
	}
}
