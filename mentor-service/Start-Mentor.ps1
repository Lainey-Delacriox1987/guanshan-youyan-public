$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$runtimeRoot=Join-Path $projectRoot 'unity/GuanshanYouyan2D/RuntimeTools~/mentor'
$modelPath=Join-Path $runtimeRoot 'models/qwen2.5-1.5b-instruct-q4_k_m.gguf'
if((Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash.ToLower() -ne '6a1a2eb6d15622bf3c96857206351ba97e1af16c30d7a74ee38970e434e9407e'){throw 'Model checksum mismatch'}
$nodeCommand=Get-Command node -ErrorAction Stop
$modelProcess=Start-Process -FilePath (Join-Path $runtimeRoot 'bin/llama-server.exe') -WorkingDirectory $runtimeRoot -ArgumentList @('-m','models/qwen2.5-1.5b-instruct-q4_k_m.gguf','--host','127.0.0.1','--port','39572','-c','4096','-ngl','99','--parallel','1','--no-webui') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimeRoot 'llama.out.log') -RedirectStandardError (Join-Path $runtimeRoot 'llama.err.log') -PassThru
$gatewayProcess=Start-Process -FilePath $nodeCommand.Source -WorkingDirectory $projectRoot -ArgumentList @((Join-Path $PSScriptRoot 'server.cjs')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimeRoot 'gateway.out.log') -RedirectStandardError (Join-Path $runtimeRoot 'gateway.err.log') -PassThru
[pscustomobject]@{modelPid=$modelProcess.Id;gatewayPid=$gatewayProcess.Id;gateway='http://127.0.0.1:39571';model='http://127.0.0.1:39572'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runtimeRoot 'processes.json')
Write-Output 'Local mentor processes started. Check http://127.0.0.1:39571/health before enabling free questions.'
