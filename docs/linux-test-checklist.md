# Linux desktop test checklist

Run on at least one GNOME desktop (Ubuntu, which includes the AppIndicator extension)
and one KDE Plasma 6 desktop. Note for each run: distro, desktop and version, session
type (`echo $XDG_SESSION_TYPE`), display scaling and where the panel/taskbar sits.

## Setup

1. Extract `Pace-linux-x64.tar.gz` and run `./Pace/install.sh`.
2. If Pace does not start, install the missing libraries and retry:
   - Ubuntu/Debian: `sudo apt install libice6 libsm6 libfontconfig1` plus your release's ICU (`apt search ^libicu[0-9]`)
   - Arch/CachyOS: `sudo pacman -S --needed icu libice libsm fontconfig`
   - GNOME without a tray (Fedora, Arch): install and enable the AppIndicator extension.
3. Install Claude Code and/or the Codex CLI if you want to test sign-in.

## Checks

| # | Do this | Expect |
|---|---------|--------|
| 1 | Open Pace from the applications menu | Tray icon appears and the panel opens once |
| 2 | Left-click the tray icon, then click it again | Panel opens, then closes. On GNOME, report if a menu opens instead |
| 3 | Look at where the panel opens | Against the top bar or panel, in the corner where the tray sits (GNOME top-right, KDE bottom-right). Not centred or under the bar |
| 4 | Move the panel to another edge (KDE), log out and back in, open Pace | Panel opens against the new edge |
| 5 | With the panel open, click elsewhere; reopen and press Esc | Closes both times. Report if it stays open |
| 6 | Right-click the tray icon: Show Pace, Refresh, Settings…, Quit | Each works; Quit ends the process (`pgrep -x Pace` prints nothing) |
| 7 | Hover the tray icon | Tooltip lists accounts (GNOME may show nothing; note it) |
| 8 | Accounts: add a Claude and a Codex account | Browser opens; after signing in, the account appears in the panel |
| 9 | Add an account, then close the browser without signing in | Report how long "Complete sign-in…" stays and whether it recovers |
| 10 | With existing CLI sign-ins (`~/.claude`, `~/.codex`) | Accounts appear without adding them |
| 11 | Settings → Launch at sign-in on, log out and in | Pace starts once with a tray icon. `~/.config/autostart/pace.desktop` exists; turning it off removes it |
| 12 | Launch Pace again while it is running (menu and terminal) | No second tray icon or process |
| 13 | Open Settings and account details; switch pages | Pages open beside the panel; fades are smooth; text is crisp |
| 14 | Repeat 3 and 13 at 125% or 150% scaling | Correct size and sharp text. If wrong, retry with `AVALONIA_GLOBAL_SCALE_FACTOR=1.5 ~/.local/share/pace/Pace` and report both |
| 15 | Two monitors, panel on the secondary | Note which screen the panel opens on (primary is a known limitation) |
| 16 | Run `install.sh` again while Pace is running | Pace restarts; settings and accounts are kept |
| 17 | `./Pace/install.sh --uninstall` | App and menu entry removed; `~/.config/Pace` kept |

## Report back

Pass/fail per check, a screenshot for checks 3, 13 and 14, and `~/.config/Pace/error.log`
if it exists. Never send files from `~/.config/Pace/accounts`, `~/.claude` or `~/.codex`:
they contain sign-in credentials.
