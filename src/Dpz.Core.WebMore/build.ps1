Write-Host "build.ps1 has moved to ../build.ps1" -ForegroundColor yellow
& ([System.IO.Path]::Combine($PSScriptRoot, '..', 'build.ps1'))