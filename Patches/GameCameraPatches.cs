using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class GameCameraPatches
    {
        /// <summary>
        /// Prevents GameCamera from capturing mouse and rotating camera when enhancement window is open.
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
                    return false; // Skip game calculation to prevent mouse capture
                }
                return true;
            }
        }
    }
}
