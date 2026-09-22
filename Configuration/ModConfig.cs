using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.ItemEnhancements.Configuration
{
    public static class ModConfig
    {
        public const int MaxLevel = 20;

        // UI & Keybindings (BepInEx Config)
        public static ConfigEntry<KeyCode> ToggleGuiKey { get; private set; }
        public static ConfigEntry<bool> ShowInventoryBadges { get; private set; }

        #region YAML-backed Configuration Properties

        // Failure Mechanics
        public static ConfigValue<int> SafeLevel => new(() => YamlConfigManager.Success.SafeLevel);
        public static ConfigValue<bool> DowngradeOnFail => new(() => YamlConfigManager.Success.DowngradeOnFail);
        public static ConfigValue<bool> BreakOnFail => new(() => YamlConfigManager.Success.BreakOnFail);
        public static ConfigValue<float> BreakChanceAboveSafeLevel => new(() => YamlConfigManager.Success.BreakChanceAboveSafeLevel);

        // Costs & Requirements
        public static ConfigValue<bool> RequireCoins => new(() => YamlConfigManager.Success.RequireCoins);
        public static ConfigValue<int> CoinsBaseCost => new(() => YamlConfigManager.Success.CoinsBaseCost);
        public static ConfigValue<float> CoinsPerLevelIncrement => new(() => YamlConfigManager.Success.CoinsPerLevelIncrement);
        public static ConfigValue<bool> RequireCraftingStation => new(() => YamlConfigManager.Success.RequireCraftingStation);

        // Enhancement Scrolls & Monster Drops
        public static ConfigValue<bool> RequireScrolls => new(() => YamlConfigManager.Success.RequireScrolls);
        public static ConfigValue<int> ScrollsRequiredPerAttempt => new(() => YamlConfigManager.Success.ScrollsRequiredPerAttempt);
        public static ConfigValue<bool> EnableMonsterDrops => new(() => YamlConfigManager.Success.EnableMonsterDrops);
        public static ConfigValue<float> Tier1_DropChance => new(() => YamlConfigManager.Success.Tier1_DropChance);
        public static ConfigValue<float> Tier2_DropChance => new(() => YamlConfigManager.Success.Tier2_DropChance);
        public static ConfigValue<float> Tier3_DropChance => new(() => YamlConfigManager.Success.Tier3_DropChance);
        public static ConfigValue<float> Tier4_DropChance => new(() => YamlConfigManager.Success.Tier4_DropChance);
        public static ConfigValue<float> StarLevelMultiplier => new(() => YamlConfigManager.Success.StarLevelMultiplier);
        public static ConfigValue<bool> BossGuaranteedDrop => new(() => YamlConfigManager.Success.BossGuaranteedDrop);
        public static ConfigValue<int> BossMinDrop => new(() => YamlConfigManager.Success.BossMinDrop);
        public static ConfigValue<int> BossMaxDrop => new(() => YamlConfigManager.Success.BossMaxDrop);

        // Stat Scaling: Weapons
        public static ConfigValue<float> WeaponDamageBonusPerLevel => new(() => YamlConfigManager.Item.WeaponDamageBonusPerLevel);
        public static ConfigValue<float> WeaponBackstabBonusPerLevel => new(() => YamlConfigManager.Item.WeaponBackstabBonusPerLevel);
        public static ConfigValue<float> WeaponStaminaReductionPerLevel => new(() => YamlConfigManager.Item.WeaponStaminaReductionPerLevel);
        public static ConfigValue<float> WeaponEitrReductionPerLevel => new(() => YamlConfigManager.Item.WeaponEitrReductionPerLevel);

        // Stat Scaling: Armor & Defense
        public static ConfigValue<float> ArmorFlatBonusPerLevel => new(() => YamlConfigManager.Armor.ArmorFlatBonusPerLevel);
        public static ConfigValue<float> ArmorPercentBonusPerLevel => new(() => YamlConfigManager.Armor.ArmorPercentBonusPerLevel);
        public static ConfigValue<float> ArmorMovementPenaltyReductionPerLevel => new(() => YamlConfigManager.Armor.ArmorMovementPenaltyReductionPerLevel);
        public static ConfigValue<float> EitrRegenBonusPerLevel => new(() => YamlConfigManager.Armor.EitrRegenBonusPerLevel);

        // Stat Scaling: Shields
        public static ConfigValue<float> ShieldBlockPowerBonusPerLevel => new(() => YamlConfigManager.Armor.ShieldBlockPowerBonusPerLevel);
        public static ConfigValue<float> ShieldDeflectionBonusPerLevel => new(() => YamlConfigManager.Armor.ShieldDeflectionBonusPerLevel);
        public static ConfigValue<float> ShieldTimedBlockBonusPerLevel => new(() => YamlConfigManager.Armor.ShieldTimedBlockBonusPerLevel);
        public static ConfigValue<float> ShieldBlockStaminaReductionPerLevel => new(() => YamlConfigManager.Armor.ShieldBlockStaminaReductionPerLevel);

        // Stat Scaling: Player Mobility & Utilities
        public static ConfigValue<float> DodgeStaminaReductionPerLevel => new(() => YamlConfigManager.Armor.DodgeStaminaReductionPerLevel);
        public static ConfigValue<float> RunStaminaReductionPerLevel => new(() => YamlConfigManager.Armor.RunStaminaReductionPerLevel);
        public static ConfigValue<float> JumpStaminaReductionPerLevel => new(() => YamlConfigManager.Armor.JumpStaminaReductionPerLevel);
        public static ConfigValue<float> CarryWeightBonusPerLevel => new(() => YamlConfigManager.Item.CarryWeightBonusPerLevel);

        // General Stat Scaling
        public static ConfigValue<float> MaxDurabilityBonusPerLevel => new(() => YamlConfigManager.Item.MaxDurabilityBonusPerLevel);
        public static ConfigValue<float> WeightReductionPerLevel => new(() => YamlConfigManager.Item.WeightReductionPerLevel);

        // Cheat Abilities
        public static ConfigValue<bool> EnableCheatAbilities => new(() => YamlConfigManager.Cheat.EnableCheatAbilities);
        public static ConfigValue<float> CheatMaxHealthPerLevel => new(() => YamlConfigManager.Cheat.CheatMaxHealthPerLevel);
        public static ConfigValue<float> CheatMaxStaminaPerLevel => new(() => YamlConfigManager.Cheat.CheatMaxStaminaPerLevel);
        public static ConfigValue<float> CheatMaxEitrPerLevel => new(() => YamlConfigManager.Cheat.CheatMaxEitrPerLevel);
        public static ConfigValue<float> CheatHealthRegenPerLevel => new(() => YamlConfigManager.Cheat.CheatHealthRegenPerLevel);
        public static ConfigValue<float> CheatStaminaRegenPerLevel => new(() => YamlConfigManager.Cheat.CheatStaminaRegenPerLevel);
        public static ConfigValue<float> CheatEitrRegenPerLevel => new(() => YamlConfigManager.Cheat.CheatEitrRegenPerLevel);
        public static ConfigValue<float> CheatHealingMultiplierPerLevel => new(() => YamlConfigManager.Cheat.CheatHealingMultiplierPerLevel);

        #endregion

        public static void Initialize(ConfigFile config)
        {
            // Initialize YAML subsystem
            YamlConfigManager.Initialize();

            // Core UI & Controls bindings in BepInEx CFG
            ToggleGuiKey = config.Bind(
                "1. General & UI (การตั้งค่าทั่วไปและปุ่มลัด)",
                "ToggleGuiKey",
                KeyCode.F8,
                "ปุ่มลัดสำหรับเปิด/ปิดหน้าต่างตีบวก (Shortcut key to toggle enhancement window)"
            );

            ShowInventoryBadges = config.Bind(
                "1. General & UI (การตั้งค่าทั่วไปและปุ่มลัด)",
                "ShowInventoryBadges",
                true,
                "แสดงป้ายระดับ +X สีตามเทียร์บนไอคอนในกระเป๋า (Show tier-colored level badge on inventory icons)"
            );
        }

        public static float GetSuccessRate(int targetLevel)
        {
            if (targetLevel < 1) return 100f;
            if (targetLevel > MaxLevel) return 0f;
            return YamlConfigManager.Success.GetSuccessRate(targetLevel);
        }

        public static int GetCoinCost(int targetLevel)
        {
            if (!RequireCoins.Value || targetLevel < 1) return 0;
            return CoinsBaseCost.Value + Mathf.RoundToInt((targetLevel - 1) * CoinsPerLevelIncrement.Value);
        }

        public static float GetTierDropChance(int tier)
        {
            switch (tier)
            {
                case 1: return Tier1_DropChance.Value;
                case 2: return Tier2_DropChance.Value;
                case 3: return Tier3_DropChance.Value;
                case 4: return Tier4_DropChance.Value;
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// Wrapper struct เพื่อให้โค้ดส่วนอื่นสามารถเรียกใช้ .Value และแปลงค่าเป็น Primitive types ได้อย่างราบรื่น
    /// </summary>
    public readonly struct ConfigValue<T>
    {
        private readonly Func<T> _getter;

        public ConfigValue(Func<T> getter)
        {
            _getter = getter;
        }

        public T Value => _getter != null ? _getter() : default;

        public static implicit operator T(ConfigValue<T> configVal) => configVal.Value;

        public override string ToString() => Value?.ToString() ?? string.Empty;
    }
}
