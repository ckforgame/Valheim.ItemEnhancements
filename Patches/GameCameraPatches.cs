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
    }
}
