param([string]$Model = 'qwen2.5:3b')

$ErrorActionPreference = 'Stop'
$bridge = Join-Path $PSScriptRoot 'village_backend\server.py'
$ollama = Get-Command ollama.exe -ErrorAction SilentlyContinue
if (-not $ollama) {
    Write-Host 'Ollama was not found. Install Ollama and reopen this terminal.' -ForegroundColor Red
    exit 1
}

# Prefer a real interpreter over the Windows Store python.exe alias.
$python = Join-Path $env:LOCALAPPDATA 'Python\pythoncore-3.14-64\python.exe'
if (-not (Test-Path -LiteralPath $python)) {
    $candidate = Get-Command python.exe -ErrorAction SilentlyContinue
    if (-not $candidate -or $candidate.Source -like '*\Microsoft\WindowsApps\*') {
        Write-Host 'Python 3 was not found. Install Python and reopen this terminal.' -ForegroundColor Red
        exit 1
    }
    $python = $candidate.Source
}

function Test-LocalPort([int]$Port) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $pending = $client.ConnectAsync('127.0.0.1', $Port)
        return $pending.Wait(500) -and $client.Connected
    } catch { return $false }
    finally { $client.Dispose() }
}

if (Test-LocalPort 8767) {
    Write-Host 'Port 8767 is already in use. If the conversation bridge is already running, return to the game and retry.' -ForegroundColor Yellow
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

Write-Host "Starting the village conversation bridge with $Model. Keep this window open while playing."
& $python -B $bridge --model $Model
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The conversation bridge stopped unexpectedly; read the error above.' -ForegroundColor Red
    exit $LASTEXITCODE
}
