using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class StatCalculator
    {
        /// <summary>
        /// Applies scaled damage bonus across all weapon damage types based on enhancement level
        /// </summary>
        public static void ApplyDamageBonus(ItemDrop.ItemData item, int level, ref HitData.DamageTypes damages)
        {
            if (level <= 0 || item == null) return;

            float bonusPercent = YamlConfigManager.GetCumulativeAbilityValue("WeaponDamage", level);
            if (bonusPercent <= 0f) return;

            float bonusMultiplier = 1f + (bonusPercent / 100f);

            if (damages.m_slash > 0f) damages.m_slash *= bonusMultiplier;
            if (damages.m_pierce > 0f) damages.m_pierce *= bonusMultiplier;
            if (damages.m_blunt > 0f) damages.m_blunt *= bonusMultiplier;
            if (damages.m_chop > 0f) damages.m_chop *= bonusMultiplier;
            if (damages.m_pickaxe > 0f) damages.m_pickaxe *= bonusMultiplier;
            if (damages.m_fire > 0f) damages.m_fire *= bonusMultiplier;
            if (damages.m_frost > 0f) damages.m_frost *= bonusMultiplier;
            if (damages.m_lightning > 0f) damages.m_lightning *= bonusMultiplier;
            if (damages.m_poison > 0f) damages.m_poison *= bonusMultiplier;
            if (damages.m_spirit > 0f) damages.m_spirit *= bonusMultiplier;
        }

        /// <summary>
        /// Calculates additional armor defense bonus (combining Flat and Percent bonuses)
        /// </summary>
        public static float GetArmorBonus(ItemDrop.ItemData item, int level, float baseArmor)
        {
            if (level <= 0 || item == null) return 0f;

            float flatBonus = YamlConfigManager.GetCumulativeAbilityValue("ArmorFlat", level);
            float percentBonus = baseArmor * (YamlConfigManager.GetCumulativeAbilityValue("ArmorPercent", level) / 100f);

            return flatBonus + percentBonus;
        }

        /// <summary>
        /// Calculates additional shield block power bonus
        /// </summary>
        public static float GetBlockPowerBonus(ItemDrop.ItemData item, int level, float baseBlock)
        {
            if (level <= 0 || item == null || baseBlock <= 0f) return 0f;
            return baseBlock * (YamlConfigManager.GetCumulativeAbilityValue("ShieldBlockPower", level) / 100f);
        }

        /// <summary>
        /// Calculates additional shield deflection force bonus
        /// </summary>
        public static float GetDeflectionBonus(ItemDrop.ItemData item, int level, float baseDeflection)
        {
            if (level <= 0 || item == null || baseDeflection <= 0f) return 0f;
            return baseDeflection * (YamlConfigManager.GetCumulativeAbilityValue("ShieldDeflectionForce", level) / 100f);
        }

        /// <summary>
        /// Calculates additional maximum item durability bonus
        /// </summary>
        public static float GetDurabilityBonus(ItemDrop.ItemData item, int level, float baseDurability)
        {
            if (level <= 0 || item == null || baseDurability <= 0f) return 0f;
            return baseDurability * (YamlConfigManager.GetCumulativeAbilityValue("MaxDurability", level) / 100f);
        }

        /// <summary>
        /// Calculates equipment weight multiplier
        /// </summary>
        public static float GetWeightMultiplier(int level)
        {
            if (level <= 0) return 1f;
            float reduction = Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("WeightReduction", level) / 100f, 0f, 0.60f);
            return 1f - reduction;
        }

        /// <summary>
        /// Calculates movement speed penalty reduction (and high-level movement bonus)
        /// </summary>
        public static float GetMovementModifierDelta(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item?.m_shared == null) return 0f;

            float originalMod = item.m_shared.m_movementModifier;
            float pct = YamlConfigManager.GetCumulativeAbilityValue("ArmorMovementPenaltyReduction", level) / 100f;
            if (pct <= 0f) return 0f;

            if (originalMod < 0f)
            {
                float penaltyRecoveryFraction = Mathf.Clamp01(pct);
                float recovered = -originalMod * penaltyRecoveryFraction;

                if (pct > 1.0f)
                {
                    recovered += (pct - 1.0f) * 0.05f;
                }
                else if (level >= 15)
                {
                    recovered += (level - 14) * 0.005f;
                }

                return recovered;
            }
            else if (level >= 10)
            {
                return (level - 9) * 0.003f;
            }

            return 0f;
        }

        /// <summary>
        /// Calculates attack stamina usage reduction
        /// </summary>
        public static float GetAttackStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("WeaponAttackStamina", level) / 100f, 0f, 0.50f);
        }

        /// <summary>
        /// Calculates magic attack Eitr usage reduction
        /// </summary>
        public static float GetAttackEitrReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("WeaponAttackEitr", level) / 100f, 0f, 0.50f);
        }

        /// <summary>
        /// Calculates block stamina usage reduction
        /// </summary>
        public static float GetBlockStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("ShieldBlockStaminaReduction", level) / 100f, 0f, 0.60f);
        }

        /// <summary>
        /// Calculates dodge stamina usage reduction
        /// </summary>
        public static float GetDodgeStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("DodgeStaminaReduction", level) / 100f, 0f, 0.50f);
        }

        /// <summary>
        /// Calculates sprint stamina usage reduction
        /// </summary>
        public static float GetRunStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("RunStaminaReduction", level) / 100f, 0f, 0.40f);
        }

        /// <summary>
        /// Calculates jump stamina usage reduction
        /// </summary>
        public static float GetJumpStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(YamlConfigManager.GetCumulativeAbilityValue("JumpStaminaReduction", level) / 100f, 0f, 0.40f);
        }

        /// <summary>
        /// Calculates Eitr regeneration rate bonus
        /// </summary>
        public static float GetEitrRegenBonus(int level)
        {
            if (level <= 0) return 0f;
            return YamlConfigManager.GetCumulativeAbilityValue("EitrRegen", level) / 100f;
        }

        /// <summary>
        /// Calculates maximum carry weight bonus for belts or utility accessories
        /// </summary>
        public static float GetCarryWeightBonus(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item?.m_shared == null) return 0f;
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                return YamlConfigManager.GetCumulativeAbilityValue("CarryWeightBonus", level);
            }
            return 0f;
        }

        public static float GetBackstabBonus(int level)
        {
            if (level <= 0) return 0f;
            return YamlConfigManager.GetCumulativeAbilityValue("WeaponBackstab", level) / 100f;
        }

        public static float GetTimedBlockBonus(int level)
        {
            if (level <= 0) return 0f;
            return YamlConfigManager.GetCumulativeAbilityValue("ShieldTimedBlock", level) / 100f;
        }

        #region Cheat Abilities Calculations

        public static bool IsCheatEnabled => ModConfig.EnableCheatAbilities.Value;

        public static bool IsArmor(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            var t = item.m_shared.m_itemType;
            return t == ItemDrop.ItemData.ItemType.Helmet ||
                   t == ItemDrop.ItemData.ItemType.Chest ||
                   t == ItemDrop.ItemData.ItemType.Legs ||
                   t == ItemDrop.ItemData.ItemType.Shoulder;
        }

        public static bool IsWeapon(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            var t = item.m_shared.m_itemType;
            return t == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                   t == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                   t == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                   t == ItemDrop.ItemData.ItemType.Bow;
        }

        public static bool IsShield(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
        }

        public static bool IsUtility(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
        }

        // Single-item cheat bonus methods (for tooltips and UI)
        public static float GetCheatItemMaxHealth(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("MaxHealth", level);
            }
            return 0f;
        }

        public static float GetCheatItemMaxStamina(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsWeapon(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("MaxStamina", level);
            }
            return 0f;
        }

        public static float GetCheatItemMaxEitr(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsWeapon(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("MaxEitr", level);
            }
            return 0f;
        }

        public static float GetCheatItemHealthRegen(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("HealthRegen", level) / 100f;
            }
            return 0f;
        }

        public static float GetCheatItemStaminaRegen(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsWeapon(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("StaminaRegen", level) / 100f;
            }
            return 0f;
        }

        public static float GetCheatItemEitrRegen(ItemDrop.ItemData item, int level)
        {
            // EitrRegen is sourced directly from Armor.yml, no separate cheat variant
            return 0f;
        }

        public static float GetCheatItemHealingBonus(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsUtility(item))
            {
                return YamlConfigManager.GetCumulativeAbilityValue("HealingMultiplier", level) / 100f;
            }
            return 0f;
        }

        // Aggregate cheat bonuses for Player across all equipped items
        public static float GetCheatMaxHealth(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemMaxHealth(item, level);
            }
            return total;
        }

        public static float GetCheatMaxStamina(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemMaxStamina(item, level);
            }
            return total;
        }

        public static float GetCheatMaxEitr(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemMaxEitr(item, level);
            }
            return total;
        }

        public static float GetCheatHealthRegenBonus(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemHealthRegen(item, level);
            }
            return total;
        }

        public static float GetCheatStaminaRegenBonus(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemStaminaRegen(item, level);
            }
            return total;
        }

        public static float GetCheatEitrRegenBonus(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemEitrRegen(item, level);
            }
            return total;
        }

        public static float GetCheatHealingBonus(Player player)
        {
            if (!IsCheatEnabled || player == null || player.GetInventory() == null) return 0f;
            float total = 0f;
            var equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null) return 0f;
            foreach (var item in equipped)
            {
                int level = EnhancementManager.GetEnhancementLevel(item);
                total += GetCheatItemHealingBonus(item, level);
            }
            return total;
        }

        #endregion
    }
}
