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
        control.AddHandler(InputElement.KeyDownEvent, (_, _) => { Visible = true; control.InvalidateVisual(); }, RoutingStrategies.Tunnel);
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
