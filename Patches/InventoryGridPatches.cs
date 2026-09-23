using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.Core;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class InventoryGridPatches
    {
        /// <summary>
        /// แสดงป้ายระดับ +X สีตามเทียร์บนไอคอนในช่องเก็บของ
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", typeof(Player), typeof(ItemDrop.ItemData))]
        public static class UpdateGui_Patch
        {
            public static void Postfix(InventoryGrid __instance, System.Collections.Generic.List<InventoryElement> ___m_elements, int ___m_width, int ___m_height)
            {
                if (!ModConfig.ShowInventoryBadges.Value || ___m_elements == null) return;
                Inventory inv = __instance.GetInventory();
                if (inv == null) return;

                for (int y = 0; y < ___m_height; y++)
                {
                    for (int x = 0; x < ___m_width; x++)
                    {
                        int index = y * ___m_width + x;
                        if (index >= ___m_elements.Count) break;

                        ItemDrop.ItemData item = inv.GetItemAt(x, y);
                        if (item != null)
                        {
                            int level = EnhancementManager.GetEnhancementLevel(item);
                            if (level > 0)
                            {
                                var element = ___m_elements[index];
                                if (element?.m_quality != null)
                                {
                                    string hex = EnhancementManager.GetTierHex(level);
                                    element.m_quality.gameObject.SetActive(true);
                                    element.m_quality.text = $"<color={hex}><b>+{level}</b></color>";
                                }
                            }
                        }
                    }
                }
            }
        }

        private static readonly System.Reflection.MethodInfo s_getButtonPos =
            AccessTools.Method(typeof(InventoryGrid), "GetButtonPos", new[] { typeof(GameObject) });

        private static Vector2i GetGridPos(InventoryGrid grid, UIInputHandler handler)
        {
            if (handler == null) return new Vector2i(-1, -1);
            var elem = handler.GetComponentInParent<InventoryElement>();
            if (elem != null) return elem.Position;
            if (s_getButtonPos != null && grid != null)
            {
                return (Vector2i)s_getButtonPos.Invoke(grid, new object[] { handler.gameObject });
            }
            return new Vector2i(-1, -1);
        }

        /// <summary>
        /// เมื่อคลิกซ้ายที่ไอเทมขณะเปิดหน้าต่างตีบวก ให้เลือกไอเทมนั้นเข้าสู่ช่องตีบวก แทนการหยิบย้ายช่องไอเทม
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "OnLeftDown")]
        public static class OnLeftDown_Patch
        {
            public static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler)
            {
                if (EnhancementGui.IsOpen && clickHandler != null)
                {
                    Vector2i pos = GetGridPos(__instance, clickHandler);
                    if (pos.x >= 0 && pos.y >= 0)
                    {
                        ItemDrop.ItemData item = __instance.GetInventory()?.GetItemAt(pos.x, pos.y);
                        if (item != null)
                        {
                            if (EnhancementManager.IsEnhanceable(item))
                            {
                                EnhancementGui.SetSelectedItem(item);
                            }
                            else
                            {
                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "<color=#f59e0b>ไอเทมนี้ไม่สามารถตีบวกได้ (Cannot be enhanced)</color>");
                            }
                        }
                    }
                    return false; // ป้องกันการหยิบ/ลากย้ายช่องไอเทมขณะเปิดหน้าต่างตีบวก
                }
                return true;
            }
        }

        /// <summary>
        /// ป้องกัน event คลิกปล่อย (Left Click) ทำงานซ้ำซ้อนขณะเปิดหน้าต่างตีบวก
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "OnLeftClick")]
        public static class OnLeftClick_Patch
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

        /// <summary>
        /// เมื่อคลิกขวาที่ไอเทมขณะเปิดหน้าต่างตีบวก ให้เลือกไอเทมนั้นเข้าสู่ช่องตีบวกแทนการกดสวมใส่
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "OnRightDown")]
        public static class OnRightDown_Patch
        {
            public static bool Prefix(InventoryGrid __instance, UIInputHandler element)
            {
                if (EnhancementGui.IsOpen && element != null)
                {
                    Vector2i pos = GetGridPos(__instance, element);
                    if (pos.x >= 0 && pos.y >= 0)
                    {
                        ItemDrop.ItemData item = __instance.GetInventory()?.GetItemAt(pos.x, pos.y);
                        if (item != null)
                        {
                            if (EnhancementManager.IsEnhanceable(item))
                            {
                                EnhancementGui.SetSelectedItem(item);
                            }
                            else
                            {
                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "<color=#f59e0b>ไอเทมนี้ไม่สามารถตีบวกได้ (Cannot be enhanced)</color>");
                            }
                        }
                    }
                    return false; // ไม่เรียกการใช้งาน/สวมใส่ไอเทม
                }
                return true;
            }
        }
    }
}
