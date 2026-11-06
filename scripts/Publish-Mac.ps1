# Builds dist/Pace-osx-arm64.tar.gz (Apple silicon) and dist/Pace-osx-x64.tar.gz (Intel): a
# self-contained Pace.app with a per-user installer. The app is not notarized; install.sh
# clears the download quarantine and signs the bundle ad hoc on the Mac.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Packaging.ps1')
$dist = Join-Path $repo 'dist'
$version = (Select-Xml -Path (Join-Path $repo 'Pace.csproj') -XPath '//Version').Node.InnerText
# Matches StartupRegistration.LaunchAgentLabel. The minimum system is the one the .NET runtime
# binaries are built for; .NET 10 itself supports macOS 15 and later.
$bundleId = 'io.github.mweint.pace'
$plist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>Pace</string>
    <key>CFBundleExecutable</key>
    <string>Pace</string>
    <key>CFBundleIconFile</key>
    <string>Pace</string>
    <key>CFBundleIdentifier</key>
    <string>$bundleId</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Pace</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$version</string>
    <key>CFBundleVersion</key>
    <string>$version</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>LSUIElement</key>
    <true/>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
"@

# An icon family with one 256px PNG image ('ic08'); lengths are big-endian.
function Get-Icns([byte[]]$png) {
    function BigEndian([int]$value) { $bytes = [BitConverter]::GetBytes($value); [Array]::Reverse($bytes); return , $bytes }
    $stream = New-Object IO.MemoryStream
    $stream.Write([Text.Encoding]::ASCII.GetBytes('icns'), 0, 4); $stream.Write((BigEndian (16 + $png.Length)), 0, 4)
    $stream.Write([Text.Encoding]::ASCII.GetBytes('ic08'), 0, 4); $stream.Write((BigEndian (8 + $png.Length)), 0, 4)
    $stream.Write($png, 0, $png.Length)
    return , $stream.ToArray()
}
$icns = Get-Icns (Get-AppIconPng $repo)
$installer = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'install-macos.sh'))

foreach ($runtime in 'osx-arm64', 'osx-x64') {
    $stage = Join-Path $dist $runtime
    $contents = Join-Path $stage 'Pace.app/Contents'
    $archive = Join-Path $dist "Pace-$runtime.tar.gz"
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
    dotnet publish (Join-Path $repo 'Pace.csproj') -c Release -r $runtime --self-contained true -p:DebugType=None -o (Join-Path $contents 'MacOS')
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $runtime." }
    Write-UnixText (Join-Path $contents 'Info.plist') $plist
    New-Item -ItemType Directory -Force (Join-Path $contents 'Resources') | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $contents 'Resources/Pace.icns'), $icns)
    Write-UnixText (Join-Path $stage 'install.sh') $installer
    Write-Tarball $stage $archive @('install.sh', 'Pace.app/Contents/MacOS/Pace', 'Pace.app/Contents/MacOS/createdump')
    Write-Output "Created $archive"
}
