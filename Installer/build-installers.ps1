$ErrorActionPreference = 'Stop'

$installerDir = $PSScriptRoot
$projectDir = Split-Path -Parent $installerDir
$portableDir = Join-Path $installerDir 'Portable'
$setupDir = Join-Path $installerDir 'Setup'
$projectFile = Join-Path $projectDir 'PCMedicSuite.csproj'
$wxsFile = Join-Path $setupDir 'PCMedicSuite.wxs'
$issFile = Join-Path $setupDir 'PCMedicSuite.iss'
$setupExe = Join-Path $setupDir 'PCMedicSuite-Setup-1.5.0-x64.exe'
$msiFile = Join-Path $setupDir 'PCMedicSuite-1.5.0-x64.msi'

New-Item -ItemType Directory -Force -Path $portableDir, $setupDir | Out-Null

# Keep the portable build independent of an installed .NET runtime.
& dotnet publish $projectFile --configuration Release --runtime win-x64 --self-contained true `
    --output $portableDir "-p:DiskDefragProject=$(Join-Path $projectDir '..\DiskDefrag\DiskDefrag.csproj')"
if ($LASTEXITCODE -ne 0) {
    throw "PC Medic publish failed with exit code $LASTEXITCODE"
}

# The companion is a separate project, so publish its standalone runtime alongside PC Medic too.
$companionProject = Join-Path $projectDir '..\DiskDefrag\DiskDefrag.csproj'
& dotnet publish $companionProject --configuration Release --runtime win-x64 --self-contained true `
    --output $portableDir
if ($LASTEXITCODE -ne 0) {
    throw "Disk Defrag publish failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path (Join-Path $portableDir 'PCMedicSuite.exe'))) {
    throw "The publish completed without producing PCMedicSuite.exe in $portableDir"
}

# Inno Setup 6 produces the primary installer: per-machine install into Program Files
# with no shortcuts or launch prompt. The WiX .wxs is kept for reference only.
$iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
}
if (Test-Path $iscc) {
    & $iscc $issFile
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compile failed with exit code $LASTEXITCODE"
    }
} else {
    Write-Warning "Inno Setup compiler not found; skipping setup.exe build."
}

Write-Host "Portable app: $portableDir"
Write-Host "Setup exe:    $setupExe"
