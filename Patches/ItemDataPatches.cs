using System;
using HarmonyLib;
using Valheim.ItemEnhancements.Core;

namespace Valheim.ItemEnhancements.Patches
{
    public static class ItemDataPatches
    {
        // 1. Damage scaling
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
        public static class GetDamage_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    StatCalculator.ApplyDamageBonus(__instance, level, ref __result);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), new Type[0])]
        public static class GetDamageSimple_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    StatCalculator.ApplyDamageBonus(__instance, level, ref __result);
                }
            }
        }

        // 2. Armor scaling
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        public static class GetArmor_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetArmorBonus(__instance, level, __result);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), new Type[0])]
        public static class GetArmorSimple_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetArmorBonus(__instance, level, __result);
                }
            }
        }

        // 3. Block Power scaling
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(int), typeof(float))]
        public static class GetBlockPower_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetBlockPowerBonus(__instance, level, __result);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
        public static class GetBlockPowerSimple_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetBlockPowerBonus(__instance, level, __result);
                }
            }
        }

        // 4. Deflection Force (Parry) scaling
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDeflectionForce), typeof(int))]
        public static class GetDeflectionForce_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetDeflectionBonus(__instance, level, __result);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDeflectionForce), new Type[0])]
        public static class GetDeflectionForceSimple_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetDeflectionBonus(__instance, level, __result);
                }
            }
        }

        // 5. Max Durability scaling
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
        public static class GetMaxDurability_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetDurabilityBonus(__instance, level, __result);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), new Type[0])]
        public static class GetMaxDurabilitySimple_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result += StatCalculator.GetDurabilityBonus(__instance, level, __result);
                }
            }
        }

        // 6. Weight Reduction
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        public static class GetWeight_Patch
        {
            public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                int level = EnhancementManager.GetEnhancementLevel(__instance);
                if (level > 0)
                {
                    __result *= StatCalculator.GetWeightMultiplier(level);
                }
            }
        }

        // 7. Tooltip
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
            typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
        public static class GetTooltip_Patch
        {
            public static void Postfix(ItemDrop.ItemData item, ref string __result)
            {
                if (item == null) return;
                int level = EnhancementManager.GetEnhancementLevel(item);
                if (level > 0)
                {
                    __result += EnhancementManager.BuildEnhancementTooltip(item, level);
                }
            }
        }

        // 8. Hover Text on dropped items in the world
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
        public static class ItemDropHoverText_Patch
        {
            public static void Postfix(ItemDrop __instance, ref string __result)
            {
                if (__instance?.m_itemData == null || string.IsNullOrEmpty(__result)) return;

                int level = EnhancementManager.GetEnhancementLevel(__instance.m_itemData);
                if (level > 0)
                {
                    string hex = EnhancementManager.GetTierHex(level);
                    __result = $"{__result} <color={hex}>+{level}</color>";
                }
            }
        }
    }
}
