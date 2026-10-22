$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
& gh repo create RylanTu/retro-duet-saver --public --description 'Macintosh x Windows 95/98 pixel screensaver for Windows 11' --source . --remote origin --push
if ($LASTEXITCODE -ne 0) { throw 'Repository creation or push failed.' }
& gh release create v3.5 './dist/RetroDuetSaver-v3.5-windows.zip' './dist/SHA256SUMS.txt' --repo RylanTu/retro-duet-saver --target main --title 'Retro Duet Saver v3.5' --notes-file './release-notes/v3.5.md'
if ($LASTEXITCODE -ne 0) { throw 'Release creation failed.' }
