# Build and Package script for AzuModsValheim1Compat
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "===> Building AzuModsValheim1Compat in Release mode..." -ForegroundColor Cyan
dotnet build -c Release

$version = "1.0.3"
$distDir = Join-Path $scriptDir "package"
$patcherTargetDir = Join-Path $distDir "patchers\AzuModsValheim1Compat"

if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $patcherTargetDir | Out-Null

Write-Host "===> Assembling Thunderstore package..." -ForegroundColor Cyan
Copy-Item (Join-Path $scriptDir "manifest.json") -Destination $distDir
Copy-Item (Join-Path $scriptDir "README.md") -Destination $distDir
Copy-Item (Join-Path $scriptDir "icon.png") -Destination $distDir
Copy-Item (Join-Path $scriptDir "bin\Release\netstandard2.0\AzuModsValheim1Compat.dll") -Destination $patcherTargetDir

$zipPath = Join-Path $scriptDir "AzuModsValheim1Compat-$version.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "===> Creating Zip archive: $zipPath..." -ForegroundColor Cyan
python -c "
import zipfile, os
dist_dir = r'$distDir'
zip_path = r'$zipPath'
with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as z:
    for root, dirs, files in os.walk(dist_dir):
        for file in files:
            full = os.path.join(root, file)
            rel = os.path.relpath(full, dist_dir).replace('\\', '/')
            z.write(full, rel)
"

Write-Host "===> Thunderstore package ready at: $zipPath" -ForegroundColor Green

# Deploy to local r2modman profile if present
$r2profile1 = "C:\Users\aborigen\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\sborka\BepInEx\patchers\AzuModsValheim1Compat"
$r2profile2 = "C:\Users\aborigen\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\sborka\BepInEx\patchers\KRINJUN-AzuModsValheim1Compat\AzuModsValheim1Compat"

if (Test-Path (Split-Path -Parent $r2profile2)) {
    Write-Host "===> Deploying to r2modman installed package: $r2profile2..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $r2profile2 | Out-Null
    Copy-Item (Join-Path $scriptDir "bin\Release\netstandard2.0\AzuModsValheim1Compat.dll") -Destination $r2profile2 -Force
    Write-Host "===> Successfully deployed to: $r2profile2" -ForegroundColor Green
} elseif (Test-Path (Split-Path -Parent $r2profile1)) {
    Write-Host "===> Deploying to r2modman profile 'sborka'..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $r2profile1 | Out-Null
    Copy-Item (Join-Path $scriptDir "bin\Release\netstandard2.0\AzuModsValheim1Compat.dll") -Destination $r2profile1 -Force
    Write-Host "===> Successfully deployed to: $r2profile1" -ForegroundColor Green
}
