param([string]$Destination='output/雾港来信-WebGL-性能优化版.zip')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$buildRoot=Join-Path $projectRoot 'unity/GuanshanYouyan2D/Builds/FullStory-web-release'
$report=Get-Content -LiteralPath (Join-Path $projectRoot 'unity/GuanshanYouyan2D/ImportReports/full-build-web-release.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if(-not $report.success){throw 'Latest build did not succeed'}
$html=Get-Content -LiteralPath (Join-Path $buildRoot 'index.html') -Raw -Encoding UTF8
$archivePath=[IO.Path]::GetFullPath((Join-Path $projectRoot $Destination))
if(Test-Path -LiteralPath $archivePath){throw "Archive already exists: $archivePath"}
Add-Type -AssemblyName System.IO.Compression
$stream=[IO.File]::Open($archivePath,[IO.FileMode]::CreateNew)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false,[Text.Encoding]::UTF8)
$included=[Collections.Generic.List[string]]::new()
try{
 foreach($file in (Get-ChildItem -LiteralPath $buildRoot -File -Recurse)){
  $relative=$file.FullName.Substring($buildRoot.Length+1).Replace('\','/')
  # Keep only the resource variants actually referenced by this export's HTML.
  if($relative.StartsWith('Build/') -and -not $html.Contains('/'+$file.Name)){continue}
  $entry=$zip.CreateEntry($relative,[IO.Compression.CompressionLevel]::NoCompression)
  $input=[IO.File]::OpenRead($file.FullName);$output=$entry.Open()
  try{$input.CopyTo($output)}finally{$input.Dispose();$output.Dispose()}
  $included.Add($relative)
 }
 $entry=$zip.CreateEntry('性能优化版说明.txt');$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false))
 try{$writer.Write("关山有眼·雾港来信 本地性能优化组合版`n构建源码："+$report.sourceSha256+"`n运行：在本目录执行 node Serve-Web.cjs . 18743，然后打开 http://127.0.0.1:18743/guanshan-youyan/ 。`n保留已有本地地址和存档；如18743已有服务，请先确认该服务实际提供本版文件，不要清空浏览器网站数据。`n本包只含HTML实际引用的Brotli资源；需由支持Content-Encoding: br的HTTP服务运行，不能双击index.html直接运行。`n图片原件和源码仍在原工程，本包为运行导出；尚未更新公网或Windows包。`n")}finally{$writer.Dispose()}
}finally{$zip.Dispose();$stream.Dispose()}
$result=[ordered]@{success=$true;path=$archivePath;bytes=(Get-Item -LiteralPath $archivePath).Length;sha256=(Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant();sourceSha256=$report.sourceSha256;files=$included}
[IO.File]::WriteAllText((Join-Path $projectRoot 'unity/GuanshanYouyan2D/ImportReports/performance-web-package.json'),($result | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
[pscustomobject]$result | Select-Object success,path,bytes,sha256,sourceSha256 | ConvertTo-Json
