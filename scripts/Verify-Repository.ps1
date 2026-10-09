# Inspect the Git index without reading user configuration or printing secret values.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$files = @(git -C $repo ls-files --cached)
if ($LASTEXITCODE -ne 0 -or !$files.Count) { throw 'Stage intended files before auditing.' }
$issues = [System.Collections.Generic.List[string]]::new()
$allowed = @('.cs', '.csproj', '.md', '.txt', '.svg', '.png', '.ico', '.ttf', '.ps1', '.sh')
$metadata = @('.gitignore', '.gitattributes', '.editorconfig')
$patterns = @(
    '(?i)\bsk-(?:proj-|ant-)?[A-Za-z0-9_-]{20,}',
    '\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}',
    '(?i)BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY',
    '(?i)(?:access_token|refresh_token|api_key|client_secret)["'']?\s*[:=]\s*["''][A-Za-z0-9_=-]{16,}'
)
foreach ($file in $files) {
    $extension = [IO.Path]::GetExtension($file)
    if ($file -match '(?i)(^|/)(bin|obj|app|\.git)/|(^|/)\.env|(?:auth|credentials|settings|retry-times)\.json$' -or
        ($extension -notin $allowed -and $file -notin $metadata) -or
        ($extension -eq '.png' -and !$file.StartsWith('Assets/Icons/') -and !$file.StartsWith('docs/screenshots/'))) {
        $issues.Add("Unexpected or local-only file: $file")
        continue
    }
    if ($extension -in @('.png', '.ico', '.ttf')) { continue }
    $lines = @(git -C $repo show ":$file")
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect staged file: $file" }
    $text = $lines -join "`n"
    foreach ($pattern in $patterns) {
        if ($text -match $pattern) { $issues.Add("Possible secret in $file (value withheld)"); break }
    }
    if ($file -notmatch 'LICENSE|^scripts/Verify-Repository.ps1$' -and
        $text -match '(?i)[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}|[A-Z]:[\\/]Users[\\/]|/home/[^/\s]+') {
        $issues.Add("Possible identity or machine path in $file (value withheld)")
    }
    if ($extension -eq '.cs') {
        if ($lines.Count -gt 350) { $issues.Add("Review responsibility: $file exceeds 350 lines") }
        $view = $file -match '^(Application|Controls|Views)/'
        if ($view -and $text -match 'Color\.(?:From\w+|Parse)\s*\(|new\s+(?:Immutable)?SolidColorBrush\b|new\s+(?:FontFamily|Typeface|Pen)\s*\(\s*"|Brushes\.(?!Transparent)') {
            $issues.Add("Theme definition outside Theme: $file")
        }
        if (!$file.StartsWith('Theme/') -and $text -match 'new\s+ToolTip\b|:\s*ToolTip\b') {
            $issues.Add("Tooltip bypasses theme: $file")
        }
    }
}
if ($issues.Count) {
    $issues | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw 'Staged-file audit failed.'
}
Write-Output "PASS: $($files.Count) staged source/asset files; no local state, suspected secrets or theme violations."
