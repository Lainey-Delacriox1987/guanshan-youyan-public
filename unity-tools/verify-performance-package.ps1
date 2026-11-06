$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$report=Get-Content -LiteralPath (Join-Path $projectRoot 'unity/GuanshanYouyan2D/ImportReports/performance-web-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$buildRoot=Join-Path $projectRoot 'unity/GuanshanYouyan2D/Builds/FullStory-web-release'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($report.path)
$sha=[Security.Cryptography.SHA256]::Create();$checks=0
try{
 foreach($relative in $report.files){
  $entry=$zip.GetEntry($relative);if($null -eq $entry){throw "Missing entry $relative"}
  $input=$entry.Open();try{$actual=([BitConverter]::ToString($sha.ComputeHash($input))).Replace('-','').ToLowerInvariant()}finally{$input.Dispose()}
  $expected=(Get-FileHash -LiteralPath (Join-Path $buildRoot $relative) -Algorithm SHA256).Hash.ToLowerInvariant()
  if($actual -ne $expected){throw "Package differs from build: $relative"};$checks++
 }
 $buildEntries=@($zip.Entries | Where-Object {$_.FullName.StartsWith('Build/')})
 if($buildEntries.Count -ne 4){throw 'Expected only the four resources referenced by the HTML'}
 if($buildEntries | Where-Object {$_.FullName.EndsWith('.gz')}){throw 'Unused gzip variant included'}
}finally{$sha.Dispose();$zip.Dispose()}
[pscustomobject]@{success=$true;checks=$checks;archiveBytes=$report.bytes;sourceSha256=$report.sourceSha256} | ConvertTo-Json
