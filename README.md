# Pace

A compact tray app for tracking AI subscription usage across Codex and Claude accounts on Windows and Linux. See how your usage compares with the time elapsed in each account's weekly cycle.

![Weekly usage overview with two Codex accounts and one Claude account](docs/screenshots/overview.png)

## Download

**[Windows](https://github.com/mweint/Pace/releases/latest/download/Pace-win-x64.zip)** — Extract the ZIP and open `Pace.exe` in the `Pace` folder. Windows 10/11, x64; .NET is included.

**[Linux (preview)](https://github.com/mweint/Pace/releases/latest/download/Pace-linux-x64.tar.gz)** — Extract the archive and run `./Pace/install.sh`. Requires an x64 desktop with X11 libraries and ICU. Tray support works with KDE Plasma or GNOME's AppIndicator extension. Linux still needs validation on real desktops.

Existing CLI sign-ins are detected automatically. To add an account, install the corresponding Codex or Claude CLI and sign in through Pace. Click the tray icon to open the panel.

## Usage and accounts

- **Mint:** below pace. **Blue:** on pace. **Orange:** above pace. **Gray:** unavailable or stale.
- Pace compares usage with the elapsed portion of the week. On pace spans 4 percentage points below to 2 points above that baseline.
- Click an account for weekly, session and model limits. Reset times are local; hover for a countdown.
- Settings lets you rename, reorder or hide accounts, choose Claude's overview bars, and enable launch at sign-in. Removing an account keeps its sign-in files.
- Usage refreshes every five minutes. Update checks run at launch and every six hours; automatic updates are optional on Windows. Linux updates are downloaded manually.

<details>
<summary>More screenshots</summary>

Account details:

![Claude account details](docs/screenshots/account-details.png)

Accounts:

![Account management](docs/screenshots/accounts.png)

Settings:

![General settings](docs/screenshots/settings.png)

Tray hover:

![Tray hover summary](docs/screenshots/tray-hover.png)

</details>

## Privacy

Credentials stay local and official CLIs manage sign-in and renewal. Tokens are sent only to the corresponding service. Pace has no telemetry. Settings and managed sign-ins live in `%APPDATA%/Pace` on Windows and `~/.config/Pace` on Linux. Subscription endpoints are undocumented and may change.

<details>
<summary>Development</summary>

Built with Avalonia and .NET 10. See [STYLEGUIDE.txt](STYLEGUIDE.txt), [AGENTS.md](AGENTS.md) and the [Linux test checklist](docs/linux-test-checklist.md).

```powershell
dotnet build -c Release
.\bin\Release\net10.0\Pace.exe --self-test .local/self-test.json
.\bin\Release\net10.0\Pace.exe --render-preview .local/overview.png
```

Offline checks and previews require an interactive desktop. Previews use synthetic accounts. Before committing or publishing, inspect the index and run `pwsh -File scripts/Verify-Repository.ps1`.

</details>

## Assets

- [Lucide icons](https://github.com/lucide-icons/lucide/tree/main/icons): ISC/Feather MIT notices in `Assets/Icons`.
- [Inter 4.1](https://github.com/rsms/inter/releases/tag/v4.1), by Rasmus Andersson: Pace Sans is a renamed derivative; SIL Open Font License and modification notice in `Assets/Fonts`.
- Original provider artwork: attribution in `Assets/Icons/Service-marks.txt`.
