# Original Pace artwork, using the same semantic colors as the application.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$palette = [IO.File]::ReadAllText((Join-Path $repo 'Theme/Palette.cs'))
Add-Type -AssemblyName System.Drawing
function ThemeColor([string]$role) {
    $match = [regex]::Match($palette, '\b' + $role + '\s*=\s*Color\.From(?:Rgb|Argb)\((\d+),\s*(\d+),\s*(\d+)\)')
    if (!$match.Success) { throw "Missing theme color: $role" }
    return [Drawing.Color]::FromArgb([int]$match.Groups[1].Value, [int]$match.Groups[2].Value, [int]$match.Groups[3].Value)
}
$colors = @((ThemeColor 'Above'), (ThemeColor 'Below'), (ThemeColor 'TrayOnPace'))
$frames = @()
foreach ($size in @(16, 20, 24, 32, 48, 64, 128, 256)) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::Transparent)
        $left = [int][Math]::Round($size / 8)
        $height = [int][Math]::Round($size / 8)
        for ($index = 0; $index -lt 3; $index++) {
            $brush = [Drawing.SolidBrush]::new($colors[$index])
            try { $graphics.FillRectangle($brush, $left, [int][Math]::Round($size * (0.1875 + $index * 0.25)), $size - 2 * $left, $height) }
            finally { $brush.Dispose() }
        }
        $memory = [IO.MemoryStream]::new()
        try { $bitmap.Save($memory, [Drawing.Imaging.ImageFormat]::Png); $frames += ,@{ Size = $size; Bytes = $memory.ToArray() } }
        finally { $memory.Dispose() }
        if ($size -eq 128) {
            $preview = Join-Path $repo '.local/app-icon-preview.png'
            [IO.Directory]::CreateDirectory((Split-Path $preview -Parent)) | Out-Null
            $bitmap.Save($preview, [Drawing.Imaging.ImageFormat]::Png)
        }
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
$stream = [IO.File]::Create((Join-Path $repo 'Assets/Icons/pace.ico'))
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
}
finally { $writer.Dispose(); $stream.Dispose() }
