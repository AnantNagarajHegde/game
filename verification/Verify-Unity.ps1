param(
 [ValidateSet('Legacy','Additive')][string]$Layout='Legacy',
 [switch]$Build,[switch]$Smoke,
 [int]$TimeoutSeconds=600,
 [string]$EditorPath='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
if($Smoke -and !$Build){throw 'Use -Build with -Smoke so the rendered test never runs an older executable.'}
$projectRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'My project (1)'
$outputRoot=Join-Path $projectRoot 'Temp\UnityVerification'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$log=Join-Path $outputRoot "$Layout-$stamp.log"
$method=if($Layout -eq 'Additive'){if($Build){'BuildModular'}else{'GenerateModular'}}else{if($Build){'ValidateAndBuild'}else{'GenerateAndCheck'}}
$arguments='-batchmode -quit -projectPath "'+$projectRoot+'" -executeMethod Paridhwani.Editor.TeaStallSceneBuilder.'+$method+' -logFile "'+$log+'"'
$process=Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if(!$process.WaitForExit($TimeoutSeconds*1000)){ $process.Kill();throw "Only the started Unity process was stopped after timeout. Inspect $log" }
$process.WaitForExit();$exit=$process.ExitCode
$content=if(Test-Path -LiteralPath $log){Get-Content -Raw -LiteralPath $log}else{''}
$marker=if($Layout -eq 'Additive'){if($Build){'PARIDHWANI_MODULAR_BUILD_OK'}else{'PARIDHWANI_MODULAR_PACKAGE_READY'}}else{if($Build){'PARIDHWANI_BUILD_OK'}else{'PARIDHWANI_CHECKS_OK'}}
$passed=$exit -eq 0 -and $content.Contains($marker)
$report=[ordered]@{layout=$Layout;buildRequested=[bool]$Build;editorExitCode=$exit;editorPassed=$passed;editorLog=$log;smokeRequested=[bool]$Smoke;smokePassed=$false}
if($passed -and $Smoke){
 $player=Join-Path $projectRoot $(if($Layout -eq 'Additive'){'Builds\Modular\Paridhwani.exe'}else{'Builds\TeaStall\Paridhwani.exe'})
 $playerLog=Join-Path $outputRoot "$Layout-player-$stamp.log"
 # -Smoke is an explicit opt-in to a visible rendered player using an isolated test save.
 $test=Start-Process -FilePath $player -ArgumentList ('-village-smoke -logFile "'+$playerLog+'"') -WindowStyle Normal -PassThru
 if(!$test.WaitForExit($TimeoutSeconds*1000)){$test.Kill();throw "The started smoke player timed out. Inspect $playerLog"}
 $test.WaitForExit();$playerContent=Get-Content -Raw -LiteralPath $playerLog
 $report['playerExitCode']=$test.ExitCode;$report['playerLog']=$playerLog;$report.smokePassed=$test.ExitCode -eq 0 -and $playerContent.Contains('VILLAGE_SMOKE_OK')
}
$reportPath=Join-Path $outputRoot "$Layout-$stamp.json";$report|ConvertTo-Json|Set-Content -Encoding utf8 -LiteralPath $reportPath
Write-Output "Verification report: $reportPath"
if(!$passed){throw "Unity did not finish verification. No success is claimed. Inspect $log"}
if($Smoke -and !$report.smokePassed){throw 'The newly built player did not pass its rendered smoke sequence.'}
Write-Output 'UNITY_VERIFICATION_OK'
