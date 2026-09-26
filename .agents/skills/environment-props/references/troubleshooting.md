# Troubleshooting Environment.props & MSBuild References

This document outlines common errors encountered when using `Environment.props` in C# game modding projects and how to resolve them.

---

## 1. Error: `CS0246: The type or namespace name 'ItemDrop' (or 'Player', 'Character', 'BepInEx') could not be found`

### Root Cause
MSBuild could not locate the referenced DLL (`assembly_valheim.dll`, `BepInEx.dll`, etc.). This happens when:
- `Environment.props` does not exist and default fallback paths point to a non-existent drive/folder.
- The path in `Environment.props` contains a typo or points to the wrong Steam library.
- The game underwent an update or moved to another drive.

### Solution
1. Verify `Environment.props` exists in the project root.
2. Check that `<ValheimPath>` points to the real directory containing `valheim.exe`.
3. Check that `<ValheimPath>\valheim_Data\Managed\assembly_valheim.dll` exists.
4. If using r2modman, verify `<R2ModmanProfileDir>\BepInEx\core\BepInEx.dll` exists.
5. Run the diagnosis script:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/verify-environment.ps1
   ```

---

## 2. Error: `MSB3021 / MSB3027: Cannot copy file ... Access to the path is denied`

### Root Cause
The destination DLL file in `BepInEx/plugins/...` is locked by Windows because:
- Valheim is currently running and has loaded the DLL into memory.
- A dedicated server instance is active.
- An antivirus or background indexer is holding a file handle.

### Solution
1. Close Valheim and any dedicated server processes before building.
2. Alternatively, use hot-reload plugins or build to a staging folder first.
3. In PowerShell, you can check if Valheim is running:
   ```powershell
   Get-Process -Name "valheim" -ErrorAction SilentlyContinue
   ```

---

## 3. Warning / Issue: Git detects changes to `Environment.props`

### Root Cause
`Environment.props` was tracked by Git before it was added to `.gitignore`.

### Solution
Remove `Environment.props` from Git cache while keeping the local file:
```bash
git rm --cached Environment.props
git commit -m "Stop tracking local Environment.props"
```
Ensure `.gitignore` contains:
```gitignore
Environment.props
*.user
```

---

## 4. Building on CI/CD (GitHub Actions / GitLab CI) Without Game Files

### Problem
CI/CD runners do not have Valheim or Steam installed, causing the build to fail with missing assembly references.

### Recommended Approaches
1. **Publicized Assemblies in Submodule / Private Feed**: Store dummy/publicized stub assemblies in a `lib/` directory or private NuGet package feed, and set `ValheimManaged` to `$(ProjectDir)lib` in CI.
2. **Conditional Reference Path**:
   ```xml
   <PropertyGroup Condition="'$(CI)' == 'true'">
     <ValheimManaged>$(ProjectDir)lib</ValheimManaged>
     <BepInExCore>$(ProjectDir)lib</BepInExCore>
   </PropertyGroup>
   ```
3. **Pass Path via CLI**:
   ```bash
   dotnet build -c Release -p:ValheimPath="/path/to/stubs"
   ```

---

## 5. Linux / Steam Deck Case Sensitivity

### Problem
Windows file systems are case-insensitive (`valheim_data` matches `valheim_Data`). Linux and SteamOS (ext4, btrfs) are **case-sensitive**.

### Solution
Always use exact casing in MSBuild paths:
- `valheim_Data/Managed` (capital `D` and `M`)
- `BepInEx/core` (capital `B`, `I`, `E`)
- `assembly_valheim.dll` (all lowercase)
