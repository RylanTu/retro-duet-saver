param([string[]]$Runs=@('baseline-1','cold-1','warm-1','baseline-2','warm-2'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$out=Join-Path $root 'output\cache-benchmark'
$env:RETRO_DUET_CACHE_DIR=Join-Path $out ('disk-cache-' + [Guid]::NewGuid().ToString('N'))
$env:RETRO_DUET_CACHE='1'
foreach($run in $Runs){
 $version=if($run.StartsWith('baseline')){'baseline'}else{'cached'}
 $p=Start-Process -FilePath (Join-Path $out "$version.exe") -ArgumentList ('"'+(Join-Path $out "$run.csv")+'"') -WindowStyle Hidden -PassThru
 $watch=[Diagnostics.Stopwatch]::StartNew()
 $samples=[Collections.Generic.List[object]]::new()
 try {
 while(!$p.HasExited){
  $counter=Get-Counter @('\GPU Engine(*)\Utilization Percentage','\GPU Process Memory(*)\Dedicated Usage','\GPU Process Memory(*)\Shared Usage') -ErrorAction SilentlyContinue
  foreach($sample in $counter.CounterSamples){if($sample.InstanceName -like "pid_$($p.Id)_*" -and $sample.Status -eq 0){$samples.Add([pscustomobject]@{seconds=$watch.Elapsed.TotalSeconds;instance=$sample.InstanceName;path=$sample.Path;value=$sample.CookedValue})}}
  $p.Refresh()
  if($watch.Elapsed.TotalSeconds -gt 85){throw "Run timed out: $run"}
 }
 if($p.ExitCode -ne 0){throw "Run failed: $run ($($p.ExitCode))"}
 }finally{if(!$p.HasExited){$p.Kill()};$samples | Export-Csv (Join-Path $out "$run-gpu.csv") -NoTypeInformation -Encoding UTF8}
 Write-Output "FINISHED $($run): $($samples.Count) GPU samples"
}
