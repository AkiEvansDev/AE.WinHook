using System;
using System.Collections.Generic;
using System.Linq;

using AE.Dal;
using AE.WinHook.Hook;

namespace AE.WinHook;

public static class HotKeyRegister
{
	private static readonly KeyboardHook KeyboardHook = new();
	private static readonly MouseHook MouseHook = new();

	static HotKeyRegister()
	{
		KeyboardHook.KeyDown = OnHookKeyDown;
		KeyboardHook.KeyUp = OnHookKeyUp;
	}

	private static readonly List<Keys> PressKeys = new();

	private static bool OnHookKeyDown(KeyModifiers keyModifiers, Keys key)
	{
		if (!PressKeys.Contains(key))
		{
			foreach (var pressKey in PressKeys.ToList())
			{
				if (!KeyboardHook.IsKeyDown(pressKey))
					PressKeys.Remove(pressKey);
			}

			if (KeyboardHook.IsKeyModifiers(key))
				keyModifiers = KeyboardHook.GetKeyModifiers(key);
			else
				PressKeys.Add(key);

			return Invoke(keyModifiers);
		}

		return false;
	}

	private static bool OnHookKeyUp(KeyModifiers keyModifiers, Keys key)
	{
		if (PressKeys.Contains(key))
			PressKeys.Remove(key);

		return false;
	}

	private class HotKey
	{
		public KeyModifiers Modifiers { get; set; }
		public string Keys { get; set; }
		public Action Action { get; set; }
		public bool Handled { get; set; }
		public bool Saved { get; set; }
		public bool Strong { get; set; }

		public bool Compare(string keys)
		{
			if (Strong)
				return Keys == keys;

			return Keys.Split('+').All(k => keys.Contains(k));
		}

		public override string ToString()
		{
			return
				$"{(Modifiers.HasFlag(KeyModifiers.Control) ? "Ctrl + " : "")}" +
				$"{(Modifiers.HasFlag(KeyModifiers.Shift) ? "Shift + " : "")}" +
				$"{(Modifiers.HasFlag(KeyModifiers.Alt) ? "Alt + " : "")}" +
				$"{(Modifiers.HasFlag(KeyModifiers.Win) ? "Win + " : "")}" +
				$"{Keys.Replace("+", " + ")}"
				.TrimEnd();
		}
	}

	private static readonly List<HotKey> HotKeys = new();

	private static bool Invoke(KeyModifiers keyModifiers)
	{
		var result = false;
		var keysString = string.Join('+', PressKeys);

		foreach (var hotKey in HotKeys.ToList().Where(hk => hk.Modifiers == keyModifiers))
		{
			if (hotKey.Compare(keysString))
			{
				hotKey.Action?.Invoke();
				result = hotKey.Handled || result;
			}
		}

		return result;
	}

	public static bool RegHotKey(KeyModifiers keyModifiers, Keys key, Action action, bool handled = true, bool saved = false, bool strong = true)
	{
		return RegHotKey(keyModifiers, new List<Keys> { key }, action, handled, saved, strong);
	}

	public static bool RegHotKey(KeyModifiers keyModifiers, IEnumerable<Keys> keys, Action action, bool handled = true, bool saved = false, bool strong = true)
	{
		var hotKey = new HotKey
		{
			Modifiers = keyModifiers,
			Keys = string.Join('+', keys),
			Action = action,
			Handled = handled,
			Saved = saved,
			Strong = strong,
		};

		var duplicateHotKey = HotKeys.FirstOrDefault(hk => hk.Modifiers == hotKey.Modifiers && hk.Keys == hotKey.Keys);
		if (duplicateHotKey != null && !duplicateHotKey.Saved)
		{
			duplicateHotKey.Action = action;
			duplicateHotKey.Handled = handled;
			duplicateHotKey.Saved = saved;
			duplicateHotKey.Strong = strong;
		}
		else
		{
			duplicateHotKey = HotKeys.FirstOrDefault(hk => hk.Modifiers == hotKey.Modifiers
				&& hk.Keys != hotKey.Keys
				&& (hk.Keys.StartsWith(hotKey.Keys) || hotKey.Keys.StartsWith(hk.Keys))
			);

			if (duplicateHotKey != null)
				throw new Exception($"Hot key `{hotKey}` coincides with `{duplicateHotKey}`!");

			HotKeys.Add(hotKey);
			KeyboardHook.Start();
		}

		return true;
	}

	public static bool UnregHotKey(KeyModifiers keyModifiers, Keys key)
	{
		return UnregHotKey(keyModifiers, new List<Keys> { key });
	}

	public static bool UnregHotKey(KeyModifiers keyModifiers, IEnumerable<Keys> keys)
	{
		var hotKey = new HotKey
		{
			Modifiers = keyModifiers,
			Keys = string.Join('+', keys),
		};

		var duplicateHotKey = HotKeys.FirstOrDefault(hk => hk.Modifiers == hotKey.Modifiers && hk.Keys == hotKey.Keys);
		if (duplicateHotKey != null)
		{
			HotKeys.Remove(duplicateHotKey);

			if (!HotKeys.Any())
				KeyboardHook.Stop();

			return true;
		}

		return false;
	}

	public static bool UnregAllHotKey()
	{
		HotKeys.RemoveAll(hk => !hk.Saved);

		if (!HotKeys.Any())
			KeyboardHook.Stop();

		return true;
	}

	public static void SetMouseHook(OnHookMouse action)
	{
		MouseHook.MouseEvent = action;
		MouseHook.Start();
	}

	public static void ClearMouseHook()
	{
		MouseHook.Stop();
		MouseHook.MouseEvent = null;
	}
}

