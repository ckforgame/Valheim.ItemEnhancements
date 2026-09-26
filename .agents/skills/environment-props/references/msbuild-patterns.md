# MSBuild Patterns & Syntax Reference

This reference explains the MSBuild mechanics behind `Environment.props`, including property evaluation order, condition syntax, built-in macros, and post-build automation.

---

## 1. Property Evaluation Order

MSBuild evaluates elements sequentially in top-to-bottom order:

1. **Imports at the top**:
   ```xml
   <Import Project="$(ProjectDir)Environment.props" Condition="Exists('$(ProjectDir)Environment.props')" />
   ```
   If `Environment.props` exists, its `<PropertyGroup>` elements are evaluated first.

2. **Fallback properties in `.csproj`**:
   ```xml
   <PropertyGroup>
     <ValheimPath Condition="'$(ValheimPath)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Valheim</ValheimPath>
   </PropertyGroup>
   ```
   If `ValheimPath` was already defined in `Environment.props`, the condition `'$(ValheimPath)' == ''` is **false**, so the fallback is skipped.
   If `Environment.props` did not exist or omitted `ValheimPath`, the fallback applies.

3. **CLI / Environment Overrides**:
   Properties passed via command line have the highest precedence:
   ```bash
   dotnet build -p:ValheimPath="D:\MyCustomValheim"
   ```

---

## 2. Useful MSBuild Built-in Properties & Macros

| MSBuild Property | Windows Value Example | Purpose |
|---|---|---|
| `$(MSBuildProjectDirectory)` / `$(ProjectDir)` | `D:\Project\MyMod\` | Directory containing the current `.csproj` |
| `$(MSBuildProjectFullPath)` | `D:\Project\MyMod\MyMod.csproj` | Full path of the project file |
| `$(TargetDir)` | `D:\Project\MyMod\bin\Release\net48\` | Output directory containing compiled binaries |
| `$(TargetName)` | `MyMod` | Output assembly name without extension |
| `$(AssemblyName)` | `MyMod` | Configured assembly name |
| `$(Configuration)` | `Debug` or `Release` | Active build configuration |
| `$(AppData)` | `C:\Users\<User>\AppData\Roaming` | Windows Roaming AppData (used for r2modman / Thunderstore) |
| `$(LocalAppData)` | `C:\Users\<User>\AppData\Local` | Windows Local AppData |
| `$(ProgramFiles)` | `C:\Program Files` | 64-bit Program Files |
| `$(ProgramFiles(x86))` | `C:\Program Files (x86)` | 32-bit Program Files |
| `$(USERPROFILE)` | `C:\Users\<User>` | User home directory |

---

## 3. Condition Expressions

### String Comparison
```xml
Condition="'$(R2ModmanProfileDir)' != ''"
Condition="'$(Configuration)' == 'Release'"
```

### File & Directory Existence Checks
```xml
Condition="Exists('$(ProjectDir)Environment.props')"
Condition="Exists('$(ValheimPath)\BepInEx\plugins')"
```

### Combined Boolean Conditions
```xml
<!-- AND condition -->
Condition="'$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)')"

<!-- OR condition -->
Condition="'$(DeployAll)' == 'true' or '$(DeployToSteam)' == 'true'"
```

---

## 4. ItemGroup & PostBuild Deployment Pattern

### Why `SkipUnchangedFiles="true"` Matters
When developing mods with the game or mod manager running, repeatedly copying files can cause OS file locking issues. `SkipUnchangedFiles="true"` skips overwriting files whose timestamps and sizes match, speeding up builds and reducing lock contention.

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <ItemGroup>
    <PluginFiles Include="$(TargetDir)$(TargetName).dll" />
    <PluginFiles Include="$(TargetDir)$(TargetName).pdb" Condition="Exists('$(TargetDir)$(TargetName).pdb')" />
  </ItemGroup>

  <!-- Auto deploy to r2modman -->
  <MakeDir Directories="$(R2ModmanPluginDir)" Condition="'$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)')" />
  <Copy SourceFiles="@(PluginFiles)"
        DestinationFolder="$(R2ModmanPluginDir)"
        Condition="'$(R2ModmanProfileDir)' != '' and Exists('$(R2ModmanProfileDir)')"
        SkipUnchangedFiles="true" />
</Target>
```

---

## 5. Multi-Project Solutions with `Directory.Build.props`

In a solution containing multiple mod projects (e.g., Core mod, Expansion mod, API mod, Unit tests), creating an `Environment.props` in every single subfolder leads to desync.

Instead:
1. Place a single `Environment.props` at the solution root.
2. Place a `Directory.Build.props` at the solution root.
3. MSBuild automatically imports `Directory.Build.props` for all child `.csproj` files, providing shared `ValheimPath` and `R2ModmanProfileDir` across the entire solution.
