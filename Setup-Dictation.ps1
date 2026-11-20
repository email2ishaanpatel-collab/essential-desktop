param([string]$Destination=$PSScriptRoot)
$ErrorActionPreference='Stop'
$voice=Join-Path $Destination 'voice'
$scratch=Join-Path ([IO.Path]::GetTempPath()) ('essential-setup-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $voice,$scratch | Out-Null
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$zip=Join-Path $scratch 'whisper.zip'
Invoke-WebRequest 'https://github.com/ggml-org/whisper.cpp/releases/download/v1.9.2/whisper-bin-x64.zip' -OutFile $zip
if((Get-FileHash $zip -Algorithm SHA256).Hash -ne '49DCC16DE826F20BD53D44F947A1AE49DFA81F86CAD67A64D80820CB192D674A'){throw 'Whisper checksum mismatch'}
Expand-Archive $zip (Join-Path $scratch 'engine')
$bin=Join-Path $scratch 'engine\Release'
Copy-Item "$bin\whisper-cli.exe","$bin\*.dll" $voice -Force
$models = @{
    'tiny.en' = '921E4CF8686FDD993DCD081A5DA5B6C365BFDE1162E72B08D75AC75289920B1F'
    'base.en' = 'A03779C86DF3323075F5E796CB2CE5029F00EC8869EEE3FDFB897AFE36C6D002'
}
foreach ($name in $models.Keys) {
    $file = "ggml-$name.bin"
    $installed = Join-Path $voice $file
    if ((Test-Path $installed) -and (Get-FileHash $installed).Hash -eq $models[$name]) { continue }
    $download = Join-Path $scratch $file
    Invoke-WebRequest "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/$file" -OutFile $download
    if ((Get-FileHash $download).Hash -ne $models[$name]) { throw "Model checksum mismatch for $file" }
    Copy-Item $download $installed -Force
}
Invoke-WebRequest 'https://raw.githubusercontent.com/ggml-org/whisper.cpp/v1.9.2/LICENSE' -OutFile (Join-Path $voice 'LICENSE-whisper.txt')
Invoke-WebRequest 'https://raw.githubusercontent.com/openai/whisper/main/LICENSE' -OutFile (Join-Path $voice 'LICENSE-model.txt')
Write-Output 'Offline English dictation is ready. Restart Essential Desktop.'
