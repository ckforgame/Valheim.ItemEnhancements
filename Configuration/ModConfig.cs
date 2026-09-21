using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.ItemEnhancements.Configuration
{
    public static class ModConfig
    {
        public const int MaxLevel = 20;

        // Success Rates for Levels 1 to 20
        public static ConfigEntry<float>[] SuccessRates = new ConfigEntry<float>[MaxLevel + 1];

        // Failure Mechanics
        public static ConfigEntry<int> SafeLevel { get; private set; }
        public static ConfigEntry<bool> DowngradeOnFail { get; private set; }
        public static ConfigEntry<bool> BreakOnFail { get; private set; }
        public static ConfigEntry<float> BreakChanceAboveSafeLevel { get; private set; }

        // Costs & Requirements
        public static ConfigEntry<bool> RequireCoins { get; private set; }
        public static ConfigEntry<int> CoinsBaseCost { get; private set; }
        public static ConfigEntry<float> CoinsPerLevelIncrement { get; private set; }
        public static ConfigEntry<bool> RequireCraftingStation { get; private set; }

        // Stat Scaling: Weapons
        public static ConfigEntry<float> WeaponDamageBonusPerLevel { get; private set; }
        public static ConfigEntry<float> WeaponBackstabBonusPerLevel { get; private set; }
        public static ConfigEntry<float> WeaponStaminaReductionPerLevel { get; private set; }
        public static ConfigEntry<float> WeaponEitrReductionPerLevel { get; private set; }

        // Stat Scaling: Armor & Defense
        public static ConfigEntry<float> ArmorFlatBonusPerLevel { get; private set; }
        public static ConfigEntry<float> ArmorPercentBonusPerLevel { get; private set; }
        public static ConfigEntry<float> ArmorMovementPenaltyReductionPerLevel { get; private set; }
        public static ConfigEntry<float> EitrRegenBonusPerLevel { get; private set; }

        // Stat Scaling: Shields
        public static ConfigEntry<float> ShieldBlockPowerBonusPerLevel { get; private set; }
        public static ConfigEntry<float> ShieldDeflectionBonusPerLevel { get; private set; }
        public static ConfigEntry<float> ShieldTimedBlockBonusPerLevel { get; private set; }
        public static ConfigEntry<float> ShieldBlockStaminaReductionPerLevel { get; private set; }

        // Stat Scaling: Player Mobility & Utilities
        public static ConfigEntry<float> DodgeStaminaReductionPerLevel { get; private set; }
        public static ConfigEntry<float> RunStaminaReductionPerLevel { get; private set; }
        public static ConfigEntry<float> JumpStaminaReductionPerLevel { get; private set; }
        public static ConfigEntry<float> CarryWeightBonusPerLevel { get; private set; }

        // General Stat Scaling
        public static ConfigEntry<float> MaxDurabilityBonusPerLevel { get; private set; }
        public static ConfigEntry<float> WeightReductionPerLevel { get; private set; }

        // UI & Keybindings
        public static ConfigEntry<KeyCode> ToggleGuiKey { get; private set; }
        public static ConfigEntry<bool> ShowInventoryBadges { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            // Default Success Rates table:
            // 1: 100%, 2: 100%, 3: 95%, 4: 90%, 5: 80%, 6: 70%, 7: 60%, 8: 50%, 9: 40%, 10: 35%,
            // 11: 30%, 12: 25%, 13: 20%, 14: 15%, 15: 12%, 16: 10%, 17: 8%, 18: 5%, 19: 3%, 20: 1%
            float[] defaultRates = new float[]
            {
                0f,
                100f, 100f, 95f, 90f, 80f,
                70f, 60f, 50f, 40f, 35f,
                30f, 25f, 20f, 15f, 12f,
                10f, 8f, 5f, 3f, 1f
            };

            for (int i = 1; i <= MaxLevel; i++)
            {
                SuccessRates[i] = config.Bind(
                    "1. Success Rates (อัตราความสำเร็จ)",
                    $"Level_{i:D2}_SuccessRate",
                    defaultRates[i],
                    new ConfigDescription(
                        $"อัตราความสำเร็จสำหรับการตีบวกขึ้นเป็นระดับ +{i} (Success rate % for +{i})",
                        new AcceptableValueRange<float>(0f, 100f),
                        new ConfigurationManagerAttributes { Order = MaxLevel - i }
                    )
                );
            }

            // Failure Rules
            SafeLevel = config.Bind(
                "2. Failure Rules (กฎการล้มเหลว)",
                "SafeLevel",
                3,
                "ระดับปลอดภัย: การตีบวกไม่เกินระดับนี้จะไม่ลดระดับและไม่แตก (Safe level: Upgrades up to this level will not downgrade or break)"
            );

            DowngradeOnFail = config.Bind(
                "2. Failure Rules (กฎการล้มเหลว)",
                "DowngradeOnFail",
                true,
                "หากล้มเหลวเกินระดับปลอดภัย ระดับจะลดลง 1 ขั้น (If failed above safe level, downgrade by 1 level)"
            );

            BreakOnFail = config.Bind(
                "2. Failure Rules (กฎการล้มเหลว)",
                "BreakOnFail",
                false,
                "หากล้มเหลวเกินระดับปลอดภัย มีโอกาสที่ไอเทมจะแตกสลาย (If failed above safe level, item has a chance to break)"
            );

            BreakChanceAboveSafeLevel = config.Bind(
                "2. Failure Rules (กฎการล้มเหลว)",
                "BreakChanceAboveSafeLevel",
                10f,
                new ConfigDescription(
                    "โอกาสแตก (%) หากเปิดใช้งาน BreakOnFail (Break chance % when BreakOnFail is true)",
                    new AcceptableValueRange<float>(0f, 100f)
                )
            );

            // Costs
            RequireCoins = config.Bind(
                "3. Costs & Requirements (ค่าธรรมเนียม)",
                "RequireCoins",
                true,
                "ต้องใช้เหรียญทอง (Coins) ในการตีบวกหรือไม่ (Whether coins are required to enhance)"
            );

            CoinsBaseCost = config.Bind(
                "3. Costs & Requirements (ค่าธรรมเนียม)",
                "CoinsBaseCost",
                25,
                "ค่าธรรมเนียมเหรียญทองพื้นฐาน (Base coin cost for enhancement)"
            );

            CoinsPerLevelIncrement = config.Bind(
                "3. Costs & Requirements (ค่าธรรมเนียม)",
                "CoinsPerLevelIncrement",
                20f,
                "เหรียญทองที่ต้องใช้เพิ่มขึ้นต่อระดับ (Coin cost increment per target level)"
            );

            RequireCraftingStation = config.Bind(
                "3. Costs & Requirements (ค่าธรรมเนียม)",
                "RequireCraftingStation",
                true,
                "ต้องอยู่ใกล้โต๊ะคราฟต์/เตาตีเหล็กเพื่อเปิดหน้าต่างตีบวก (Must be near a crafting station to enhance)"
            );

            // Stat Scaling: Weapons
            WeaponDamageBonusPerLevel = config.Bind(
                "4. Abilities - Weapons (อาวุธ)",
                "WeaponDamageBonusPerLevel",
                0.05f,
                "โบนัสความเสียหายทุกประเภทต่อระดับ (Damage bonus multiplier per level, e.g. 0.05 = +5% per level)"
            );

            WeaponBackstabBonusPerLevel = config.Bind(
                "4. Abilities - Weapons (อาวุธ)",
                "WeaponBackstabBonusPerLevel",
                0.05f,
                "โบนัสตัวคูณ Backstab เพิ่มขึ้นต่อระดับ (Additional backstab multiplier bonus per level)"
            );

            WeaponStaminaReductionPerLevel = config.Bind(
                "4. Abilities - Weapons (อาวุธ)",
                "WeaponStaminaReductionPerLevel",
                0.01f,
                "ลดการใช้ Stamina โจมตีต่อระดับ (Attack stamina cost reduction per level, 0.01 = -1% per level)"
            );

            WeaponEitrReductionPerLevel = config.Bind(
                "4. Abilities - Weapons (อาวุธ)",
                "WeaponEitrReductionPerLevel",
                0.01f,
                "ลดการใช้ Eitr ของอาวุธเวทมนตร์ต่อระดับ (Attack eitr cost reduction per level, 0.01 = -1% per level)"
            );

            // Stat Scaling: Armor & Defense
            ArmorFlatBonusPerLevel = config.Bind(
                "5. Abilities - Armor (ชุดเกราะ)",
                "ArmorFlatBonusPerLevel",
                1.5f,
                "พลังป้องกันเกราะคงที่เพิ่มขึ้นต่อระดับ (Flat armor bonus added per level)"
            );

            ArmorPercentBonusPerLevel = config.Bind(
                "5. Abilities - Armor (ชุดเกราะ)",
                "ArmorPercentBonusPerLevel",
                0.02f,
                "พลังป้องกันเกราะแบบ % ต่อระดับ (Percentage armor bonus per level, 0.02 = +2%)"
            );

            ArmorMovementPenaltyReductionPerLevel = config.Bind(
                "5. Abilities - Armor (ชุดเกราะ)",
                "ArmorMovementPenaltyReductionPerLevel",
                0.05f,
                "ลดโทษติดลบความเร็วเดินของเกราะหนัก/โล่ต่อระดับ (Movement penalty reduction per level, 0.05 = reduces penalty by 5% per level)"
            );

            EitrRegenBonusPerLevel = config.Bind(
                "5. Abilities - Armor (ชุดเกราะ)",
                "EitrRegenBonusPerLevel",
                0.02f,
                "เพิ่มอัตราฟื้นฟู Eitr ต่อระดับสำหรับชุดนักเวท (Eitr regen modifier bonus per level, 0.02 = +2%)"
            );

            // Stat Scaling: Shields
            ShieldBlockPowerBonusPerLevel = config.Bind(
                "6. Abilities - Shields (โล่)",
                "ShieldBlockPowerBonusPerLevel",
                0.05f,
                "โบนัสพลังบล็อกของโล่ต่อระดับ (Block power bonus multiplier per level, 0.05 = +5%)"
            );

            ShieldDeflectionBonusPerLevel = config.Bind(
                "6. Abilities - Shields (โล่)",
                "ShieldDeflectionBonusPerLevel",
                0.05f,
                "โบนัสแรงปัดป้องต่อระดับ (Deflection force bonus multiplier per level)"
            );

            ShieldTimedBlockBonusPerLevel = config.Bind(
                "6. Abilities - Shields (โล่)",
                "ShieldTimedBlockBonusPerLevel",
                0.02f,
                "โบนัสตัวคูณ Parry เมื่อบล็อกตรงจังหวะต่อระดับ (Parry bonus multiplier per level)"
            );

            ShieldBlockStaminaReductionPerLevel = config.Bind(
                "6. Abilities - Shields (โล่)",
                "ShieldBlockStaminaReductionPerLevel",
                0.015f,
                "ลด Stamina ที่ใช้ในการบล็อกต่อระดับ (Block stamina cost reduction per level, 0.015 = -1.5%)"
            );

            // Stat Scaling: Mobility & Utilities
            DodgeStaminaReductionPerLevel = config.Bind(
                "7. Abilities - Mobility & Utilities (ความคล่องตัวและยูทิลิตี้)",
                "DodgeStaminaReductionPerLevel",
                0.01f,
                "ลด Stamina ที่ใช้ในการกลิ้งหลบต่อระดับ (Dodge stamina reduction per level)"
            );

            RunStaminaReductionPerLevel = config.Bind(
                "7. Abilities - Mobility & Utilities (ความคล่องตัวและยูทิลิตี้)",
                "RunStaminaReductionPerLevel",
                0.005f,
                "ลด Stamina ที่ใช้ในการวิ่งสปรินต์ต่อระดับ (Run stamina reduction per level)"
            );

            JumpStaminaReductionPerLevel = config.Bind(
                "7. Abilities - Mobility & Utilities (ความคล่องตัวและยูทิลิตี้)",
                "JumpStaminaReductionPerLevel",
                0.005f,
                "ลด Stamina ที่ใช้ในการกระโดดต่อระดับ (Jump stamina reduction per level)"
            );

            CarryWeightBonusPerLevel = config.Bind(
                "7. Abilities - Mobility & Utilities (ความคล่องตัวและยูทิลิตี้)",
                "CarryWeightBonusPerLevel",
                5.0f,
                "เพิ่มน้ำหนักบรรทุกสูงสุดสำหรับเข็มขัด/เครื่องประดับต่อระดับ (Max carry weight bonus per level for accessories like Megingjord)"
            );

            MaxDurabilityBonusPerLevel = config.Bind(
                "8. Abilities - General (คุณสมบัติทั่วไป)",
                "MaxDurabilityBonusPerLevel",
                0.05f,
                "เพิ่มความทนทานสูงสุดต่อระดับ (Max durability bonus multiplier per level, 0.05 = +5%)"
            );

            WeightReductionPerLevel = config.Bind(
                "8. Abilities - General (คุณสมบัติทั่วไป)",
                "WeightReductionPerLevel",
                0.015f,
                "ลดน้ำหนักของไอเทมต่อระดับ (Weight reduction per level, 0.015 = -1.5% lighter)"
            );

            // UI & Controls
            ToggleGuiKey = config.Bind(
                "9. UI & Controls (ปุ่มควบคุมและการแสดงผล)",
                "ToggleGuiKey",
                KeyCode.F8,
                "ปุ่มลัดสำหรับเปิด/ปิดหน้าต่างตีบวก (Shortcut key to toggle enhancement window)"
            );

            ShowInventoryBadges = config.Bind(
                "9. UI & Controls (ปุ่มควบคุมและการแสดงผล)",
                "ShowInventoryBadges",
                true,
                "แสดงป้ายระดับ +X สีตามเทียร์บนไอคอนในกระเป๋า (Show tier-colored level badge on inventory icons)"
            );
        }

        public static float GetSuccessRate(int targetLevel)
        {
            if (targetLevel < 1) return 100f;
            if (targetLevel > MaxLevel) return 0f;
            return SuccessRates[targetLevel] != null ? SuccessRates[targetLevel].Value : 0f;
        }

        public static int GetCoinCost(int targetLevel)
        {
            if (!RequireCoins.Value || targetLevel < 1) return 0;
            return CoinsBaseCost.Value + Mathf.RoundToInt((targetLevel - 1) * CoinsPerLevelIncrement.Value);
        }
    }

    public class ConfigurationManagerAttributes
    {
        public int? Order;
    }
}
