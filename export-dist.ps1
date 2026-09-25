<#
.SYNOPSIS
    Exports and packages Valheim.ItemEnhancements for Thunderstore distribution.

.DESCRIPTION
    Automates the entire export and distribution pipeline for Thunderstore:
    1. Validates or updates semantic versioning across .csproj and manifest.json.
    2. Builds the project in Release configuration.
    3. Validates required Thunderstore assets:
       - manifest.json (structure, semver, name regex, description length <= 250)
       - icon.png (exact 256x256 PNG dimensions)
       - README.md & CHANGELOG.md
    4. Stages binaries (Valheim.ItemEnhancements.dll, YamlDotNet.dll) and configs
       (ckforgame.ItemEnhancements.cfg, YAML definition files) into standard Thunderstore BepInEx structure:
       - /manifest.json
       - /icon.png
       - /README.md
       - /CHANGELOG.md
       - /plugins/Valheim.ItemEnhancements/...
       - /config/ckforgame.ItemEnhancements/...
    5. Creates a compressed, ready-to-upload ZIP package in the 'dist' directory.
    6. Validates the archive integrity, calculates SHA256 hash, and displays upload instructions.

.PARAMETER Configuration
    Build configuration to compile (Release or Debug). Defaults to Release.

.PARAMETER Version
    Optional semantic version override (e.g., 1.0.0). If omitted, auto-detected from .csproj.

.PARAMETER SkipBuild
    Skips running 'dotnet build' if binaries are already up-to-date.

.PARAMETER Clean
    Cleans dist and staging directories before packaging.

.PARAMETER IncludePdb
    Includes .pdb debug symbols in the plugin package directory.

.PARAMETER NoPause
    Suppresses pause prompt when executed through wrapper scripts.

.PARAMETER OutputDir
    Output directory for the generated zip package. Defaults to 'dist'.

.EXAMPLE
    .\export-dist.ps1
    Builds in Release mode and exports dist\ItemEnhancements-1.0.0.zip.

.EXAMPLE
    .\export-dist.ps1 -Version 1.0.1 -Clean
    Bumps version to 1.0.1, cleans dist folder, compiles Release, and packages.
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$Version = "",

    [switch]$SkipBuild,

    [switch]$Clean,

    [switch]$IncludePdb,

    [switch]$NoPause,

    [string]$OutputDir = "dist"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Setup color helpers
function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor DarkCyan
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Failure {
    param([string]$Message)
    Write-Host ""
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Resolve directory paths
$projectRoot  = $PSScriptRoot
$csprojPath   = Join-Path $projectRoot "Valheim.ItemEnhancements.csproj"
$pluginCsPath = Join-Path $projectRoot "Plugin.cs"
$manifestPath = Join-Path $projectRoot "manifest.json"
$iconPath     = Join-Path $projectRoot "icon.png"
$readmePath   = Join-Path $projectRoot "README.md"
$changelogPath = Join-Path $projectRoot "CHANGELOG.md"
$licensePath   = Join-Path $projectRoot "LICENSE"
$configDir    = Join-Path $projectRoot "Configuration"
$yamlDir      = Join-Path $configDir "Yaml"
$binDir       = Join-Path $projectRoot "bin\$Configuration\net48"
$distDir      = Join-Path $projectRoot $OutputDir
$stagingDir   = Join-Path $distDir "staging"

Write-Header "Valheim Item Enhancements - Thunderstore Export & Dist Pipeline"

# -------------------------------------------------------------------------------------------------
# 1. Version Detection & Synchronization
# -------------------------------------------------------------------------------------------------
Write-Info "Checking version configuration..."
$currentVersion = ""

if ([System.IO.File]::Exists($csprojPath)) {
    $csprojContent = [System.IO.File]::ReadAllText($csprojPath)
    if ($csprojContent -match '<Version>([^<]+)</Version>') {
        $currentVersion = $matches[1].Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ([string]::IsNullOrWhiteSpace($currentVersion)) {
        $currentVersion = "1.0.0"
        Write-Warn "Could not detect version from .csproj, defaulting to $currentVersion"
    } else {
        Write-Info "Detected version $currentVersion from Valheim.ItemEnhancements.csproj"
    }
} else {
    $currentVersion = $Version.Trim()
    Write-Info "Using user-specified version: $currentVersion"
    
    # Update .csproj if version was explicitly provided and differs
    if ([System.IO.File]::Exists($csprojPath)) {
        $updatedCsproj = [System.Text.RegularExpressions.Regex]::Replace(
            $csprojContent,
            '<Version>[^<]+</Version>',
            "<Version>$currentVersion</Version>"
        )
        if ($updatedCsproj -ne $csprojContent) {
            [System.IO.File]::WriteAllText($csprojPath, $updatedCsproj)
            Write-Info "Updated version in Valheim.ItemEnhancements.csproj to $currentVersion"
        }
    }
}

# Ensure manifest.json exists and version matches
if (-not [System.IO.File]::Exists($manifestPath)) {
    Write-Failure "manifest.json not found at '$manifestPath'!"
}

$manifestRaw = [System.IO.File]::ReadAllText($manifestPath)
$manifestObj = ConvertFrom-Json $manifestRaw

if ($manifestObj.version_number -ne $currentVersion) {
    Write-Info "Updating manifest.json version from '$($manifestObj.version_number)' to '$currentVersion'..."
    $manifestObj.version_number = $currentVersion
    $newManifestJson = ConvertTo-Json $manifestObj -Depth 5
    [System.IO.File]::WriteAllText($manifestPath, $newManifestJson)
}

# -------------------------------------------------------------------------------------------------
# 2. Thunderstore Requirements Validation
# -------------------------------------------------------------------------------------------------
Write-Info "Validating Thunderstore package rules..."

# Validate Manifest fields
if ([string]::IsNullOrWhiteSpace($manifestObj.name)) {
    Write-Failure "manifest.json: 'name' field cannot be empty!"
}
if ($manifestObj.name -notmatch '^[a-zA-Z0-9_]+$') {
    Write-Failure "manifest.json: 'name' field ($($manifestObj.name)) must contain only alphanumeric characters and underscores!"
}
if ($manifestObj.version_number -notmatch '^\d+\.\d+\.\d+$') {
    Write-Failure "manifest.json: 'version_number' ($($manifestObj.version_number)) must follow Semantic Versioning (X.Y.Z)!"
}
if ([string]::IsNullOrWhiteSpace($manifestObj.description)) {
    Write-Failure "manifest.json: 'description' cannot be empty!"
}
if ($manifestObj.description.Length -gt 250) {
    Write-Failure "manifest.json: 'description' is $($manifestObj.description.Length) characters. Thunderstore limit is 250 characters!"
}
if ($null -eq $manifestObj.dependencies -or $manifestObj.dependencies.Count -eq 0) {
    Write-Warn "manifest.json: 'dependencies' array is empty. Usually 'denikson-BepInExPack_Valheim-5.4.2202' is required."
}

# Validate icon.png
if (-not [System.IO.File]::Exists($iconPath)) {
    Write-Failure "icon.png not found at '$iconPath'! Thunderstore requires a 256x256 icon.png."
}

Add-Type -AssemblyName System.Drawing
try {
    $iconImg = [System.Drawing.Image]::FromFile($iconPath)
    $iconW = $iconImg.Width
    $iconH = $iconImg.Height
    $iconImg.Dispose()

    if ($iconW -ne 256 -or $iconH -ne 256) {
        Write-Failure "icon.png is ${iconW}x${iconH}. Thunderstore strictly requires exactly 256x256 pixels!"
    }
    Write-Success "icon.png verified (256x256 PNG)."
} catch {
    Write-Failure "Failed to read icon.png as a valid image: $_"
}

# Validate README.md & CHANGELOG.md
if (-not [System.IO.File]::Exists($readmePath) -or (Get-Item $readmePath).Length -eq 0) {
    Write-Failure "README.md is missing or empty! Thunderstore requires a valid README.md."
}
Write-Success "README.md verified."

if (-not [System.IO.File]::Exists($changelogPath)) {
    Write-Warn "CHANGELOG.md not found. It is strongly recommended to include CHANGELOG.md for Thunderstore releases."
}

# -------------------------------------------------------------------------------------------------
# 3. Compile Project
# -------------------------------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Info "Compiling project with: dotnet build -c $Configuration..."
    $buildOutput = & dotnet build $csprojPath -c $Configuration 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host $buildOutput
        Write-Failure "Compilation failed with exit code $LASTEXITCODE!"
    }
    Write-Success "Compilation succeeded ($Configuration)."
} else {
    Write-Info "Skipping build (-SkipBuild specified)."
}

# Verify output binaries exist
$mainDllPath = Join-Path $binDir "Valheim.ItemEnhancements.dll"
$yamlDllPath = Join-Path $binDir "YamlDotNet.dll"

if (-not [System.IO.File]::Exists($mainDllPath)) {
    Write-Failure "Target binary not found: '$mainDllPath'! Please compile the project first."
}
if (-not [System.IO.File]::Exists($yamlDllPath)) {
    Write-Failure "Required dependency 'YamlDotNet.dll' not found in '$binDir'!"
}

# -------------------------------------------------------------------------------------------------
# 4. Clean & Prepare Staging Area
# -------------------------------------------------------------------------------------------------
Write-Info "Preparing staging directory at '$stagingDir'..."

if ($Clean -and [System.IO.Directory]::Exists($distDir)) {
    Write-Info "Cleaning output directory '$distDir'..."
    Remove-Item -Path $distDir -Recurse -Force
}

if ([System.IO.Directory]::Exists($stagingDir)) {
    Remove-Item -Path $stagingDir -Recurse -Force
}

$stagePluginsDir = Join-Path $stagingDir "plugins\Valheim.ItemEnhancements"
$stageConfigDir  = Join-Path $stagingDir "config"
$stageYamlDir    = Join-Path $stageConfigDir "ckforgame.ItemEnhancements"

[System.IO.Directory]::CreateDirectory($stagingDir) | Out-Null
[System.IO.Directory]::CreateDirectory($stagePluginsDir) | Out-Null
[System.IO.Directory]::CreateDirectory($stageYamlDir) | Out-Null

# Copy root metadata files
Copy-Item -Path $manifestPath -Destination $stagingDir -Force
Copy-Item -Path $iconPath     -Destination $stagingDir -Force
Copy-Item -Path $readmePath   -Destination $stagingDir -Force
if ([System.IO.File]::Exists($changelogPath)) {
    Copy-Item -Path $changelogPath -Destination $stagingDir -Force
}
if ([System.IO.File]::Exists($licensePath)) {
    Copy-Item -Path $licensePath -Destination $stagingDir -Force
}

# Copy plugin binaries
Copy-Item -Path $mainDllPath -Destination $stagePluginsDir -Force
Copy-Item -Path $yamlDllPath -Destination $stagePluginsDir -Force
if ($IncludePdb) {
    $pdbPath = Join-Path $binDir "Valheim.ItemEnhancements.pdb"
    if ([System.IO.File]::Exists($pdbPath)) {
        Copy-Item -Path $pdbPath -Destination $stagePluginsDir -Force
        Write-Info "Included debug symbols: Valheim.ItemEnhancements.pdb"
    }
}

# Copy configuration files
$cfgFile = Join-Path $configDir "ckforgame.ItemEnhancements.cfg"
if ([System.IO.File]::Exists($cfgFile)) {
    Copy-Item -Path $cfgFile -Destination $stageConfigDir -Force
}

if ([System.IO.Directory]::Exists($yamlDir)) {
    Get-ChildItem -Path $yamlDir -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $stageYamlDir -Force
    }
}

Write-Success "Staging directory populated successfully."

# -------------------------------------------------------------------------------------------------
# 5. Build ZIP Package
# -------------------------------------------------------------------------------------------------
[System.IO.Directory]::CreateDirectory($distDir) | Out-Null

$zipFileName = "$($manifestObj.name)-$currentVersion.zip"
$zipPath = Join-Path $distDir $zipFileName

if ([System.IO.File]::Exists($zipPath)) {
    Write-Info "Overwriting existing archive '$zipFileName'..."
    Remove-Item -Path $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Info "Compressing package into '$zipFileName' (POSIX forward-slash compliant)..."
$zipStream = [System.IO.File]::Create($zipPath)
$archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)

try {
    $stagedFiles = Get-ChildItem -Path $stagingDir -Recurse -File
    foreach ($file in $stagedFiles) {
        $relativePath = $file.FullName.Substring($stagingDir.Length).TrimStart('\', '/')
        $normalizedEntryName = $relativePath.Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $normalizedEntryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $archive.Dispose()
    $zipStream.Dispose()
}

# Create dual convenience copy named ckforgame-ItemEnhancements-X.Y.Z.zip
$teamZipName = "ckforgame-$($manifestObj.name)-$currentVersion.zip"
$teamZipPath = Join-Path $distDir $teamZipName
Copy-Item -Path $zipPath -Destination $teamZipPath -Force

# -------------------------------------------------------------------------------------------------
# 6. Post-Package Verification & Summary
# -------------------------------------------------------------------------------------------------
Write-Info "Verifying generated ZIP archive..."
$zipArchive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$entryCount = $zipArchive.Entries.Count

$hasManifest = $false
$hasIcon     = $false
$hasReadme   = $false
$hasMainDll  = $false

Write-Host ""
Write-Host "  Archive Contents ($zipFileName):" -ForegroundColor DarkGray
foreach ($entry in $zipArchive.Entries) {
    Write-Host "    |-- $($entry.FullName) ($($entry.Length) bytes)" -ForegroundColor DarkGray
    if ($entry.FullName -eq "manifest.json") { $hasManifest = $true }
    if ($entry.FullName -eq "icon.png")     { $hasIcon = $true }
    if ($entry.FullName -eq "README.md")    { $hasReadme = $true }
    if ($entry.FullName -like "*Valheim.ItemEnhancements.dll") { $hasMainDll = $true }
}
$zipArchive.Dispose()

if (-not ($hasManifest -and $hasIcon -and $hasReadme -and $hasMainDll)) {
    Write-Failure "Archive verification failed! One or more critical files missing from root of archive."
}

# Calculate checksum & size
$fileInfo = Get-Item $zipPath
$sizeKb   = [math]::Round($fileInfo.Length / 1024, 2)
$hashInfo = Get-FileHash -Path $zipPath -Algorithm SHA256

Write-Header "EXPORT COMPLETE & THUNDERSTORE READY"

Write-Host "Package Details:" -ForegroundColor White
Write-Host "  * Mod Name       : $($manifestObj.name)" -ForegroundColor Green
Write-Host "  * Version        : $currentVersion" -ForegroundColor Green
Write-Host "  * File Name      : $zipFileName" -ForegroundColor Green
Write-Host "  * Alternate Name : $teamZipName" -ForegroundColor Green
Write-Host "  * Package Size   : $sizeKb KB" -ForegroundColor Green
Write-Host "  * Total Entries  : $entryCount files" -ForegroundColor Green
Write-Host "  * Output Path    : $zipPath" -ForegroundColor Yellow
Write-Host "  * SHA-256 Hash   : $($hashInfo.Hash)" -ForegroundColor Cyan

Write-Host ""
Write-Host "Next Steps for Uploading to Thunderstore:" -ForegroundColor Yellow
Write-Host "  1. Web Upload:" -ForegroundColor White
Write-Host "     - Open browser: https://valheim.thunderstore.io/package/create/" -ForegroundColor DarkCyan
Write-Host "     - Log in with your Thunderstore / Discord / GitHub account." -ForegroundColor DarkCyan
Write-Host "     - Select or create your Team (e.g. 'ckforgame')." -ForegroundColor DarkCyan
Write-Host "     - Upload file: '$zipPath'" -ForegroundColor Green
Write-Host "     - Select Communities: Valheim" -ForegroundColor DarkCyan
Write-Host "     - Select Categories: Mods, Server-side, Client-side, Mechanics, Tools, etc." -ForegroundColor DarkCyan
Write-Host "     - Click 'Submit'." -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  2. CLI Upload (Optional via tcli):" -ForegroundColor White
Write-Host "     tcli publish --package-path `"$zipPath`"" -ForegroundColor DarkCyan
Write-Host ""