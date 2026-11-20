param([string]$OutputDirectory, [switch]$NoLaunch)
$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'dist'
$localOutput = Join-Path $PSScriptRoot '.build-output'
if (Test-Path $localOutput) {
    $out = (Get-Content -LiteralPath $localOutput -Raw).Trim()
}
$installed = (Test-Path $localOutput) -and !$OutputDirectory
if ($OutputDirectory) { $out = $OutputDirectory }
New-Item -ItemType Directory -Force $out | Out-Null
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('WPF\PresentationFramework.dll','WPF\PresentationCore.dll','WPF\WindowsBase.dll','System.Xaml.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Core.dll','System.dll') | ForEach-Object { '/reference:'+(Join-Path $framework $_) }
$target = Join-Path $out 'EssentialDesktop.exe'
$staging = New-Item -ItemType Directory -Path (Join-Path $out ('.build-' + [guid]::NewGuid().ToString('N')))
$stage = Join-Path $staging.FullName 'EssentialDesktop.exe'
& "$framework\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /main:EssentialDesktopEntry "/win32icon:$PSScriptRoot\src\EssentialDesktop.ico" "/out:$stage" @refs "$PSScriptRoot\src\EssentialDesktop.cs" "$PSScriptRoot\src\Voice.cs"
if($LASTEXITCODE -ne 0){throw 'Desktop build failed'}
if ($installed) {
    $quit = $null
    try {
        $canSignal = [Threading.EventWaitHandle]::TryOpenExisting('Local\EssentialDesktopUpdateQuit', [ref]$quit)
    } catch [UnauthorizedAccessException] {
        # The interactive instance will handle the version handoff when opened.
        $canSignal = $false
    }
    if ($canSignal) {
        try { [void]$quit.Set() } finally { $quit.Dispose() }
        $instance = $null
        if ([Threading.Mutex]::TryOpenExisting('Local\SuperDeck_SingleInstance_9f21', [ref]$instance)) {
            try {
                if (!$instance.WaitOne(10000)) { throw 'The previous launcher is still closing.' }
                $instance.ReleaseMutex()
            } finally { $instance.Dispose() }
        }
    }
}
if (Test-Path -LiteralPath $target) {
    $previous = Join-Path $out ('.previous-' + [guid]::NewGuid().ToString('N'))
    [IO.File]::Replace($stage, $target, $previous)
    Remove-Item -LiteralPath $previous -ErrorAction SilentlyContinue
} else {
    [IO.File]::Move($stage, $target)
}
Remove-Item -LiteralPath $staging.FullName
if ($installed) { [IO.File]::WriteAllText((Join-Path $out '.installed'), '') }
Copy-Item "$PSScriptRoot\Setup-Dictation.ps1","$PSScriptRoot\README.md","$PSScriptRoot\THIRD-PARTY.md" $out -Force
Write-Output "Built $out"
if ($installed -and !$NoLaunch) { Start-Process -FilePath $target -WorkingDirectory $out }
