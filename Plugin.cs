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
        public const string ModGUID = "com.customs.valheim.itemenhancements";
        public const string ModName = "Valheim Item Enhancements (MMORPG Refinement)";
        public const string ModVersion = "1.0.0";

        public static ManualLogSource Log { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // 1. โหลดการตั้งค่าทั้งหมด (Config 1-20 success rates, failure rules, costs, abilities)
            ModConfig.Initialize(Config);

            // 2. สร้าง UI GameObject
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
            // ตรวจสอบปุ่มลัดเพื่อเปิด/ปิดหน้าต่างตีบวก
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
