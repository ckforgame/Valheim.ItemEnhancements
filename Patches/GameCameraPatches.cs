using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class GameCameraPatches
    {
        /// <summary>
        /// Prevents GameCamera from locking the cursor while the enhancement window is open.
        /// </summary>
        [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
        public static class UpdateMouseCapture_Patch
        {
            public static bool Prefix(ref bool ___m_mouseCapture)
            {
                if (EnhancementGui.IsOpen)
                {
                    ZCursor.LockState = CursorLockMode.None;
                    ZCursor.Show();
                    return false; // Skip game calculation to prevent mouse capture while GUI is open
                }

                // Ensure m_mouseCapture remains true so vanilla camera can lock cursor when GUI is closed
                if (!___m_mouseCapture)
                {
                    ___m_mouseCapture = true;
                }

                return true;
            }
        }

        /// <summary>
        /// Prevents game camera zoom and hotkey bar scrolling while the enhancement window is open.
        /// </summary>
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
        public static class GetMouseScrollWheel_Patch
        {
            public static bool Prefix(ref float __result)
            {
                if (EnhancementGui.IsOpen)
                {
                    __result = 0f;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Prevents camera rotation and movement while the enhancement window is open.
        /// </summary>
        [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
        public static class UpdateCamera_Patch
        {
            public static bool Prefix()
            {
                if (EnhancementGui.IsOpen)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
