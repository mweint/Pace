namespace Usage;

internal sealed class GeneralSettingsView : SectionList
{
    readonly Settings settings;
    readonly AppUpdates updates;
    readonly Label updateStatus;
    readonly Button check, install, dismiss;
    readonly Action changed;
    readonly Action persist;
    public GeneralSettingsView(Settings settings, AppUpdates updates, Action changed, Action installUpdate, Action persist)
    {
        this.settings = settings;
        this.updates = updates;
        this.changed = changed;
        this.persist = persist;
        Dock = DockStyle.Fill;
        int width = UiMetrics.PanelWidth - 2 * UiMetrics.WindowBorderWidth;
        var time = new SettingsSection(width);
        time.Label("Reset time", true);
        var when = Palette.Button("When it resets");
        var remaining = Palette.Button("Time remaining");
        void SelectTime(bool relative)
        {
            settings.RelativeResetTime = relative;
            ((FilledButton)when).Selected = !relative;
            ((FilledButton)remaining).Selected = relative;
            when.Invalidate(); remaining.Invalidate();
        }
        SelectTime(settings.RelativeResetTime);
        when.Click += (_, _) => { SelectTime(false); Save(); };
        remaining.Click += (_, _) => { SelectTime(true); Save(); };
        time.Controls.Add(Buttons(when, remaining));
        string Example() => settings.RelativeResetTime ? "Resets in 2d 4h" : "Resets Mon · 5:30 PM";
        var example = time.Label(Example());
        when.Click += (_, _) => example.Text = Example();
        remaining.Click += (_, _) => example.Text = Example();
        Controls.Add(time);
        var startup = new SettingsSection(width);
        startup.Label("Startup", true);
        var launch = Toggle("Launch at sign-in", StartupRegistration.Enabled);
        bool restoring = false;
        launch.CheckedChanged += (_, _) =>
        {
            if (restoring) return;
            try { StartupRegistration.Enabled = launch.Checked; }
            catch { restoring = true; launch.Checked = StartupRegistration.Enabled; restoring = false; MessageBox.Show(FindForm(), "Couldn't change Windows startup. Please try again.", "Pace"); }
        };
        startup.Controls.Add(launch);
        Controls.Add(startup);
        var update = new SettingsSection(width);
        update.Label("Updates", true);
        update.Label("Pace " + AppUpdates.CurrentVersion);
        var automatic = Toggle("Automatic updates", settings.AutomaticUpdates);
        automatic.CheckedChanged += async (_, _) => { settings.AutomaticUpdates = automatic.Checked; Save(); if (automatic.Checked) await updates.Check(); };
        update.Controls.Add(automatic);
        updateStatus = update.Label(updates.Status);
        check = Palette.Button("Check for updates");
        install = Palette.Button("Update");
        dismiss = Palette.Button("Dismiss");
        check.Click += async (_, _) => await updates.Check();
        install.Click += async (_, _) => { await updates.Download(); if (updates.Ready) installUpdate(); };
        dismiss.Click += (_, _) => updates.Dismiss();
        update.Controls.Add(Buttons(check, install, dismiss));
        Controls.Add(update);
        updates.Changed += RefreshUpdate;
        RefreshUpdate();
    }
    static AccountToggle Toggle(string text, bool value)
    {
        var result = new AccountToggle { Text = text, Checked = value, Margin = new Padding(0, 0, 0, UiMetrics.InlineGap) };
        result.Size = result.GetPreferredSize(Size.Empty);
        return result;
    }
    static FlowLayoutPanel Buttons(params Button[] buttons)
    {
        var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, UiMetrics.CardGap) };
        foreach (var button in buttons)
        {
            button.Width = TextRenderer.MeasureText(button.Text, button.Font).Width + 2 * UiMetrics.ContentInset;
            button.Height = UiMetrics.TextButtonHeight;
            button.Margin = new Padding(0, 0, UiMetrics.InlineGap, 0);
            flow.Controls.Add(button);
        }
        return flow;
    }
    void Save() { persist(); changed(); }
    void RefreshUpdate()
    {
        if (IsDisposed) return;
        updateStatus.Text = updates.Status;
        updateStatus.Visible = updates.Status.Length > 0;
        updateStatus.ForeColor = updates.Available != null ? Palette.Warning : Palette.Muted;
        check.Enabled = !updates.Busy;
        install.Visible = updates.Available != null;
        dismiss.Visible = updates.Notify;
        install.Enabled = !updates.Busy;
        dismiss.Enabled = !updates.Busy && updates.Notify;
        PerformLayout();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) updates.Changed -= RefreshUpdate;
        base.Dispose(disposing);
    }
}
