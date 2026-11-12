param([string]$Editor='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$movieRoot=Join-Path $PSScriptRoot 'Temp/ArrivalMovie'
$movieProject=Join-Path $movieRoot 'UnityProject'
$movieAsset=Join-Path $gameRoot 'My project (1)/Assets/StreamingAssets/ArrivalMovie/arrival.mp4'
if(!(Test-Path -LiteralPath $movieAsset)){throw 'Encode the arrival movie before testing playback.'}
# The source project is read-only to this operation. The test uses its own copied
# scene, imported assets, settings and temporary save, without computer input.
& robocopy (Join-Path $gameRoot 'My project (1)/Assets') (Join-Path $movieProject 'Assets') /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
if($LASTEXITCODE -ge 8){throw 'Failed to copy movie verification assets.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ArrivalMovie/ArrivalMovieValidate.cs') -Destination (Join-Path $movieProject 'Assets/_Paridhwani/Scripts/Editor/ArrivalMovieValidate.cs') -Force
$log=Join-Path $movieRoot 'validation-editor.log'
$arguments='-batchmode -force-d3d11 -projectPath "'+$movieProject+'" -executeMethod Paridhwani.MovieAuthoring.ArrivalMovieValidate.Start -logFile "'+$log+'"'
$process=Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Testing native movie playback, isolated Editor PID '+$process.Id)
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Get-Content -Raw -LiteralPath $log).Contains('ARRIVAL_MOVIE_VALIDATION_OK')){throw ('Native movie verification failed. See '+$log)}
Write-Output 'ARRIVAL_MOVIE_VALIDATION_OK'
