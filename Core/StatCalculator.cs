using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class StatCalculator
    {
        /// <summary>
        /// คำนวณเพิ่มค่าความเสียหายทุกประเภทของอาวุธตามระดับการตีบวก
        /// </summary>
        public static void ApplyDamageBonus(ItemDrop.ItemData item, int level, ref HitData.DamageTypes damages)
        {
            if (level <= 0 || item == null) return;

            float bonusMultiplier = 1f + (level * ModConfig.WeaponDamageBonusPerLevel.Value);

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
        /// คำนวณพลังป้องกันเกราะเพิ่มเติม (ทั้งแบบ Flat และแบบ Percent)
        /// </summary>
        public static float GetArmorBonus(ItemDrop.ItemData item, int level, float baseArmor)
        {
            if (level <= 0 || item == null) return 0f;

            float flatBonus = level * ModConfig.ArmorFlatBonusPerLevel.Value;
            float percentBonus = baseArmor * (level * ModConfig.ArmorPercentBonusPerLevel.Value);

            return flatBonus + percentBonus;
        }

        /// <summary>
        /// คำนวณพลังบล็อกของโล่
        /// </summary>
        public static float GetBlockPowerBonus(ItemDrop.ItemData item, int level, float baseBlock)
        {
            if (level <= 0 || item == null || baseBlock <= 0f) return 0f;
            return baseBlock * (level * ModConfig.ShieldBlockPowerBonusPerLevel.Value);
        }

        /// <summary>
        /// คำนวณแรงปัดป้อง (Deflection force)
        /// </summary>
        public static float GetDeflectionBonus(ItemDrop.ItemData item, int level, float baseDeflection)
        {
            if (level <= 0 || item == null || baseDeflection <= 0f) return 0f;
            return baseDeflection * (level * ModConfig.ShieldDeflectionBonusPerLevel.Value);
        }

        /// <summary>
        /// คำนวณความทนทานสูงสุดเพิ่มเติม
        /// </summary>
        public static float GetDurabilityBonus(ItemDrop.ItemData item, int level, float baseDurability)
        {
            if (level <= 0 || item == null || baseDurability <= 0f) return 0f;
            return baseDurability * (level * ModConfig.MaxDurabilityBonusPerLevel.Value);
        }

        /// <summary>
        /// คำนวณการลดน้ำหนักของอุปกรณ์
        /// </summary>
        public static float GetWeightMultiplier(int level)
        {
            if (level <= 0) return 1f;
            float reduction = Mathf.Clamp(level * ModConfig.WeightReductionPerLevel.Value, 0f, 0.60f); // ลดน้ำหนักได้สูงสุด 60%
            return 1f - reduction;
        }

        /// <summary>
        /// คำนวณการลดโทษความเร็วเคลื่อนที่ของเกราะ/โล่ (และเปลี่ยนเป็นโบนัสความเร็วที่ระดับสูง)
        /// </summary>
        public static float GetMovementModifierDelta(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item?.m_shared == null) return 0f;

            float originalMod = item.m_shared.m_movementModifier;
            if (originalMod < 0f)
            {
                // ลดโทษติดลบลง
                float penaltyRecoveryFraction = Mathf.Clamp01(level * ModConfig.ArmorMovementPenaltyReductionPerLevel.Value);
                float recovered = -originalMod * penaltyRecoveryFraction;

                // หากเลเวล 15 ขึ้นไป ให้โบนัสความเร็วเล็กน้อย
                if (level >= 15)
                {
                    recovered += (level - 14) * 0.005f; // +0.5% ถึง +3%
                }

                return recovered;
            }
            else if (level >= 10)
            {
                // ถ้าไอเทมไม่มีโทษติดลบ (เช่น เสื้อผ้าเบา) ให้โบนัสความเร็วเล็กน้อย
                return (level - 9) * 0.003f;
            }

            return 0f;
        }

        /// <summary>
        /// คำนวณการลดการใช้ Stamina โจมตี
        /// </summary>
        public static float GetAttackStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.WeaponStaminaReductionPerLevel.Value, 0f, 0.40f);
        }

        /// <summary>
        /// คำนวณการลดการใช้ Eitr ในการร่ายมนตร์
        /// </summary>
        public static float GetAttackEitrReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.WeaponEitrReductionPerLevel.Value, 0f, 0.40f);
        }

        /// <summary>
        /// คำนวณการลด Stamina ในการบล็อก
        /// </summary>
        public static float GetBlockStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.ShieldBlockStaminaReductionPerLevel.Value, 0f, 0.50f);
        }

        /// <summary>
        /// คำนวณการลด Stamina ในการกลิ้งหลบ
        /// </summary>
        public static float GetDodgeStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.DodgeStaminaReductionPerLevel.Value, 0f, 0.40f);
        }

        /// <summary>
        /// คำนวณการลด Stamina ในการวิ่ง
        /// </summary>
        public static float GetRunStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.RunStaminaReductionPerLevel.Value, 0f, 0.30f);
        }

        /// <summary>
        /// คำนวณการลด Stamina ในการกระโดด
        /// </summary>
        public static float GetJumpStaminaReduction(int level)
        {
            if (level <= 0) return 0f;
            return Mathf.Clamp(level * ModConfig.JumpStaminaReductionPerLevel.Value, 0f, 0.30f);
        }

        /// <summary>
        /// คำนวณโบนัสฟื้นฟู Eitr Regen
        /// </summary>
        public static float GetEitrRegenBonus(int level)
        {
            if (level <= 0) return 0f;
            return level * ModConfig.EitrRegenBonusPerLevel.Value;
        }

        /// <summary>
        /// คำนวณโบนัสเพิ่มน้ำหนักบรรทุก (Carry Weight) สำหรับเข็มขัดหรือเครื่องประดับ
        /// </summary>
        public static float GetCarryWeightBonus(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item?.m_shared == null) return 0f;
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                return level * ModConfig.CarryWeightBonusPerLevel.Value;
            }
            return 0f;
        }

        #region Cheat Abilities Calculations

        public static bool IsCheatEnabled => ModConfig.EnableCheatAbilities != null && ModConfig.EnableCheatAbilities.Value;

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
                return level * ModConfig.CheatMaxHealthPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemMaxStamina(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsWeapon(item) || IsUtility(item))
            {
                return level * ModConfig.CheatMaxStaminaPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemMaxEitr(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsWeapon(item) || IsUtility(item))
            {
                return level * ModConfig.CheatMaxEitrPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemHealthRegen(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsUtility(item))
            {
                return level * ModConfig.CheatHealthRegenPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemStaminaRegen(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsWeapon(item) || IsUtility(item))
            {
                return level * ModConfig.CheatStaminaRegenPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemEitrRegen(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsWeapon(item) || IsUtility(item))
            {
                return level * ModConfig.CheatEitrRegenPerLevel.Value;
            }
            return 0f;
        }

        public static float GetCheatItemHealingBonus(ItemDrop.ItemData item, int level)
        {
            if (!IsCheatEnabled || level <= 0 || item == null) return 0f;
            if (IsArmor(item) || IsShield(item) || IsUtility(item))
            {
                return level * ModConfig.CheatHealingMultiplierPerLevel.Value;
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
