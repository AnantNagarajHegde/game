param(
 [string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
 [string]$Python='C:\Users\amogh\AppData\Local\Python\pythoncore-3.14-64\python.exe',
 [switch]$CompileOnly,
 [switch]$ServeMock,
 [switch]$NetworkChecks,
 [int]$Port=8765
)
$ErrorActionPreference='Stop'
if($ServeMock -or $NetworkChecks){throw 'The retired v1 mock/assessment service was removed. Use village_backend/benchmark.py for an optional real local-model check.'}
& (Join-Path $PSScriptRoot 'Verify-Sources.ps1') -EditorRoot $EditorRoot
if($LASTEXITCODE -ne 0){throw 'Unity source checks failed.'}
if(!$CompileOnly){
 Push-Location (Join-Path (Split-Path $PSScriptRoot -Parent) 'village_backend')
 try {& $Python -B -m unittest discover -v -p 'test*.py'; if($LASTEXITCODE -ne 0){throw 'Village model backend checks failed.'}}
 finally {Pop-Location}
}
Write-Output 'VILLAGE_MODEL_SOURCE_CHECKS_OK (no model, sockets or Unity playtest)'
