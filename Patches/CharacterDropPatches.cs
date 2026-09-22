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
        /// Harmony Patch สำหรับ CharacterDrop.GenerateDropList เพื่อสอดแทรก Enhancement Scrolls
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
        /// Harmony Patch เมื่อ ObjectDB ตื่นตัว ให้ลงทะเบียน Prefabs ม้วนคัมภีร์และสูตรคราฟต์
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
        /// Harmony Patch เมื่อ ObjectDB ถูกคัดลอกในฉากโลก
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
        /// Harmony Patch เมื่อ ZNetScene พร้อม ให้ลงทะเบียน Prefabs เพื่อซิงค์ในโลก Multiplayer
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            public static void Postfix(ZNetScene __instance)
            {
                ScrollItemManager.RegisterZNetScenePrefabs(__instance);
            }
        }

        /// <summary>
        /// Harmony Patch ป้องกัน NullReferenceException ใน ZNetScene.RemoveObjects
        /// หากมี GameObject ที่ถูกทำลายหรือ ZDO สูญหายหลงเหลืออยู่ใน m_instances
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
        public static class ZNetScene_RemoveObjects_Patch
        {
            private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<ZDOID, ZNetView>> _instancesRef =
                AccessTools.FieldRefAccess<ZNetScene, Dictionary<ZDOID, ZNetView>>("m_instances");

            public static void Prefix(ZNetScene __instance)
            {
                if (__instance == null) return;
                try
                {
                    var instances = _instancesRef(__instance);
                    if (instances == null || instances.Count == 0) return;

                    List<ZDOID> deadKeys = null;
                    foreach (var kvp in instances)
                    {
                        if (kvp.Value == null || kvp.Value.GetZDO() == null)
                        {
                            if (deadKeys == null) deadKeys = new List<ZDOID>();
                            deadKeys.Add(kvp.Key);
                        }
                    }

                    if (deadKeys != null)
                    {
                        for (int i = 0; i < deadKeys.Count; i++)
                        {
                            instances.Remove(deadKeys[i]);
                        }
                    }
                }
                catch
                {
                    // Ignore safety errors
                }
            }
        }
    }
}
