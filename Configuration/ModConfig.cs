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
        public static ConfigValue<int> SafeLevel => YamlConfigManager.Success.SafeLevel;
        public static ConfigValue<bool> DowngradeOnFail => YamlConfigManager.Success.DowngradeOnFail;
        public static ConfigValue<bool> BreakOnFail => YamlConfigManager.Success.BreakOnFail;
        public static ConfigValue<float> BreakChanceAboveSafeLevel => YamlConfigManager.Success.BreakChanceAboveSafeLevel;

        // Costs & Requirements
        public static ConfigValue<bool> RequireCoins => YamlConfigManager.Success.RequireCoins;
        public static ConfigValue<int> CoinsBaseCost => YamlConfigManager.Success.CoinsBaseCost;
        public static ConfigValue<float> CoinsPerLevelIncrement => YamlConfigManager.Success.CoinsPerLevelIncrement;
        public static ConfigValue<bool> RequireCraftingStation => YamlConfigManager.Success.RequireCraftingStation;
        public static ConfigValue<float> CraftingStationRange => YamlConfigManager.Success.CraftingStationRange;

        // Enhancement Scrolls & Monster Drops
        public static ConfigValue<bool> RequireScrolls => YamlConfigManager.Success.RequireScrolls;
        public static ConfigValue<int> ScrollsRequiredPerAttempt => YamlConfigManager.Success.ScrollsRequiredPerAttempt;
        public static ConfigValue<bool> EnableMonsterDrops => YamlConfigManager.Drops.EnableMonsterDrops;
        public static ConfigValue<float> StarLevelMultiplier => YamlConfigManager.Drops.StarLevelMultiplier;
        public static ConfigValue<bool> BossGuaranteedDrop => YamlConfigManager.Drops.BossDrops.GuaranteedDrop;
        public static ConfigValue<int> BossMinDrop => YamlConfigManager.Drops.BossDrops.MinAmount;
        public static ConfigValue<int> BossMaxDrop => YamlConfigManager.Drops.BossDrops.MaxAmount;

        // Cheat Abilities
        public static ConfigValue<bool> EnableCheatAbilities => YamlConfigManager.Cheat.EnableCheatAbilities;

        #endregion

        public static void Initialize(ConfigFile config)
        {
            // Initialize YAML subsystem
            YamlConfigManager.Initialize();

            // Enable auto-saving on change
            config.SaveOnConfigSet = true;

            // Core UI & Controls bindings in BepInEx CFG
            ToggleGuiKey = config.Bind(
                "1. General & UI",
                "ToggleGuiKey",
                KeyCode.F8,
                "Shortcut key to toggle the item enhancement window."
            );

            ShowInventoryBadges = config.Bind(
                "1. General & UI",
                "ShowInventoryBadges",
                true,
                "Display tier-colored enhancement level badges (+X) on inventory icons."
            );

            // Save to disk so any missing or newly added config options are populated while keeping user values intact
            config.Save();
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

        public static float GetTierDropChance(int tier, Heightmap.Biome biome = Heightmap.Biome.Meadows)
        {
            return YamlConfigManager.Drops.GetBiomeTierChance(biome, tier);
        }
    }

    /// <summary>
    /// Wrapper struct allowing callers to access .Value and seamlessly cast to primitive types.
    /// Eliminates delegate allocations when accessing configuration values.
    /// </summary>
    public readonly struct ConfigValue<T>
    {
        private readonly T _value;

        public ConfigValue(T value)
        {
            _value = value;
        }

        public T Value => _value;

        public static implicit operator T(ConfigValue<T> configVal) => configVal.Value;
        public static implicit operator ConfigValue<T>(T val) => new ConfigValue<T>(val);

        public override string ToString() => Value?.ToString() ?? string.Empty;
    }
}
