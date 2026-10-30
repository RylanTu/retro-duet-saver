param([ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$|^[0-9]+\.[0-9]+$')][string]$Version = '3.6.7')
$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
$distDir = Join-Path $rootDir 'dist'
$packageDir = Join-Path $distDir ('RetroDuetSaver-v' + $Version)
$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkDir 'csc.exe'
if (!(Test-Path -LiteralPath $compilerPath)) { throw 'Windows x64 .NET Framework 4.x compiler was not found.' }
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
$exePath = Join-Path $packageDir 'RetroDuetSaver.exe'
$compileArgs = @('/nologo','/target:winexe','/optimize+','/platform:anycpu','/main:RetroWindows.DuetApp', "/out:$exePath")
$compileArgs += ('/resource:' + (Join-Path $rootDir 'shaders\PixelShrink.ps') + ',PixelShrink.ps')
$refs = @('System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','System.Core.dll','System.Web.Extensions.dll',(Join-Path $frameworkDir 'WPF\WindowsBase.dll'),(Join-Path $frameworkDir 'WPF\PresentationCore.dll'),(Join-Path $frameworkDir 'WPF\PresentationFramework.dll'),(Join-Path $frameworkDir 'System.Xaml.dll'))
foreach ($refPath in $refs) { $compileArgs += "/reference:$refPath" }
foreach ($sourceFile in Get-ChildItem -LiteralPath (Join-Path $rootDir 'src') -Filter '*.cs') { $compileArgs += $sourceFile.FullName }
& $compilerPath @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath $exePath -Destination (Join-Path $packageDir 'RetroDuetSaver.scr') -Force
foreach ($fileName in @('README.md','LICENSE','THIRD_PARTY_NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $rootDir $fileName) -Destination $packageDir -Force }
Copy-Item -LiteralPath (Join-Path $rootDir 'docs') -Destination $packageDir -Recurse -Force
Copy-Item -LiteralPath (Join-Path $rootDir 'examples') -Destination $packageDir -Recurse -Force
Get-ChildItem -LiteralPath (Join-Path $rootDir 'scripts') -Filter '*.cmd' | Copy-Item -Destination $packageDir -Force
$zipPath = Join-Path $distDir ('RetroDuetSaver-v' + $Version + '-windows.zip')
Compress-Archive -Path (Join-Path $packageDir '*') -DestinationPath $zipPath -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $distDir 'SHA256SUMS.txt'), $hash + '  ' + [IO.Path]::GetFileName($zipPath) + [Environment]::NewLine)
Write-Host "Package: $zipPath"
