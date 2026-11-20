$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'dist'
$localOutput = Join-Path $PSScriptRoot '.build-output'
if (Test-Path $localOutput) {
    $out = (Get-Content -LiteralPath $localOutput -Raw).Trim()
}
New-Item -ItemType Directory -Force $out | Out-Null
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('WPF\PresentationFramework.dll','WPF\PresentationCore.dll','WPF\WindowsBase.dll','System.Xaml.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Core.dll','System.dll') | ForEach-Object { '/reference:'+(Join-Path $framework $_) }
& "$framework\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /main:EssentialDesktopEntry "/out:$out\EssentialDesktop.exe" @refs "$PSScriptRoot\src\EssentialDesktop.cs" "$PSScriptRoot\src\Voice.cs"
if($LASTEXITCODE -ne 0){throw 'Desktop build failed'}
Copy-Item "$PSScriptRoot\Setup-Dictation.ps1","$PSScriptRoot\README.md","$PSScriptRoot\THIRD-PARTY.md" $out -Force
Write-Output "Built $out"
