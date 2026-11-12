param([switch]$Proofs,[string]$Editor='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$renderRoot=Join-Path $PSScriptRoot 'Temp/ArrivalMovie'
$renderProject=Join-Path $renderRoot 'UnityProject'
if(!(Test-Path -LiteralPath (Join-Path $renderProject 'Assets'))){throw 'Create the isolated Assets/Packages/ProjectSettings/Library/PackageCache copy first.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ArrivalMovie/ArrivalMovieRender.cs') -Destination (Join-Path $renderProject 'Assets/_Paridhwani/Scripts/Editor/ArrivalMovieRender.cs') -Force
Copy-Item -LiteralPath (Join-Path $gameRoot 'My project (1)/Assets/_Paridhwani/Resources/ArrivalIntro/MovieTimeline.json') -Destination (Join-Path $renderProject 'Assets/_Paridhwani/Resources/ArrivalIntro/MovieTimeline.json') -Force
$log=Join-Path $renderRoot $(if($Proofs){'proof-editor.log'}else{'render-editor.log'})
$arguments='-batchmode -force-d3d11 -projectPath "'+$renderProject+'" -executeMethod Paridhwani.MovieAuthoring.ArrivalMovieRender.Start -logFile "'+$log+'"'
if($Proofs){$arguments+=' -movie-proofs'}
$renderProcess=Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Rendering in isolated project, PID '+$renderProcess.Id)
$renderProcess.WaitForExit()
if($renderProcess.ExitCode -ne 0){throw ('Movie render failed with exit '+$renderProcess.ExitCode+'. See '+$log)}
if(!(Get-Content -Raw -LiteralPath $log).Contains('ARRIVAL_MOVIE_RENDER_OK')){throw ('Movie rendering did not finish. See '+$log)}
Write-Output 'ARRIVAL_MOVIE_RENDER_OK'
