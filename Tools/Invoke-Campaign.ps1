param([Parameter(Mandatory=$true)][ValidateSet('setup','characters','refresh','3d','tests','play','stop','state','view','scenario','journey','traversal','branches','capture','build')][string]$Action,[string]$Argument='', [int]$TimeoutSeconds=50)
$ErrorActionPreference='Stop'
$id=[guid]::NewGuid().ToString()
$command=@{id=$id;action=$Action;argument=$Argument}|ConvertTo-Json -Compress
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'command.json'),$command,[Text.UTF8Encoding]::new($false))
$deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while([DateTime]::UtcNow -lt $deadline){
 Start-Sleep -Milliseconds 300
 $resultPath=Join-Path $PSScriptRoot 'result.json'
 if(Test-Path -LiteralPath $resultPath){
  try{$result=Get-Content -LiteralPath $resultPath -Raw -Encoding utf8|ConvertFrom-Json}catch{continue}
  if($result.id -eq $id){$result|ConvertTo-Json -Depth 5;if($result.status -ne 'ok'){exit 1};exit 0}
 }
}
throw 'Unity has not responded yet. Inspect compilation / modal state, then read Tools/result.json.'
