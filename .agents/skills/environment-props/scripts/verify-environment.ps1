<#
.SYNOPSIS
  Verifies Environment.props configuration and reference paths for game modding projects.
.DESCRIPTION
  This script inspects Environment.props, validates that configured directories exist,
  verifies critical game and BepInEx assemblies, and checks .gitignore protection.
.PARAMETER ProjectPath
  Path to the mod project directory (defaults to current directory).
#>

[CmdletBinding()]
param (
    [string]$ProjectPath = (Get-Location).Path
)

function Write-Status {
    param(
        [string]$Status, # OK, WARN, FAIL, INFO
        [string]$Message
    )
    switch ($Status) {
        "OK"   { Write-Host "  [+] OK:   " -ForegroundColor Green -NoNewline; Write-Host $Message }
        "WARN" { Write-Host "  [!] WARN: " -ForegroundColor Yellow -NoNewline; Write-Host $Message }
        "FAIL" { Write-Host "  [-] FAIL: " -ForegroundColor Red -NoNewline; Write-Host $Message }
        "INFO" { Write-Host "  [*] INFO: " -ForegroundColor Cyan -NoNewline; Write-Host $Message }
    }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "   Environment.props Diagnostic & Validation Tool       " -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

$envPropsPath = Join-Path $ProjectPath "Environment.props"
$examplePropsPath = Join-Path $ProjectPath "Environment.props.example"
$gitignorePath = Join-Path $ProjectPath ".gitignore"

# 1. Check Environment.props existence
if (Test-Path $envPropsPath) {
    Write-Status "OK" "Found Environment.props at: $envPropsPath"
} elseif (Test-Path $examplePropsPath) {
    Write-Status "WARN" "Environment.props not found, but Environment.props.example exists!"
    Write-Status "INFO" "Run: Copy-Item '$examplePropsPath' '$envPropsPath' and adjust paths."
} else {
    Write-Status "FAIL" "Neither Environment.props nor Environment.props.example found in: $ProjectPath"
}

# 2. Check .gitignore
if (Test-Path $gitignorePath) {
    $gitignoreContent = Get-Content $gitignorePath -Raw
    if ($gitignoreContent -match "(?m)^Environment\.props") {
        Write-Status "OK" ".gitignore properly ignores 'Environment.props'"
    } else {
        Write-Status "WARN" "'Environment.props' is NOT listed in .gitignore! Add it to prevent committing local paths."
    }
} else {
    Write-Status "WARN" "No .gitignore found in project directory."
}

# 3. Parse XML if Environment.props exists
if (Test-Path $envPropsPath) {
    Write-Host "`nValidating Configured Paths..." -ForegroundColor White
    try {
        [xml]$xml = Get-Content $envPropsPath
        $propGroup = $xml.Project.PropertyGroup

        # Game Path
        $valheimPath = $propGroup.ValheimPath
        if (-not $valheimPath) { $valheimPath = $propGroup.GamePath }

        if ($valheimPath) {
            # Expand environment variables like $(AppData)
            $expandedPath = [System.Environment]::ExpandEnvironmentVariables($valheimPath.Replace("`$(AppData)", $env:APPDATA).Replace("`$(UserProfile)", $env:USERPROFILE))
            if (Test-Path $expandedPath) {
                Write-Status "OK" "Game directory exists: $expandedPath"

                # Check Managed DLLs
                $managedDir = Join-Path $expandedPath "valheim_Data\Managed"
                if (Test-Path $managedDir) {
                    Write-Status "OK" "Managed directory exists: $managedDir"
                    $coreAssemblies = @("assembly_valheim.dll", "assembly_guiutils.dll", "UnityEngine.dll", "UnityEngine.CoreModule.dll")
                    foreach ($asm in $coreAssemblies) {
                        $asmPath = Join-Path $managedDir $asm
                        if (Test-Path $asmPath) {
                            Write-Status "OK" "  Found assembly: $asm"
                        } else {
                            Write-Status "FAIL" "  Missing assembly: $asm (Path: $asmPath)"
                        }
                    }
                } else {
                    Write-Status "FAIL" "valheim_Data\Managed directory not found inside: $expandedPath"
                }
            } else {
                Write-Status "FAIL" "Game directory not found: $expandedPath"
            }
        } else {
            Write-Status "WARN" "ValheimPath (or GamePath) not defined in Environment.props."
        }

        # r2modman Profile Dir
        $r2Dir = $propGroup.R2ModmanProfileDir
        if ($r2Dir) {
            $expandedR2 = [System.Environment]::ExpandEnvironmentVariables($r2Dir.Replace("`$(AppData)", $env:APPDATA).Replace("`$(UserProfile)", $env:USERPROFILE))
            if (Test-Path $expandedR2) {
                Write-Status "OK" "r2modman profile directory exists: $expandedR2"
                $bepCore = Join-Path $expandedR2 "BepInEx\core\BepInEx.dll"
                if (Test-Path $bepCore) {
                    Write-Status "OK" "Found profile BepInEx.dll: $bepCore"
                } else {
                    Write-Status "WARN" "Profile BepInEx.dll not found at: $bepCore"
                }
            } else {
                Write-Status "WARN" "r2modman profile directory does not exist: $expandedR2"
            }
        } else {
            Write-Status "INFO" "R2ModmanProfileDir is not defined (optional)."
        }

    } catch {
        Write-Status "FAIL" "Failed to parse Environment.props as XML: $_"
    }
}

Write-Host "`nDiagnostic complete.`n" -ForegroundColor Cyan
