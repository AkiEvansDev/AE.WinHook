using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using AE.Dal;

namespace AE.WinHook;

public struct MonitorInfo
{
	public Rectangle Monitor { get; set; }
	public Rectangle Work { get; set; }

	internal MonitorInfo(MONITORINFO info)
	{
		Monitor = info.rcMonitor.Rectangle;
		Work = info.rcWork.Rectangle;
	}
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
	public int x;
	public int y;

	public POINT(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;

	public readonly Size Size => new(right - left, bottom - top);
	public readonly Rectangle Rectangle => new(left, top, right - left, bottom - top);

	public RECT(int left, int top, int right, int bottom)
	{
		this.left = left;
		this.top = top;
		this.right = right;
		this.bottom = bottom;
	}

	public RECT(Rectangle r)
	{
		left = r.Left;
		top = r.Top;
		right = r.Right;
		bottom = r.Bottom;
	}

	public static RECT FromXYWH(int x, int y, int width, int height) => new(x, y, x + width, y + height);
}

[StructLayout(LayoutKind.Sequential)]
internal struct SIZE
{
	public int cx;
	public int cy;

	public SIZE(int cx, int cy)
	{
		this.cx = cx;
		this.cy = cy;
	}
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
internal class MONITORINFO
{
	internal int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
	internal RECT rcMonitor = new();
	internal RECT rcWork = new();
	internal int dwFlags = 0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct BLENDFUNCTION
{
	public byte BlendOp;
	public byte BlendFlags;
	public byte SourceConstantAlpha;
	public byte AlphaFormat;
}

public static class WinHelper
{
	#region Windows API Code

	private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
	private const uint KEYEVENTF_KEYUP = 0x0002;

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_SHOWWINDOW = 0x0040;

	private const int SW_SHOW = 9;
	private const int GWL_EXSTYLE = -20;
	private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

	private const int WS_EX_LAYERED = 0x80000;
	private const int WS_EX_TRANSPARENT = 0x20;

	private const int LWA_ALPHA = 0x2;
	private const int ULW_ALPHA = 0x02;

	private const byte AC_SRC_OVER = 0x00;
	private const byte AC_SRC_ALPHA = 0x01;

	private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
	private const int MOUSEEVENTF_LEFTUP = 0x0004;
	private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
	private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
	private const int MOUSEEVENTF_MOVE = 0x0001;
	private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
	private const int MOUSEEVENTF_RIGHTUP = 0x0010;
	private const int MOUSEEVENTF_WHEEL = 0x0800;

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

	[DllImport("user32.dll", EntryPoint = "SetCursorPos")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetCursorPos(int X, int Y);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpMousePoint);

	[DllImport("user32.dll")]
	private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

	[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
	private static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindow(IntPtr className, string windowName);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(HandleRef hmonitor, MONITORINFO info);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll")]
	private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);

	[DllImport("user32.dll")]
	private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
		ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc,
		int crKey, ref BLENDFUNCTION pblend, int dwFlags);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteObject(IntPtr hObject);

	#endregion

	public static void MouseDown(MouseButtonType buttonType)
	{
		var position = GetCursorPosition();
		MouseDown(position.X, position.Y, buttonType);
	}

	public static void MouseDown(int x, int y, MouseButtonType buttonType)
	{
		mouse_event(buttonType switch
		{
			MouseButtonType.Left => MOUSEEVENTF_LEFTDOWN,
			MouseButtonType.Middle => MOUSEEVENTF_MIDDLEDOWN,
			MouseButtonType.Right => MOUSEEVENTF_RIGHTDOWN,
			_ => throw new NotImplementedException(),
		},
			x, y, 0, 0
		);
	}

	public static void MouseUp(MouseButtonType buttonType)
	{
		var position = GetCursorPosition();
		MouseUp(position.X, position.Y, buttonType);
	}

	public static void MouseUp(int x, int y, MouseButtonType buttonType)
	{
		mouse_event(buttonType switch
		{
			MouseButtonType.Left => MOUSEEVENTF_LEFTUP,
			MouseButtonType.Middle => MOUSEEVENTF_MIDDLEUP,
			MouseButtonType.Right => MOUSEEVENTF_RIGHTUP,
			_ => throw new NotImplementedException(),
		},
			x, y, 0, 0
		);
	}

	public static void MouseMove(int x, int y)
	{
		mouse_event(MOUSEEVENTF_MOVE, x, y, 0, 0);
	}

	public static void MouseWheel(int value)
	{
		var position = GetCursorPosition();
		MouseWheel(position.X, position.Y, value);
	}

	public static void MouseWheel(int x, int y, int value)
	{
		mouse_event(MOUSEEVENTF_WHEEL, x, y, value, 0);
	}

	public static void KeyEvent(Keys key, bool extended = false, bool up = false)
	{
		uint dwFlag = 0;

		if (extended)
			dwFlag = KEYEVENTF_EXTENDEDKEY;
		else if (up)
			dwFlag = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;

		if (key != Keys.None)
			keybd_event((uint)key, 0, dwFlag, 0);
	}

	public static Point GetCursorPosition()
	{
		var gotPoint = GetCursorPos(out POINT currentMousePoint);

		if (!gotPoint)
			currentMousePoint = new POINT(0, 0);

		return new Point(currentMousePoint.x, currentMousePoint.y);
	}

	public static void SetCursorPosition(int x, int y)
	{
		SetCursorPos(x, y);
	}

	public static Bitmap GetScreen(int x, int y, int width, int height)
	{
		var screen = new Bitmap(width, height);

		try
		{
			using var graphics = Graphics.FromImage(screen);
			graphics.CopyFromScreen(new Point(x, y), Point.Empty, screen.Size, CopyPixelOperation.SourceCopy);
		}
		catch { }

		return screen;
	}

	public static IntPtr GetWindow(string name)
	{
		return FindWindow(IntPtr.Zero, name);
	}

	public static IntPtr GetCurrentWindow()
	{
		return Process.GetCurrentProcess().MainWindowHandle;
	}

	public static Rectangle GetWindowSize(IntPtr window)
	{
		var rect = new RECT();
		GetWindowRect(window, ref rect);

		return rect.Rectangle;
	}

	public static MonitorInfo GetMonitorInfo(IntPtr window)
	{
		var monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);

		var info = new MONITORINFO();
		GetMonitorInfo(new HandleRef(null, monitor), info);

		return new MonitorInfo(info);
	}

	public static void SetWindowPos(IntPtr window, int x, int y)
	{
		SetForegroundWindow(window);
		SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	}

	public static void SetWindowOptions(IntPtr window, int x, int y, int width, int height, byte opacity, bool topmost, bool noClickable)
	{
		ShowWindow(window, SW_SHOW);
		SetForegroundWindow(window);

		SetWindowPos(window, topmost ? new IntPtr(-1) : new IntPtr(-2), x, y, width, height, SWP_SHOWWINDOW);
		MoveWindow(window, x, y, width, height, true);

		var style = GetWindowLong(window, GWL_EXSTYLE);

		if (opacity < 255 || noClickable)
			style |= WS_EX_LAYERED;
		else
			style &= ~WS_EX_LAYERED;

		if (noClickable)
			style |= WS_EX_TRANSPARENT;
		else
			style &= ~WS_EX_TRANSPARENT;

		_ = SetWindowLong(window, GWL_EXSTYLE, style);
		SetLayeredWindowAttributes(window, 0, opacity, LWA_ALPHA);

		ShowWindow(window, SW_SHOW);
		SetForegroundWindow(window);
	}

	public static void SetClickThrough(IntPtr window)
	{
		var style = GetWindowLong(window, GWL_EXSTYLE);
		_ = SetWindowLong(window, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
	}

	public static void SelectBitmap(IntPtr window, Bitmap bitmap, int left, int top, byte opacity = 255)
	{
		if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
			throw new ApplicationException("The bitmap must be 32bpp with alpha-channel.");

		var screenDc = GetDC(IntPtr.Zero);
		var memDc = CreateCompatibleDC(screenDc);
		var hBitmap = IntPtr.Zero;
		var hOldBitmap = IntPtr.Zero;

		try
		{
			hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
			hOldBitmap = SelectObject(memDc, hBitmap);

			var newSize = new SIZE(bitmap.Width, bitmap.Height);
			var sourceLocation = new POINT(0, 0);
			var newLocation = new POINT(left, top);
			var blend = new BLENDFUNCTION
			{
				BlendOp = AC_SRC_OVER,
				BlendFlags = 0,
				SourceConstantAlpha = opacity,
				AlphaFormat = AC_SRC_ALPHA
			};

			var q = UpdateLayeredWindow(window, screenDc, ref newLocation, ref newSize, memDc, ref sourceLocation, 0, ref blend, ULW_ALPHA);
		}
		finally
		{
			_ = ReleaseDC(IntPtr.Zero, screenDc);

			if (hBitmap != IntPtr.Zero)
			{
				_ = SelectObject(memDc, hOldBitmap);
				DeleteObject(hBitmap);
			}

			DeleteDC(memDc);
		}
	}
}
