param(
    [switch]$Prepare, [switch]$Build, [switch]$Benchmark,
    [string]$Python = 'python', [string]$Engine,
    [string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$cache = Join-Path $PSScriptRoot 'verification\Temp\TeaVoice'
$project = Join-Path $PSScriptRoot 'My project (1)'
$venv = Join-Path $cache 'venv'
$localPython = Join-Path $venv 'Scripts\python.exe'
function Invoke-Checked($exe, [string[]]$parameters) {
    & $exe @parameters
    if ($LASTEXITCODE -ne 0) { throw "$exe exited with $LASTEXITCODE" }
}
function Get-Asset($url, $name, $sha) {
    $file = Join-Path $cache $name
    if ((Test-Path -LiteralPath $file) -and (Get-FileHash -LiteralPath $file).Hash -eq $sha) { return }
    Invoke-Checked 'curl.exe' @('-fsSL','--retry','2',$url,'-o',"$file.download")
    if ((Get-FileHash -LiteralPath "$file.download").Hash -ne $sha) { throw "Checksum failed for $name" }
    Move-Item -LiteralPath "$file.download" -Destination $file -Force
}
if ($Prepare) {
    New-Item -ItemType Directory -Force -Path $cache | Out-Null
    if (!(Test-Path -LiteralPath $localPython)) { Invoke-Checked $Python @('-m','venv',$venv) }
    Invoke-Checked $localPython @('-m','pip','install','numpy','onnxruntime==1.30.0','onnx==1.23.2','psutil','pandas')
    $speech = 'https://huggingface.co/onnx-community/mms-tts-kan-ONNX/resolve/cac3976902fe64861618432d00337d19e8c2ec61'
    Get-Asset "$speech/onnx/model.onnx" 'model.onnx' 'e0365f4af5056ceb112c2cd410287606c540393a146351026637ed4d388adf75'
    Get-Asset "$speech/vocab.json" 'vocab.json' '56e00b687cf6c32bc603e055fe142351c16a136584bae988f333b9dc54c0c3ce'
    Get-Asset 'https://huggingface.co/ggml-org/gemma-3-270m-it-GGUF/resolve/e7647be17ae1108f2f605ed061ca0608b171afff/gemma-3-270m-it-Q8_0.gguf' 'text.gguf' '0ef57d2c838458a1952664260dcba38e5bdda37494f3af732f06e4add24068e3'
    Get-Asset 'https://github.com/ggml-org/llama.cpp/releases/download/b7400/llama-b7400-bin-win-cpu-x64.zip' 'llama.zip' 'ab2fdce9d3cc88e1e810bd3aaea3abfe6f528e9c4daa91b4f5d964e92e0c6611'
    Expand-Archive -LiteralPath "$cache\llama.zip" -DestinationPath "$cache\llama" -Force
    Invoke-Checked $localPython @('-B',"$PSScriptRoot\verification\PrepareTeaVoiceModel.py",$cache)
}
if ($Build) {
    New-Item -ItemType Directory -Force -Path $cache | Out-Null
    $p = Start-Process $Editor -ArgumentList ('-batchmode -quit -projectPath "'+$project+'" -buildWindows64Player "'+$cache+'\Player\TeaVoice.exe" -logFile "'+$cache+'\build.log"') -WindowStyle Hidden -PassThru -Wait
    if ($p.ExitCode -ne 0 -or !(Select-String -Quiet -LiteralPath "$cache\build.log" -Pattern 'Build Finished, Result: Success')) { throw "Build failed. See $cache\build.log" }
}
if (Test-Path -LiteralPath $localPython) { $Python = $localPython }
$params = @('-B',"$PSScriptRoot\verification\TeaVoiceLaptop.py")
if ($Benchmark) {
    if (!$Engine) { $Engine = Join-Path (Split-Path $PSScriptRoot -Parent) 'paridhwani\language-engine\Paridhwani-NLP\src\assessment_host.py' }
    if (!(Test-Path -LiteralPath $Engine)) { throw 'Supply -Engine with the path to assessment_host.py' }
    $params += @('--editor',$Editor,'--engine',$Engine)
} else { $params += '--demo' }
Invoke-Checked $Python $params
