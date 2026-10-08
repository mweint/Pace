namespace Pace;

public sealed class AccountEditorRow : PaintedPanel, IThemedSection
{
    public Reading Reading { get; private set; }
    public Preference Preference { get; }
    public AccountNameEditor NameEditor { get; }
    public AccountToggle PanelToggle { get; }
    public AccountToggle TrayToggle { get; }
    public AccountToggle FiveHourToggle { get; }
    public AccountToggle FableToggle { get; }
    public IconButton Grip { get; } = new("drag", "Drag to reorder account");
    internal IconButton RemoveButton { get; } = new("delete", "Remove account");
    readonly TextBlock account, reason;
    readonly FilledButton reconnect = Palette.Button("Reconnect");
    readonly ServiceIcon service;
    bool separator;
    bool dragging;
    bool pointerInteraction;
    public bool Dragging { get => dragging; set { dragging = value; InvalidateVisual(); } }
    public bool ShowSeparator { get => separator; set { separator = value; InvalidateVisual(); } }
    public event Action? EditedChanged;
    public event Action? RemoveRequested;
    public AccountEditorRow(Reading reading, Preference preference, Action renew)
    {
        Reading = reading; Preference = preference;
        NameEditor = new(preference.Alias, reading.Account.Label);
        account = Palette.Label(reading.Account.Label);
        reason = Palette.Label(reading.ConnectionReason, color: Palette.Warning);
        service = new(reading.Account.Service);
        PanelToggle = new("Panel", preference.Show); TrayToggle = new("Tray", preference.Tray);
        FiveHourToggle = new("5H bar", preference.ShowFiveHour); FableToggle = new("Fable bar", preference.ShowFable);
        FiveHourToggle.IsVisible = FableToggle.IsVisible = reading.Account.Service == Services.Claude;
        Children.AddRange([Grip, service, account, NameEditor, PanelToggle, TrayToggle, FiveHourToggle, FableToggle, reconnect, reason, RemoveButton]);
        Grip.Focusable = false;
        Grip.Cursor = new Cursor(StandardCursorType.SizeAll);
        reconnect.Click += (_, _) => renew();
        RemoveButton.Click += (_, _) => RemoveRequested?.Invoke();
        NameEditor.Committed += () => EditedChanged?.Invoke();
        NameEditor.EditingChanged += UpdateRemoveVisibility;
        foreach (var toggle in new[] { PanelToggle, TrayToggle, FiveHourToggle, FableToggle })
            toggle.IsCheckedChanged += (_, _) => EditedChanged?.Invoke();
        PointerEntered += (_, _) => UpdateRemoveVisibility();
        PointerExited += (_, _) => UpdateRemoveVisibility();
        GotFocus += (_, _) => UpdateRemoveVisibility();
        LostFocus += (_, _) => Dispatcher.UIThread.Post(UpdateRemoveVisibility);
        AddHandler(PointerPressedEvent, (_, _) => { pointerInteraction = true; UpdateRemoveVisibility(); }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, (_, _) => { pointerInteraction = false; UpdateRemoveVisibility(); }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        RemoveButton.Opacity = 0;
        RemoveButton.IsHitTestVisible = RemoveButton.Focusable = false;
        UpdateConnection(reading);
    }
    internal bool ShouldRevealRemoval(bool hovered) => !NameEditor.IsEditing && (hovered || !pointerInteraction && IsKeyboardFocusWithin);
    void UpdateRemoveVisibility() => RemoveButton.FadeVisible(ShouldRevealRemoval(IsPointerOver));
    public void UpdateConnection(Reading reading)
    {
        Reading = reading;
        reconnect.IsVisible = reason.IsVisible = reading.NeedsReconnect;
        reason.Text = reading.ConnectionReason;
        InvalidateMeasure();
    }
    internal double PreferredHeight => UiMetrics.ContentInset + Math.Max(Palette.TextHeight(), UiMetrics.ServiceIconSize) +
        UiMetrics.CardGap + UiMetrics.NameEditorHeight + UiMetrics.CardGap + UiMetrics.TextButtonHeight +
        (Reading.NeedsReconnect && Reading.Account.Service == Services.Claude ? UiMetrics.InlineGap + UiMetrics.TextButtonHeight : 0) + UiMetrics.ContentInset;
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(availableSize.Width, PreferredHeight));
        return new(availableSize.Width, PreferredHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double inset = UiMetrics.ContentInset, header = Math.Max(Palette.TextHeight(), UiMetrics.ServiceIconSize);
        double left = UiMetrics.InlineGap + UiMetrics.IconButtonSize + UiMetrics.InlineGap;
        double nameY = inset + header + UiMetrics.CardGap;
        double actionsY = nameY + UiMetrics.NameEditorHeight + UiMetrics.CardGap;
        service.Arrange(new Rect(left, inset + (header - UiMetrics.ServiceIconSize) / 2, UiMetrics.ServiceIconSize, UiMetrics.ServiceIconSize));
        RemoveButton.Arrange(new Rect(finalSize.Width - inset - UiMetrics.IconButtonSize, inset + (header - UiMetrics.IconButtonSize) / 2, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize));
        double reasonWidth = Math.Ceiling(reason.DesiredSize.Width);
        double reasonLeft = finalSize.Width - inset - UiMetrics.IconButtonSize - UiMetrics.CardGap - reasonWidth;
        reason.Arrange(new Rect(reasonLeft, inset, reasonWidth, header));
        account.Arrange(new Rect(left + UiMetrics.ServiceIconSize + UiMetrics.CardGap, inset,
            Math.Max(1, (Reading.NeedsReconnect ? reasonLeft : finalSize.Width - inset - UiMetrics.IconButtonSize) - left - UiMetrics.ServiceIconSize - 2 * UiMetrics.CardGap), header));
        NameEditor.Arrange(new Rect(left, nameY, finalSize.Width - left - inset, UiMetrics.NameEditorHeight));
        Grip.Arrange(new Rect(UiMetrics.InlineGap, nameY + (UiMetrics.NameEditorHeight - UiMetrics.IconButtonSize) / 2d, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize));
        double x = left;
        foreach (var toggle in new[] { PanelToggle, TrayToggle, FiveHourToggle, FableToggle }.Where(t => t.IsVisible))
        {
            toggle.Arrange(new Rect(x, actionsY + Math.Truncate((UiMetrics.TextButtonHeight - UiMetrics.ToggleHeight) / 2d), toggle.Width, UiMetrics.ToggleHeight));
            x += toggle.Width + UiMetrics.InlineGap;
        }
        reconnect.Arrange(new Rect(finalSize.Width - inset - reconnect.MinWidth, Reading.Account.Service == Services.Claude ? actionsY + UiMetrics.TextButtonHeight + UiMetrics.InlineGap : actionsY, reconnect.MinWidth, UiMetrics.TextButtonHeight));
        return finalSize;
    }
    public Preference Edited() => new()
    {
        Key = Preference.Key, Alias = NameEditor.Alias, RememberedAccount = Preference.RememberedAccount,
        Show = PanelToggle.Checked, Tray = TrayToggle.Checked,
        ShowFiveHour = FiveHourToggle.Checked, ShowFable = FableToggle.Checked
    };
    protected override void DrawSurface(DrawingContext context)
    {
        SectionStyle.DrawSeparator(context, Bounds.Width, ShowSeparator);
        if (Dragging) context.DrawRectangle(null, new Pen(Palette.Brush(Palette.SelectionBorder), UiMetrics.EmphasisBorderWidth), new Rect(1, 1, Bounds.Width - 2, Bounds.Height - 2));
    }
}
