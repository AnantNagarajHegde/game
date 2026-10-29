param([string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference='Stop'
$workspace=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$nlp=Join-Path $workspace 'language-engine\Paridhwani-NLP'
$python='C:\Users\amogh\AppData\Local\Python\pythoncore-3.14-64\python.exe'
& $python -I -B (Join-Path $nlp 'verification\tea-milestone\generate.py')
if($LASTEXITCODE -ne 0){throw 'Evaluator fixtures failed'}
& (Join-Path $PSScriptRoot 'Verify-Nlp.ps1') -EditorRoot $EditorRoot
if($LASTEXITCODE -ne 0){throw 'Source checks failed'}
$project=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
$out=Join-Path $PSScriptRoot 'Temp\NlpVerification'
$dotnet=Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
$csc=(Get-ChildItem (Join-Path $EditorRoot 'Data\DotNetSdk') -Recurse -Filter csc.dll | Select-Object -First 1).FullName
$compileArgs=Get-Content (Join-Path $out 'DialogueHttpChecks.rsp') | Where-Object {$_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^-out:'}
$compileArgs+=@('-out:"'+(Join-Path $out 'TeaAssessmentChecks.dll')+'"','"'+(Join-Path $PSScriptRoot 'TeaAssessmentChecks.cs')+'"')
$rsp=Join-Path $out 'TeaAssessmentChecks.rsp';$compileArgs | Set-Content -LiteralPath $rsp -Encoding utf8
Push-Location $project
try{& $dotnet $csc "@$rsp";if($LASTEXITCODE -ne 0){throw 'Assessment checks compilation failed'}}finally{Pop-Location}
Copy-Item -LiteralPath (Join-Path $out 'DialogueHttpChecks.runtimeconfig.json') -Destination (Join-Path $out 'TeaAssessmentChecks.runtimeconfig.json') -Force
& $dotnet (Join-Path $out 'TeaAssessmentChecks.dll') (Join-Path $nlp 'verification\tea-milestone\results')
if($LASTEXITCODE -ne 0){throw 'Assessment checks failed'}
