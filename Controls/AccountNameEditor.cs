namespace Pace;

public sealed class AccountNameEditor : PaintedPanel
{
    readonly string fallback;
    readonly IconButton confirm = new("confirm", "Save display name");
    string alias;
    readonly FocusCue focus;
    public TextBox Input { get; } = new();
    public string Alias => alias;
    public bool IsEditing { get; private set; }
    public event Action? Committed;
    public event Action? EditingChanged;
    public AccountNameEditor(string alias, string fallback)
    {
        this.alias = alias; this.fallback = fallback;
        Height = UiMetrics.NameEditorHeight;
        Focusable = true; Cursor = new Cursor(StandardCursorType.Hand);
        focus = new(this);
        Palette.ConfigureInput(Input);
        Input.Text = DisplayName();
        Input.IsVisible = false;
        confirm.IsVisible = false;
        Children.Add(Input); Children.Add(confirm);
        Input.KeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Enter or Key.Escape)) return;
            FinishEditing(e.Key == Key.Enter); e.Handled = true;
        };
        Input.LostFocus += (_, _) => { if (IsEditing) FinishEditing(true); };
        confirm.Click += (_, _) => FinishEditing(true);
        Avalonia.Automation.AutomationProperties.SetName(this, "Display name");
        Input.PlaceholderText = "Display name";
        PropertyChanged += (_, _) => InvalidateVisual();
    }
    string DisplayName() => string.IsNullOrWhiteSpace(alias) ? fallback : alias;
    public void BeginEditing()
    {
        if (IsEditing) return;
        IsEditing = true;
        Input.Text = DisplayName();
        Input.IsVisible = confirm.IsVisible = true;
        Input.SelectionStart = Input.SelectionEnd = 0;
        Input.Focus();
        InvalidateVisual(); EditingChanged?.Invoke();
    }
    public void FinishEditing(bool save)
    {
        if (!IsEditing) return;
        string value = Input.Text?.Trim() ?? "";
        bool changed = save && alias != value;
        if (save) alias = value;
        IsEditing = false;
        Input.IsVisible = confirm.IsVisible = false;
        Input.Text = DisplayName();
        Input.SelectionStart = Input.SelectionEnd = 0;
        InvalidateVisual();
        if (changed) Committed?.Invoke();
        EditingChanged?.Invoke();
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // A confirm press can first commit through the input's lost-focus handler.
        // Do not reopen editing when that same press bubbles from a child control.
        if (e.Source == this && !IsEditing && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { BeginEditing(); e.Handled = true; }
        base.OnPointerPressed(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEditing && e.Key is Key.Enter or Key.Space) { BeginEditing(); e.Handled = true; }
        base.OnKeyDown(e);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Input.Measure(new Size(Math.Max(1, availableSize.Width - UiMetrics.IconButtonSize - 3 * UiMetrics.CardGap), Height));
        confirm.Measure(new Size(UiMetrics.IconButtonSize, UiMetrics.IconButtonSize));
        return new(availableSize.Width, Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double textHeight = Palette.TextHeight(true, Palette.AccountSize);
        Input.Arrange(new Rect(UiMetrics.CardGap, (finalSize.Height - textHeight) / 2, Math.Max(1, finalSize.Width - UiMetrics.IconButtonSize - 3 * UiMetrics.CardGap), textHeight));
        confirm.Arrange(new Rect(finalSize.Width - UiMetrics.IconButtonSize, (finalSize.Height - UiMetrics.IconButtonSize) / 2, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize));
        return finalSize;
    }
    protected override void DrawSurface(DrawingContext context)
    {
        double width = Math.Max(1, Bounds.Width - UiMetrics.IconButtonSize - UiMetrics.CardGap);
        context.DrawRectangle(null, new Pen(Palette.Brush(Palette.InputOutline(IsEditing, IsPointerOver || IsFocused && focus.Visible)), UiMetrics.BorderWidth), new Rect(.5, .5, width - 1, Bounds.Height - 1));
        if (!IsEditing)
        {
            var text = Palette.Line(DisplayName(), Palette.Text, width - 2 * UiMetrics.CardGap, Palette.AccountSize, true);
            context.DrawText(text, new Point(UiMetrics.CardGap, Math.Round((Bounds.Height - text.Height) / 2)));
        }
    }
}
