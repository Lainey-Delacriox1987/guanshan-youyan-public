param([ValidateSet('windows','web')][string[]]$Kinds=@('windows','web'))
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$projectRoot=Join-Path $taskRoot 'unity/GuanshanYouyan2D'
$buildRoot=Join-Path $projectRoot 'Builds'
$records=@()
foreach($kind in $Kinds) {
    $mode=$kind+'-release'
    $buildReport=Get-Content -LiteralPath (Join-Path $projectRoot ('ImportReports/full-build-'+$mode+'.json')) -Raw | ConvertFrom-Json
    if(-not $buildReport.success){throw ('No successful build report for '+$mode)}
    $source=Join-Path $buildRoot ('FullStory-'+$mode)
    if($kind -eq 'web') {
        & node (Join-Path $PSScriptRoot 'prepare-fast-local-web.cjs') $source
        if($LASTEXITCODE -ne 0){throw 'Fast web preparation failed'}
    }
    $entry=if($kind -eq 'windows'){'雾港来信.exe'}else{'index.html'}
    if(-not (Test-Path -LiteralPath (Join-Path $source $entry))){throw ('Missing entry '+$entry)}
    $label=if($kind -eq 'windows'){'Windows'}else{'Web'}
    $zip=Join-Path $buildRoot ('雾港来信-四幕整版-'+$label+'.zip')
    # Preserve any previous delivery as a timestamped file before replacing its name.
    if(Test-Path -LiteralPath $zip){Move-Item -LiteralPath $zip -Destination ($zip+'.'+(Get-Date -Format 'yyyyMMddHHmmss')+'.previous')}
    [System.IO.Compression.ZipFile]::CreateFromDirectory($source,$zip,[System.IO.Compression.CompressionLevel]::Optimal,$false)
    $archive=[System.IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $names=@($archive.Entries | ForEach-Object {$_.FullName.Replace('\','/')})
        foreach($required in @($entry,'开始游玩.txt','Noto-OFL.txt','背景音乐来源.md')){if($required -notin $names){throw ('Archive missing '+$required)}}
        if($kind -eq 'windows' -and 'UnityPlayer.dll' -notin $names){throw 'Archive missing UnityPlayer.dll'}
        if($kind -eq 'web' -and @($names | Where-Object {$_ -like '*.wasm.gz'}).Count -ne 1){throw 'Archive missing gzip wasm'}
        if($kind -eq 'web' -and 'Serve-Web.cjs' -notin $names){throw 'Archive missing local gzip server'}
        $records += [pscustomobject]@{mode=$mode;path=$zip;bytes=(Get-Item -LiteralPath $zip).Length;sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLower();entries=$names.Count}
    } finally {$archive.Dispose()}
}
[pscustomobject]@{localOnly=$true;published=$false;verifiedAt=[DateTime]::UtcNow.ToString('o');packages=$records} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $projectRoot 'ImportReports/full-package-validation.json')
$records | ConvertTo-Json
