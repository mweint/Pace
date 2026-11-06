# macOS test checklist

Pace for macOS is built on Windows and has not yet run on a real Mac. Run these checks on
Apple silicon and, if possible, an Intel Mac. Note for each run: macOS version, chip,
display scaling (System Settings → Displays), whether the Dock auto-hides, and how Claude
Code and Codex were installed (native installer, Homebrew or npm).

## Setup

1. Download `Pace-osx-arm64.tar.gz` (Apple silicon) or `Pace-osx-x64.tar.gz` (Intel) and
   open it in Finder.
2. In Terminal, run `sh ~/Downloads/Pace/install.sh`. Report any warning it prints.
3. If Pace does not start, run `~/Applications/Pace.app/Contents/MacOS/Pace` in Terminal
   and send the output.
4. Install Claude Code and/or the Codex CLI if you want to test sign-in.

## Checks

| # | Do this | Expect |
|---|---------|--------|
| 1 | Run the installer | Pace opens: an icon in the menu bar and the panel just below the menu bar at the top right. No Dock icon |
| 2 | Click the menu bar icon | A menu with Show Pace, Settings… and Quit. Show Pace opens the panel below the menu bar |
| 3 | Look at the panel's size and position at 1x and Retina scaling | Panel width matches the screenshots in the README and it fits the screen. Report if it is offset to the left, half height or blurry |
| 4 | With the panel open, click elsewhere; reopen and press Esc | Closes both times |
| 5 | Hover the menu bar icon | Tooltip lists accounts and their pace |
| 6 | Check the icon in a light and a dark menu bar | Coloured bars are sharp and visible in both |
| 7 | With Claude Code already signed in (`claude` in Terminal) | The Claude account appears without adding it. Report any Keychain prompt with its exact text |
| 8 | With the Codex CLI already signed in (`~/.codex/auth.json`) | The Codex account appears without adding it |
| 9 | Accounts: add a Claude and a Codex account | Browser opens; after signing in, the account appears in the panel |
| 10 | Add an account, then close the browser without signing in; press Cancel | Cancel returns to the add buttons and no account is added. Copy link copies a working sign-in link |
| 11 | Keep Pace running for a day with a Claude account | Readings keep updating after the Claude sign-in's token expires (Pace renews it with `claude doctor`) |
| 12 | Settings → Launch at sign-in on, then log out and in | Pace starts once. `~/Library/LaunchAgents/io.github.mweint.pace.plist` exists; turning it off removes it. macOS may show a Background Items notice |
| 13 | Open `~/Applications/Pace.app` again while Pace runs, with the panel closed | The running copy's panel opens; no second icon or process (`pgrep -x Pace` prints one id) |
| 14 | Open Settings and account details; switch pages | Pages open where the panel was; fades are smooth; text is crisp |
| 15 | Two displays | Panel opens below the menu bar of the main display |
| 16 | Run `install.sh` again while Pace is running | Pace restarts; settings and accounts are kept |
| 17 | `sh ~/Downloads/Pace/install.sh --uninstall` | App and launch agent removed; `~/Library/Application Support/Pace` kept |

## Report back

Pass/fail per check, a screenshot for checks 1, 3 and 14, and
`~/Library/Application Support/Pace/error.log` if it exists. Never send files from
`~/Library/Application Support/Pace/accounts`, `~/.claude` or `~/.codex`, or Keychain
items: they contain sign-in credentials.
