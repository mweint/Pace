namespace Usage;

public sealed class AccountsDialog : PageDialog
{
    readonly AnimatedAccountList list = new()
    {
        Dock = DockStyle.None,
        Padding = Padding.Empty,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = false,
        AllowDrop = true
    };
    readonly Settings settings;
    readonly GeneralSettingsView general;
    readonly TabStrip tabs = new("General", "Accounts") { Dock = DockStyle.Top };
    readonly Panel accountTools = new() { Dock = DockStyle.Top, Height = UiMetrics.AccountsToolbarHeight, BackColor = Palette.SectionBackground };
    int viewportHeight;
    readonly ScrollViewport accountViewport;
    internal AnimatedAccountList AccountList => list;
    bool showingGeneral;
    readonly Action rescan;
    readonly List<Reading> readings;
    bool signingIn;
    bool reverting;
    readonly System.Windows.Forms.Timer saveTimer = new()
    {
        Interval = UiMetrics.AutoSaveMilliseconds
    };
    readonly Action persist;
    readonly Action? changed;
    readonly Label trayCount = new()
    {
        AutoSize = true, Margin = Padding.Empty,
        Font = Palette.BodyFont(), ForeColor = Palette.Muted
    };
    public AccountsDialog(List<Reading> readings, Settings settings, Action rescan, Action? persist = null, Action? changed = null, AppUpdates? updates = null, Action? installUpdate = null, bool showGeneral = false)
    {
        this.readings = readings;
        this.settings = settings;
        this.rescan = rescan;
        this.persist = persist ?? settings.Save;
        this.changed = changed;
        accountViewport = new ScrollViewport(list) { Dock = DockStyle.Fill };
        saveTimer.Tick += (_, _) =>
        {
            saveTimer.Stop();
            SaveEdits();
        };
        Text = "Pace · Settings";
        ClientSize = new Size(UiMetrics.PanelWidth, UiMetrics.AccountsToolbarHeight);
        StartPosition = FormStartPosition.Manual;
        var footer = CreateFooter();
        general = new GeneralSettingsView(settings, updates ?? new AppUpdates(settings), changed ?? (() => { }), installUpdate ?? (() => { }), this.persist);
        tabs.SelectionChanged += index => { if (showingGeneral != (index == 0)) SelectTab(index == 0); };
        foreach (var reading in readings)
            AddAccount(reading);
        if (readings.Count == 0)
        {
            var empty = new InfoSection { Width = UiMetrics.ContentWidth };
            empty.Add("Add a Claude or Codex account to get started.", Palette.Muted);
            list.Controls.Add(empty);
        }
        ConfigureDragAndDrop();
        Controls.Add(accountViewport);
        Controls.Add(general);
        Controls.Add(accountTools);
        Controls.Add(tabs);
        Controls.Add(footer);
        SelectTab(showGeneral);
        FitContent();
        UpdateTrayCapacity();
    }

    Panel CreateFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UiMetrics.ToolbarHeight,
            BackColor = Palette.Footer
        };
        var back = new IconButton("back", "Back to usage")
        {
            BackColor = footer.BackColor
        };
        back.SetBounds(UiMetrics.ToolbarInset, UiMetrics.ToolbarInset, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        back.Click += (_, _) => Close();
        var title = new Label
        {
            Text = "Settings",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, UiMetrics.BorderWidth * 2),
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        title.Location = new Point(UiMetrics.ToolbarLabelLeft, UiMetrics.ToolbarTextTop);
        trayCount.AutoSize = false;
        trayCount.TextAlign = ContentAlignment.MiddleLeft;
        trayCount.SetBounds(UiMetrics.ContentInset, UiMetrics.OuterInset,
            174 - UiMetrics.ContentInset - UiMetrics.CardGap, UiMetrics.TextButtonHeight);
        var claude = Palette.Button("Add Claude");
        claude.SetBounds(174, UiMetrics.OuterInset, 96, UiMetrics.TextButtonHeight);
        claude.Click += async (_, _) => await Login("Claude");
        var codex = Palette.Button("Add Codex");
        codex.SetBounds(278, UiMetrics.OuterInset, 96, UiMetrics.TextButtonHeight);
        codex.Click += async (_, _) => await Login("Codex");
        footer.Controls.AddRange([back, title]);
        accountTools.Controls.AddRange([trayCount, claude, codex]);
        NavigationFocus = back;
        return footer;
    }

    void AddAccount(Reading reading)
    {
        var row = new AccountEditorRow(reading, settings.For(reading.Account), async () => await Login(reading.Account.Service, reading.Account.CredentialPath));
        row.RemoveRequested += () =>
        {
            saveTimer.Stop();
            if (!SaveEdits())
                return;
            list.FinishMotion();
            list.Controls.Remove(row);
            row.Dispose();
            settings.RemoveAccount(reading.Account);
            this.readings.Remove(reading);
            SaveEdits();
            FitContent();
            Place();
        };
        Point dragStart = Point.Empty;
        row.Grip.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                dragStart = e.Location;
        };
        row.Grip.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && Math.Abs(e.X - dragStart.X) + Math.Abs(e.Y - dragStart.Y) > 5)
            {
                row.Dragging = true;
                row.Invalidate();
                int originalIndex = list.Controls.GetChildIndex(row);
                try
                {
                    if (row.Grip.DoDragDrop(row, DragDropEffects.Move) == DragDropEffects.None)
                        list.MoveRow(row, originalIndex);
                }
                finally
                {
                    list.FinishMotion();
                    row.Dragging = false;
                    row.Invalidate();
                    SaveEdits();
                }
            }
        };
        list.Controls.Add(row);
        row.EditedChanged += () =>
        {
            if (!reverting)
            {
                UpdateTrayCapacity();
                saveTimer.Stop();
                saveTimer.Start();
            }
        };
    }

    void FitContent()
    {
        int GeneralHeight = general.Controls.Cast<Control>().Sum(control => control.Height + control.Margin.Vertical);
        int accountsHeight = list.Controls.Cast<Control>().Sum(control => control.Height + control.Margin.Vertical) + accountTools.Height;
        int threeAccountsHeight = list.Controls.Cast<Control>().Take(3).Sum(control => control.Height + control.Margin.Vertical) + accountTools.Height;
        int contentCap = Math.Max(LogicalToDeviceUnits(UiMetrics.SettingsContentMaxHeight), threeAccountsHeight);
        viewportHeight = Math.Max(viewportHeight, Math.Min(Math.Max(GeneralHeight, accountsHeight), contentCap));
        ClientSize = new Size(ClientSize.Width, Math.Min(viewportHeight + LogicalToDeviceUnits(UiMetrics.ToolbarHeight + UiMetrics.SettingsTabsHeight) + Padding.Vertical, AnchorArea.Height - 80));
        list.PerformLayout();
    }

    internal void SelectTab(bool showGeneral)
    {
        if (showGeneral && !SaveEdits()) { tabs.SelectedIndex = showingGeneral ? 0 : 1; return; }
        SuspendLayout();
        showingGeneral = showGeneral;
        accountViewport.Visible = !showGeneral;
        general.Visible = showGeneral;
        accountTools.Visible = !showGeneral;
        tabs.SelectedIndex = showGeneral ? 0 : 1;
        FitContent();
        ResumeLayout(true);
        if (Visible) Place();
    }

    public void UpdateConnections(List<Reading> latest)
    {
        foreach (var row in list.Controls.OfType<AccountEditorRow>())
            if (latest.FirstOrDefault(r => r.Account.Key == row.Preference.Key) is { } reading)
                row.UpdateConnection(reading);
    }

    void ConfigureDragAndDrop()
    {
        list.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(typeof(AccountEditorRow)) == true)
                e.Effect = DragDropEffects.Move;
        };
        list.DragOver += (_, e) => MoveRowToPointer(e);
        list.DragDrop += (_, e) =>
        {
            if (!MoveRowToPointer(e))
                return;
            list.FinishMotion();
            SaveEdits();
        };
    }

    bool MoveRowToPointer(DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(AccountEditorRow)) is not AccountEditorRow row)
        {
            e.Effect = DragDropEffects.None;
            return false;
        }

        e.Effect = DragDropEffects.Move;
        var position = list.PointToClient(new Point(e.X, e.Y));
        var others = list.Controls.OfType<AccountEditorRow>().Where(control => control != row);
        int index = others.TakeWhile(control => position.Y > list.TargetTop(control) + control.Height / 2).Count();
        list.MoveRow(row, index);
        return true;
    }

    protected override void OnPageShown()
    {
        foreach (var row in list.Controls.OfType<AccountEditorRow>())
            row.NameInput.Select(0, 0);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        saveTimer.Stop();
        foreach (var row in list.Controls.OfType<AccountEditorRow>())
            row.NameEditor.FinishEditing(true);
        SaveEdits();
        base.OnFormClosing(e);
    }

    internal bool SaveEdits()
    {
        var preferences = list.Controls.OfType<AccountEditorRow>().Select(row => row.Edited()).ToList();
        if (preferences.Count(p => p.Show && p.Tray) > AccountRules.MaxTrayAccounts)
        {
            reverting = true;
            foreach (var row in list.Controls.OfType<AccountEditorRow>())
            {
                row.PanelToggle.Checked = row.Preference.Show;
                row.TrayToggle.Checked = row.Preference.Tray;
            }

            reverting = false;
            UpdateTrayCapacity();
            MessageBox.Show(this, $"Choose up to {AccountRules.MaxTrayAccounts} accounts for the tray icon.", "Pace");
            return false;
        }

        // Keep saved preferences for accounts temporarily absent during sign-in changes.
        settings.Accounts = preferences.Concat(settings.Accounts.Where(p => preferences.All(edited => edited.Key != p.Key))).ToList();
        foreach (var row in list.Controls.OfType<AccountEditorRow>())
        {
            var edit = row.Edited();
            row.Preference.Alias = edit.Alias;
            row.Preference.Show = edit.Show;
            row.Preference.Tray = edit.Tray;
            row.Preference.ShowFiveHour = edit.ShowFiveHour;
            row.Preference.ShowFable = edit.ShowFable;
        }

        persist();
        changed?.Invoke();
        UpdateTrayCapacity();
        return true;
    }

    void UpdateTrayCapacity()
    {
        var rows = list.Controls.OfType<AccountEditorRow>().ToList();
        int selected = rows.Count(row => row.PanelToggle.Checked && row.TrayToggle.Checked);
        trayCount.Text = $"Tray {selected}/{AccountRules.MaxTrayAccounts}";
        foreach (var row in rows)
        {
            row.TrayToggle.Enabled = row.TrayToggle.Checked || selected < AccountRules.MaxTrayAccounts;
            row.TrayToggle.Text = "Tray";
            row.TrayToggle.AccessibleDescription = row.TrayToggle.Enabled ? "Show account in tray" : "Tray full";
        }
    }

    async Task Login(string service, string? existingFile = null)
    {
        if (signingIn || !SaveEdits())
            return;
        try
        {
            signingIn = true;
            await SignIn.Begin(service, settings, existingFile);
            if (!IsDisposed)
            {
                DialogResult = DialogResult.Retry;
                Close();
            }

            rescan();
        }
        catch (Exception e)
        {
            if (!IsDisposed)
                MessageBox.Show(this, e.Message, "Sign-in");
        }
        finally
        {
            signingIn = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            saveTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
