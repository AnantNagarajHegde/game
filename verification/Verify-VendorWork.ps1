param([string]$EditorRoot='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor')
$ErrorActionPreference='Stop'
$taskOutput=Join-Path $PSScriptRoot 'Temp\VendorWorkVerification'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$dotnet=Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
$csc=(Get-ChildItem (Join-Path $EditorRoot 'Data\DotNetSdk') -Recurse -Filter csc.dll | Select-Object -First 1).FullName
$framework=Get-ChildItem (Join-Path $EditorRoot 'Data\NetCoreRuntime\shared\Microsoft.NETCore.App') -Directory | Select-Object -First 1
$model=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)\Assets\_Paridhwani\Scripts\Runtime\VendorWorkRoutine.cs'
$compilerArgs=@('-nostdlib+','-target:exe',('-out:"'+(Join-Path $taskOutput 'VendorWorkChecks.dll')+'"'))
$compilerArgs+=@('System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll','System.Collections.dll') | ForEach-Object {'-r:"'+(Join-Path $framework.FullName $_)+'"'}
$compilerArgs+='"'+$model+'"'
$compilerArgs+='"'+(Join-Path $PSScriptRoot 'VendorWorkChecks.cs')+'"'
$response=Join-Path $taskOutput 'Checks.rsp'
$compilerArgs | Set-Content -Encoding utf8 -LiteralPath $response
& $dotnet $csc "@$response"
if($LASTEXITCODE -ne 0){throw 'Vendor work model/check compilation failed'}
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version=$framework.Name}}}|ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskOutput 'VendorWorkChecks.runtimeconfig.json')
& $dotnet (Join-Path $taskOutput 'VendorWorkChecks.dll')
if($LASTEXITCODE -ne 0){throw 'Vendor work behavior checks failed'}
