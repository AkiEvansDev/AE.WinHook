using System.Windows.Input;

using AE.WinHook.Hook;

namespace AE.WinHook;

public static class HotKeyRegister
{
    private static readonly KeyboardHook KeyboardHook = new();

    static HotKeyRegister()
    {
        KeyboardHook.KeyDown = OnHookKeyDown;
        KeyboardHook.KeyUp = OnHookKeyUp;
    }

    private static readonly List<Key> PressKeys = new();

    private static bool OnHookKeyDown(KeyModifiers keyModifiers, Key key)
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

    private static bool OnHookKeyUp(KeyModifiers keyModifiers, Key key)
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

        foreach (var hotKey in HotKeys.Where(hk => hk.Modifiers == keyModifiers))
            if (hotKey.Keys == keysString)
            {
                hotKey.Action?.Invoke();
                result = hotKey.Handled || result;
            }

        return result;
    }

    public static bool RegHotKey(KeyModifiers keyModifiers, Key key, Action action, bool handled = true, bool saved = false)
    {
        return RegHotKey(keyModifiers, new List<Key> { key }, action, handled, saved);
    }

    public static bool RegHotKey(KeyModifiers keyModifiers, IEnumerable<Key> keys, Action action, bool handled = true, bool saved = false)
    {
        var hotKey = new HotKey
        {
            Modifiers = keyModifiers,
            Keys = string.Join('+', keys),
            Action = action,
            Handled = handled,
            Saved = saved,
        };

        var duplicateHotKey = HotKeys.FirstOrDefault(hk => hk.Modifiers == hotKey.Modifiers && hk.Keys == hotKey.Keys);
        if (duplicateHotKey != null && !duplicateHotKey.Saved)
        {
            duplicateHotKey.Action = action;
            duplicateHotKey.Handled = handled;
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

            if (!KeyboardHook.IsStarted)
                KeyboardHook.Start();
        }

        return true;
    }

    public static bool UnregHotKey(KeyModifiers keyModifiers, Key key)
    {
        return UnregHotKey(keyModifiers, new List<Key> { key });
    }

    public static bool UnregHotKey(KeyModifiers keyModifiers, IEnumerable<Key> keys)
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
}

