namespace Usage;

public sealed class AccountNameEditor : Panel
{
    readonly string fallback;
    readonly IconButton confirm = new("confirm", "Save display name");
    string alias;
    bool editing;
    bool hovered;
    public TextBox Input { get; }
    public string Alias => alias;
    public bool IsEditing => editing;
    public event Action? Committed;
    public event Action? EditingChanged;

    public AccountNameEditor(string alias, string fallback)
    {
        this.alias = alias;
        this.fallback = fallback;
        BackColor = Palette.SectionBackground;
        Height = UiMetrics.NameEditorHeight;
        TabStop = true;
        Cursor = Cursors.Hand;
        Input = new CompletionInput(FinishEditing)
        {
            Text = DisplayName(), ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = Palette.SectionBackground, ForeColor = Palette.Text,
            Font = Palette.AccountFont(), Cursor = Cursors.Hand,
            AccessibleName = "Display name", Visible = false
        };
        Input.Leave += (_, _) => FinishEditing(true);
        MouseClick += (_, _) => { BeginEditing(); Input.Focus(); };
        confirm.BackColor = Palette.SectionBackground;
        confirm.Visible = false;
        confirm.Click += (_, _) => FinishEditing(true);
        Controls.AddRange([Input, confirm]);
        SizeChanged += (_, _) => Arrange();
        Arrange();
    }

    string DisplayName() => string.IsNullOrWhiteSpace(alias) ? fallback : alias;

    void Arrange()
    {
        int fieldWidth = Width - UiMetrics.IconButtonSize - UiMetrics.CardGap;
        Input.SetBounds(UiMetrics.CardGap, (Height - Input.PreferredHeight) / 2,
            Math.Max(1, fieldWidth - 2 * UiMetrics.CardGap), Input.PreferredHeight);
        confirm.SetBounds(Width - UiMetrics.IconButtonSize, (Height - UiMetrics.IconButtonSize) / 2,
            UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
    }

    public void BeginEditing()
    {
        if (editing)
            return;
        editing = true;
        Input.ReadOnly = false;
        Input.Visible = true;
        Input.Cursor = Cursors.IBeam;
        confirm.Visible = true;
        Arrange();
        Invalidate();
        EditingChanged?.Invoke();
    }

    public void FinishEditing(bool save)
    {
        if (!editing)
            return;
        bool changed = save && alias != Input.Text.Trim();
        if (save)
            alias = Input.Text.Trim();
        editing = false;
        Input.Text = DisplayName();
        Input.ReadOnly = true;
        Input.Visible = false;
        Input.Cursor = Cursors.Hand;
        Input.Select(0, 0);
        confirm.Visible = false;
        Arrange();
        Invalidate();
        if (changed)
            Committed?.Invoke();
        EditingChanged?.Invoke();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!editing)
        {
            TextRenderer.DrawText(e.Graphics, DisplayName(), Input.Font,
                new Rectangle(Input.Left, 0, Input.Width, Height), Palette.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
        Color border = Palette.InputOutline(editing, hovered || (Focused && ShowFocusCues));
        using var edge = new Pen(border, UiMetrics.BorderWidth);
        e.Graphics.DrawRectangle(edge, 0, 0, Width - UiMetrics.IconButtonSize - UiMetrics.CardGap - UiMetrics.BorderWidth, Height - UiMetrics.BorderWidth);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            BeginEditing();
            Input.Focus();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    sealed class CompletionInput(Action<bool> finish) : TextBox
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!ReadOnly && keyData is Keys.Enter or Keys.Escape)
            {
                finish(keyData == Keys.Enter);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
