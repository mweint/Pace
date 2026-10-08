using System.Text.Json;

namespace Pace;

internal static class InterfaceChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        bool animations = Motion.Enabled;
        var open = new List<Window>();
        try
        {
            check(new Typeface(Palette.FontFamily).GlyphTypeface.FamilyName == "Pace Sans" &&
                new Typeface(Palette.HeadingFontFamily).GlyphTypeface.FamilyName == "Pace Sans SemiBold", "Both packaged font families load");
            check((int)new Typeface(Palette.FontFamily).GlyphTypeface.Weight == 400 &&
                (int)new Typeface(Palette.HeadingFontFamily).GlyphTypeface.Weight == 600 &&
                new Typeface(Palette.HeadingFontFamily).GlyphTypeface.FontSimulations == Avalonia.Media.FontSimulations.None,
                "Packaged fonts use regular 400 and semibold 600 weights without synthetic bolding");
            foreach (string name in new[] { "arrow-left", "check", "grip-vertical", "pin", "refresh-cw", "settings", "trash-2", "users", "x" })
            {
                using var svgStream = Avalonia.Platform.AssetLoader.Open(new Uri($"avares://Pace/Assets/Icons/{name}.svg"));
                var svg = System.Xml.Linq.XDocument.Load(svgStream).Root!;
                var bounds = IconArtwork.Shape(name).Bounds;
                check((string?)svg.Attribute("viewBox") == "0 0 24 24" && bounds.Width > 0 && bounds.Left >= 0 && bounds.Right <= 24 && bounds.Top >= 0 && bounds.Bottom <= 24,
                    $"{name} draws from its Lucide 24px SVG geometry");
            }
            var demo = SampleData.Readings();
            var settings = new Settings();
            foreach (var reading in demo) settings.For(reading.Account);
            var panel = new UsagePanel { PreviewMode = true };
            open.Add(panel); panel.UpdateRows(demo, settings, false); panel.Show(); await Task.Delay(80);
            var root = (Control)panel.Content!;
            var cards = root.GetVisualDescendants().OfType<AccountRow>().ToList();
            check(panel.Width == UiMetrics.PanelWidth && cards.Count == 4, "Overview retains compact width and all sample accounts");
            var previous = cards[0];
            panel.UpdateRows(demo, settings, true);
            check(((Control)panel.Content!).GetVisualDescendants().OfType<AccountRow>().First() == previous, "Usage refresh reuses existing account controls");
            var hidden = settings.For(demo[2].Account);
            double visibleHeight = cards[2].DesiredSize.Height;
            hidden.ShowFiveHour = hidden.ShowFable = false;
            panel.UpdateRows(demo, settings, false); await Task.Delay(50);
            check(cards[2].DesiredSize.Height < visibleHeight && LimitWarning.Hidden(demo[2], hidden, DateTimeOffset.UtcNow).Count > 0, "Hiding secondary bars resizes rows and retains limit warnings");
            hidden.ShowFiveHour = hidden.ShowFable = true;
            var three = new AccountsDialog(demo.Take(3).ToList(), settings, () => { }, persist: () => { }) { PreviewMode = true };
            open.Add(three); three.Show(); await Task.Delay(80);
            var threeViewport = ((Control)three.Content!).GetVisualDescendants().OfType<ScrollViewport>().Single(v => v.Content == three.AccountList);
            check(!threeViewport.Overflow, "Three complete account rows fit without scrolling");
            var accounts = new AccountsDialog(demo, settings, () => { }, persist: () => { }) { PreviewMode = true };
            open.Add(accounts); accounts.Show(); await Task.Delay(80);
            await Theme(check, accounts);
            await PointerInteraction(check, accounts, settings);
            var viewport = ((Control)accounts.Content!).GetVisualDescendants().OfType<ScrollViewport>().Single(v => v.Content == accounts.AccountList);
            check(viewport.Overflow && viewport.Thumb.Width == UiMetrics.ScrollbarWidth, "Four accounts use the slim theme scrollbar");
            viewport.SetOffset(48); await Task.Delay(30);
            check(viewport.Offset == 48 && accounts.AccountList.Bounds.Y < 0, "Scrolling offsets content inside the clipped viewport");
            double height = accounts.Height;
            accounts.SelectTab(true); accounts.SelectTab(false);
            check(accounts.Height == height, "Settings tabs preserve window dimensions and footer placement");
            var editor = accounts.AccountList.Children.OfType<AccountEditorRow>().First();
            check(!editor.NameEditor.IsEditing && !editor.NameEditor.Input.IsVisible, "Names open without selection or editing");
            editor.NameEditor.BeginEditing(); editor.NameEditor.Input.Text = "Changed";
            editor.NameEditor.FinishEditing(false);
            check(editor.NameEditor.Alias == "", "Escape cancels a name edit");
            editor.NameEditor.BeginEditing(); editor.NameEditor.Input.Text = "Saved name"; editor.NameEditor.FinishEditing(true);
            editor.PanelToggle.Checked = false;
            await Task.Delay(UiMetrics.AutoSaveMilliseconds + 100);
            check(settings.For(demo[0].Account).Alias == "Saved name" && !settings.For(demo[0].Account).Show, "Names and visibility autosave while Accounts is open");
            var claude = accounts.AccountList.Children.OfType<AccountEditorRow>().First(r => r.Reading.Account.Service == "Claude");
            check(claude.FableToggle.Bounds.Right <= claude.Bounds.Width - UiMetrics.ContentInset && claude.PanelToggle.Bounds.Y == claude.FableToggle.Bounds.Y, "All four Claude switches fit on one aligned row");
            Motion.Enabled = true;
            accounts.AccountList.MoveRow(editor, 2); await Task.Delay(50);
            check(editor.RenderTransform is TranslateTransform transform && Math.Abs(transform.Y) > .01, "Reordered accounts animate toward their new positions");
            await Task.Delay((int)Motion.ReorderMilliseconds + 50);
            check(accounts.AccountList.Children.IndexOf(editor) == 2 && editor.RenderTransform == null && accounts.SaveEdits() && settings.Accounts[2].Key == editor.Preference.Key, "Completed reorder persists the displayed order");
            editor.UpdateConnection(editor.Reading with { Error = "Sign-in missing", ConnectionIssue = ConnectionIssue.CredentialsMissing });
            check(editor.Children.OfType<FilledButton>().Single().IsVisible, "Missing sign-ins expose reconnect");
            editor.UpdateConnection(editor.Reading with { Error = null, ConnectionIssue = ConnectionIssue.None });
            check(!editor.Children.OfType<FilledButton>().Single().IsVisible, "Successful authentication clears reconnect");
            editor.RemoveButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(settings.IsRemoved(editor.Reading.Account) && accounts.AccountList.Children.Count == 3, "Removing an account persists exclusion without deleting credentials");
            var restored = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;
            check(restored.IsRemoved(editor.Reading.Account), "Account removal survives settings reload");
            await Navigation(check, demo);
            if (OperatingSystem.IsWindows())
            {
                using var tray = new NativeTrayIcon();
                tray.Update(TrayDrawing.Png(demo));
                await Task.Delay(300);
                check(tray.Added, $"Windows tray registers the dynamic icon (icon error {tray.IconError}, shell error {tray.ShellError})");
                check(tray.Bounds is { Width: > 0 }, "Windows tray exposes its hover anchor");
            }
        }
        finally
        {
            Motion.Enabled = animations;
            foreach (var window in open)
            {
                if (window is UsagePanel panel) panel.Shutdown();
                else window.Close();
            }
        }
    }
    static async Task Theme(Action<bool, string> check, AccountsDialog accounts)
    {
        var descendants = ((Control)accounts.Content!).GetVisualDescendants().OfType<Control>().ToList();
        var tabs = descendants.OfType<TabStrip>().Single();
        check(Enumerable.Range(0, 2).All(i => tabs.TabWidth(i) >= Palette.TextWidth(i == 0 ? "General" : "Accounts", Palette.TitleSize, true) + 2 * UiMetrics.TabTextInset),
            "Tabs fit their measured labels with the shared inset");
        var footer = descendants.OfType<FooterBar>().Single();
        var caption = footer.Children.OfType<TextBlock>().Single();
        check(caption.FontFamily == Palette.HeadingFontFamily && caption.FontSize == Palette.TitleSize && Canvas.GetLeft(caption) == UiMetrics.ToolbarLabelLeft,
            "Footer caption uses the toolbar heading role and shared placement");
        accounts.SelectTab(true); await Task.Delay(40);
        var sections = descendants.OfType<SettingsSection>().ToList();
        check(sections.Count == 3 && sections.All(s => s.Children.All(c => c.Bounds.Bottom <= s.Bounds.Height - UiMetrics.ContentInset + .5)),
            "General sections contain their measured rows within the shared inset");
        var buttons = descendants.OfType<FilledButton>().Where(b => b.Text is "When it resets" or "Time remaining" or "Check for updates" or "Update" or "Dismiss").ToList();
        check(buttons.Count == 5 && buttons.All(b => b.MinWidth == UiMetrics.TextButtonWidth(b.Text) && b.MinWidth > Palette.TextWidth(b.Text)),
            "Filled buttons size to their measured text plus shared padding");
        var icon = descendants.OfType<IconButton>().First(b => b.Kind == "back");
        icon.Focus(NavigationMethod.Pointer);
        check(!icon.ShowsFocusOutline && ToolTip.GetTip(icon) == null, "Mouse-focused icon has no outline or added tooltip");
        buttons[0].Focus(NavigationMethod.Pointer);
        check(!buttons[0].ShowsFocusOutline, "Mouse-selected text button keeps no focus outline");
        icon.Focus(NavigationMethod.Tab);
        check(icon.ShowsFocusOutline, "Keyboard-focused icon uses theme focus outline");
        FocusCue.HideForPointer((Control)accounts.Content!);
        check(!icon.ShowsFocusOutline, "Pointer interaction anywhere clears retained keyboard outlines");
        accounts.SelectTab(false);
    }
    static async Task PointerInteraction(Action<bool, string> check, AccountsDialog accounts, Settings settings)
    {
        var root = (Control)accounts.Content!;
        var tabs = root.GetVisualDescendants().OfType<TabStrip>().Single();
        accounts.SelectTab(true); await Task.Delay(40);
        ClickAt(accounts, tabs, new Point(UiMetrics.ContentInset + tabs.TabWidth(0) + UiMetrics.TabGap + tabs.TabWidth(1) / 2, UiMetrics.TabStripHeight / 2));
        check(!accounts.ShowingGeneral, "Pointer hit-testing and routed input select Accounts");
        ClickAt(accounts, tabs, new Point(UiMetrics.ContentInset + tabs.TabWidth(0) / 2, UiMetrics.TabStripHeight / 2));
        check(accounts.ShowingGeneral, "Pointer hit-testing and routed input select General");
        var button = root.GetVisualDescendants().OfType<FilledButton>().First(b => b.Text == "Time remaining");
        ClickAt(accounts, button, new Point(2, 2));
        check(button.Selected, "Filled buttons accept pointer input across their full surface");
        accounts.SelectTab(false);
        await Task.Delay(40);
        var row = accounts.AccountList.Children.OfType<AccountEditorRow>().First();
        ClickAt(accounts, row.NameEditor, new Point(10, 10));
        check(row.NameEditor.IsEditing, "Pointer input opens the name editor");
        row.NameEditor.Input.Text = "";
        await Task.Delay(40);
        var confirm = row.NameEditor.Children.OfType<IconButton>().Single();
        await Task.Delay(120);
        ClickAt(accounts, confirm, new Point(2, 2));
        check(!row.NameEditor.IsEditing && row.NameEditor.Alias == "", "Icon button margins accept pointer input and confirm the name edit");
        bool wasChecked = row.PanelToggle.Checked;
        ClickAt(accounts, row.PanelToggle, new Point(row.PanelToggle.Bounds.Width - 2, 2));
        check(row.PanelToggle.Checked != wasChecked, "Switch labels and empty margins accept pointer input");
        ClickAt(accounts, row.PanelToggle, new Point(2, 2));
        check(row.PanelToggle.Checked == wasChecked, "Switch tracks accept pointer input");
        var position = row.Grip.TranslatePoint(new Point(2, 2), accounts)!.Value;
        var target = accounts.InputHitTest(position) as InputElement ?? throw new InvalidOperationException("No drag target");
        using var pointer = new Pointer(2, PointerType.Mouse, true);
        target.RaiseEvent(new PointerPressedEventArgs(target, pointer, accounts, position, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        check(pointer.Captured == row.Grip, "Drag handle receives the pointer press and captures it");
        var destination = accounts.AccountList.Children[1].TranslatePoint(new Point(20, accounts.AccountList.Children[1].Bounds.Height - 10), accounts)!.Value;
        row.Grip.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, row.Grip, pointer, accounts, destination, 1,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        check(row.Dragging && accounts.AccountList.Children.IndexOf(row) == 1, "Dragging across the next row changes account order");
        row.Grip.RaiseEvent(new PointerReleasedEventArgs(row.Grip, pointer, accounts, destination, 2,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        check(!row.Dragging && pointer.Captured == null, "Dropping ends dragging and releases pointer capture");
        await Task.Delay((int)Motion.ReorderMilliseconds + 50);
        check(settings.Accounts.Select(p => p.Key).SequenceEqual(accounts.AccountList.Children.OfType<AccountEditorRow>().Select(r => r.Preference.Key)), "Pointer drop persists the displayed account order");
        var first = accounts.AccountList.Children[0];
        DragTo(accounts, row, first.TranslatePoint(new Point(20, 5), accounts)!.Value);
        await Task.Delay(50);
        check(accounts.AccountList.Children.IndexOf(row) == 0 && settings.Accounts[0].Key == row.Preference.Key, "Upward pointer drops insert before the preceding account and persist");
        int outsideIndex = DragTo(accounts, row, new Point(-10, accounts.Height + 10));
        await Task.Delay(50);
        check(outsideIndex > 0 && accounts.AccountList.Children.IndexOf(row) == 0, "Drops outside the account viewport restore the original order");
        int cancelledIndex = DragTo(accounts, row, accounts.AccountList.Children[1].TranslatePoint(new Point(20, accounts.AccountList.Children[1].Bounds.Height - 10), accounts)!.Value, cancel: true);
        await Task.Delay(50);
        check(cancelledIndex > 0 && accounts.AccountList.Children.IndexOf(row) == 0 && !row.Dragging, "Escape cancels dragging without navigating away");
        var list = new SectionList();
        var obsolete = new FilledButton("Obsolete");
        list.Children.Add(obsolete); list.Children.Clear();
        check(!list.GetVisualChildren().Any() && obsolete.Parent == null, "Clearing painted sections detaches obsolete visual and logical children");
    }
    static int DragTo(AccountsDialog window, AccountEditorRow row, Point destination, bool cancel = false)
    {
        var position = row.Grip.TranslatePoint(new Point(15, 15), window)!.Value;
        var target = window.InputHitTest(position) as InputElement ?? throw new InvalidOperationException("No drag target");
        using var pointer = new Pointer(3, PointerType.Mouse, true);
        target.RaiseEvent(new PointerPressedEventArgs(target, pointer, window, position, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        if (pointer.Captured != row.Grip) throw new InvalidOperationException("Drag handle did not capture the pointer");
        row.Grip.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, row.Grip, pointer, window, destination, 1,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        int movedIndex = window.AccountList.Children.IndexOf(row);
        if (cancel) window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        else row.Grip.RaiseEvent(new PointerReleasedEventArgs(row.Grip, pointer, window, destination, 2,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        return movedIndex;
    }
    static void ClickAt(Window window, Control control, Point local)
    {
        var position = control.TranslatePoint(local, window)!.Value;
        var target = window.InputHitTest(position) as InputElement ?? throw new InvalidOperationException("No pointer target");
        using var pointer = new Pointer(1, PointerType.Mouse, true);
        target.RaiseEvent(new PointerPressedEventArgs(target, pointer, window, position, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        var released = pointer.Captured as InputElement ?? target;
        released.RaiseEvent(new PointerReleasedEventArgs(released, pointer, window, position, 1,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
    }
    static async Task Navigation(Action<bool, string> check, List<Reading> demo)
    {
        var panel = new UsagePanel { PreviewMode = true };
        panel.UpdateRows(demo, new Settings(), false); panel.OpenNearTray();
        await Task.Delay((int)Motion.FadeMilliseconds + 50);
        check(panel.ActualTransparencyLevel == WindowTransparencyLevel.Transparent, $"Overview supports native whole-frame fading (actual {panel.ActualTransparencyLevel})");
        panel.FrameOpacity = .5;
        using (var frame = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)panel.Width, (int)panel.Height), new Vector(96, 96)))
        {
            frame.Render(panel);
            string image = Path.Combine(Path.GetTempPath(), $"pace-half-opacity-{Guid.NewGuid():N}.png");
            frame.Save(image, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            using (var pixels = SkiaSharp.SKBitmap.Decode(image))
            {
                var alpha = pixels.GetPixel(3, 3).Alpha;
                check(alpha is >= 126 and <= 129, $"Half-opacity fades the entire painted frame uniformly (alpha {alpha})");
            }
            File.Delete(image);
        }
        panel.FrameOpacity = 1;
        var navigation = new PageNavigation(panel);
        var settingsIcon = ((Control)panel.Content!).GetVisualDescendants().OfType<IconButton>().Single(b => b.Kind == "settings");
        settingsIcon.Focus(NavigationMethod.Tab);
        check(settingsIcon.ShowsFocusOutline, "Overview footer retains focus cues for deliberate keyboard navigation");
        var page = new AccountsDialog(demo, new Settings(), () => { }, persist: () => { });
        navigation.Show(page);
        check(page.Position.Y == PopupPlacement.BottomRight(page.AnchorArea, new Size(page.Width, page.Height), page.RenderScaling).Y + Motion.SlideOffset(0, page.RenderScaling), "Page entry starts at the full slide offset before its first frame");
        await Task.Delay(60);
        check(page.IsVisible && page.FrameOpacity > .4 && page.FrameOpacity < 1 && panel.IsVisible, "Separate pages enter over the overview");
        check(panel.FrameOpacity == 1, "The overview stays opaque under the entering page so the desktop never shows through");
        var anchor = panel.AnchorArea;
        await Task.Delay((int)Motion.NavigationFadeMilliseconds + 50);
        check(!panel.IsVisible && page.IsVisible && page.AnchorArea == anchor, "Navigation leaves one visible page on the same monitor");
        page.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Task.Delay(60);
        check(page.IsActive, "Back keeps the outgoing page in front while the overview fades in behind it");
        settingsIcon.Focus(NavigationMethod.Unspecified);
        check(!settingsIcon.ShowsFocusOutline, "Closing Settings with Escape does not restore a footer outline");
        check(panel.IsVisible && page.IsVisible && page.FrameOpacity < 1, "Back overlaps the page exit and overview entrance");
        await Task.Delay((int)Motion.NavigationFadeMilliseconds + 50);
        check(navigation.ActivePage == null && panel.IsVisible, "Back completes without a stale active-page reference");
        var interrupted = new AccountsDialog(demo, new Settings(), () => { }, persist: () => { });
        navigation.Show(interrupted); await Task.Delay(60);
        interrupted.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Task.Delay(60);
        var interruptedAnchor = PopupPlacement.BottomRight(interrupted.AnchorArea, new Size(interrupted.Width, interrupted.Height), interrupted.RenderScaling);
        check(interrupted.Position.Y == interruptedAnchor.Y + Motion.SlideOffset(interrupted.FrameOpacity, interrupted.RenderScaling), "Reversing page entry uses the fixed anchor without adding a second slide offset");
        await Task.Delay((int)Motion.NavigationFadeMilliseconds + 50);
        settingsIcon.Focus(NavigationMethod.Tab);
        panel.Dismiss(); panel.OpenNearTray(false);
        settingsIcon.Focus(NavigationMethod.Unspecified);
        check(!settingsIcon.ShowsFocusOutline && !settingsIcon.SuppressFocusOutline, "Interrupted dismissal clears retained cues and restores normal footer focus behavior");
        var details = new AccountDetailsDialog(SampleData.Detail(demo[2]), new Settings());
        navigation.Show(details); await Task.Delay((int)Motion.NavigationFadeMilliseconds + 50);
        details.DismissAll(); await Task.Delay((int)Motion.FadeMilliseconds + 80);
        check(!panel.IsVisible && navigation.ActivePage == null, "Dismiss-all closes the session without reopening the overview");
        navigation.Shutdown(); panel.Shutdown();
    }
}
