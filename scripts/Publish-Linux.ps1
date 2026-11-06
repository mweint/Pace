# Builds dist/Pace-linux-x64.tar.gz: a self-contained build with a per-user installer.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Packaging.ps1')
$dist = Join-Path $repo 'dist'
$stage = Join-Path $dist 'linux-x64'
$archive = Join-Path $dist 'Pace-linux-x64.tar.gz'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
dotnet publish (Join-Path $repo 'Pace.csproj') -c Release -r linux-x64 --self-contained true -p:DebugType=None -o (Join-Path $stage 'app')
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-UnixText (Join-Path $stage 'install.sh') ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'install-linux.sh')))
# The menu icon is the 256px PNG frame of the application icon.
[IO.File]::WriteAllBytes((Join-Path $stage 'pace.png'), (Get-AppIconPng $repo))
Write-Tarball $stage $archive @('install.sh', 'app/Pace', 'app/createdump')
Write-Output "Created $archive"
