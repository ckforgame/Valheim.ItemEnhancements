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
                ScrollItemManager.CleanupOrphanScrolls();
            }
        }

        /// <summary>
        /// Harmony Patch ป้องกันไม่ให้ ZNetView ถูกทำลายหรือลงทะเบียน ZDO ในขณะโคลน Prefab ต้นแบบ
        /// </summary>
        [HarmonyPatch(typeof(ZNetView), "Awake")]
        public static class ZNetView_Awake_PrefabGuard_Patch
        {
            public static bool Prefix(ZNetView __instance)
            {
                if (ScrollItemManager.IsCloningCustomPrefab)
                {
                    return false; // ข้าม Awake ชั่วคราวเพื่อให้ ZNetView คงอยู่บน Prefab โดยไม่สร้าง ZDO
                }
                return true;
            }
        }

        /// <summary>
        /// Harmony Patch ป้องกันไม่ให้ ItemDrop ลงทะเบียนเข้า s_instances ในขณะโคลน Prefab ต้นแบบ
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        public static class ItemDrop_Awake_PrefabGuard_Patch
        {
            public static bool Prefix(ItemDrop __instance)
            {
                if (ScrollItemManager.IsCloningCustomPrefab)
                {
                    return false; // ข้าม Awake ชั่วคราวไม่ให้ Prefab เข้าไปอยู่ในรายการไอเทมในโลก
                }
                return true;
            }
        }

        /// <summary>
        /// Harmony Patch ป้องกันไม่ให้ Floating ทำงานในขณะโคลน Prefab ต้นแบบ
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
        /// Harmony Patch ป้องกัน NullReferenceException ใน Floating.CustomFixedUpdate
        /// หาก m_nview เป็น null หรือยังไม่ IsValid
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
                        return false; // ข้ามการคำนวณฟิสิกส์ลอยน้ำหาก ZNetView ไม่สมบูรณ์
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
        /// Harmony Patch ดักจับ NullReferenceException ใน Player.AutoPickup
        /// เผื่อมีไอเทมผิดปกติหรือไม่มี ZNetView ตกอยู่ในระยะเก็บของ
        /// </summary>
        [HarmonyPatch(typeof(Player), "AutoPickup")]
        public static class Player_AutoPickup_Patch
        {
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception is NullReferenceException)
                {
                    return null; // ระงับข้อผิดพลาดและปล่อยให้เกมดำเนินต่อไป
                }
                return __exception;
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
