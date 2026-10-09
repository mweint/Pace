namespace Pace;

public sealed class AccountsDialog : PageDialog
{
    readonly Settings settings;
    readonly Action rescan, persist;
    readonly Action? changed;
    readonly AppUpdates updates;
    readonly GeneralSettingsView general;
    readonly TabStrip tabs = new("General", "Accounts");
    readonly AccountToolbar accountTools = new();
    TextBlock trayCount => accountTools.TrayCount;

    readonly ScrollViewport accountViewport, generalViewport;
    readonly Grid body = new();
    readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(UiMetrics.AutoSaveMilliseconds) };
    readonly List<Reading> readings;
    readonly SignInFlow signIn;
    bool reverting;
    public AnimatedAccountList AccountList { get; } = new();
    public bool ShowingGeneral { get; private set; }
    public AccountsDialog(List<Reading> readings, Settings settings, Action rescan, Action? persist = null, Action? changed = null, AppUpdates? updates = null, Action? installUpdate = null, bool showGeneral = false, SignInFlow? signIn = null)
    {
        this.readings = readings.ToList(); this.settings = settings; this.rescan = rescan;
        this.signIn = signIn ?? new();
        this.persist = persist ?? settings.Save; this.changed = changed;
        this.updates = updates ?? new(settings);
        Title = "Pace · Settings";
        accountViewport = new(AccountList);
        general = new(settings, this.updates, changed ?? (() => { }), installUpdate ?? (() => { }), this.persist);
        generalViewport = new(general);
        var layout = new DockPanel();
        var back = new IconButton("back", "Back to usage");
        back.Click += (_, _) => Close();
        NavigationFocus = back;
        var footer = new FooterBar("Settings", back);
        DockPanel.SetDock(footer, Dock.Bottom); DockPanel.SetDock(tabs, Dock.Top);
        layout.Children.Add(footer); layout.Children.Add(tabs);
        var accountBody = new DockPanel();
        DockPanel.SetDock(accountTools, Dock.Top);
        accountBody.Children.Add(accountTools); accountBody.Children.Add(accountViewport);
        body.Children.Add(generalViewport); body.Children.Add(accountBody);
        layout.Children.Add(body); SetBody(layout);
        tabs.SelectionChanged += index => SelectTab(index == 0);
        accountTools.AddRequested += async service => await Login(service);
        accountTools.CancelRequested += this.signIn.Cancel;
        this.signIn.Changed += ShowSignIn;
        foreach (var reading in this.readings) AddAccount(reading);
        AccountList.Reordered += () => { saveTimer.Stop(); SaveEdits(); };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); SaveEdits(); };
        Closed += (_, _) => { saveTimer.Stop(); general.Dispose(); this.signIn.Changed -= ShowSignIn; };
        if (readings.Count == 0) AddEmpty();
        FitContent();
        SelectTab(showGeneral);
        UpdateTrayCapacity();
        ShowSignIn();
    }
    void AddEmpty()
    {
        var empty = new InfoSection();
        empty.Add("Add a Claude or Codex account to get started.", Palette.Muted);
        AccountList.Children.Add(empty);
    }
    void AddAccount(Reading reading)
    {
        var row = new AccountEditorRow(reading, settings.For(reading.Account), async () => await Login(reading.Account.Service, reading.Account.CredentialPath));
        row.EditedChanged += () =>
        {
            if (reverting) return;
            UpdateTrayCapacity(); saveTimer.Stop(); saveTimer.Start();
        };
        row.RemoveRequested += () =>
        {
            if (!SaveEdits()) return;
            AccountList.FinishMotion(); AccountList.Children.Remove(row);
            readings.RemoveAll(r => r.Account.Key == reading.Account.Key);
            settings.RemoveAccount(reading.Account);
            SaveEdits();
            if (AccountList.Children.Count == 0) AddEmpty();
        };
        AccountList.Children.Add(row); AccountList.Watch(row);
    }
    void FitContent()
    {
        general.Measure(new Size(UiMetrics.ContentWidth, double.PositiveInfinity));
        AccountList.Measure(new Size(UiMetrics.ContentWidth, double.PositiveInfinity));
        double threeRows = AccountList.Children.Take(3).Sum(c => c.DesiredSize.Height) + UiMetrics.AccountsToolbarHeight;
        double height = Math.Min(Math.Max(general.DesiredSize.Height, AccountList.DesiredSize.Height + UiMetrics.AccountsToolbarHeight), Math.Max(UiMetrics.SettingsContentMaxHeight, threeRows));
        Height = Math.Min(height + UiMetrics.TabStripHeight + UiMetrics.ToolbarHeight + 2 * UiMetrics.WindowBorderWidth, AvailableHeight);
    }
    public void SelectTab(bool showGeneral)
    {
        if (showGeneral && !SaveEdits()) { tabs.SelectedIndex = ShowingGeneral ? 0 : 1; return; }
        ShowingGeneral = showGeneral;
        body.Children[0].IsVisible = showGeneral; body.Children[1].IsVisible = !showGeneral;
        tabs.SelectedIndex = showGeneral ? 0 : 1;
    }
    void UpdateTrayCapacity()
    {
        var rows = AccountList.Children.OfType<AccountEditorRow>().ToList();
        int count = rows.Count(r => r.PanelToggle.Checked && r.TrayToggle.Checked);
        foreach (var row in rows)
            row.TrayToggle.IsEnabled = row.PanelToggle.Checked && (row.TrayToggle.Checked || count < AccountRules.MaxTrayAccounts);
        trayCount.Text = $"Tray {count}/{AccountRules.MaxTrayAccounts}";
    }
    public bool SaveEdits()
    {
        var rows = AccountList.Children.OfType<AccountEditorRow>().ToList();
        foreach (var row in rows) row.NameEditor.FinishEditing(true);
        var edited = rows.Select(r => r.Edited()).ToList();
        if (edited.Count(p => p.Show && p.Tray) > AccountRules.MaxTrayAccounts)
        {
            reverting = true;
            foreach (var row in rows) row.TrayToggle.Checked = row.Preference.Tray;
            reverting = false; UpdateTrayCapacity(); return false;
        }
        var previous = settings.Accounts;
        settings.Accounts = edited;
        try { persist(); changed?.Invoke(); return true; }
        catch
        {
            settings.Accounts = previous;
            ShowWarning("Couldn't save. Try again.");
            return false;
        }
    }
    public override bool SaveBeforeLeave() { saveTimer.Stop(); return SaveEdits(); }
    public void UpdateConnections(List<Reading> current)
    {
        foreach (var row in AccountList.Children.OfType<AccountEditorRow>())
            if (current.FirstOrDefault(r => r.Account.Key == row.Reading.Account.Key) is { } reading) row.UpdateConnection(reading);
    }
    void ShowSignIn()
    {
        accountTools.ShowSignIn(signIn.Active, signIn.Link);
        if (signIn.Active)
        {
            trayCount.Text = "Signing in…";
            trayCount.Foreground = Palette.Brush(Palette.Muted);
        }
        else if (trayCount.Text is "Signing in…" or "Link copied") UpdateTrayCapacity();
    }
    async Task Login(string service, string? existingFile = null)
    {
        if (signIn.Active || IsClosing) return;
        if (!SaveEdits()) return;
        var result = await signIn.Run(service, settings, existingFile);
        // Settings may have closed while the browser was open; the account still refreshes.
        if (IsClosing) { if (result.Succeeded) rescan(); return; }
        if (!result.Succeeded) { if (result.Message.Length > 0) ShowWarning(result.Message); return; }
        try
        {
            var accounts = await Providers.Discover(settings);
            if (IsClosing) return;
            AccountList.Children.Clear(); readings.Clear();
            foreach (var account in accounts)
            {
                var reading = new Reading(account, null, "Refreshing…", DateTimeOffset.UtcNow);
                readings.Add(reading); AddAccount(reading);
            }
            if (readings.Count == 0) AddEmpty();
        }
        catch (Exception) { ShowWarning("Couldn't read accounts. Try again."); }
        rescan();
    }
    void ShowWarning(string message)
    {
        trayCount.Text = message;
        trayCount.Foreground = Palette.Brush(Palette.Warning);
    }
}
