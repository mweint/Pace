namespace Usage;

public sealed class AccountEditorRow : Panel
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

    public event Action? EditedChanged;
    public event Action? RemoveRequested;
    public AccountEditorRow(Reading reading, Preference preference, Action renew)
    {
        DoubleBuffered = true;
        Preference = preference;
        BackColor = Palette.Card;
        Height = UiMetrics.EditorHeight;
        Width = UiMetrics.ContentWidth;
        Margin = new Padding(0, 0, 0, UiMetrics.CardGap);
        Font = Palette.BodyFont();
        Grip = new IconButton("drag", "Drag to reorder account")
        {
            Left = 7,
            Top = 39,
            BackColor = Palette.Card,
            Cursor = Cursors.SizeAll,
            TabStop = false
        };
        var service = new Panel
        {
            Left = 14,
            Top = 14,
            Width = 16,
            Height = 16
        };
        service.Paint += (_, e) => ServiceMark.Draw(e.Graphics, reading.Account.Service, 0, 0);
        var account = new Label
        {
            Text = reading.Account.Label,
            Left = 38,
            Top = 12,
            Width = 280,
            Height = 22,
            AutoEllipsis = true,
            ForeColor = Palette.Muted
        };
        RemoveButton = new IconButton("close", "Remove account")
        {
            Left = 322,
            Top = 6,
            BackColor = Palette.Card,
            Visible = false
        };
        RemoveButton.Click += (_, _) => RemoveRequested?.Invoke();
        NameInput = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(preference.Alias) ? reading.Account.Label : preference.Alias,
            PlaceholderText = "Display name",
            Left = 44,
            Top = 45,
            Width = 298,
            Height = 24,
            BackColor = Palette.Card,
            ForeColor = Palette.Text,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Cursor = Cursors.Hand,
            Font = Palette.AccountFont(),
            AccessibleName = "Display name"
        };
        NameInput.MouseDown += (_, _) =>
        {
            NameInput.ReadOnly = false;
            NameInput.Cursor = Cursors.IBeam;
            Invalidate();
        };
        NameInput.Enter += (_, _) =>
        {
            NameInput.ReadOnly = false;
            NameInput.Cursor = Cursors.IBeam;
            Invalidate();
        };
        NameInput.Leave += (_, _) =>
        {
            NameInput.ReadOnly = true;
            NameInput.Cursor = Cursors.Hand;
            Invalidate();
            EditedChanged?.Invoke();
        };
        NameInput.TextChanged += (_, _) => EditedChanged?.Invoke();
        Paint += (_, e) =>
        {
            using var edge = new Pen(NameInput.Focused ? Palette.Muted : Palette.InputBorder);
            e.Graphics.DrawRectangle(edge, NameInput.Left - 6, NameInput.Top - 5, NameInput.Width + 12, NameInput.Height + 10);
        };
        Paint += (_, e) =>
        {
            if (Dragging)
            {
                using var edge = new Pen(Palette.OnPace, 2);
                e.Graphics.DrawRectangle(edge, 1, 1, Width - 3, Height - 3);
            }
        };
        PanelToggle = new AccountToggle
        {
            Text = "In panel",
            Checked = preference.Show,
            Left = 38,
            Top = 80,
            Width = 96,
            Height = 27,
            ForeColor = Palette.Text
        };
        TrayToggle = new AccountToggle
        {
            Text = "In tray",
            Checked = preference.Tray,
            Left = 142,
            Top = 80,
            Width = 94,
            Height = 27,
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
        var signIn = Palette.Button("Sign in again", compact: true);
        signIn.SetBounds(244, 76, 104, UiMetrics.TextButtonHeight);
        signIn.Click += (_, _) => renew();
        Controls.AddRange([Grip, service, account, NameInput, PanelToggle, TrayToggle, signIn, RemoveButton]);
        foreach (Control control in Controls.Cast<Control>().Append(this))
        {
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

    void UpdateRemoveVisibility()
    {
        if (IsDisposed)
            return;
        RemoveButton.Visible = ContainsFocus || ClientRectangle.Contains(PointToClient(Cursor.Position));
    }

    public Preference Edited() => new()
    {
        Key = Preference.Key,
        Alias = NameInput.Text.Trim(),
        Show = PanelToggle.Checked,
        Tray = TrayToggle.Checked
    };
}
