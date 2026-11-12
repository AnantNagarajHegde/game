param(
    [ValidateSet('ollama','openrouter','groq')][string]$Provider = 'groq',
    [string]$Model = '',
    [ValidateRange(1,65535)][int]$Port = 8767,
    [switch]$CheckConfiguration,
    [switch]$CheckProvider
)

$ErrorActionPreference = 'Stop'
if ($CheckConfiguration -and $CheckProvider) { throw 'Choose either -CheckConfiguration or -CheckProvider.' }
$bridge = Join-Path $PSScriptRoot 'village_backend\server.py'
if (-not $Model) { $Model = if ($Provider -eq 'ollama') { 'gemma3:4b' } elseif ($Provider -eq 'groq') { 'qwen/qwen3.8-27b' } else { $env:PARIDHWANI_MODEL } }

if ($Provider -eq 'groq') {
    $groqUserKey = [Environment]::GetEnvironmentVariable('GROQ_API_KEY', [EnvironmentVariableTarget]::User)
    if (-not [string]::IsNullOrWhiteSpace($groqUserKey)) {
        $env:GROQ_API_KEY = $groqUserKey.Trim()
    }
    $groqUserKey = $null
}

# Prefer a real interpreter over the Windows Store python.exe alias.
$python = Join-Path $env:LOCALAPPDATA 'Python\pythoncore-3.14-64\python.exe'
if (-not (Test-Path -LiteralPath $python)) {
    $candidate = Get-Command python3.14.exe,python3.exe,python.exe -All -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -and $_.Source -notlike '*\Microsoft\WindowsApps\*' } | Select-Object -First 1
    if (-not $candidate) {
        Write-Host 'Python 3 was not found. Install Python and reopen this terminal.' -ForegroundColor Red
        exit 1
    }
    $python = $candidate.Source
}

if ($CheckConfiguration) {
    & $python -B $bridge --provider $Provider --model $Model --check
    exit $LASTEXITCODE
}
if ($CheckProvider) {
    & $python -B $bridge --provider $Provider --model $Model --check-provider
    exit $LASTEXITCODE
}
if ($Provider -eq 'openrouter' -and (-not $Model -or -not $env:OPENROUTER_API_KEY)) {
    Write-Host 'Set OPENROUTER_API_KEY and PARIDHWANI_MODEL, or pass -Model with your hosted model ID.' -ForegroundColor Red
    Write-Host 'Run this script with -CheckConfiguration to inspect setup without making provider calls.'
    exit 1
}
if ($Provider -eq 'groq' -and (-not $Model -or [string]::IsNullOrWhiteSpace($env:GROQ_API_KEY))) {
    Write-Host 'Set GROQ_API_KEY in Windows user variables or this terminal before starting Groq.' -ForegroundColor Red
    exit 1
}

function Test-LocalPort([int]$Port) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $pending = $client.ConnectAsync('127.0.0.1', $Port)
        return $pending.Wait(500) -and $client.Connected
    } catch { return $false }
    finally { $client.Dispose() }
}

if (Test-LocalPort $Port) {
    try {
        $running = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/health" -TimeoutSec 2
        if ($running.version -eq 2 -and $running.provider -in @('ollama','openrouter','groq')) {
            if ($running.provider -ne $Provider) {
                Write-Host "Port $Port is occupied by a $($running.provider) bridge. Stop that bridge before starting $Provider." -ForegroundColor Yellow
            } else {
                Write-Host "A $Provider bridge is already running on port $Port. Stop it with Ctrl+C in its terminal before restarting after code or key changes." -ForegroundColor Yellow
                if (-not $running.model_configured) { Write-Host 'The running bridge reports missing model configuration.' -ForegroundColor Yellow }
            }
        } else { Write-Host "Port $Port is in use by an incompatible service. The requested bridge was not started." -ForegroundColor Yellow }
    } catch { Write-Host "Port $Port is in use, but the conversation health check failed. The requested bridge was not started." -ForegroundColor Yellow }
    exit 1
}
if ($Provider -eq 'ollama') {
$ollama = Get-Command ollama.exe -ErrorAction SilentlyContinue
if (-not $ollama) {
    Write-Host 'Ollama was not found. Install Ollama and reopen this terminal.' -ForegroundColor Red
    exit 1
}
if (-not (Test-LocalPort 11434)) {
    Write-Host 'Starting Ollama...'
    Start-Process -FilePath $ollama.Source -ArgumentList 'serve' -WindowStyle Hidden
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if (Test-LocalPort 11434) { $ready = $true; break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) {
        Write-Host 'Ollama did not start. Open Ollama and check that it is running, then try again.' -ForegroundColor Red
        exit 1
    }
}

$models = & $ollama.Source list 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Could not query Ollama. Check that it is running, then retry.' -ForegroundColor Red
    exit 1
}
$names = @($models | Select-Object -Skip 1 | ForEach-Object { ($_ -split '\s+')[0] })
if ($Model -notin $names) {
    Write-Host "Model '$Model' is not installed. Install it with: ollama pull $Model" -ForegroundColor Yellow
    Write-Host 'Or run this script with -Model and the name of an installed model.'
    exit 1
}
}

Write-Host "Starting the village conversation bridge with $Provider / $Model. Keep this window open while playing."
if (-not $env:AZURE_SPEECH_KEY -or -not $env:AZURE_SPEECH_REGION) {
    Write-Host 'NPC speech is not configured. Set AZURE_SPEECH_KEY and AZURE_SPEECH_REGION to enable spoken replies.' -ForegroundColor Yellow
}
& $python -B $bridge --provider $Provider --model $Model --port $Port
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The conversation bridge stopped unexpectedly; read the error above.' -ForegroundColor Red
    exit $LASTEXITCODE
}
