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
        /// รองรับทั้งโครงสร้าง ZDO (Valheim เวอร์ชั่นใหม่) และ ZDOID ผ่าน IDictionary อย่างปลอดภัย
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
        public static class ZNetScene_RemoveObjects_Patch
        {
            private static readonly System.Reflection.FieldInfo _instancesField =
                AccessTools.Field(typeof(ZNetScene), "m_instances");

            public static void Prefix(ZNetScene __instance)
            {
                if (__instance == null || _instancesField == null) return;
                try
                {
                    if (!(_instancesField.GetValue(__instance) is System.Collections.IDictionary dict) || dict.Count == 0) return;

                    List<object> deadKeys = null;
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        if (entry.Value is ZNetView znv)
                        {
                            if (znv == null || znv.GetZDO() == null)
                            {
                                if (deadKeys == null) deadKeys = new List<object>();
                                deadKeys.Add(entry.Key);
                            }
                        }
                        else if (entry.Value == null)
                        {
                            if (deadKeys == null) deadKeys = new List<object>();
                            deadKeys.Add(entry.Key);
                        }
                    }

                    if (deadKeys != null)
                    {
                        for (int i = 0; i < deadKeys.Count; i++)
                        {
                            dict.Remove(deadKeys[i]);
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
