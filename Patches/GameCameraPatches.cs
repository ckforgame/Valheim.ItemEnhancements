using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class GameCameraPatches
    {
        /// <summary>
        /// ป้องกันไม่ให้ GameCamera ล็อกเมาส์และหมุนมุมกล้องขณะที่หน้าต่างตีบวกเปิดอยู่ (โดยเฉพาะเมื่อไม่ได้เปิด Inventory)
        /// </summary>
        [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
        public static class UpdateMouseCapture_Patch
        {
            public static bool Prefix(ref bool ___m_mouseCapture)
            {
                if (EnhancementGui.IsOpen)
                {
                    ___m_mouseCapture = false;
                    ZCursor.LockState = CursorLockMode.None;
                    ZCursor.Show();
                    return false; // ข้ามการคำนวณของเกมเพื่อไม่ให้เมาส์ถูกล็อก
                }
                return true;
            }
        }
    }
}
