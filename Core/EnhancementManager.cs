using System;
using System.Collections.Generic;
using System.Text;
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
        /// ดึงระดับการตีบวกของไอเทม (0 - 20)
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
        /// บันทึกระดับการตีบวกของไอเทมลงใน m_customData (ระบบเซฟอัตโนมัติของ Valheim)
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
        /// ตรวจสอบว่าไอเทมนี้สามารถนำมาตีบวกได้หรือไม่
        /// </summary>
        public static bool IsEnhanceable(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;

            // ตรวจสอบประเภทไอเทมที่อนุญาตให้ตีบวกได้
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
        /// ดึงจำนวนเหรียญทอง (Coins) ทั้งหมดในตัวผู้เล่น
        /// </summary>
        public static int GetPlayerCoins(Player player)
        {
            if (player == null || player.GetInventory() == null) return 0;
            return player.GetInventory().CountItems("$item_coins", -1, true);
        }

        /// <summary>
        /// หักเหรียญทองออกจากตัวผู้เล่น
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
        /// ตรวจสอบว่าผู้เล่นสามารถจ่ายค่าธรรมเนียมการตีบวกได้หรือไม่
        /// </summary>
        public static bool CanAfford(Player player, int targetLevel)
        {
            if (!ModConfig.RequireCoins.Value) return true;
            int cost = ModConfig.GetCoinCost(targetLevel);
            return GetPlayerCoins(player) >= cost;
        }

        /// <summary>
        /// ดำเนินการตีบวกไอเทม
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

            if (ModConfig.RequireCoins.Value && !CanAfford(player, targetLevel))
            {
                newLevel = currentLevel;
                return EnhanceResult.CannotAfford;
            }

            // หักค่าธรรมเนียม
            if (ModConfig.RequireCoins.Value && cost > 0)
            {
                DeductPlayerCoins(player, cost);
            }

            // สุ่มความสำเร็จ
            float roll = UnityEngine.Random.Range(0f, 100f);
            float successRate = ModConfig.GetSuccessRate(targetLevel);

            if (roll < successRate)
            {
                // สำเร็จ!
                newLevel = targetLevel;
                SetEnhancementLevel(item, newLevel);
                return EnhanceResult.Success;
            }

            // ล้มเหลว!
            if (currentLevel <= ModConfig.SafeLevel.Value)
            {
                // อยู่ในระดับปลอดภัย ระดับไม่ลดลง
                newLevel = currentLevel;
                return EnhanceResult.FailedSafe;
            }

            // เกินระดับปลอดภัย
            if (ModConfig.BreakOnFail.Value)
            {
                float breakRoll = UnityEngine.Random.Range(0f, 100f);
                if (breakRoll < ModConfig.BreakChanceAboveSafeLevel.Value)
                {
                    // แตกสลาย!
                    if (player?.GetInventory() != null)
                    {
                        player.GetInventory().RemoveItem(item);
                    }
                    newLevel = 0;
                    return EnhanceResult.FailedBroken;
                }
            }

            if (ModConfig.DowngradeOnFail.Value)
            {
                // ลดระดับลง 1 ขั้น
                newLevel = Mathf.Max(0, currentLevel - 1);
                SetEnhancementLevel(item, newLevel);
                return EnhanceResult.FailedDowngraded;
            }

            newLevel = currentLevel;
            return EnhanceResult.FailedSafe;
        }

        #region UI & Presentation Helpers

        /// <summary>
        /// ดึงรหัสสี Hex ประจำแต่ละระดับการตีบวก
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
        /// ดึงสี Color สำหรับ IMGUI หรือ Canvas
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
        /// ชื่อระดับฉายาของเทียร์
        /// </summary>
        public static string GetTierRankName(int level)
        {
            if (level <= 0) return "Normal";
            if (level <= 3) return "Standard (พื้นฐาน)";
            if (level <= 6) return "Refined (ขัดเกลา)";
            if (level <= 9) return "Rare (หายาก)";
            if (level <= 12) return "Epic (มหากาพย์)";
            if (level <= 15) return "Legendary (ตำนาน)";
            if (level <= 19) return "Mythic (เทวตำนาน)";
            return "Divine (เทวะสูงสุด)";
        }

        /// <summary>
        /// จัดรูปแบบชื่อไอเทมพร้อมแสดงระดับ +X
        /// </summary>
        public static string FormatItemNameWithLevel(ItemDrop.ItemData item, string originalName, int level)
        {
            if (level <= 0) return originalName;
            string hex = GetTierHex(level);
            return $"{originalName} <color={hex}>+{level}</color>";
        }

        /// <summary>
        /// สร้างข้อความ Tooltip สไตล์ MMORPG แสดงข้อมูลการตีบวกอย่างละเอียด
        /// </summary>
        public static string BuildEnhancementTooltip(ItemDrop.ItemData item, int level)
        {
            if (level <= 0 || item == null) return string.Empty;

            string hex = GetTierHex(level);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"<color=#ffd700>══════════════════════════════</color>");
            sb.AppendLine($"<color={hex}><b>★ MMORPG ENHANCEMENT (+{level}/{ModConfig.MaxLevel}) ★</b></color>");
            sb.AppendLine($"<color=#94a3b8>ระดับ: {GetTierRankName(level)}</color>");

            // แสดงโบนัสตามประเภทไอเทม
            // 1. อาวุธ
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
            {
                float dmgBonusPct = level * ModConfig.WeaponDamageBonusPerLevel.Value * 100f;
                sb.AppendLine($"<color=#4ade80>• พลังโจมตีทุกธาตุ (All Damage): +{dmgBonusPct:F0}%</color>");

                if (item.m_shared.m_backstabBonus > 1f)
                {
                    float bsBonus = level * ModConfig.WeaponBackstabBonusPerLevel.Value;
                    sb.AppendLine($"<color=#4ade80>• โบนัสแทงข้างหลัง (Backstab): +{bsBonus:F2}x</color>");
                }

                float atkStam = StatCalculator.GetAttackStaminaReduction(level) * 100f;
                if (atkStam > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• ลดการใช้ Stamina โจมตี: -{atkStam:F0}%</color>");
                }

                float atkEitr = StatCalculator.GetAttackEitrReduction(level) * 100f;
                if (atkEitr > 0f && item.m_shared.m_attack.m_attackEitr > 0f)
                {
                    sb.AppendLine($"<color=#c084fc>• ลดการใช้ Eitr ร่ายเวท: -{atkEitr:F0}%</color>");
                }
            }

            // 2. ชุดเกราะ
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
            {
                float armorBonus = StatCalculator.GetArmorBonus(item, level, item.m_shared.m_armor);
                sb.AppendLine($"<color=#4ade80>• พลังป้องกันเกราะ (Armor): +{armorBonus:F1}</color>");

                float moveDelta = StatCalculator.GetMovementModifierDelta(item, level) * 100f;
                if (moveDelta > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• ลดโทษความเร็วเดิน / โบนัส: +{moveDelta:F1}%</color>");
                }

                float dodgeRed = StatCalculator.GetDodgeStaminaReduction(level) * 100f;
                if (dodgeRed > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• ลด Stamina กลิ้งหลบ: -{dodgeRed:F0}%</color>");
                }

                float eitrRegen = StatCalculator.GetEitrRegenBonus(level) * 100f;
                if (eitrRegen > 0f && item.m_shared.m_eitrRegenModifier > 0f)
                {
                    sb.AppendLine($"<color=#c084fc>• โบนัสฟื้นฟู Eitr: +{eitrRegen:F0}%</color>");
                }
            }

            // 3. โล่
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                float blkPct = level * ModConfig.ShieldBlockPowerBonusPerLevel.Value * 100f;
                sb.AppendLine($"<color=#4ade80>• พลังบล็อก (Block Power): +{blkPct:F0}%</color>");

                float defPct = level * ModConfig.ShieldDeflectionBonusPerLevel.Value * 100f;
                sb.AppendLine($"<color=#4ade80>• แรงปัดป้อง (Parry Force): +{defPct:F0}%</color>");

                float blkStam = StatCalculator.GetBlockStaminaReduction(level) * 100f;
                if (blkStam > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• ลด Stamina ในการบล็อก: -{blkStam:F0}%</color>");
                }

                float moveDelta = StatCalculator.GetMovementModifierDelta(item, level) * 100f;
                if (moveDelta > 0f)
                {
                    sb.AppendLine($"<color=#38bdf8>• ลดโทษความเร็วเดินของโล่: +{moveDelta:F1}%</color>");
                }
            }

            // 4. เข็มขัด / ยูทิลิตี้
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                float carry = StatCalculator.GetCarryWeightBonus(item, level);
                if (carry > 0f)
                {
                    sb.AppendLine($"<color=#f59e0b>• เพิ่มน้ำหนักบรรทุก (Max Carry): +{carry:F0}</color>");
                }
            }

            // 5. คุณสมบัติทั่วไป
            float durPct = level * ModConfig.MaxDurabilityBonusPerLevel.Value * 100f;
            sb.AppendLine($"<color=#94a3b8>• ความทนทานสูงสุด (Durability): +{durPct:F0}%</color>");

            float wtPct = Mathf.Clamp(level * ModConfig.WeightReductionPerLevel.Value, 0f, 0.60f) * 100f;
            if (wtPct > 0f)
            {
                sb.AppendLine($"<color=#94a3b8>• น้ำหนักอุปกรณ์ (Weight): -{wtPct:F1}%</color>");
            }

            // Crafter
            if (item.m_customData != null && item.m_customData.TryGetValue(CrafterDataKey, out string crafter) && !string.IsNullOrEmpty(crafter))
            {
                sb.AppendLine($"<color=#64748b>ช่างตีบวก: {crafter}</color>");
            }

            sb.AppendLine($"<color=#ffd700>══════════════════════════════</color>");
            return sb.ToString();
        }

        #endregion
    }
}
