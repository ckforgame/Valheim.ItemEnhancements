using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "ckforgame.ItemEnhancements";
        public const string ModName = "ItemEnhancements";
        public const string ModVersion = "1.0.0";

        public static ManualLogSource Log { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Migrate configuration from legacy GUID if needed
            string oldConfigPath = System.IO.Path.Combine(Paths.ConfigPath, "com.customs.valheim.itemenhancements.cfg");
            string newConfigPath = System.IO.Path.Combine(Paths.ConfigPath, $"{ModGUID}.cfg");
            if (System.IO.File.Exists(oldConfigPath) && !System.IO.File.Exists(newConfigPath))
            {
                try
                {
                    System.IO.File.Copy(oldConfigPath, newConfigPath);
                    Log.LogInfo($"Migrated old configuration from '{oldConfigPath}' to '{newConfigPath}'.");
                    Config.Reload();
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Failed to migrate old configuration: {ex.Message}");
                }
            }

            // 1. Initialize configuration (Config 1-20 success rates, failure rules, costs, abilities)
            ModConfig.Initialize(Config);

            // 2. Create UI GameObject
            GameObject guiObj = new GameObject("Valheim_ItemEnhancement_GUI");
            guiObj.AddComponent<EnhancementGui>();
            DontDestroyOnLoad(guiObj);

            // 3. Harmony Patching
            _harmony = new Harmony(ModGUID);
            try
            {
                _harmony.PatchAll();
                Log.LogInfo($"[{ModName}] Successfully loaded and patched all methods!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[{ModName}] Harmony Patching failed: {ex}");
            }
        }

        private void Update()
        {
            if (Player.m_localPlayer == null) return;

            // Check if chat input or any text field is focused
            if (Chat.instance != null && Chat.instance.HasFocus()) return;
            if (TextInput.IsVisible()) return;

            // Check shortcut key to toggle enhancement window
            if (Input.GetKeyDown(ModConfig.ToggleGuiKey.Value))
            {
                EnhancementGui.Toggle();
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
