param(
 [ValidateSet('Legacy','Additive')][string]$Layout='Legacy',
 [switch]$Build,[switch]$Smoke,
 [ValidateRange(1,3600)][int]$TimeoutSeconds=600,
 [string]$EditorPath='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
if($Smoke -and !$Build){throw 'Use -Build with -Smoke so the rendered test never runs an older executable.'}
$projectRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
# Keep reports/logs outside the project Temp folder that Unity clears at startup.
$outputRoot=Join-Path $PSScriptRoot 'Temp\UnityVerification'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmssfff'
$log=Join-Path $outputRoot "$Layout-$stamp.log"
$reportPath=Join-Path $outputRoot "$Layout-$stamp.json"
$method=if($Layout -eq 'Additive'){if($Build){'BuildModular'}else{'GenerateModular'}}else{if($Build){'ValidateAndBuild'}else{'GenerateAndCheck'}}
$arguments='-batchmode -quit -projectPath "'+$projectRoot+'" -executeMethod Paridhwani.Editor.TeaStallSceneBuilder.'+$method+' -logFile "'+$log+'"'
$report=[ordered]@{layout=$Layout;buildRequested=[bool]$Build;editorExitCode=$null;editorPassed=$false;editorLog=$log;smokeRequested=[bool]$Smoke;smokePassed=$false;failure=$null}
$process=$null;$test=$null
try {
 $process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
 while(!$process.WaitForExit(500)) {
  if((Test-Path -LiteralPath $log) -and ([string](Get-Content -Raw -LiteralPath $log)).Contains('Licensing initialization failed')) {
   throw 'Unity licensing initialization failed. Open the project through desktop Unity Hub and use the in-Editor standalone test command.'
  }
  if([DateTime]::UtcNow -ge $deadline){throw 'The started Unity process exceeded its verification timeout.'}
 }
 $process.WaitForExit();$report.editorExitCode=$process.ExitCode
 $content=[string]$(if(Test-Path -LiteralPath $log){Get-Content -Raw -LiteralPath $log}else{''})
 $marker=if($Layout -eq 'Additive'){if($Build){'PARIDHWANI_MODULAR_BUILD_OK'}else{'PARIDHWANI_MODULAR_PACKAGE_READY'}}else{if($Build){'PARIDHWANI_BUILD_OK'}else{'PARIDHWANI_CHECKS_OK'}}
 $report.editorPassed=$process.ExitCode -eq 0 -and $content.Contains($marker)
 if(!$report.editorPassed){throw 'Unity did not complete the requested checks/build; no existing player was launched.'}
 if($Smoke){
  $player=Join-Path $projectRoot $(if($Layout -eq 'Additive'){'Builds\Modular\Paridhwani.exe'}else{'Builds\TeaStall\Paridhwani.exe'})
  $playerLog=Join-Path $outputRoot "$Layout-player-$stamp.log";$report['playerLog']=$playerLog
  # Explicit rendered verification uses an isolated temporary save in the development player.
  $test=Start-Process -FilePath $player -ArgumentList ('-village-smoke -screen-fullscreen 0 -screen-width 960 -screen-height 540 -logFile "'+$playerLog+'"') -WindowStyle Normal -PassThru
  $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
  while(!$test.WaitForExit(500)) {if([DateTime]::UtcNow -ge $deadline){throw 'The started smoke player exceeded its verification timeout.'}}
  $test.WaitForExit();$playerContent=Get-Content -Raw -LiteralPath $playerLog
  $report['playerExitCode']=$test.ExitCode;$report.smokePassed=$test.ExitCode -eq 0 -and $playerContent -match '(?m)^VILLAGE_SMOKE_OK '
  if(!$report.smokePassed){throw 'The newly built player did not pass its rendered smoke sequence.'}
 }
} catch {$report.failure=$_.Exception.Message}
finally {
 # Never discover or stop other Editors/players: these are only this invocation's returned handles.
 foreach($owned in @($test,$process)) {if($null -ne $owned){try {if(!$owned.HasExited){$owned.Kill()}} catch {Write-Warning $_.Exception.Message} finally {$owned.Dispose()}}}
 $report|ConvertTo-Json|Set-Content -Encoding utf8 -LiteralPath $reportPath
 Write-Output "Verification report: $reportPath"
}
if($report.failure){throw $report.failure}
Write-Output 'UNITY_VERIFICATION_OK'
