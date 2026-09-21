using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.Core;

namespace Valheim.ItemEnhancements.Patches
{
    public static class PlayerPatches
    {
        // 1. Movement Speed Modifier (ลดโทษความเร็วเดินของเกราะหนักและโล่)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
        public static class GetEquipmentMovementModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float bonus = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0)
                    {
                        bonus += StatCalculator.GetMovementModifierDelta(item, level);
                    }
                }

                __result += bonus;
            }
        }

        // 2. Dodge Stamina Modifier (ลด Stamina กลิ้งหลบ)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
        public static class GetEquipmentDodgeStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float reduction = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0)
                    {
                        reduction += StatCalculator.GetDodgeStaminaReduction(level);
                    }
                }

                // ค่า modifier ติดลบแปลว่าลดการใช้ stamina ใน Valheim
                __result -= Mathf.Clamp(reduction, 0f, 0.50f);
            }
        }

        // 3. Run Stamina Modifier (ลด Stamina วิ่งสปรินต์)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentRunStaminaModifier))]
        public static class GetEquipmentRunStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float reduction = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0)
                    {
                        reduction += StatCalculator.GetRunStaminaReduction(level);
                    }
                }

                __result -= Mathf.Clamp(reduction, 0f, 0.40f);
            }
        }

        // 4. Jump Stamina Modifier (ลด Stamina กระโดด)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentJumpStaminaModifier))]
        public static class GetEquipmentJumpStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float reduction = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0)
                    {
                        reduction += StatCalculator.GetJumpStaminaReduction(level);
                    }
                }

                __result -= Mathf.Clamp(reduction, 0f, 0.40f);
            }
        }

        // 5. Block Stamina Modifier (ลด Stamina ในการบล็อก)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentBlockStaminaModifier))]
        public static class GetEquipmentBlockStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float reduction = 0f;
                foreach (var item in equipped)
                {
                    if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                    {
                        int level = EnhancementManager.GetEnhancementLevel(item);
                        if (level > 0)
                        {
                            reduction += StatCalculator.GetBlockStaminaReduction(level);
                        }
                    }
                }

                __result -= Mathf.Clamp(reduction, 0f, 0.50f);
            }
        }

        // 6. Eitr Regen Modifier (เพิ่มการฟื้นฟูพลังเวทมนตร์)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        public static class GetEquipmentEitrRegenModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float bonus = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0 && item.m_shared.m_eitrRegenModifier > 0f)
                    {
                        bonus += StatCalculator.GetEitrRegenBonus(level);
                    }
                }

                __result += bonus;
            }
        }

        // 7. Carry Weight Modifier (เพิ่มน้ำหนักบรรทุกสูงสุด เช่น Megingjord)
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        public static class GetMaxCarryWeight_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null || __instance.GetInventory() == null) return;

                List<ItemDrop.ItemData> equipped = __instance.GetInventory().GetEquippedItems();
                if (equipped == null) return;

                float bonus = 0f;
                foreach (var item in equipped)
                {
                    int level = EnhancementManager.GetEnhancementLevel(item);
                    if (level > 0)
                    {
                        bonus += StatCalculator.GetCarryWeightBonus(item, level);
                    }
                }

                __result += bonus;
            }
        }

        // 8. Weapon Attack Stamina Reduction (ลด Stamina ต่อการโจมตีของอาวุธ)
        [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
        public static class GetAttackStamina_Patch
        {
            public static void Postfix(ItemDrop.ItemData ___m_weapon, ref float __result)
            {
                if (___m_weapon == null) return;

                int level = EnhancementManager.GetEnhancementLevel(___m_weapon);
                if (level > 0)
                {
                    float reduction = StatCalculator.GetAttackStaminaReduction(level);
                    __result *= (1f - reduction);
                }
            }
        }

        // 9. Weapon Attack Eitr Reduction (ลด Eitr ต่อการร่ายเวทของคทา)
        [HarmonyPatch(typeof(Attack), "GetAttackEitr")]
        public static class GetAttackEitr_Patch
        {
            public static void Postfix(ItemDrop.ItemData ___m_weapon, ref float __result)
            {
                if (___m_weapon == null) return;

                int level = EnhancementManager.GetEnhancementLevel(___m_weapon);
                if (level > 0)
                {
                    float reduction = StatCalculator.GetAttackEitrReduction(level);
                    __result *= (1f - reduction);
                }
            }
        }

        // 10. Backstab Bonus (เพิ่มความรุนแรงเมื่อลอบโจมตีข้างหลัง)
        [HarmonyPatch(typeof(Attack), "ModifyDamage")]
        public static class ModifyDamage_Patch
        {
            public static void Postfix(ItemDrop.ItemData ___m_weapon, HitData hitData)
            {
                if (___m_weapon == null || hitData == null) return;

                int level = EnhancementManager.GetEnhancementLevel(___m_weapon);
                if (level > 0 && hitData.m_backstabBonus > 1f)
                {
                    hitData.m_backstabBonus += level * ModConfig.WeaponBackstabBonusPerLevel.Value;
                }
            }
        }

        // 11. Timed Block / Parry Bonus Multiplier (เพิ่มโบนัสการปัดป้องตรงจังหวะ)
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyTimedBlockBonus))]
        public static class ModifyTimedBlockBonus_Patch
        {
            public static void Postfix(Character ___m_character, ref float timedBlockBonus)
            {
                if (___m_character is Humanoid humanoid)
                {
                    ItemDrop.ItemData shieldOrWeapon = humanoid.LeftItem ?? humanoid.RightItem;
                    if (shieldOrWeapon != null)
                    {
                        int level = EnhancementManager.GetEnhancementLevel(shieldOrWeapon);
                        if (level > 0)
                        {
                            timedBlockBonus += level * ModConfig.ShieldTimedBlockBonusPerLevel.Value;
                        }
                    }
                }
            }
        }
    }
}
