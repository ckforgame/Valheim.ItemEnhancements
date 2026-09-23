using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public enum EnhanceResult
    {
        Success,
        FailedSafe,
        FailedDowngraded,
        FailedBroken,
        CannotAfford,
        MaxLevelReached,
        NotEligible
    }

    public static class EnhancementManager
    {
        public const string CustomDataKey = "ValheimEnhancement_Level";
        public const string CrafterDataKey = "ValheimEnhancement_Crafter";

        /// <summary>
        /// Gets the enhancement level of an item (0 - 20)
        /// </summary>
        public static int GetEnhancementLevel(ItemDrop.ItemData item)
        {
            if (item?.m_customData == null) return 0;

            if (item.m_customData.TryGetValue(CustomDataKey, out string val) && int.TryParse(val, out int level))
            {
                return Mathf.Clamp(level, 0, ModConfig.MaxLevel);
            }
            return 0;
        }

        /// <summary>
        /// Saves the enhancement level into m_customData (Valheim's native persistence system)
        /// </summary>
        public static void SetEnhancementLevel(ItemDrop.ItemData item, int level)
        {
            if (item == null) return;
            if (item.m_customData == null)
            {
                item.m_customData = new Dictionary<string, string>();
            }

            level = Mathf.Clamp(level, 0, ModConfig.MaxLevel);
            item.m_customData[CustomDataKey] = level.ToString();

            if (Player.m_localPlayer != null)
            {
                item.m_customData[CrafterDataKey] = Player.m_localPlayer.GetPlayerName();
            }
        }

        /// <summary>
        /// Checks whether the item can be enhanced
        /// </summary>
        public static bool IsEnhanceable(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;

            // Check permitted item types
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Tool:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the total amount of Coins in player inventory
        /// </summary>
        public static int GetPlayerCoins(Player player)
        {
            if (player == null || player.GetInventory() == null) return 0;
            return player.GetInventory().CountItems("$item_coins", -1, true);
        }

        /// <summary>
        /// Deducts coins from player inventory
        /// </summary>
        public static bool DeductPlayerCoins(Player player, int amount)
        {
            if (amount <= 0) return true;
            if (player == null || player.GetInventory() == null) return false;

            if (GetPlayerCoins(player) < amount) return false;

            player.GetInventory().RemoveItem("$item_coins", amount, -1, true);
            return true;
        }

        /// <summary>
        /// Determines the required scroll tier based on target level (1-20)
        /// +1 to +5: Tier 1
        /// +6 to +10: Tier 2
        /// +11 to +15: Tier 3
        /// +16 to +20: Tier 4
        /// </summary>
        public static int GetRequiredScrollTier(int targetLevel)
        {
            if (targetLevel <= 5) return 1;
            if (targetLevel <= 10) return 2;
            if (targetLevel <= 15) return 3;
            return 4;
        }

        /// <summary>
        /// Gets the count of scrolls of the specified tier in player inventory
        /// </summary>
        public static int GetPlayerScrollCount(Player player, int tier)
        {
            if (player == null || player.GetInventory() == null) return 0;
            string prefabName = ScrollItemManager.GetScrollPrefabName(tier);
            string tokenName = $"$item_scroll_enhance_t{tier}";

            int count = 0;
            List<ItemDrop.ItemData> allItems = player.GetInventory().GetAllItems();
            foreach (var item in allItems)
            {
                if (item == null) continue;
                if ((item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName) ||
                    (item.m_shared != null && item.m_shared.m_name == tokenName))
                {
                    count += item.m_stack;
                }
            }
            return count;
        }

        /// <summary>
        /// Deducts scrolls from player inventory
        /// </summary>
        public static bool DeductPlayerScrolls(Player player, int tier, int amount)
        {
            if (amount <= 0) return true;
            if (player == null || player.GetInventory() == null) return false;

            int current = GetPlayerScrollCount(player, tier);
            if (current < amount) return false;

            string prefabName = ScrollItemManager.GetScrollPrefabName(tier);
            string tokenName = $"$item_scroll_enhance_t{tier}";

            int remaining = amount;
            List<ItemDrop.ItemData> allItems = new List<ItemDrop.ItemData>(player.GetInventory().GetAllItems());
            foreach (var item in allItems)
            {
                if (item == null) continue;
                if ((item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName) ||
                    (item.m_shared != null && item.m_shared.m_name == tokenName))
                {
                    if (item.m_stack <= remaining)
                    {
                        remaining -= item.m_stack;
                        player.GetInventory().RemoveItem(item);
                    }
                    else
                    {
                        player.GetInventory().RemoveItem(item, remaining);
                        remaining = 0;
                    }

                    if (remaining <= 0) break;
                }
            }

            return remaining <= 0;
        }

        /// <summary>
        /// Checks whether the player can afford the enhancement attempt
        /// </summary>
        public static bool CanAfford(Player player, int targetLevel)
        {
            if (player == null) return false;

            // Check enhancement scrolls
            if (ModConfig.RequireScrolls.Value)
            {
                int tier = GetRequiredScrollTier(targetLevel);
                int needed = ModConfig.ScrollsRequiredPerAttempt.Value;
                if (GetPlayerScrollCount(player, tier) < needed)
                {
                    return false;
                }
            }

            // Check coins (if enabled)
            if (ModConfig.RequireCoins.Value)
            {
                int cost = ModConfig.GetCoinCost(targetLevel);
                if (GetPlayerCoins(player) < cost)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Executes enhancement attempt on the target item
        /// </summary>
        public static EnhanceResult TryEnhance(Player player, ItemDrop.ItemData item, out int newLevel)
        {
            newLevel = 0;
            if (item == null || !IsEnhanceable(item))
            {
                return EnhanceResult.NotEligible;
            }

            int currentLevel = GetEnhancementLevel(item);
            if (currentLevel >= ModConfig.MaxLevel)
            {
                newLevel = currentLevel;
                return EnhanceResult.MaxLevelReached;
            }

            int targetLevel = currentLevel + 1;
            int cost = ModConfig.GetCoinCost(targetLevel);

            if (!CanAfford(player, targetLevel))
            {
                newLevel = currentLevel;
                return EnhanceResult.CannotAfford;
            }

            // Deduct enhancement scrolls
            if (ModConfig.RequireScrolls.Value)
            {
                int tier = GetRequiredScrollTier(targetLevel);
                int scrollAmount = ModConfig.ScrollsRequiredPerAttempt.Value;
                if (!DeductPlayerScrolls(player, tier, scrollAmount))
                {
                    newLevel = currentLevel;
                    return EnhanceResult.CannotAfford;
                }
            }

            // Deduct coins (if enabled)
            if (ModConfig.RequireCoins.Value && cost > 0)
            {
                if (!DeductPlayerCoins(player, cost))
                {
                    // Rollback deducted scrolls if coin deduction fails
                    if (ModConfig.RequireScrolls.Value)
                    {
                        int tier = GetRequiredScrollTier(targetLevel);
                        int scrollAmount = ModConfig.ScrollsRequiredPerAttempt.Value;
                        GameObject scrollPrefab = ScrollItemManager.GetScrollPrefab(tier);
                        if (scrollPrefab != null)
                        {
                            player?.GetInventory()?.AddItem(scrollPrefab, scrollAmount);
                        }
                    }
                    newLevel = currentLevel;
                    return EnhanceResult.CannotAfford;
                }
            }

            // Roll success rate
            float roll = UnityEngine.Random.Range(0f, 100f);
            float successRate = ModConfig.GetSuccessRate(targetLevel);

            if (roll < successRate)
            {
                // Success!
                newLevel = targetLevel;
                SetEnhancementLevel(item, newLevel);
                PostEnhanceUpdate(player, item);
                return EnhanceResult.Success;
            }

            // Failed!
            if (currentLevel <= ModConfig.SafeLevel.Value)
            {
                // In safe zone, level does not decrease
                newLevel = currentLevel;
                PostEnhanceUpdate(player, item);
                return EnhanceResult.FailedSafe;
            }

            // Above safe zone
            if (ModConfig.BreakOnFail.Value)
            {
                float breakRoll = UnityEngine.Random.Range(0f, 100f);
                if (breakRoll < ModConfig.BreakChanceAboveSafeLevel.Value)
                {
                    // Item broken!
                    if (player != null)
                    {
                        if (item.m_equipped)
                        {
                            player.UnequipItem(item, true);
                        }

                        // Remove from the actual inventory (player inventory or container)
                        if (player.GetInventory() != null && player.GetInventory().ContainsItem(item))
                        {
                            player.GetInventory().RemoveItem(item);
                        }
                        else
                        {
                            Inventory containerInv = (InventoryGui.instance != null && InventoryGui.instance.IsContainerOpen())
                                ? InventoryGui.instance.ContainerGrid?.GetInventory()
                                : null;
                            if (containerInv != null && containerInv.ContainsItem(item))
                            {
                                containerInv.RemoveItem(item);
                            }
                            else
                            {
                                player.GetInventory()?.RemoveItem(item);
                            }
                        }
                    }
                    newLevel = 0;
                    PostEnhanceUpdate(player, item);
                    return EnhanceResult.FailedBroken;
                }
            }

            if (ModConfig.DowngradeOnFail.Value)
            {
                // Downgrade 1 level
                newLevel = Mathf.Max(0, currentLevel - 1);
                SetEnhancementLevel(item, newLevel);
                PostEnhanceUpdate(player, item);
                return EnhanceResult.FailedDowngraded;
            }

            newLevel = currentLevel;
            PostEnhanceUpdate(player, item);
            return EnhanceResult.FailedSafe;
        }

        private static readonly MethodInfo s_updateTotalWeightMethod =
            AccessTools.Method(typeof(Inventory), "UpdateTotalWeight");

        private static readonly MethodInfo s_updateModifiersMethod =
            AccessTools.Method(typeof(Player), "UpdateModifiers");

        private static void PostEnhanceUpdate(Player player, ItemDrop.ItemData item)
        {
            if (player == null) return;
            try
            {
                PlayerBonusCache.MarkDirty();
                Inventory inv = player.GetInventory();
                if (inv != null)
                {
                    s_updateTotalWeightMethod?.Invoke(inv, null);
                }
                s_updateModifiersMethod?.Invoke(player, null);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[EnhancementManager] PostEnhanceUpdate error: {ex.Message}");
            }
        }

        #region UI & Presentation Helpers

        /// <summary>
        /// Gets the Hex color string corresponding to the enhancement level
        /// </summary>
        public static string GetTierHex(int level)
        {
            if (level <= 0) return "#d1d5db"; // Gray
            if (level <= 3) return "#4ade80"; // Light Green
            if (level <= 6) return "#22d3ee"; // Cyan
            if (level <= 9) return "#3b82f6"; // Rare Blue
            if (level <= 12) return "#a855f7"; // Epic Purple
            if (level <= 15) return "#f59e0b"; // Legendary Amber
            if (level <= 19) return "#ef4444"; // Mythic Crimson
            return "#ffd700"; // Divine Gold
        }

        /// <summary>
        /// Gets the Color object for IMGUI or Canvas
        /// </summary>
        public static Color GetTierColor(int level)
        {
            if (ColorUtility.TryParseHtmlString(GetTierHex(level), out Color color))
            {
                return color;
            }
            return Color.white;
        }

        /// <summary>
        /// Tier rank title based on enhancement level
        /// </summary>
        public static string GetTierRankName(int level)
        {
            if (level <= 0) return "Normal";
            if (level <= 3) return "Standard";
            if (level <= 6) return "Refined";
            if (level <= 9) return "Rare";
            if (level <= 12) return "Epic";
            if (level <= 15) return "Legendary";
            if (level <= 19) return "Mythic";
            return "Divine";
        }

        /// <summary>
        /// Formats item display name with +X level tag
        /// </summary>
        public static string FormatItemNameWithLevel(ItemDrop.ItemData item, string originalName, int level)
        {
            if (level <= 0) return originalName;
            string hex = GetTierHex(level);
            return $"{originalName} <color={hex}>+{level}</color>";
        }

        /// <summary>
        /// Builds detailed MMORPG-style enhancement tooltip for an item
        /// </summary>
        public static string BuildEnhancementTooltip(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item == null) return string.Empty;

            string hex = GetTierHex(level);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"<color=#ffd700>═══════════════</color>");
            sb.AppendLine($"<color={hex}><b>★ MMORPG ENHANCEMENT (+{level}/{ModConfig.MaxLevel}) ★</b></color>");
            sb.AppendLine($"<color=#94a3b8>Rank: {GetTierRankName(level)}</color>");

            // 1. Weapon Bonuses
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
            {
                float dmgBonusPct = YamlConfigManager.GetCumulativeAbilityValue("WeaponDamage", level);
                if (dmgBonusPct > 0f)
                {
                    sb.AppendLine($"<color=#4ade80>• All Damage: +{dmgBonusPct:F0}%</color>");
                }

                if (item.m_shared.m_backstabBonus > 1f)
                {
                    float bsBonus = StatCalculator.GetBackstabBonus(level);
                    if (bsBonus > 0f)
                    {
                        sb.AppendLine($"<color=#4ade80>• Backstab Bonus: +{bsBonus:F2}x</color>");
                    }
                }

                float atkStam = StatCalculator.GetAttackStaminaReduction(level) * 100f;
                if (atkStam > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• Attack Stamina Usage: -{atkStam:F0}%</color>");
                }

                float atkEitr = StatCalculator.GetAttackEitrReduction(level) * 100f;
                if (atkEitr > 0f && item.m_shared.m_attack.m_attackEitr > 0f)
                {
                    sb.AppendLine($"<color=#c084fc>• Attack Eitr Usage: -{atkEitr:F0}%</color>");
                }
            }

            // 2. Armor Bonuses
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
            {
                float armorBonus = StatCalculator.GetArmorBonus(item, level, item.m_shared.m_armor);
                if (armorBonus > 0f)
                {
                    sb.AppendLine($"<color=#4ade80>• Armor: +{armorBonus:F1}</color>");
                }

                float moveDelta = StatCalculator.GetMovementModifierDelta(item, level) * 100f;
                if (moveDelta > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• Movement Speed Bonus: +{moveDelta:F1}%</color>");
                }

                float dodgeRed = StatCalculator.GetDodgeStaminaReduction(level) * 100f;
                if (dodgeRed > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• Dodge Stamina Cost: -{dodgeRed:F0}%</color>");
                }

                float eitrRegen = StatCalculator.GetEitrRegenBonus(level) * 100f;
                if (eitrRegen > 0f && item.m_shared.m_eitrRegenModifier > 0f)
                {
                    sb.AppendLine($"<color=#c084fc>• Eitr Regeneration: +{eitrRegen:F0}%</color>");
                }
            }

            // 3. Shield Bonuses
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                float blkPct = YamlConfigManager.GetCumulativeAbilityValue("ShieldBlockPower", level);
                if (blkPct > 0f)
                {
                    sb.AppendLine($"<color=#4ade80>• Block Power: +{blkPct:F0}%</color>");
                }

                float defPct = YamlConfigManager.GetCumulativeAbilityValue("ShieldDeflectionForce", level);
                if (defPct > 0f)
                {
                    sb.AppendLine($"<color=#4ade80>• Parry Force: +{defPct:F0}%</color>");
                }

                float blkStam = StatCalculator.GetBlockStaminaReduction(level) * 100f;
                if (blkStam > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• Block Stamina Cost: -{blkStam:F0}%</color>");
                }

                float moveDelta = StatCalculator.GetMovementModifierDelta(item, level) * 100f;
                if (moveDelta > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• Shield Movement Penalty Reduction: +{moveDelta:F1}%</color>");
                }
            }

            // 4. Utility / Belt Bonuses
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                float carry = StatCalculator.GetCarryWeightBonus(item, level);
                if (carry > 0f)
                {
                    sb.AppendLine($"<color=#f59e0b>• Max Carry Weight: +{carry:F0}</color>");
                }
            }

            // 5. General Attributes
            float durPct = YamlConfigManager.GetCumulativeAbilityValue("MaxDurability", level);
            if (durPct > 0f)
            {
                sb.AppendLine($"<color=#94a3b8>• Max Durability: +{durPct:F0}%</color>");
            }

            float wtPct = Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("WeightReduction", level), 0f, 60f);
            if (wtPct > 0f)
            {
                sb.AppendLine($"<color=#94a3b8>• Item Weight: -{wtPct:F1}%</color>");
            }

            // 6. Cheat Abilities (If enabled in config)
            if (StatCalculator.IsCheatEnabled)
            {
                float cheatHp = StatCalculator.GetCheatItemMaxHealth(item, level);
                float cheatStam = StatCalculator.GetCheatItemMaxStamina(item, level);
                float cheatEitr = StatCalculator.GetCheatItemMaxEitr(item, level);
                float cheatHpRegen = StatCalculator.GetCheatItemHealthRegen(item, level) * 100f;
                float cheatStamRegen = StatCalculator.GetCheatItemStaminaRegen(item, level) * 100f;
                float cheatEitrRegen = StatCalculator.GetCheatItemEitrRegen(item, level) * 100f;
                float cheatHeal = StatCalculator.GetCheatItemHealingBonus(item, level) * 100f;

                if (cheatHp > 0f || cheatStam > 0f || cheatEitr > 0f || cheatHpRegen > 0f || cheatStamRegen > 0f || cheatEitrRegen > 0f || cheatHeal > 0f)
                {
                    sb.AppendLine($"<color=#f43f5e>--- [CHEAT ABILITIES] ---</color>");
                    if (cheatHp > 0f) sb.AppendLine($"<color=#fb7185>★ Max Health: +{cheatHp:F0}</color>");
                    if (cheatStam > 0f) sb.AppendLine($"<color=#fb7185>★ Max Stamina: +{cheatStam:F0}</color>");
                    if (cheatEitr > 0f) sb.AppendLine($"<color=#fb7185>★ Max Eitr: +{cheatEitr:F0}</color>");
                    if (cheatHpRegen > 0f) sb.AppendLine($"<color=#fb7185>★ Health Regen: +{cheatHpRegen:F0}%</color>");
                    if (cheatStamRegen > 0f) sb.AppendLine($"<color=#fb7185>★ Stamina Regen: +{cheatStamRegen:F0}%</color>");
                    if (cheatEitrRegen > 0f) sb.AppendLine($"<color=#fb7185>★ Eitr Regen: +{cheatEitrRegen:F0}%</color>");
                    if (cheatHeal > 0f) sb.AppendLine($"<color=#fb7185>★ Healing Bonus: +{cheatHeal:F0}%</color>");
                }
            }

            // Crafter
            if (item.m_customData != null && item.m_customData.TryGetValue(CrafterDataKey, out string crafter) && !string.IsNullOrEmpty(crafter))
            {
                sb.AppendLine($"<color=#64748b>Enhanced by: {crafter}</color>");
            }

            sb.AppendLine($"<color=#ffd700>═══════════════</color>");
            return sb.ToString();
        }

        #endregion
    }
}
