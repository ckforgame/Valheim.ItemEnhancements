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

        /// <summary>
        /// เมื่อคลิกขวาที่ไอเทมขณะเปิดหน้าต่างตีบวก ให้เลือกไอเทมนั้นเข้าสู่ช่องตีบวกแทนการกดสวมใส่
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "OnRightDown")]
        public static class OnRightDown_Patch
        {
            public static bool Prefix(InventoryGrid __instance, InventoryElement element)
            {
                if (EnhancementGui.IsOpen && element != null)
                {
                    Vector2i pos = element.Position;
                    ItemDrop.ItemData item = __instance.GetInventory()?.GetItemAt(pos.x, pos.y);
                    if (item != null && EnhancementManager.IsEnhanceable(item))
                    {
                        EnhancementGui.SetSelectedItem(item);
                        return false; // ไม่เรียกการใช้งาน/สวมใส่ไอเทม
                    }
                }
                return true;
            }
        }
    }
}
