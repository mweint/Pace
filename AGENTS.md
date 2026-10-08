# Pace contribution rules

Preserve the compact design and separate-window navigation unless requested.
Refactor for clear ownership, not arbitrary file-count targets.

- Keep startup small and application coordination separate from views.
- Use Palette for colors/fonts, UiMetrics for shared dimensions, Motion for
  animation and PaceMath for pace thresholds/labels.
- Never create fonts, RGB colors or unthemed tooltips in views/controls. Icon
  buttons and filled text buttons intentionally have different styles.
- Theme ownership covers all shared visual choices: semantic colors and control
  states in Palette, typography there, spacing/dimensions/borders/DPI in
  UiMetrics, and timing/easing in Motion. Use Palette.Warning for all warnings.
  Keep component field placement local; do not duplicate shared design values.
- Text uses Avalonia's layout (FormattedText, TextBlock). Never place glyphs by
  hand or reproduce metrics from the old WinForms/GDI version.
- Fades animate WidgetWindow.FrameOpacity, never Window.Opacity (which fades each
  painted surface separately and stays nearly opaque on screen).
- All account and limit sections use SectionStyle and SectionList (including
  AnimatedAccountList). The theme owns the flat section background and divider
  policy across overview, Accounts and details. Palette.InteractionSurface is
  for hover/selected controls only, never a section container. Views must not
  choose a different section surface or independently place dividers.
- AccountRowLayout owns account content bounds and preferred height. Lay out
  measured font rows sequentially with shared gaps; height is the final content
  bottom plus ContentInset. Never override account-row height in a view or
  position secondary rows relative to an old fixed card height. The same inset
  applies at the top, bottom, left and right of an account section.
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
