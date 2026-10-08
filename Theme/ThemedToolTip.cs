namespace Pace;

internal static class ThemedToolTip
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, Lifetime> lifetimes = new();
    sealed class Lifetime
    {
        readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(UiMetrics.DetailDurationMilliseconds) };
        public Lifetime(Control control)
        {
            timer.Tick += (_, _) => { timer.Stop(); ToolTip.SetIsOpen(control, false); };
            control.PropertyChanged += (_, e) =>
            {
                if (e.Property != ToolTip.IsOpenProperty) return;
                timer.Stop();
                if (ToolTip.GetIsOpen(control)) timer.Start();
            };
            control.DetachedFromVisualTree += (_, _) => timer.Stop();
        }
    }
    public static void Set(Control control, string text, int delay = UiMetrics.DetailDelayMilliseconds)
    {
        lifetimes.GetValue(control, c => new Lifetime(c));
        ToolTip.SetShowDelay(control, delay);
        // Native ReshowDelay was also 1000ms; always retain the initial delay.
        ToolTip.SetBetweenShowDelay(control, -1);
        ToolTip.SetTip(control, string.IsNullOrWhiteSpace(text) ? null : new Border
        {
            Background = Palette.Brush(Palette.Background), BorderBrush = Palette.Brush(Palette.WindowBorder),
            BorderThickness = new Thickness(UiMetrics.BorderWidth), Padding = new Thickness(UiMetrics.TooltipInset - UiMetrics.BorderWidth), MaxWidth = UiMetrics.TooltipMaxWidth,
            Child = new TextBlock { Text = text, FontFamily = Palette.FontFamily, FontSize = Palette.BodySize,
                Foreground = Palette.Brush(Palette.Text), TextWrapping = TextWrapping.Wrap }
        });
    }
}
