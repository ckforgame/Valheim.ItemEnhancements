---
name: environment-props
description: >-
  Comprehensive guide and reference for configuring, creating, and troubleshooting `Environment.props` in C# / .NET game modding projects (Valheim, Unity, BepInEx, MSBuild). Use this skill whenever setting up local machine paths (ValheimPath, R2ModmanProfileDir, BepInExCore, ValheimManaged), configuring .csproj with conditional imports and fallback defaults, creating Environment.props.example templates, setting up post-build auto-copy deployment to Steam/r2modman profiles, troubleshooting MSBuild reference/path errors, or configuring .gitignore for local environment files.
---

# Environment.props Guide & Best Practices (C# / Game Modding)

In C# game modding (e.g., Valheim, Unity games, BepInEx, MelonLoader), each developer's computer has game installations and mod manager profiles in different locations (e.g., `C:` vs `D:` drives, Steam vs Xbox Game Pass, custom r2modman profiles).

**`Environment.props`** is the standard architecture pattern that cleanly separates **local developer machine paths** from the **shared, version-controlled project configuration (`.csproj`)**.

---

## 1. Core Architecture: The 3-Tier Layering Model

```mermaid
flowchart TD
    A["Environment.props.example (Version Controlled Template)"] -.->|Developer copies to| B["Environment.props (Local Machine Only, Git-Ignored)"]
    B -->|Imported conditionally| C[".csproj Project File"]
    D["MSBuild Default Fallbacks (Condition=\"'$(Var)' == ''\")"] -->|Applies if not overridden| C
    E["CLI / CI/CD Arguments (-p:ValheimPath=...)"] -->|Highest Priority Override| C
    C --> F["Resolved References (assembly_valheim.dll, BepInEx.dll, UnityEngine.dll)"]
    C --> G["Post-Build Auto Deployment (Steam & r2modman)"]
```

### The Golden Rules
1. **Never commit `Environment.props` to Git**: Always add `Environment.props` and `*.user` to `.gitignore`.
2. **Always provide `Environment.props.example`**: Commit a template with clear comments and default paths.
3. **Always use MSBuild conditions in `.csproj`**:
   - Conditionally import: `<Import Project="$(ProjectDir)Environment.props" Condition="Exists('$(ProjectDir)Environment.props')" />`
   - Provide fallback defaults: `<ValheimPath Condition="'$(ValheimPath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Valheim</ValheimPath>`
4. **Never set `<Private>true</Private>` on game assemblies**: Keep `<Private>false</Private>` to prevent MSBuild from copying massive game engine DLLs into your mod output folder.

---

## 2. Directory Layout & Recommended Structure

```text
MyGameMod/
├── .gitignore                      # Must ignore Environment.props
├── Environment.props.example       # Committed template for other developers
├── Environment.props               # LOCAL ONLY: Created by developer (not in Git)
├── MyGameMod.csproj                # Contains conditional import + fallback defaults
├── Plugin.cs
└── scripts/
    ├── verify-environment.ps1      # Health check script
    └── init-environment.ps1        # Auto-detection script
```

---

## 3. Step-by-Step Implementation Guide

### Step 1: Create `Environment.props.example`
Commit this template to your repository:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!--
    Environment Configuration Template
    Copy this file to 'Environment.props' and adjust the paths to your local setup.
    'Environment.props' is excluded from Git (.gitignore).
  -->
  <PropertyGroup>
    <!-- Path to your game installation -->
    <ValheimPath>C:\Program Files (x86)\Steam\steamapps\common\Valheim</ValheimPath>

    <!-- Path to your r2modman profile directory containing BepInEx (optional) -->
    <R2ModmanProfileDir>$(AppData)\r2modmanPlus-local\Valheim\profiles\Default</R2ModmanProfileDir>
  </PropertyGroup>
</Project>
```
*(See more variations in [examples/Environment.props.example](./examples/Environment.props.example), [examples/Environment.props.minimal.example](./examples/Environment.props.minimal.example), and [examples/Environment.props.advanced.example](./examples/Environment.props.advanced.example))*

### Step 2: Ensure `.gitignore` Ignores Local Props
Add the following to your project's `.gitignore`:
```gitignore
# Local environment configuration
Environment.props
*.user
```

### Step 3: Configure `.csproj`
Add the conditional import right after the opening `<Project>` tag, followed by fallback properties:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- 1. Conditionally import local user environment if present -->
  <Import Project="$(ProjectDir)Environment.props" Condition="Exists('$(ProjectDir)Environment.props')" />
  <Import Project="$(MSBuildProjectFullPath).user" Condition="Exists('$(MSBuildProjectFullPath).user')" />

  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>MyValheimMod</AssemblyName>
    
    <!-- 2. Fallback defaults if not set in Environment.props -->
    <ValheimPath Condition="'$(ValheimPath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Valheim</ValheimPath>
    <ValheimManaged Condition="'$(ValheimManaged)' == ''">$(ValheimPath)\valheim_Data\Managed</ValheimManaged>

    <!-- Optional r2modman Profile Directory -->
    <R2ModmanProfileDir Condition="'$(R2ModmanProfileDir)' == ''"></R2ModmanProfileDir>

    <!-- BepInEx Core Path (searches profile first if set, falls back to game path) -->
    <BepInExCore Condition="'$(BepInExCore)' == '' and '$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)\BepInEx\core')">$(R2ModmanProfileDir)\BepInEx\core</BepInExCore>
    <BepInExCore Condition="'$(BepInExCore)' == ''">$(ValheimPath)\BepInEx\core</BepInExCore>

    <!-- Post-build deployment paths -->
    <R2ModmanPluginDir Condition="'$(R2ModmanPluginDir)' == '' and '$(R2ModmanProfileDir)' != ''">$(R2ModmanProfileDir)\BepInEx\plugins\$(AssemblyName)</R2ModmanPluginDir>
    <SteamPluginDir Condition="'$(SteamPluginDir)' == ''">$(ValheimPath)\BepInEx\plugins\$(AssemblyName)</SteamPluginDir>
  </PropertyGroup>

  <!-- 3. Assembly References using resolved paths -->
  <ItemGroup>
    <Reference Include="assembly_valheim">
      <HintPath>$(ValheimManaged)\assembly_valheim.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>$(ValheimManaged)\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="BepInEx">
      <HintPath>$(BepInExCore)\BepInEx.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(BepInExCore)\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- 4. Automated Post-Build Deployment to Steam & r2modman -->
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <ItemGroup>
      <PluginFiles Include="$(TargetDir)$(TargetName).dll" />
      <PluginFiles Include="$(TargetDir)$(TargetName).pdb" Condition="Exists('$(TargetDir)$(TargetName).pdb')" />
    </ItemGroup>

    <!-- Deploy to r2modman if configured and exists -->
    <MakeDir Directories="$(R2ModmanPluginDir)" Condition="'$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)')" />
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(R2ModmanPluginDir)" Condition="'$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)')" SkipUnchangedFiles="true" />

    <!-- Deploy to Steam if plugins folder exists -->
    <MakeDir Directories="$(SteamPluginDir)" Condition="Exists('$(ValheimPath)\BepInEx\plugins')" />
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(SteamPluginDir)" Condition="Exists('$(ValheimPath)\BepInEx\plugins')" SkipUnchangedFiles="true" />
  </Target>

</Project>
```
*(See full working `.csproj` in [examples/sample-game-mod.csproj](./examples/sample-game-mod.csproj))*

---

## 4. Multi-Project Solutions (`Directory.Build.props`)

When working on a multi-project solution (e.g. `ModCore`, `ModUI`, `ModServer`), avoid duplicating `Environment.props` in every project folder.

Place a single `Directory.Build.props` at the solution root:
```xml
<Project>
  <!-- Import solution-wide Environment.props -->
  <Import Project="$(MSBuildThisFileDirectory)Environment.props" Condition="Exists('$(MSBuildThisFileDirectory)Environment.props')" />

  <PropertyGroup>
    <ValheimPath Condition="'$(ValheimPath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Valheim</ValheimPath>
    <ValheimManaged Condition="'$(ValheimManaged)' == ''">$(ValheimPath)\valheim_Data\Managed</ValheimManaged>
  </PropertyGroup>
</Project>
```
*(See [examples/Directory.Build.props](./examples/Directory.Build.props))*

---

## 5. Built-in Diagnostic & Setup Scripts

This skill includes ready-to-run PowerShell helper scripts:

1. **Verification**: Validate your `Environment.props`, game paths, and assemblies:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/verify-environment.ps1
   ```
2. **Auto-Initialization**: Auto-detect Steam library registry and r2modman profiles to create `Environment.props`:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/init-environment.ps1
   ```

---

## 6. Common Troubleshooting

| Symptom / Error | Root Cause | Solution |
|---|---|---|
| **CS0246: The type or namespace could not be found** | `assembly_valheim.dll` or `BepInEx.dll` not found. | Check path in `Environment.props`. Ensure `valheim_Data\Managed\` exists at that path. |
| **MSB3021 / MSB3027: Access to path denied in PostBuild** | The game is running and has locked the destination DLL. | Close Valheim before building, or use `SkipUnchangedFiles="true"`. |
| **Git tracks `Environment.props`** | File was committed before being added to `.gitignore`. | Run `git rm --cached Environment.props` and commit `.gitignore`. |
| **Linux / Steam Deck Build Failure** | Case sensitivity in paths (`valheim_data` vs `valheim_Data`). | Ensure exact casing: `valheim_Data/Managed` and `BepInEx/core`. |

*(Detailed troubleshooting guide in [references/troubleshooting.md](./references/troubleshooting.md))*
*(Platform paths reference in [references/path-resolution-guide.md](./references/path-resolution-guide.md))*
*(MSBuild syntax and macro reference in [references/msbuild-patterns.md](./references/msbuild-patterns.md))*
