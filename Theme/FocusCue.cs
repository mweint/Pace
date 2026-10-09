using Avalonia.Interactivity;

namespace Pace;

// Mouse focus is quiet; keyboard focus shows Pace's outline.
// Framework adorners are disabled so only the theme paints it.
internal sealed class FocusCue
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, FocusCue> cues = new();
    public bool Visible { get; private set; }
    public FocusCue(Control control)
    {
        cues.Add(control, this);
        control.FocusAdorner = null;
        control.GotFocus += (_, e) =>
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional) Visible = true;
            else if (e.NavigationMethod == NavigationMethod.Pointer) Visible = false;
            control.InvalidateVisual();
        };
        control.LostFocus += (_, _) => control.InvalidateVisual();
        control.AddHandler(InputElement.PointerPressedEvent, (_, _) => { Visible = false; control.InvalidateVisual(); }, RoutingStrategies.Tunnel);
        // Only navigation keys reveal the cue: Escape, Alt+Tab, the Windows key and other
        // keys that close or leave the window must not outline a button as it fades out.
        control.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key is not (Key.Tab or Key.Left or Key.Right or Key.Up or Key.Down)) return;
            Visible = true; control.InvalidateVisual();
        }, RoutingStrategies.Tunnel);
    }
    public static void HideForPointer(Control root)
    {
        foreach (var control in root.GetVisualDescendants().OfType<Control>().Prepend(root))
            if (cues.TryGetValue(control, out var cue) && cue.Visible)
            {
                cue.Visible = false;
                control.InvalidateVisual();
            }
    }
}
