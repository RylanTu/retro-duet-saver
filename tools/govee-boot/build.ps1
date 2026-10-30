param([string]$Version='1.0.1')
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$dist=Join-Path $repoRoot 'dist'
$package=Join-Path $dist ('GoveeBootLighting-v'+$Version)
New-Item -ItemType Directory -Path $package -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $package 'RetroDuetLighting.exe'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /main:RetroDuetLighting.BootService /reference:System.Web.Extensions.dll "/out:$exe" (Join-Path $PSScriptRoot 'BootService.cs')
if($LASTEXITCODE -ne 0){throw 'Service compilation failed'}
foreach($name in @('Manage.ps1','Install.cmd','Uninstall.cmd','settings.json','README.md','BootService.cs')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $package -Force}
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $package -Force
$zip=Join-Path $dist ('GoveeBootLighting-v'+$Version+'-windows.zip')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $dist 'GoveeBootLighting-SHA256SUMS.txt'),$hash+'  '+[IO.Path]::GetFileName($zip)+[Environment]::NewLine)
Write-Host ('Package: '+$zip)
