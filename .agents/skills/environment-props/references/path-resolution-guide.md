# Path Resolution Guide for Game Modding Projects

This reference provides canonical paths for Steam, Mod Managers (r2modman, Thunderstore, Gale, Vortex), and game installations across Windows, Linux, and Steam Deck.

---

## 1. Game Client Paths (`ValheimPath` / `GamePath`)

### Windows
| Source | Default Path | Notes |
|---|---|---|
| **Steam Default** | `C:\Program Files (x86)\Steam\steamapps\common\Valheim` | Standard 32-bit Program Files |
| **Steam Custom Drive** | `<Drive>:\SteamLibrary\steamapps\common\Valheim` | e.g. `D:\SteamLibrary\...` |
| **PC Game Pass / Xbox App** | `C:\XboxGames\Valheim\Content` | Encrypted/UWP packaging in older versions; direct folders in modern Xbox app |

### Linux & Steam Deck (SteamOS)
| Source | Default Path | Notes |
|---|---|---|
| **Native / Proton** | `~/.local/share/Steam/steamapps/common/Valheim` | Standard Linux Steam root |
| **Steam Deck MicroSD** | `/run/media/mmcblk0p1/steamapps/common/Valheim` | Default SD card mount |
| **Flatpak Steam** | `~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Valheim` | Sandboxed Flatpak path |

---

## 2. Mod Manager Profile Directories (`R2ModmanProfileDir`)

Mod managers isolate mod configurations, plugins, and BepInEx versions into profile directories. Setting `R2ModmanProfileDir` allows MSBuild to directly target your active testing profile.

### Windows
| Mod Manager | Profile Path Format |
|---|---|
| **r2modman** | `$(AppData)\r2modmanPlus-local\Valheim\profiles\<ProfileName>` |
| **Thunderstore Mod Manager** | `$(AppData)\Thunderstore Mod Manager\DataFolder\Valheim\profiles\<ProfileName>` |
| **Gale Mod Manager** | `$(LocalAppData)\Gale\profiles\Valheim\<ProfileName>` |

*(Note: `$(AppData)` expands to `C:\Users\<User>\AppData\Roaming`, and `$(LocalAppData)` expands to `C:\Users\<User>\AppData\Local`)*

### Linux & Steam Deck
| Mod Manager | Profile Path Format |
|---|---|
| **r2modman (AppImage)** | `~/.config/r2modmanPlus-local/Valheim/profiles/<ProfileName>` |
| **r2modman (Flatpak)** | `~/.var/app/com.ebkr.r2modman/config/r2modmanPlus-local/Valheim/profiles/<ProfileName>` |

---

## 3. Dedicated Server Paths (`ValheimServerPath`)

| Platform | Default Path |
|---|---|
| **Steam Dedicated Server (Windows)** | `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server` |
| **SteamCMD / Linux Dedicated Server** | `/home/steam/valheim-server` |

---

## 4. Key Subdirectories & Managed Assemblies

Under `ValheimPath`:
- **`valheim_Data\Managed\`**: Contains game logic and Unity engine assemblies (`assembly_valheim.dll`, `assembly_guiutils.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `Unity.TextMeshPro.dll`, etc.).
- **`BepInEx\core\`**: Contains mod loader runtime (`BepInEx.dll`, `0Harmony.dll`).
- **`BepInEx\plugins\`**: Destination directory where mod DLLs and assets are loaded.
- **`BepInEx\config\`**: Destination directory where BepInEx `.cfg` and mod config files reside.

Under `R2ModmanProfileDir`:
- **`BepInEx\core\`**: Profile-specific BepInEx core loader.
- **`BepInEx\plugins\`**: Active profile mod directory.
- **`BepInEx\config\`**: Active profile mod configuration directory.
