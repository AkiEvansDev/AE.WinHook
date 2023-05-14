using AE.WinHook.Hook;

namespace AE.WinHook;

public static class MouseEventRegister
{
    private static readonly MouseHook MouseHook = new();

    static MouseEventRegister()
    {
        MouseHook.MouseEvent = OnMouseEvent;
    }

    private static bool OnMouseEvent(int x, int y, MouseButtonType buttonType, MouseEventType eventType)
    {
        return Invoke(x, y, buttonType, eventType);
    }

    private class MouseEvent
    {
        public MouseButtonType ButtonType { get; set; }
        public MouseEventType EventType { get; set; }
        public Action<int, int> Action { get; set; }
        public bool Handled { get; set; }
    }

    private static readonly List<MouseEvent> MouseEvents = new();

    private static bool Invoke(int x, int y, MouseButtonType buttonType, MouseEventType eventType)
    {
        var mouseEvent = MouseEvents.FirstOrDefault(me => me.ButtonType == buttonType && me.EventType == eventType);
        
        if (mouseEvent != null)
        {
            mouseEvent.Action?.Invoke(x, y);
            return mouseEvent.Handled;
        }

        return false;
    }

    public static bool RegMouseEvent(MouseButtonType buttonType, MouseEventType eventType, Action<int, int> action, bool handled = false)
    {
        var mouseEvent = new MouseEvent
        {
            ButtonType = buttonType,
            EventType = eventType,
            Action = action,
            Handled = handled,
        };

        var duplicateMouseEvent = MouseEvents.FirstOrDefault(me => me.ButtonType == buttonType && me.EventType == eventType);
        if (duplicateMouseEvent != null)
        {
            duplicateMouseEvent.Action = action;
            duplicateMouseEvent.Handled = handled;
        }
        else
        {
            MouseEvents.Add(mouseEvent);

            if (!MouseHook.IsStarted)
                MouseHook.Start();
        }

        return true;
    }

    public static bool UnregMouseEvent(MouseButtonType buttonType, MouseEventType eventType)
    {
        var mouseEvent = new MouseEvent
        {
            ButtonType = buttonType,
            EventType = eventType,
        };

        var duplicateMouseEvent = MouseEvents.FirstOrDefault(me => me.ButtonType == buttonType && me.EventType == eventType);
        if (duplicateMouseEvent != null)
        {
            MouseEvents.Remove(duplicateMouseEvent);

            if (!MouseEvents.Any())
                MouseHook.Stop();

            return true;
        }

        return false;
    }

    public static bool UnregAllHotKey()
    {
        MouseEvents.Clear();
        MouseHook.Stop();

        return true;
    }
}
