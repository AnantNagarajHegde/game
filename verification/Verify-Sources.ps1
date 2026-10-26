param([string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference='Stop'
$projectRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
$outputRoot=Join-Path $projectRoot 'Temp\IterationVerification'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$runtimeResponse=Get-ChildItem (Join-Path $projectRoot 'Library\Bee\artifacts') -Recurse -Filter 'Assembly-CSharp.rsp' | Select-Object -First 1
$editorResponse=Get-ChildItem (Join-Path $projectRoot 'Library\Bee\artifacts') -Recurse -Filter 'Assembly-CSharp-Editor.rsp' | Select-Object -First 1
if(!$runtimeResponse -or !$editorResponse) { throw 'Unity cached compiler responses are needed. Import the project in Unity first.' }
$dotnet=Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
$csc=(Get-ChildItem (Join-Path $EditorRoot 'Data\DotNetSdk') -Recurse -Filter csc.dll | Select-Object -First 1).FullName
Push-Location $projectRoot
try {
 foreach($kind in @('Runtime','Editor')) {
 $response=if($kind -eq 'Runtime'){$runtimeResponse}else{$editorResponse}
 $argsText=Get-Content -LiteralPath $response.FullName | Where-Object {$_ -notmatch '^"Assets/' -and $_ -notmatch '^-(out|refout):'}
 if($kind -eq 'Editor') { $argsText=$argsText -replace 'Library/Bee/artifacts/[^/]+/Assembly-CSharp.ref.dll','Temp/IterationVerification/Assembly-CSharp.dll' }
 $assembly=if($kind -eq 'Runtime'){'Assembly-CSharp'}else{'Assembly-CSharp-Editor'}
 $argsText+='-out:"Temp/IterationVerification/'+$assembly+'.dll"'
 $argsText+=Get-ChildItem (Join-Path $projectRoot "Assets\_Paridhwani\Scripts\$kind") -Filter *.cs | ForEach-Object {'"'+$_.FullName+'"'}
 $responsePath=Join-Path $outputRoot "$kind.rsp"
 $argsText | Set-Content -Encoding utf8 -LiteralPath $responsePath
 & $dotnet $csc "@$responsePath"
 if($LASTEXITCODE -ne 0) { throw "$kind source compilation failed" }
 }
 $argsText=Get-Content (Join-Path $outputRoot 'Runtime.rsp') | Where-Object { $_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^-(out|refout|target):' -and $_ -notmatch '^/analyzer' }
 $argsText+=@('-target:exe','-out:"Temp/IterationVerification/ChapterChecks.dll"','-r:"Temp/IterationVerification/Assembly-CSharp.dll"','"../verification/ChapterChecks.cs"')
 $responsePath=Join-Path $outputRoot 'Checks.rsp'; $argsText | Set-Content -Encoding utf8 -LiteralPath $responsePath
 & $dotnet $csc "@$responsePath"
 if($LASTEXITCODE -ne 0) { throw 'Chapter runner compilation failed' }
 '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}' | Set-Content -LiteralPath (Join-Path $outputRoot 'ChapterChecks.runtimeconfig.json')
 Copy-Item -LiteralPath (Join-Path $EditorRoot 'Data\Managed\UnityEngine\UnityEngine.CoreModule.dll') -Destination $outputRoot -Force
 & $dotnet (Join-Path $outputRoot 'ChapterChecks.dll')
 if($LASTEXITCODE -ne 0) { throw 'Chapter behavior checks failed' }
 Write-Output 'SOURCE_VERIFICATION_OK (does not run Unity scene, save serialization, physics, or rendering)'
} finally { Pop-Location }
