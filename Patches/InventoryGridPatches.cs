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
        /// Displays colored +X enhancement tier badge on item icon in inventory grid.
        /// </summary>
        /// <summary>
        /// Displays colored +X enhancement tier badge on item icon in inventory grid.
        /// Precalculates badge strings and avoids redundant text mesh rebuilds.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", typeof(Player), typeof(ItemDrop.ItemData))]
        public static class UpdateGui_Patch
        {
            private static readonly string[] s_badgeStrings = new string[ModConfig.MaxLevel + 1];

            static UpdateGui_Patch()
            {
                for (int lvl = 1; lvl <= ModConfig.MaxLevel; lvl++)
                {
                    string hex = EnhancementManager.GetTierHex(lvl);
                    s_badgeStrings[lvl] = $"<color={hex}><b>+{lvl}</b></color>";
                }
            }

            private static string GetBadgeString(int level)
            {
                if (level >= 1 && level <= ModConfig.MaxLevel)
                {
                    return s_badgeStrings[level];
                }
                string hex = EnhancementManager.GetTierHex(level);
                return $"<color={hex}><b>+{level}</b></color>";
            }

            public static void Postfix(InventoryGrid __instance, System.Collections.Generic.List<InventoryElement> ___m_elements, int ___m_width, int ___m_height)
            {
                if (!ModConfig.ShowInventoryBadges.Value || ___m_elements == null) return;
                Inventory inv = __instance.GetInventory();
                if (inv == null) return;

                System.Collections.Generic.List<ItemDrop.ItemData> allItems = inv.GetAllItems();
                if (allItems == null) return;

                for (int i = 0; i < allItems.Count; i++)
                {
                    ItemDrop.ItemData item = allItems[i];
                    if (item == null) continue;

                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level <= 0) continue;

                    int index = item.m_gridPos.y * ___m_width + item.m_gridPos.x;
                    if (index >= 0 && index < ___m_elements.Count)
                    {
                        var element = ___m_elements[index];
                        if (element?.m_quality != null)
                        {
                            string badge = GetBadgeString(level);
                            if (element.m_quality.text != badge)
                            {
                                element.m_quality.text = badge;
                            }
                            if (!element.m_quality.gameObject.activeSelf)
                            {
                                element.m_quality.gameObject.SetActive(true);
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
        /// When left-clicking an item while enhancement window is open, selects that item into enhancement slot instead of moving it.
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
                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "<color=#f59e0b>This item cannot be enhanced!</color>");
                            }
                        }
                    }
                    return false; // Prevent moving/dragging item slot while enhancement window is open
                }
                return true;
            }
        }

        /// <summary>
        /// Prevents redundant left click release events while enhancement window is open.
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
        /// When right-clicking an item while enhancement window is open, selects that item into enhancement slot instead of equipping/using it.
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
                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "<color=#f59e0b>This item cannot be enhanced!</color>");
                            }
                        }
                    }
                    return false; // Prevent using or equipping item
                }
                return true;
            }
        }
    }
}
