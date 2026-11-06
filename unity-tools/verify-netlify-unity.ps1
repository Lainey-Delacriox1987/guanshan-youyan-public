param([string]$PackageRoot='')
$ErrorActionPreference='Stop'
$taskRoot=if($PackageRoot){[System.IO.Path]::GetFullPath($PackageRoot)}else{Join-Path (Split-Path $PSScriptRoot -Parent) 'output\netlify-unity'}
$taskManifest=Get-Content -Raw -LiteralPath "$taskRoot\package-validation.json" | ConvertFrom-Json
$taskFiles=@('index.html','release.json','unity-transport.js','landscape-only.js','Build/FullStory-web-release.loader.js','Build/FullStory-web-release.framework.js','TemplateData/style.css','TemplateData/cover.png','h5/index.html','h5/mist-walk.js','h5/mist-game.js','legacy.html') + @($taskManifest.files | Where-Object {$_.file -like 'Build/*.bin'} | ForEach-Object file)
$taskResults=@()
foreach($taskFile in $taskFiles){
  $taskOutput=Join-Path "$taskRoot\download-check" $taskFile
  [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($taskOutput)) | Out-Null
  Invoke-WebRequest -Uri ($taskManifest.site+$taskFile) -OutFile $taskOutput -TimeoutSec 45
  $taskExpected=$taskManifest.files | Where-Object file -EQ $taskFile
  $taskActual=(Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256).Hash.ToLower()
  $taskResults += [pscustomobject]@{file=$taskFile;match=($taskActual -eq $taskExpected.sha256);sha256=$taskActual}
  Write-Output ('Verified '+$taskFile+' '+($taskActual -eq $taskExpected.sha256))
}
$taskReport=[ordered]@{success=(@($taskResults | Where-Object match -EQ $false).Count -eq 0);checkedAt=[DateTime]::UtcNow.ToString('o');site=$taskManifest.site;sourceSha256=$taskManifest.sourceSha256;checks=$taskResults}
$taskReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$taskRoot\public-file-verification.json" -Encoding utf8
if(-not $taskReport.success){throw 'Public file mismatch'}
