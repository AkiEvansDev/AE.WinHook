using System;
using System.Runtime.InteropServices;

using AE.Dal;

namespace AE.WinHook.Hook;

public delegate bool OnHookMouse(int x, int y, MouseButtonType buttonType, MouseEventType eventType);

public class MouseHook : BaseHook
{
	#region Windows API Code

	[StructLayout(LayoutKind.Sequential)]
	private class MouseLLHookStruct
	{
		public POINT pt;
		public int mouseData;
		public int flags;
		public int time;
		public int dwExtraInfo;
	}

	#endregion

	public OnHookMouse MouseEvent { get; set; }

	public MouseHook()
	{
		HookType = WH_MOUSE_LL;
	}

	protected override int HookCallbackProcedure(int nCode, int wParam, IntPtr lParam)
	{
		if (nCode > -1 && MouseEvent != null)
		{
			var mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

			var mouseButton = wParam switch
			{
				WM_LBUTTONDOWN or WM_LBUTTONUP or WM_LBUTTONDBLCLK => MouseButtonType.Left,
				WM_RBUTTONDOWN or WM_RBUTTONUP or WM_RBUTTONDBLCLK => MouseButtonType.Right,
				WM_MBUTTONDOWN or WM_MBUTTONUP or WM_MBUTTONDBLCLK => MouseButtonType.Middle,
				_ => MouseButtonType.None,
			};

			var eventType = wParam switch
			{
				WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN => MouseEventType.MouseDown,
				WM_LBUTTONUP or WM_RBUTTONUP or WM_MBUTTONUP => MouseEventType.MouseUp,
				WM_MOUSEMOVE => MouseEventType.MouseMove,
				WM_MOUSEWHEEL => MouseEventType.MouseWheel,
				WM_LBUTTONDBLCLK or WM_RBUTTONDBLCLK or WM_MBUTTONDBLCLK => MouseEventType.DoubleClick,
				_ => MouseEventType.None,
			};

			if (eventType != MouseEventType.None && MouseEvent(mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseButton, eventType))
				return 1;
		}

		return base.HookCallbackProcedure(nCode, wParam, lParam);
	}
}
