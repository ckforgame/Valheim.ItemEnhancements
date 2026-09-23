using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    /// <summary>
    /// Event-driven cache that maintains aggregated equipment stat bonuses for the local player.
    /// Eliminates per-frame List allocations and redundant calculations in hot game loops.
    /// </summary>
    public static class PlayerBonusCache
    {
        public static float MovementBonus { get; private set; }
        public static float DodgeStaminaReduction { get; private set; }
        public static float RunStaminaReduction { get; private set; }
        public static float JumpStaminaReduction { get; private set; }
        public static float BlockStaminaReduction { get; private set; }
        public static float EitrRegenBonus { get; private set; }
        public static float CarryWeightBonus { get; private set; }

        public static float CheatMaxHealth { get; private set; }
        public static float CheatMaxStamina { get; private set; }
        public static float CheatMaxEitr { get; private set; }
        public static float CheatHealthRegen { get; private set; }
        public static float CheatStaminaRegen { get; private set; }
        public static float CheatEitrRegen { get; private set; }
        public static float CheatHealingBonus { get; private set; }

        private static bool s_isDirty = true;

        static PlayerBonusCache()
        {
            YamlConfigManager.OnConfigReloaded += MarkDirty;
        }

        public static void MarkDirty()
        {
            s_isDirty = true;
        }

        public static void EnsureFresh(Player player)
        {
            if (s_isDirty)
            {
                Recalculate(player);
            }
        }

        public static void Recalculate(Player player)
        {
            s_isDirty = false;
            if (player == null || player.GetInventory() == null)
            {
                Reset();
                return;
            }

            List<ItemDrop.ItemData> equipped = player.GetInventory().GetEquippedItems();
            if (equipped == null || equipped.Count == 0)
            {
                Reset();
                return;
            }

            float move = 0f;
            float dodge = 0f;
            float run = 0f;
            float jump = 0f;
            float blockStam = 0f;
            float eitrRegen = 0f;
            float carry = 0f;

            float cHp = 0f;
            float cStam = 0f;
            float cEitr = 0f;
            float cHpRegen = 0f;
            float cStamRegen = 0f;
            float cEitrRegen = 0f;
            float cHeal = 0f;

            bool cheat = StatCalculator.IsCheatEnabled;

            for (int i = 0; i < equipped.Count; i++)
            {
                ItemDrop.ItemData item = equipped[i];
                if (item == null) continue;

                int level = EnhancementManager.GetEnhancementLevel(item);
                if (level <= 0) continue;

                bool isArmor = StatCalculator.IsArmor(item);
                bool isShield = StatCalculator.IsShield(item);
                bool isWeapon = StatCalculator.IsWeapon(item);
                bool isUtility = StatCalculator.IsUtility(item);

                if (isArmor || isShield)
                {
                    move += StatCalculator.GetMovementModifierDelta(item, level);
                }

                if (isArmor)
                {
                    dodge += StatCalculator.GetDodgeStaminaReduction(level);
                    run += StatCalculator.GetRunStaminaReduction(level);
                    jump += StatCalculator.GetJumpStaminaReduction(level);
                }

                if (isShield)
                {
                    blockStam += StatCalculator.GetBlockStaminaReduction(level);
                }

                if (item.m_shared != null && item.m_shared.m_eitrRegenModifier > 0f)
                {
                    eitrRegen += StatCalculator.GetEitrRegenBonus(level);
                }

                if (isUtility)
                {
                    carry += StatCalculator.GetCarryWeightBonus(item, level);
                }

                if (cheat)
                {
                    cHp += StatCalculator.GetCheatItemMaxHealth(item, level);
                    cStam += StatCalculator.GetCheatItemMaxStamina(item, level);
                    cEitr += StatCalculator.GetCheatItemMaxEitr(item, level);
                    cHpRegen += StatCalculator.GetCheatItemHealthRegen(item, level);
                    cStamRegen += StatCalculator.GetCheatItemStaminaRegen(item, level);
                    cEitrRegen += StatCalculator.GetCheatItemEitrRegen(item, level);
                    cHeal += StatCalculator.GetCheatItemHealingBonus(item, level);
                }
            }

            MovementBonus = move;
            DodgeStaminaReduction = Mathf.Clamp(dodge, 0f, 0.50f);
            RunStaminaReduction = Mathf.Clamp(run, 0f, 0.40f);
            JumpStaminaReduction = Mathf.Clamp(jump, 0f, 0.40f);
            BlockStaminaReduction = Mathf.Clamp(blockStam, 0f, 0.50f);
            EitrRegenBonus = eitrRegen;
            CarryWeightBonus = carry;

            CheatMaxHealth = cHp;
            CheatMaxStamina = cStam;
            CheatMaxEitr = cEitr;
            CheatHealthRegen = cHpRegen;
            CheatStaminaRegen = cStamRegen;
            CheatEitrRegen = cEitrRegen;
            CheatHealingBonus = cHeal;
        }

        private static void Reset()
        {
            MovementBonus = 0f;
            DodgeStaminaReduction = 0f;
            RunStaminaReduction = 0f;
            JumpStaminaReduction = 0f;
            BlockStaminaReduction = 0f;
            EitrRegenBonus = 0f;
            CarryWeightBonus = 0f;

            CheatMaxHealth = 0f;
            CheatMaxStamina = 0f;
            CheatMaxEitr = 0f;
            CheatHealthRegen = 0f;
            CheatStaminaRegen = 0f;
            CheatEitrRegen = 0f;
            CheatHealingBonus = 0f;
        }
    }
}
