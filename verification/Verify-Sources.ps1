param([string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference='Stop'
$projectRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
$surfaceRoot=Join-Path $projectRoot 'Assets\_Paridhwani\Art\Textures\Surfaces'
$surfaceNames=@('WetEarth','WeatheredStone','LimePlaster','ClayTile','Timber','CottonWeave','Bark')
$surfaceGuids=@{}
foreach($surface in $surfaceNames) {
 $source=Join-Path $surfaceRoot ($surface+'.png');$metadata=$source+'.meta'
 if(!(Test-Path -LiteralPath $source) -or !(Test-Path -LiteralPath $metadata)){throw "Missing surface source or metadata: $surface"}
 $bytes=[IO.File]::ReadAllBytes($source)
 if($bytes.Length -lt 24 -or [BitConverter]::ToString($bytes,0,8) -ne '89-50-4E-47-0D-0A-1A-0A'){throw "Invalid PNG source: $surface"}
 $width=[Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,16));$height=[Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,20))
 if($width -ne 512 -or $height -ne 512){throw "Surface source must be 512x512: $surface"}
 $metaText=Get-Content -Raw -LiteralPath $metadata
 $version=[regex]::Match($metaText,'(?m)^  serializedVersion:\s*(\d+)')
 if(!$version.Success -or [int]$version.Groups[1].Value -lt 10){throw "Surface TextureImporter metadata is missing or too old for Unity 6: $surface"}
 $shape=[regex]::Match($metaText,'(?m)^  textureShape:\s*(\d+)')
 if(!$shape.Success -or [int]$shape.Groups[1].Value -ne 1){throw "Surface must import as Texture2D (shape 1), not a Cubemap: $surface"}
 $guid=[regex]::Match($metaText,'(?m)^guid: ([a-f0-9]{32})\s*$').Groups[1].Value
 if(!$guid -or $surfaceGuids.ContainsKey($guid)){throw "Missing or duplicated surface GUID: $surface"};$surfaceGuids[$guid]=$true
}
Write-Output 'SURFACE_SOURCES_OK: seven 512px PNGs with supported TextureImporter metadata and unique GUIDs'
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
 # This checks runtime-only source branches against cached Unity references, not a Player build.
 $playerArgs=Get-Content (Join-Path $outputRoot 'Runtime.rsp') | Where-Object {$_ -notmatch '^-(out|refout):'}
 $playerArgs=$playerArgs | ForEach-Object {
 if($_ -match '^-define:(.*)$') {
 $symbols=$Matches[1] -split '[;,]' | Where-Object {$_ -notmatch '^(UNITY_EDITOR(_.*)?|DEVELOPMENT_BUILD|DEBUG|UNITY_ENABLE_CHECKS|UNITY_INCLUDE_INSTRUMENTATION|UNITY_ASSERTIONS|ENABLE_PROFILER|UNITY_INCLUDE_TESTS)$'}
 if($symbols) {'-define:'+($symbols -join ';')}
 } else {$_}
 }
 $playerArgs+='-out:"Temp/IterationVerification/Assembly-CSharp-PlayerBranches.dll"'
 $playerResponse=Join-Path $outputRoot 'PlayerBranches.rsp';$playerArgs | Set-Content -Encoding utf8 -LiteralPath $playerResponse
 & $dotnet $csc "@$playerResponse"
 if($LASTEXITCODE -ne 0) {throw 'Runtime source branches without Editor/diagnostic symbols failed compilation'}
 Write-Output 'PLAYER_SOURCE_BRANCHES_OK (cached Editor references; not a native Player build)'
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
