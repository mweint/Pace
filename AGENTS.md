# Pace contribution rules

Preserve the compact design and separate-window navigation unless requested.
Refactor for clear ownership, not arbitrary file-count targets.

- Keep startup small and application coordination separate from views.
- Use Palette for colors/fonts, UiMetrics for shared dimensions, Motion for
  animation and PaceMath for pace thresholds/labels.
- Never create fonts, RGB colors or unthemed tooltips in views/controls. Icon
  buttons and filled text buttons intentionally have different styles.
- PageDialog owns common placement, animation, dismissal and focus. Derived
  dialogs own content; keep native windows separate.
- Prefer focused methods and cohesive files. Keep field geometry local.
- Credentials stay local; official CLIs manage them. Never log tokens, login
  output, raw authenticated responses, identities or environment values.
  Preserve settings and sign-ins.
- Build with dotnet build -c Release. Run offline --self-test on an interactive
  Windows desktop after logic/lifecycle changes; render visual changes.
- Before commit/push, inspect the index and run
  pwsh -File scripts/Verify-Repository.ps1. Commit source and required assets only;
  exclude environments, credentials, settings, previews, reports and builds.
- Preserve asset licenses and font modification notices.
