param([string]$Source, [string]$Destination, [int]$ProcessId, [string]$Marker)
$ErrorActionPreference = 'Stop'
$paceProcess = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
if ($paceProcess -and !$paceProcess.WaitForExit(30000)) { exit 1 }
$paceBackup = Join-Path (Split-Path $Marker) ('backup-' + [Guid]::NewGuid().ToString('N'))
$paceCopied = [Collections.Generic.List[string]]::new()
try {
    foreach ($paceFile in Get-ChildItem -LiteralPath $Source -File -Recurse) {
        $paceRelative = $paceFile.FullName.Substring($Source.TrimEnd('\').Length + 1)
        $paceTarget = Join-Path $Destination $paceRelative
        if (Test-Path -LiteralPath $paceTarget) {
            $paceOriginal = Join-Path $paceBackup $paceRelative
            New-Item -ItemType Directory -Path (Split-Path $paceOriginal) -Force | Out-Null
            Copy-Item -LiteralPath $paceTarget -Destination $paceOriginal
        }
        New-Item -ItemType Directory -Path (Split-Path $paceTarget) -Force | Out-Null
        $paceCopied.Add($paceRelative)
        Copy-Item -LiteralPath $paceFile.FullName -Destination $paceTarget -Force
    }
    Remove-Item -LiteralPath $Marker
}
catch {
    foreach ($paceRelative in $paceCopied) {
        $paceTarget = Join-Path $Destination $paceRelative
        $paceOriginal = Join-Path $paceBackup $paceRelative
        if (Test-Path -LiteralPath $paceOriginal) { Copy-Item -LiteralPath $paceOriginal -Destination $paceTarget -Force }
        elseif (Test-Path -LiteralPath $paceTarget) { Remove-Item -LiteralPath $paceTarget }
    }
    # Clear the pending marker so a failed update cannot loop on every launch.
    if (Test-Path -LiteralPath $Marker) { Remove-Item -LiteralPath $Marker }
}
finally {
    # The backup is only needed until the copy succeeds or is rolled back; the payload is consumed.
    foreach ($paceTemporary in @($paceBackup, $Source)) {
        if (Test-Path -LiteralPath $paceTemporary) { Remove-Item -LiteralPath $paceTemporary -Recurse -Force -ErrorAction SilentlyContinue }
    }
}
Start-Process -FilePath (Join-Path $Destination 'Pace.exe') -WorkingDirectory $Destination -WindowStyle Hidden
