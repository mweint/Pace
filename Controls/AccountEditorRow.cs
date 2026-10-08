namespace Usage;

public sealed class AccountEditorRow : Panel, IThemedSection
{
    public bool Dragging
    {
        get; set;
    }
    public Preference Preference
    {
        get;
    }
    public TextBox NameInput
    {
        get;
    }
    public AccountNameEditor NameEditor { get; }
    public AccountToggle PanelToggle
    {
        get;
    }
    public AccountToggle TrayToggle
    {
        get;
    }
    public IconButton Grip
    {
        get;
    }
    internal IconButton RemoveButton
    {
        get;
    }
    readonly Button reconnect = Palette.Button("Reconnect", compact: true);
    readonly AccountToggle fiveHourToggle;
    readonly AccountToggle fableToggle;
    readonly bool hasSupplementalLimits;
    readonly Label connectionReason;
    readonly Label account;
    bool pointerInteraction;
    bool arranging, needsReconnect;
    bool showSeparator;
    public bool ShowSeparator { get => showSeparator; set { if (showSeparator != value) { showSeparator = value; Invalidate(); } } }
    readonly Panel service;

    public event Action? EditedChanged;
    public event Action? RemoveRequested;
    public AccountEditorRow(Reading reading, Preference preference, Action renew)
    {
        DoubleBuffered = true;
        Preference = preference;
        hasSupplementalLimits = reading.Account.Service == "Claude";
        SectionStyle.Apply(this);
        Width = UiMetrics.ContentWidth;
        Font = Palette.BodyFont();
        Grip = new IconButton("drag", "Drag to reorder account")
        {
            BackColor = Palette.SectionBackground,
            Cursor = Cursors.SizeAll,
            TabStop = false
        };
        service = new Panel();
        service.Paint += (_, e) => ServiceMark.Draw(e.Graphics, reading.Account.Service, 0, 0);
        account = new Label
        {
            Text = reading.Account.Label,
            AutoEllipsis = true,
            ForeColor = Palette.Muted
        };
        RemoveButton = new IconButton("delete", "Remove account")
        {
            BackColor = Palette.SectionBackground,
            Visible = false
        };
        RemoveButton.Click += (_, _) => RemoveRequested?.Invoke();
        NameEditor = new AccountNameEditor(preference.Alias, reading.Account.Label);
        NameInput = NameEditor.Input;
        NameEditor.Committed += () => EditedChanged?.Invoke();
        NameEditor.EditingChanged += UpdateRemoveVisibility;
        Paint += (_, e) =>
        {
            var state = e.Graphics.Save();
            float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
            e.Graphics.ScaleTransform(scale, scale);
            SectionStyle.DrawSeparator(e.Graphics, Width / scale, ShowSeparator);
            e.Graphics.Restore(state);
            if (Dragging)
            {
                using var edge = new Pen(Palette.SelectionBorder, UiMetrics.EmphasisBorderWidth);
                e.Graphics.DrawRectangle(edge, UiMetrics.BorderWidth, UiMetrics.BorderWidth,
                    Width - UiMetrics.EmphasisBorderWidth - UiMetrics.BorderWidth,
                    Height - UiMetrics.EmphasisBorderWidth - UiMetrics.BorderWidth);
            }
        };
        PanelToggle = new AccountToggle
        {
            Text = "Panel",
            Checked = preference.Show,
            ForeColor = Palette.Text
        };
        TrayToggle = new AccountToggle
        {
            Text = "Tray",
            Checked = preference.Tray,
            ForeColor = Palette.Text
        };
        PanelToggle.CheckedChanged += (_, _) =>
        {
            PanelToggle.Invalidate();
            EditedChanged?.Invoke();
        };
        TrayToggle.CheckedChanged += (_, _) =>
        {
            TrayToggle.Invalidate();
            EditedChanged?.Invoke();
        };
        fiveHourToggle = new AccountToggle { Text = "5H bar", Checked = preference.ShowFiveHour, Visible = hasSupplementalLimits, ForeColor = Palette.Text };
        fableToggle = new AccountToggle { Text = "Fable bar", Checked = preference.ShowFable, Visible = hasSupplementalLimits, ForeColor = Palette.Text };
        fiveHourToggle.CheckedChanged += (_, _) => EditedChanged?.Invoke();
        fableToggle.CheckedChanged += (_, _) => EditedChanged?.Invoke();
        connectionReason = new Label
        {
            Font = Palette.BodyFont(), ForeColor = Palette.Warning,
            AutoEllipsis = true, TextAlign = ContentAlignment.TopRight
        };
        reconnect.Click += (_, _) => renew();
        Controls.AddRange([Grip, service, account, NameEditor, PanelToggle, TrayToggle, fiveHourToggle, fableToggle, reconnect, connectionReason, RemoveButton]);
        SizeChanged += (_, _) => ArrangeContent();
        DpiChangedAfterParent += (_, _) => ArrangeContent();
        UpdateConnection(reading);
        foreach (Control control in Controls.Cast<Control>().Append(NameInput).Append(this))
        {
            control.MouseDown += (_, _) => { pointerInteraction = true; UpdateRemoveVisibility(); };
            control.PreviewKeyDown += (_, _) => { pointerInteraction = false; UpdateRemoveVisibility(); };
            control.MouseEnter += (_, _) => UpdateRemoveVisibility();
            control.MouseLeave += (_, _) =>
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke((Action)UpdateRemoveVisibility);
            };
            control.Enter += (_, _) => UpdateRemoveVisibility();
            control.Leave += (_, _) =>
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke((Action)UpdateRemoveVisibility);
            };
        }
    }

    public void UpdateConnection(Reading reading)
    {
        needsReconnect = reading.NeedsReconnect;
        reconnect.Visible = connectionReason.Visible = reading.NeedsReconnect;
        connectionReason.Text = reading.ConnectionReason;
        ArrangeContent();
    }

    void ArrangeContent()
    {
        if (arranging)
            return;
        arranging = true;
        try
        {
            int inset = LogicalToDeviceUnits(UiMetrics.ContentInset);
            int gap = LogicalToDeviceUnits(UiMetrics.CardGap);
            int inline = LogicalToDeviceUnits(UiMetrics.InlineGap);
            int icon = LogicalToDeviceUnits(UiMetrics.IconButtonSize);
            int body = (int)Math.Ceiling(account.Font.GetHeight(DeviceDpi));
            int header = Math.Max(body, LogicalToDeviceUnits(UiMetrics.ServiceIconSize));
            Grip.SetBounds(inline, 0, icon, icon);
            int fieldLeft = Grip.Right + inline;
            service.SetBounds(fieldLeft, inset + (header - LogicalToDeviceUnits(UiMetrics.ServiceIconSize)) / 2,
                LogicalToDeviceUnits(UiMetrics.ServiceIconSize), LogicalToDeviceUnits(UiMetrics.ServiceIconSize));
            RemoveButton.SetBounds(Width - inset - icon, inset + (header - icon) / 2, icon, icon);
            connectionReason.SetBounds(RemoveButton.Left - gap - LogicalToDeviceUnits(110), inset,
                LogicalToDeviceUnits(110), header);
            account.SetBounds(service.Right + gap, inset + (header - body) / 2,
                Math.Max(1, (needsReconnect ? connectionReason.Left : RemoveButton.Left) - service.Right - 2 * gap), body);
            NameEditor.SetBounds(fieldLeft, inset + header + gap, Width - inset - fieldLeft, LogicalToDeviceUnits(UiMetrics.NameEditorHeight));
            Grip.Top = NameEditor.Top + (NameEditor.Height - Grip.Height) / 2;
            int actionsTop = NameEditor.Bottom + gap;
            int actionsHeight = LogicalToDeviceUnits(UiMetrics.TextButtonHeight);
            int toggleHeight = LogicalToDeviceUnits(UiMetrics.ToggleHeight);
            PanelToggle.SetBounds(fieldLeft, actionsTop + (actionsHeight - toggleHeight) / 2, PanelToggle.GetPreferredSize(Size.Empty).Width, toggleHeight);
            TrayToggle.SetBounds(PanelToggle.Right + inline, PanelToggle.Top, TrayToggle.GetPreferredSize(Size.Empty).Width, toggleHeight);
            int bottom = actionsTop + actionsHeight;
            if (hasSupplementalLimits)
            {
                fiveHourToggle.SetBounds(TrayToggle.Right + inline, PanelToggle.Top, fiveHourToggle.GetPreferredSize(Size.Empty).Width, toggleHeight);
                fableToggle.SetBounds(fiveHourToggle.Right + inline, PanelToggle.Top, fableToggle.GetPreferredSize(Size.Empty).Width, toggleHeight);
            }
            reconnect.SetBounds(Width - inset - LogicalToDeviceUnits(104), hasSupplementalLimits ? bottom + inline : actionsTop, LogicalToDeviceUnits(104), actionsHeight);
            if (needsReconnect && hasSupplementalLimits)
                bottom = reconnect.Bottom;
            Height = bottom + inset;
        }
        finally { arranging = false; }
    }

    void UpdateRemoveVisibility()
    {
        if (IsDisposed)
            return;
        RemoveButton.FadeVisible(ShouldRevealRemoval(ClientRectangle.Contains(PointToClient(Cursor.Position))));
    }

    internal bool ShouldRevealRemoval(bool hovered) => !NameEditor.IsEditing &&
        (hovered || (!pointerInteraction && ContainsFocus));

    public Preference Edited() => new()
    {
        Key = Preference.Key,
        Alias = NameEditor.Alias,
        RememberedAccount = Preference.RememberedAccount,
        Show = PanelToggle.Checked,
        Tray = TrayToggle.Checked,
        ShowFiveHour = fiveHourToggle.Checked,
        ShowFable = fableToggle.Checked
    };
}
