using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Core;

namespace Valheim.ItemEnhancements.Patches
{
    public static class CharacterDropPatches
    {
        /// <summary>
        /// Harmony Patch for CharacterDrop.GenerateDropList to inject Enhancement Scrolls.
        /// </summary>
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        public static class CharacterDrop_GenerateDropList_Patch
        {
            public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
            {
                if (__instance == null || __result == null) return;

                Character character = __instance.GetComponent<Character>();
                if (character == null) return;

                ScrollDropManager.TryAddScrollDrop(__instance, character, __result);
            }
        }

        /// <summary>
        /// Harmony Patch for CharacterDrop.DropItems to ensure all dropping prefabs are active,
        /// ensuring Object.Instantiate creates visible and pickable GameObjects.
        /// </summary>
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.DropItems))]
        public static class CharacterDrop_DropItems_Patch
        {
            public static void Prefix(List<KeyValuePair<GameObject, int>> drops)
            {
                if (drops == null) return;
                for (int i = 0; i < drops.Count; i++)
                {
                    GameObject prefab = drops[i].Key;
                    if (prefab != null && !prefab.activeSelf)
                    {
                        prefab.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Harmony Patch for ObjectDB Awake to register scroll prefabs and recipes.
        /// </summary>
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDB_Awake_Patch
        {
            public static void Postfix(ObjectDB __instance)
            {
                ScrollItemManager.InitCustomItems(__instance);
            }
        }

        /// <summary>
        /// Harmony Patch for ObjectDB CopyOtherDB.
        /// </summary>
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class ObjectDB_CopyOtherDB_Patch
        {
            public static void Postfix(ObjectDB __instance)
            {
                ScrollItemManager.InitCustomItems(__instance);
            }
        }

        /// <summary>
        /// Harmony Patch when ZNetScene is ready to register prefabs for multiplayer synchronization.
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix(ZNetScene __instance)
            {
                ScrollItemManager.RegisterZNetScenePrefabs(__instance);
                ScrollItemManager.CleanupOrphanScrolls();
            }
        }

        /// <summary>
        /// Harmony Patch preventing ZNetView from being destroyed or registering ZDO while cloning template prefab.
        /// </summary>
        [HarmonyPatch(typeof(ZNetView), "Awake")]
        public static class ZNetView_Awake_PrefabGuard_Patch
        {
            public static bool Prefix(ZNetView __instance)
            {
                if (ScrollItemManager.IsCloningCustomPrefab)
                {
                    return false; // Temporarily skip Awake so ZNetView remains on prefab without creating a ZDO
                }
                return true;
            }
        }

        /// <summary>
        /// Harmony Patch preventing ItemDrop from registering into s_instances while cloning template prefab.
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        public static class ItemDrop_Awake_PrefabGuard_Patch
        {
            public static bool Prefix(ItemDrop __instance)
            {
                if (ScrollItemManager.IsCloningCustomPrefab)
                {
                    return false; // Temporarily skip Awake so prefab does not enter active world item list
                }
                return true;
            }
        }

        /// <summary>
        /// Harmony Patch preventing Floating component from running while cloning template prefab.
        /// </summary>
        [HarmonyPatch(typeof(Floating), "Awake")]
        public static class Floating_Awake_PrefabGuard_Patch
        {
            public static bool Prefix(Floating __instance)
            {
                if (ScrollItemManager.IsCloningCustomPrefab)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Harmony Patch preventing NullReferenceException in Floating.CustomFixedUpdate
        /// if m_nview is null or not yet IsValid.
        /// </summary>
        [HarmonyPatch(typeof(Floating), nameof(Floating.CustomFixedUpdate))]
        public static class Floating_CustomFixedUpdate_Patch
        {
            private static readonly AccessTools.FieldRef<Floating, ZNetView> _nviewRef =
                AccessTools.FieldRefAccess<Floating, ZNetView>("m_nview");

            public static bool Prefix(Floating __instance)
            {
                if (__instance == null) return false;
                try
                {
                    ZNetView nview = _nviewRef != null ? _nviewRef(__instance) : __instance.GetComponent<ZNetView>();
                    if (nview == null || !nview.IsValid())
                    {
                        return false; // Skip floating physics calculation if ZNetView is not valid
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }

            public static Exception Finalizer(Exception __exception)
            {
                if (__exception is NullReferenceException)
                {
                    return null;
                }
                return __exception;
            }
        }

        /// <summary>
        /// Harmony Patch catching NullReferenceException in Player.AutoPickup
        /// in case an abnormal item or item without ZNetView drops within pickup range.
        /// </summary>
        [HarmonyPatch(typeof(Player), "AutoPickup")]
        public static class Player_AutoPickup_Patch
        {
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception is NullReferenceException)
                {
                    return null; // Suppress exception and let game continue normally
                }
                return __exception;
            }
        }

        /// <summary>
        /// Harmony Finalizer catching NullReferenceException in ZNetScene.RemoveObjects
        /// without incurring any per-cycle dictionary sweeps or performance overhead.
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
        public static class ZNetScene_RemoveObjects_Patch
        {
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception is NullReferenceException)
                {
                    return null; // Suppress NRE and allow execution to continue safely
                }
                return __exception;
            }
        }
    }
}
