param([ValidateSet('Install','Uninstall')][string]$Action='Install')
$ErrorActionPreference='Stop'
$serviceName='RetroDuetLighting'
$installRoot=Join-Path $env:ProgramFiles 'RetroDuetLighting'
$dataRoot=Join-Path $env:ProgramData 'RetroDuetLighting'
$exePath=Join-Path $installRoot 'RetroDuetLighting.exe'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
if(!([Security.Principal.WindowsPrincipal]$identity).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
 $pwsh=(Get-Process -Id $PID).Path
 $p=Start-Process -FilePath $pwsh -ArgumentList @('-NoProfile','-File',('"'+$PSCommandPath+'"'),'-Action',$Action) -Verb RunAs -WindowStyle Hidden -Wait -PassThru
 exit $p.ExitCode
}
Start-Transcript -Path (Join-Path $env:TEMP 'RetroDuetLighting-Unified-Install.log') -Append | Out-Null
$wasRunning=$false
$previousAcl=$null
$replaced=$false
try{
 foreach($path in @($installRoot,$dataRoot)){
  $item=Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
  if($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Installation and data directories must not be redirected'}
 }
 $existing=Get-CimInstance Win32_Service -Filter "Name='RetroDuetLighting'"
 if($existing -and $existing.PathName.Trim('"') -ine $exePath){throw 'Different service uses this name'}
 if($existing){$svc=Get-Service $serviceName;$wasRunning=$svc.Status -eq 'Running';if($svc.Status -ne 'Stopped'){Stop-Service $serviceName;$svc.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(15))}}
 if($Action -eq 'Uninstall'){
  if($existing){& sc.exe delete $serviceName;if($LASTEXITCODE){throw 'Service deletion failed'}}
  Get-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
  if(Test-Path -LiteralPath $exePath){Remove-Item -LiteralPath $exePath -Force}
  Write-Host 'Unified lighting service removed; settings and recovery data retained.'
 }else{
  $source=Join-Path $PSScriptRoot 'RetroDuetLighting.exe'
  if(!(Test-Path -LiteralPath $source)){throw 'Extract the complete test package'}
  New-Item -ItemType Directory -Path $installRoot,$dataRoot -Force | Out-Null
  if(Test-Path -LiteralPath $exePath){Copy-Item -LiteralPath $exePath -Destination ($exePath+'.previous') -Force}
  Copy-Item -LiteralPath $source -Destination $exePath -Force
  $replaced=$true
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot "engine_worker.py") -Destination $installRoot -Force
  if(!(Test-Path -LiteralPath (Join-Path $installRoot "runtime"))){Copy-Item -LiteralPath (Join-Path $PSScriptRoot "runtime") -Destination $installRoot -Recurse}

  if(!(Test-Path -LiteralPath (Join-Path $installRoot 'settings.json'))){Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'settings.json') -Destination $installRoot}
  # Recovery state is written only by the service and administrators.
  $previousAcl=Get-Acl -LiteralPath $dataRoot
  $acl=Get-Acl -LiteralPath $dataRoot
  $acl.SetAccessRuleProtection($true,$false)
  foreach($sid in @('S-1-5-18','S-1-5-32-544')){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new($sid),'FullControl','ContainerInherit,ObjectInherit','None','Allow'))}
  $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new('S-1-5-32-545'),'ReadAndExecute','ContainerInherit,ObjectInherit','None','Allow'))
  Set-Acl -LiteralPath $dataRoot -AclObject $acl
  if(!$existing){New-Service -Name $serviceName -BinaryPathName ('"'+$exePath+'"') -DisplayName 'Retro Duet Lighting' -StartupType Automatic -DependsOn Tcpip | Out-Null}
  else{& sc.exe config $serviceName obj= LocalSystem start= auto;if($LASTEXITCODE){throw 'Cannot update unified service account'}}
  & sc.exe description $serviceName 'Govee boot/shutdown lighting and Lenovo official screensaver sync.' | Out-Null
  & $exePath --configure;if($LASTEXITCODE){throw 'Cannot configure shutdown recovery timeout'}
  Get-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
  New-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -DisplayName 'Retro Duet Lighting - Govee LAN discovery' -Direction Inbound -Action Allow -Program $exePath -Protocol UDP -LocalPort 4002 -RemoteAddress LocalSubnet -Profile Private,Domain | Out-Null
  Start-Service $serviceName
  (Get-Service $serviceName).WaitForStatus('Running',[TimeSpan]::FromSeconds(10))
  Write-Host 'Unified service installed. Screensaver runs without elevation.'
 }
}catch{
 if($Action -eq 'Install' -and $existing){
  try{
   Stop-Service $serviceName -ErrorAction SilentlyContinue
   if($replaced -and (Test-Path -LiteralPath ($exePath+'.previous'))){Copy-Item -LiteralPath ($exePath+'.previous') -Destination $exePath -Force}
   if($previousAcl){Set-Acl -LiteralPath $dataRoot -AclObject $previousAcl}
   & sc.exe config $serviceName obj= $existing.StartName | Out-Null
   if($wasRunning){Start-Service $serviceName}
  }catch{Write-Warning ('Previous service restore needs attention: '+$_)}
 }
 throw
}finally{Stop-Transcript | Out-Null}
