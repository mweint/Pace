# Builds dist/Pace-linux-x64.tar.gz: a self-contained build with a per-user installer.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $repo 'dist'
$stage = Join-Path $dist 'linux-x64'
$archive = Join-Path $dist 'Pace-linux-x64.tar.gz'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
dotnet publish (Join-Path $repo 'Pace.csproj') -c Release -r linux-x64 --self-contained true -p:DebugType=None -o (Join-Path $stage 'app')
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

# The installer must keep LF line endings whatever the checkout uses.
$installer = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'install-linux.sh')).Replace("`r`n", "`n")
[IO.File]::WriteAllText((Join-Path $stage 'install.sh'), $installer)

# The menu icon is the 256px PNG frame of the application icon.
$ico = [IO.File]::ReadAllBytes((Join-Path $repo 'Assets/Icons/pace.ico'))
$count = [BitConverter]::ToUInt16($ico, 4)
$frame = 0..($count - 1) | ForEach-Object { 6 + 16 * $_ } | Where-Object { $ico[$_] -eq 0 } | Select-Object -First 1
if ($null -eq $frame) { throw 'pace.ico has no 256px frame.' }
$length = [BitConverter]::ToInt32($ico, $frame + 8); $offset = [BitConverter]::ToInt32($ico, $frame + 12)
[IO.File]::WriteAllBytes((Join-Path $stage 'pace.png'), $ico[$offset..($offset + $length - 1)])

# A plain ustar archive, written directly so Unix permissions survive packaging on Windows.
function Write-TarField([byte[]]$header, [int]$offset, [int]$length, [string]$value) {
    $bytes = [Text.Encoding]::ASCII.GetBytes($value)
    if ($bytes.Length -gt $length) { throw "Tar field too long: $value" }
    [Array]::Copy($bytes, 0, $header, $offset, $bytes.Length)
}
function Write-TarOctal([byte[]]$header, [int]$offset, [int]$length, [long]$value) {
    Write-TarField $header $offset $length ([Convert]::ToString($value, 8).PadLeft($length - 1, '0'))
}
if (Test-Path $archive) { Remove-Item $archive }
$output = [IO.File]::Create($archive)
try {
    $gzip = New-Object IO.Compression.GZipStream($output, [IO.Compression.CompressionLevel]::Optimal)
    try {
        foreach ($file in Get-ChildItem $stage -Recurse -File | Sort-Object FullName) {
            $relative = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
            $name = "Pace/$relative"
            $header = New-Object byte[] 512
            if ($name.Length -le 100) { Write-TarField $header 0 100 $name }
            else { $split = $name.LastIndexOf('/', 155); Write-TarField $header 345 155 $name.Substring(0, $split); Write-TarField $header 0 100 $name.Substring($split + 1) }
            $mode = if ($relative -in @('install.sh', 'app/Pace', 'app/createdump')) { 493 } else { 420 }
            Write-TarOctal $header 100 8 $mode
            Write-TarOctal $header 108 8 0; Write-TarOctal $header 116 8 0
            Write-TarOctal $header 124 12 $file.Length
            Write-TarOctal $header 136 12 ([DateTimeOffset]$file.LastWriteTimeUtc).ToUnixTimeSeconds()
            $header[156] = [byte][char]'0'
            Write-TarField $header 257 6 "ustar"; Write-TarField $header 263 2 "00"
            for ($i = 148; $i -lt 156; $i++) { $header[$i] = 32 }
            $sum = 0; foreach ($b in $header) { $sum += $b }
            Write-TarField $header 148 8 ([Convert]::ToString($sum, 8).PadLeft(6, '0') + "`0 ")
            $gzip.Write($header, 0, 512)
            $content = [IO.File]::ReadAllBytes($file.FullName)
            $gzip.Write($content, 0, $content.Length)
            $padding = (512 - $content.Length % 512) % 512
            if ($padding) { $gzip.Write((New-Object byte[] $padding), 0, $padding) }
        }
        $gzip.Write((New-Object byte[] 1024), 0, 1024)
    }
    finally { $gzip.Dispose() }
}
finally { $output.Dispose() }
Write-Output "Created $archive"
