param(
 [string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
 [string]$Python='C:\Users\amogh\AppData\Local\Python\pythoncore-3.14-64\python.exe'
)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Verify-Nlp.ps1') -EditorRoot $EditorRoot -Python $Python
if($LASTEXITCODE -ne 0){throw 'Village conversation checks failed.'}
