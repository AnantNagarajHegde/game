param(
    [switch]$Benchmark, [switch]$Prepare, [switch]$Build, [switch]$Train,
    [string]$Python='python', [string]$Teacher, [string]$Engine,
    [string]$Editor='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe',
    [ValidateRange(320,7680)][int]$Width=960, [ValidateRange(240,4320)][int]$Height=540,
    [ValidateSet('1','1.3')][string]$TextScale='1'
)
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot 'verification\Temp\JointTea'
$pyLocal=Join-Path $cache 'venv\Scripts\python.exe'
$code=Join-Path $PSScriptRoot 'village_backend\joint_tea'
New-Item -ItemType Directory -Force $cache | Out-Null
function Invoke-Checked($exe,[string[]]$parameters) {
    & $exe @parameters
    if($LASTEXITCODE -ne 0){throw "$exe exited with $LASTEXITCODE"}
}
function Get-TeacherAsset($url,$file,$digest) {
    if((Test-Path -LiteralPath $file) -and (Get-FileHash -LiteralPath $file).Hash -eq $digest){return}
    Invoke-Checked 'curl.exe' @('-fsSL','--retry','2',$url,'-o',"$file.download")
    if((Get-FileHash -LiteralPath "$file.download").Hash -ne $digest){throw "Checksum mismatch for $file"}
    Move-Item -LiteralPath "$file.download" -Destination $file -Force
}
if($Prepare){
    if(!(Test-Path -LiteralPath $pyLocal)){Invoke-Checked $Python @('-m','venv',(Join-Path $cache 'venv'))}
    Invoke-Checked $pyLocal @('-m','pip','install','numpy','onnxruntime==1.30.0','onnx==1.23.2','psutil','pandas')
}
if(Test-Path -LiteralPath $pyLocal){$Python=$pyLocal}
if($Prepare -or $Train){
    if(!$Teacher){
        $Teacher=Join-Path $cache 'teacher'
        New-Item -ItemType Directory -Force $Teacher | Out-Null
        $source='https://huggingface.co/onnx-community/mms-tts-kan-ONNX/resolve/cac3976902fe64861618432d00337d19e8c2ec61'
        Get-TeacherAsset "$source/onnx/model.onnx" "$Teacher\model.onnx" 'e0365f4af5056ceb112c2cd410287606c540393a146351026637ed4d388adf75'
        Get-TeacherAsset "$source/vocab.json" "$Teacher\vocab.json" '56e00b687cf6c32bc603e055fe142351c16a136584bae988f333b9dc54c0c3ce'
        Invoke-Checked $Python @('-B',"$PSScriptRoot\verification\PrepareTeaVoiceModel.py",$Teacher)
    }
    $prep=@('-B',"$code\prepare.py",'--teacher',$Teacher)
    if(!$Train){$prep+='--renderer-only'}
    Invoke-Checked $Python $prep
    if($Train){
        if($Prepare){Invoke-Checked $Python @('-m','pip','install','torch==2.11.0','--index-url','https://download.pytorch.org/whl/cpu')}
        Invoke-Checked $Python @('-B',"$code\train.py")
        Invoke-Checked $Python @('-B',"$code\evaluate.py")
    }
}
$project=Join-Path $PSScriptRoot 'My project (1)'
$player=Join-Path $cache 'Player\Manjunatha.exe'
if($Build){
    $log=Join-Path $cache 'build.log'
    $p=Start-Process $Editor -ArgumentList ('-batchmode -quit -projectPath "'+$project+'" -buildWindows64Player "'+$player+'" -logFile "'+$log+'"') -WindowStyle Hidden -PassThru -Wait
    if($p.ExitCode -ne 0 -or !(Select-String -Quiet -LiteralPath $log -Pattern 'Build Finished, Result: Success')){throw "Build failed. See $log"}
}
if(!(Test-Path -LiteralPath $player) -or !(Test-Path -LiteralPath "$cache\renderer.onnx")){throw 'Run this launcher with -Prepare -Build on a fresh checkout.'}
$runner=@('-B',"$PSScriptRoot\verification\TeaVoiceLaptop.py",'--joint','--width',$Width,'--height',$Height,'--scale',$TextScale)
if($Benchmark){
    if(!$Engine){
        $common=(& git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir).Trim()
        $Engine=Join-Path (Split-Path (Split-Path $common -Parent) -Parent) 'language-engine\Paridhwani-NLP\src\assessment_host.py'
    }
    if(!(Test-Path -LiteralPath $Engine)){throw 'Supply -Engine with the path to the assessment host.'}
    $runner+=@('--engine',$Engine,'--editor',$Editor)
}else{$runner+='--demo'}
Invoke-Checked $Python $runner
