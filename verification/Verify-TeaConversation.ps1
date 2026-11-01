param(
 [string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
 [string]$Python='C:\Users\amogh\AppData\Local\Python\pythoncore-3.14-64\python.exe'
)
$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$projectRoot=Join-Path $gameRoot 'My project (1)'
$runtimeRoot=Join-Path $projectRoot 'Assets\_Paridhwani\Scripts\Runtime'
$outputRoot=Join-Path $PSScriptRoot 'Temp\NlpVerification'
$nlpRoot=Join-Path (Split-Path $gameRoot -Parent) 'language-engine\Paridhwani-NLP'

# This checks the applied project, never the isolated candidate overlay. No Unity
# launch, asset generation, live listener, or socket-based test is requested here.
foreach($source in @('TeaTopicRouter.cs','TeaKeyboardModel.cs','TeaKeyboardUI.cs')) {
 if(!(Test-Path -LiteralPath (Join-Path $runtimeRoot $source))) {
  throw "Apply the typed conversation sources before running this check: $source is missing."
 }
}
foreach($name in @('TeaTopicChecks','TeaKeyboardChecks','TeaAssessmentChecks')) {
 if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot ($name+'.cs')))) {
  throw "The applied verification source $name.cs is missing."
 }
}

# Refresh the compiled live runtime and run the existing game/dialogue checks.
# Do not use -NetworkChecks or -ServeMock: the transport checks stay in process.
& (Join-Path $PSScriptRoot 'Verify-Nlp.ps1') -EditorRoot $EditorRoot
if($LASTEXITCODE -ne 0) {throw 'Existing game/dialogue checks failed.'}

# Refresh synthetic fixtures using the real local evaluator. This generator does
# not start the host; it preserves the recorded expected assessment labels.
& $Python -I -B (Join-Path $nlpRoot 'verification\tea-milestone\generate.py')
if($LASTEXITCODE -ne 0) {throw 'Evaluator fixture generation failed.'}

$dotnet=Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
$csc=(Get-ChildItem (Join-Path $EditorRoot 'Data\DotNetSdk') -Recurse -Filter csc.dll | Select-Object -First 1).FullName
$baseArguments=@(Get-Content -LiteralPath (Join-Path $outputRoot 'DialogueHttpChecks.rsp') | Where-Object {
 $_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^-(out|refout):'
})

Push-Location $projectRoot
try {
 foreach($name in @('TeaTopicChecks','TeaKeyboardChecks','TeaAssessmentChecks')) {
  # Parentheses keep each source/output path a separate array item in PowerShell.
  $arguments=$baseArguments+@(
   ('-out:"'+(Join-Path $outputRoot ($name+'.dll'))+'"'),
   ('"'+(Join-Path $PSScriptRoot ($name+'.cs'))+'"')
  )
  $responsePath=Join-Path $outputRoot ($name+'.rsp')
  $arguments | Set-Content -LiteralPath $responsePath -Encoding utf8
  & $dotnet $csc "@$responsePath"
  if($LASTEXITCODE -ne 0) {throw "$name compilation failed."}
  Copy-Item -LiteralPath (Join-Path $outputRoot 'DialogueHttpChecks.runtimeconfig.json') -Destination (Join-Path $outputRoot ($name+'.runtimeconfig.json')) -Force
 }

 $failures=@()
 foreach($name in @('TeaTopicChecks','TeaKeyboardChecks')) {
  & $dotnet (Join-Path $outputRoot ($name+'.dll'))
  if($LASTEXITCODE -ne 0) {$failures+=$name}
 }
 & $dotnet (Join-Path $outputRoot 'TeaAssessmentChecks.dll') (Join-Path $nlpRoot 'verification\tea-milestone\results')
 if($LASTEXITCODE -ne 0) {$failures+='TeaAssessmentChecks'}
 if($failures.Count) {throw ('Failed suites: '+($failures -join ', '))}

 Write-Output 'TEA_CONVERSATION_CHECKS_OK: applied runtime; authored topics, keyboard policy and real assessment fixtures; no sockets or native input.'
} finally {Pop-Location}
