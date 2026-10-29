param(
 [string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor',
 [switch]$ServeMock,
 [int]$Port=8765,
 [switch]$NetworkChecks,
 [switch]$CompileOnly
)
$ErrorActionPreference='Stop'
# Own output directory: does not start Unity, regenerate assets, or contend with other check runners.
$projectRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
$outputRoot=Join-Path $PSScriptRoot 'Temp\NlpVerification'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$dotnet=Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
$csc=(Get-ChildItem (Join-Path $EditorRoot 'Data\DotNetSdk') -Recurse -Filter csc.dll | Select-Object -First 1).FullName
$cached=Join-Path $projectRoot 'Library\Bee\artifacts'
$runtimeResponse=Get-ChildItem $cached -Recurse -Filter 'Assembly-CSharp.rsp' | Select-Object -First 1
$editorResponse=Get-ChildItem $cached -Recurse -Filter 'Assembly-CSharp-Editor.rsp' | Select-Object -First 1
if(!$runtimeResponse -or !$editorResponse) {throw 'Import the Unity project once to supply cached compiler references.'}
function Compile([string]$Name,[string[]]$Arguments) {
 $responsePath=Join-Path $outputRoot ($Name+'.rsp')
 $Arguments | Set-Content -Encoding utf8 -LiteralPath $responsePath
 & $dotnet $csc "@$responsePath"
 if($LASTEXITCODE -ne 0) {throw "$Name compilation failed"}
}
Push-Location $projectRoot
try {
 $runtimeArgs=Get-Content -LiteralPath $runtimeResponse.FullName | Where-Object {$_ -notmatch '^"Assets/' -and $_ -notmatch '^-(out|refout):'}
 $runtimeArgs+='-out:"'+(Join-Path $outputRoot 'Assembly-CSharp.dll')+'"'
 $runtimeArgs+=Get-ChildItem 'Assets\_Paridhwani\Scripts\Runtime' -Filter *.cs | ForEach-Object {'"'+$_.FullName+'"'}
 Compile 'Runtime' $runtimeArgs
 $editorArgs=Get-Content -LiteralPath $editorResponse.FullName | Where-Object {$_ -notmatch '^"Assets/' -and $_ -notmatch '^-(out|refout):'}
 $editorArgs=$editorArgs | ForEach-Object {if($_ -match '^-r:"Library/Bee/artifacts/[^/]+/Assembly-CSharp.ref.dll"$') {'-r:"'+(Join-Path $outputRoot 'Assembly-CSharp.dll')+'"'}else{$_}}
 $editorArgs+='-out:"'+(Join-Path $outputRoot 'Assembly-CSharp-Editor.dll')+'"'
 $editorArgs+=Get-ChildItem 'Assets\_Paridhwani\Scripts\Editor' -Filter *.cs | ForEach-Object {'"'+$_.FullName+'"'}
 Compile 'Editor' $editorArgs
 $playerArgs=$runtimeArgs | Where-Object {$_ -notmatch '^-(out|refout):'}
 $playerArgs=$playerArgs | Where-Object {$_ -notmatch '^-define:(UNITY_EDITOR(_.*)?|DEVELOPMENT_BUILD|DEBUG|UNITY_ENABLE_CHECKS|UNITY_INCLUDE_INSTRUMENTATION|UNITY_ASSERTIONS|ENABLE_PROFILER|UNITY_INCLUDE_TESTS)$'}
 $playerArgs+='-out:"'+(Join-Path $outputRoot 'Assembly-CSharp-PlayerBranches.dll')+'"'
 Compile 'PlayerBranches' $playerArgs
 $checksArgs=$runtimeArgs | Where-Object {$_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^-(out|refout|target):' -and $_ -notmatch '^[-/]analyzer'}
 $checksArgs+=@('-target:exe',('-out:"'+(Join-Path $outputRoot 'DialogueHttpChecks.dll')+'"'),('-r:"'+(Join-Path $outputRoot 'Assembly-CSharp.dll')+'"'),
  ('"'+(Join-Path $PSScriptRoot 'DialogueHttpChecks.cs')+'"'),('"'+(Join-Path $PSScriptRoot 'NlpMockServer.cs')+'"'),
  ('"'+(Join-Path $PSScriptRoot 'DialogueContractFixtures.cs')+'"'))
 Compile 'DialogueHttpChecks' $checksArgs
 $regressionArgs=$checksArgs | Where-Object {$_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^-out:'}
 $regressionArgs+=@(
  ('-out:"'+(Join-Path $outputRoot 'ChapterChecks.dll')+'"'),
  ('"'+(Join-Path $PSScriptRoot 'ChapterChecks.cs')+'"'),
  ('"'+(Join-Path $PSScriptRoot 'DemoPresentationChecks.cs')+'"')
 )
 Compile 'ChapterChecks' $regressionArgs
 '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}' | Set-Content -LiteralPath (Join-Path $outputRoot 'DialogueHttpChecks.runtimeconfig.json')
 Copy-Item -LiteralPath (Join-Path $outputRoot 'DialogueHttpChecks.runtimeconfig.json') -Destination (Join-Path $outputRoot 'ChapterChecks.runtimeconfig.json') -Force
 Copy-Item -LiteralPath (Join-Path $EditorRoot 'Data\Managed\UnityEngine\UnityEngine.CoreModule.dll') -Destination $outputRoot -Force
 Write-Output 'NLP_SOURCE_COMPILE_OK (runtime/editor/player source branches; cached references; no Unity build)'
 if(!$CompileOnly) {
  if(!$ServeMock) {
   & $dotnet (Join-Path $outputRoot 'ChapterChecks.dll')
   if($LASTEXITCODE -ne 0) {throw 'Existing chapter/adapter regression checks failed'}
  }
  if($ServeMock) { & $dotnet (Join-Path $outputRoot 'DialogueHttpChecks.dll') --serve $Port }
  elseif($NetworkChecks) { & $dotnet (Join-Path $outputRoot 'DialogueHttpChecks.dll') --network }
  else { & $dotnet (Join-Path $outputRoot 'DialogueHttpChecks.dll') }
  if($LASTEXITCODE -ne 0) {throw 'NLP HTTP runner failed'}
 }
} finally {Pop-Location}
