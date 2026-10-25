param([ValidateSet('Install','Uninstall')][string]$Action='Install')
$ErrorActionPreference='Stop'
$serviceName='RetroDuetLighting'
$installRoot=Join-Path $env:ProgramFiles 'RetroDuetLighting'
$dataRoot=Join-Path $env:ProgramData 'RetroDuetLighting'
$exePath=Join-Path $installRoot 'RetroDuetLighting.exe'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=New-Object Security.Principal.WindowsPrincipal($identity)
if(!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
 try{
  $process=Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"'),'-Action',$Action) -Verb RunAs -WindowStyle Hidden -Wait -PassThru
  if($process.ExitCode -ne 0){throw "Installer failed. See $env:TEMP\RetroDuetLighting-Install.log"}
  Write-Host "$Action completed."
  exit 0
 }catch{Write-Error $_;exit 1}
}
Start-Transcript -Path (Join-Path $env:TEMP 'RetroDuetLighting-Install.log') -Append | Out-Null
try{
 $existing=Get-WmiObject Win32_Service -Filter "Name='RetroDuetLighting'"
 if($existing -and $existing.PathName.Trim('"') -ine $exePath){throw 'A different service uses this name. Refusing to overwrite it.'}
 if($existing){
  $svc=Get-Service -Name $serviceName
  if($svc.Status -ne 'Stopped'){Stop-Service -Name $serviceName;$svc.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(10))}
 }
 if($Action -eq 'Uninstall'){
  if($existing){& "$env:SystemRoot\System32\sc.exe" delete $serviceName;if($LASTEXITCODE -ne 0){throw 'Service deletion failed'}}
  Get-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
  # Only named files are removed. Preserve configuration, discovery history and logs.
  if([IO.Path]::GetFullPath($installRoot) -ine [IO.Path]::GetFullPath((Join-Path $env:ProgramFiles 'RetroDuetLighting'))){throw 'Unexpected installation path'}
  $item=Get-Item -LiteralPath $installRoot -ErrorAction SilentlyContinue
  if($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Refusing to modify a redirected installation folder'}
  $installedExe=Join-Path $installRoot 'RetroDuetLighting.exe'
  if(Test-Path -LiteralPath $installedExe){Remove-Item -LiteralPath $installedExe -Force}
  Write-Host 'Removed service and firewall rule. Settings and logs were kept.'
 }else{
  $sourceExe=Join-Path $PSScriptRoot 'RetroDuetLighting.exe'
  if(!(Test-Path -LiteralPath $sourceExe)){throw 'Extract the complete ZIP before installing.'}
  New-Item -ItemType Directory -Path $installRoot,$dataRoot -Force | Out-Null
  if((Get-Item -LiteralPath $installRoot).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Installation directory must not be redirected'}
  if((Get-Item -LiteralPath $dataRoot).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Data directory must not be redirected'}
  & "$env:SystemRoot\System32\icacls.exe" $dataRoot /grant '*S-1-5-19:(OI)(CI)M' | Out-Null
  if($LASTEXITCODE -ne 0){throw 'Cannot grant LocalService data access'}
  if([IO.Path]::GetFullPath($sourceExe) -ine [IO.Path]::GetFullPath($exePath)){Copy-Item -LiteralPath $sourceExe -Destination $exePath -Force}
  $config=Join-Path $installRoot 'settings.json'
  if(!(Test-Path -LiteralPath $config)){Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'settings.json') -Destination $config}
  Get-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
  New-NetFirewallRule -Name 'RetroDuetLighting.Discovery' -DisplayName 'Retro Duet Lighting - Govee LAN discovery' -Direction Inbound -Action Allow -Program $exePath -Protocol UDP -LocalPort 4002 -RemoteAddress LocalSubnet -Profile Private,Domain | Out-Null
  if(!$existing){
   $class=[wmiclass]'\\.\root\cimv2:Win32_Service'
   $parameters=$class.GetMethodParameters('Create')
   $parameters.Name=$serviceName
   $parameters.DisplayName='Retro Duet Lighting'
   $parameters.PathName='"'+$exePath+'"'
   $parameters.ServiceType=16
   $parameters.ErrorControl=1
   $parameters.StartMode='Automatic'
   $parameters.DesktopInteract=$false
   $parameters.StartName='NT AUTHORITY\LocalService'
   $parameters.ServiceDependencies=@('Tcpip')
   $result=$class.InvokeMethod('Create',$parameters,$null)
   if($result.ReturnValue -ne 0){throw ('Service creation failed: '+$result.ReturnValue)}
  }else{Set-Service -Name $serviceName -StartupType Automatic}
  & "$env:SystemRoot\System32\sc.exe" description $serviceName 'Govee LAN: 5400 K / 100% at boot; off at normal shutdown. No cloud account.' | Out-Null
  if($LASTEXITCODE -ne 0){throw 'Cannot set service description'}
  & $exePath --configure
  if($LASTEXITCODE -ne 0){throw 'Cannot configure preshutdown timeout'}
  Start-Service -Name $serviceName
  (Get-Service -Name $serviceName).WaitForStatus('Running',[TimeSpan]::FromSeconds(10))
  Write-Host 'Installed and started. Background operation has no visible window.'
 }
 Stop-Transcript | Out-Null
 exit 0
}catch{
 Write-Output $_
 try{Stop-Transcript | Out-Null}catch{}
 exit 1
}
