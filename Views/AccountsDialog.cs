namespace Usage;

public sealed class AccountsDialog : PageDialog
{
    readonly AnimatedAccountList list = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(UiMetrics.OuterInset),
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        AllowDrop = true
    };
    readonly Settings settings;
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
    public AccountsDialog(List<Reading> readings, Settings settings, Action rescan, Action? persist = null, Action? changed = null)
    {
        this.readings = readings;
        this.settings = settings;
        this.rescan = rescan;
        this.persist = persist ?? settings.Save;
        this.changed = changed;
        saveTimer.Tick += (_, _) =>
        {
            saveTimer.Stop();
            SaveEdits();
        };
        Text = "Pace · Accounts";
        ClientSize = new Size(UiMetrics.PanelWidth, Math.Min(UiMetrics.AccountsToolbarHeight + 18 + Math.Max(1, readings.Count) * (UiMetrics.EditorHeight + UiMetrics.CardGap), Screen.PrimaryScreen!.WorkingArea.Height - 80));
        StartPosition = FormStartPosition.Manual;
        var footer = CreateFooter();
        foreach (var reading in readings)
            AddAccount(reading);
        if (readings.Count == 0)
            list.Controls.Add(new Label { Text = "Add a Claude or Codex account below.", Width = UiMetrics.PanelWidth - 2 * UiMetrics.OuterInset, Height = 70, ForeColor = Palette.Muted });
        ConfigureDragAndDrop();
        Controls.Add(list);
        Controls.Add(footer);
    }

    Panel CreateFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UiMetrics.AccountsToolbarHeight,
            BackColor = Palette.Footer
        };
        var back = new IconButton("back", "Back to usage")
        {
            BackColor = footer.BackColor
        };
        back.SetBounds(8, 12, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        back.Click += (_, _) => Close();
        var title = new Label
        {
            Text = "Accounts",
            Left = 44,
            Top = 19,
            Width = 116,
            Height = 24,
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        var claude = Palette.Button("Add Claude");
        claude.SetBounds(174, 12, 96, UiMetrics.TextButtonHeight);
        claude.Click += async (_, _) => await Login("Claude");
        var codex = Palette.Button("Add Codex");
        codex.SetBounds(278, 12, 96, UiMetrics.TextButtonHeight);
        codex.Click += async (_, _) => await Login("Codex");
        footer.Controls.AddRange([back, title, claude, codex]);
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
            var area = AnchorArea;
            ClientSize = new Size(ClientSize.Width, Math.Min(UiMetrics.AccountsToolbarHeight + 18 + Math.Max(1, this.readings.Count) * (UiMetrics.EditorHeight + UiMetrics.CardGap), area.Height - 80));
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
                saveTimer.Stop();
                saveTimer.Start();
            }
        };
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
        }

        persist();
        changed?.Invoke();
        return true;
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
