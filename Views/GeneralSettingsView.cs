namespace Pace;

internal sealed class GeneralSettingsView : SectionList, IDisposable
{
    readonly Settings settings;
    readonly AppUpdates updates;
    readonly TextBlock updateStatus;
    readonly FilledButton check, install, dismiss;
    readonly Action changed, persist;
    public GeneralSettingsView(Settings settings, AppUpdates updates, Action changed, Action installUpdate, Action persist)
    {
        this.settings = settings; this.updates = updates; this.changed = changed; this.persist = persist;
        var time = new SettingsSection();
        time.Label("Reset time", true);
        var when = Palette.Button("When it resets"); var remaining = Palette.Button("Time remaining");
        void SelectTime(bool relative)
        {
            settings.RelativeResetTime = relative;
            when.Selected = !relative; remaining.Selected = relative;
            when.InvalidateVisual(); remaining.InvalidateVisual();
        }
        SelectTime(settings.RelativeResetTime);
        time.Children.Add(new ButtonGroup(when, remaining));
        string Example() => settings.RelativeResetTime ? "Resets in 2d 4h" : "Resets Mon · 5:30 PM";
        var example = time.Label(Example());
        when.Click += (_, _) => { SelectTime(false); example.Text = Example(); Save(); };
        remaining.Click += (_, _) => { SelectTime(true); example.Text = Example(); Save(); };
        Children.Add(time);
        var startup = new SettingsSection();
        startup.Label("Startup", true);
        var launch = startup.Toggle("Launch at sign-in", StartupRegistration.Enabled);

        launch.IsEnabled = OperatingSystem.IsWindows();

        bool restoring = false;
        launch.IsCheckedChanged += (_, _) =>
        {
            if (restoring) return;
            try { StartupRegistration.Enabled = launch.Checked; }
            catch
            {
                restoring = true; launch.Checked = StartupRegistration.Enabled; restoring = false;
                DesktopIntegration.ShowStartupError(TopLevel.GetTopLevel(launch) as Window);
            }
        };
        startup.Children.Insert(1, launch);
        Children.Add(startup);
        var update = new SettingsSection();
        update.Label("Updates", true); update.Label("Pace " + AppUpdates.CurrentVersion);
        var automatic = update.Toggle("Automatic updates", settings.AutomaticUpdates);

        automatic.IsCheckedChanged += async (_, _) => { settings.AutomaticUpdates = automatic.Checked; Save(); if (automatic.Checked) await updates.Check(); };
        update.Children.Add(automatic);
        updateStatus = update.Label(updates.Status);
        check = Palette.Button("Check for updates"); install = Palette.Button("Update"); dismiss = Palette.Button("Dismiss");
        check.Click += async (_, _) => await updates.Check();
        install.Click += async (_, _) => { await updates.Download(); if (updates.Ready) installUpdate(); };
        dismiss.Click += (_, _) => updates.Dismiss();
        update.Children.Add(new ButtonGroup(check, install, dismiss));
        Children.Add(update);
        updates.Changed += RefreshUpdate; RefreshUpdate();
    }
    void Save() { persist(); changed(); }
    void RefreshUpdate()
    {
        updateStatus.Text = updates.Status; updateStatus.IsVisible = updates.Status.Length > 0;
        updateStatus.Foreground = Palette.Brush(updates.Available != null ? Palette.Warning : Palette.Muted);
        check.IsEnabled = !updates.Busy;
        install.IsVisible = updates.Available != null; dismiss.IsVisible = updates.Notify;
        install.IsEnabled = !updates.Busy; dismiss.IsEnabled = !updates.Busy && updates.Notify;
    }
    public void Dispose() => updates.Changed -= RefreshUpdate;
}
