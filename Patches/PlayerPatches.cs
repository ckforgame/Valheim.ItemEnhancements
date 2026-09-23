using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.Core;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class PlayerPatches
    {
        // 1. Movement Speed Modifier (Reduces movement penalty of heavy armor and shields)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
        public static class GetEquipmentMovementModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result += PlayerBonusCache.MovementBonus;
            }
        }

        // 2. Dodge Stamina Modifier (Reduces dodge roll stamina cost)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
        public static class GetEquipmentDodgeStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result -= PlayerBonusCache.DodgeStaminaReduction;
            }
        }

        // 3. Run Stamina Modifier (Reduces sprinting stamina cost)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentRunStaminaModifier))]
        public static class GetEquipmentRunStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result -= PlayerBonusCache.RunStaminaReduction;
            }
        }

        // 4. Jump Stamina Modifier (Reduces jump stamina cost)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentJumpStaminaModifier))]
        public static class GetEquipmentJumpStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result -= PlayerBonusCache.JumpStaminaReduction;
            }
        }

        // 5. Block Stamina Modifier (Reduces block stamina cost)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentBlockStaminaModifier))]
        public static class GetEquipmentBlockStaminaModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result -= PlayerBonusCache.BlockStaminaReduction;
            }
        }

        // 6. Eitr Regen Modifier (Increases magical energy regeneration)
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        public static class GetEquipmentEitrRegenModifier_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result += PlayerBonusCache.EitrRegenBonus;
            }
        }

        // 7. Carry Weight Modifier (Increases maximum carry weight, e.g. Megingjord)
        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        public static class GetMaxCarryWeight_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                __result += PlayerBonusCache.CarryWeightBonus;
            }
        }

        // 8. Weapon Attack Stamina Reduction (Reduces stamina cost per weapon attack)
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

        // 9. Weapon Attack Eitr Reduction (Reduces eitr cost per staff spellcast)
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        public static class GetAttackEitr_Patch
        {
            public static void Postfix(ItemDrop.ItemData weapon, ref float __result)
            {
                if (weapon == null) return;

                int level = EnhancementManager.GetEnhancementLevel(weapon);
                if (level > 0)
                {
                    float reduction = StatCalculator.GetAttackEitrReduction(level);
                    __result *= (1f - reduction);
                }
            }
        }

        // 10. Backstab Bonus (Increases sneak attack multiplier)
        [HarmonyPatch(typeof(Attack), "ModifyDamage")]
        public static class ModifyDamage_Patch
        {
            public static void Postfix(ItemDrop.ItemData ___m_weapon, HitData hitData)
            {
                if (___m_weapon == null || hitData == null) return;

                int level = EnhancementManager.GetEnhancementLevel(___m_weapon);
                if (level > 0 && hitData.m_backstabBonus > 1f)
                {
                    hitData.m_backstabBonus += StatCalculator.GetBackstabBonus(level);
                }
            }
        }

        // 11. Timed Block / Parry Bonus Multiplier (Increases parry window bonus multiplier)
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyTimedBlockBonus))]
        public static class ModifyTimedBlockBonus_Patch
        {
            private static readonly MethodInfo _getCurrentBlockerMethod =
                AccessTools.Method(typeof(Humanoid), "GetCurrentBlocker");

            public static void Postfix(Character ___m_character, ref float timedBlockBonus)
            {
                if (___m_character is Humanoid humanoid)
                {
                    ItemDrop.ItemData blocker = _getCurrentBlockerMethod?.Invoke(humanoid, null) as ItemDrop.ItemData;
                    if (blocker == null)
                    {
                        blocker = humanoid.LeftItem ?? humanoid.RightItem;
                    }

                    if (blocker != null)
                    {
                        int level = EnhancementManager.GetEnhancementLevel(blocker);
                        if (level > 0)
                        {
                            timedBlockBonus += StatCalculator.GetTimedBlockBonus(level);
                        }
                    }
                }
            }
        }

        #region Cheat Abilities Patches (Capacity, Regens, Healing)

        // 12. Cheat Ability: Max Health Capacity
        [HarmonyPatch(typeof(Player), nameof(Player.SetMaxHealth), typeof(float), typeof(bool))]
        public static class Player_SetMaxHealth_Patch
        {
            public static void Prefix(Player __instance, ref float health)
            {
                if (!StatCalculator.IsCheatEnabled || __instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                health += PlayerBonusCache.CheatMaxHealth;
            }
        }

        // 13. Cheat Ability: Max Stamina Capacity
        [HarmonyPatch(typeof(Player), nameof(Player.SetMaxStamina), typeof(float), typeof(bool))]
        public static class Player_SetMaxStamina_Patch
        {
            public static void Prefix(Player __instance, ref float stamina)
            {
                if (!StatCalculator.IsCheatEnabled || __instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                stamina += PlayerBonusCache.CheatMaxStamina;
            }
        }

        // 14. Cheat Ability: Max Eitr Capacity
        [HarmonyPatch(typeof(Player), "SetMaxEitr", typeof(float), typeof(bool))]
        public static class Player_SetMaxEitr_Patch
        {
            public static void Prefix(Player __instance, ref float eitr)
            {
                if (!StatCalculator.IsCheatEnabled || __instance != Player.m_localPlayer) return;
                PlayerBonusCache.EnsureFresh(__instance);
                eitr += PlayerBonusCache.CheatMaxEitr;
            }
        }

        // 15. Equipment cache invalidation & Cheat Food refresh on equip/unequip
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Humanoid_EquipItem_Patch
        {
            private static readonly MethodInfo _updateFoodMethod =
                AccessTools.Method(typeof(Player), "UpdateFood", new Type[] { typeof(float), typeof(bool) });

            public static void Postfix(Humanoid __instance)
            {
                if (__instance is Player player && player == Player.m_localPlayer)
                {
                    PlayerBonusCache.MarkDirty();
                    if (StatCalculator.IsCheatEnabled)
                    {
                        _updateFoodMethod?.Invoke(player, new object[] { 0f, true });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class Humanoid_UnequipItem_Patch
        {
            private static readonly MethodInfo _updateFoodMethod =
                AccessTools.Method(typeof(Player), "UpdateFood", new Type[] { typeof(float), typeof(bool) });

            public static void Postfix(Humanoid __instance)
            {
                if (__instance is Player player && player == Player.m_localPlayer)
                {
                    PlayerBonusCache.MarkDirty();
                    if (StatCalculator.IsCheatEnabled)
                    {
                        _updateFoodMethod?.Invoke(player, new object[] { 0f, true });
                    }
                }
            }
        }

        // 16. Invalidate cache on Player spawn
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Player_OnSpawned_Patch
        {
            public static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    PlayerBonusCache.MarkDirty();
                }
            }
        }

        // 17. Cheat Ability: Health Regen Multiplier
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
        public static class SEMan_ModifyHealthRegen_Patch
        {
            public static void Postfix(Character ___m_character, ref float regenMultiplier)
            {
                if (!StatCalculator.IsCheatEnabled) return;
                if (___m_character == Player.m_localPlayer)
                {
                    PlayerBonusCache.EnsureFresh(Player.m_localPlayer);
                    regenMultiplier += PlayerBonusCache.CheatHealthRegen;
                }
            }
        }

        // 18. Cheat Ability: Stamina Regen Multiplier
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
        public static class SEMan_ModifyStaminaRegen_Patch
        {
            public static void Postfix(Character ___m_character, ref float staminaMultiplier)
            {
                if (!StatCalculator.IsCheatEnabled) return;
                if (___m_character == Player.m_localPlayer)
                {
                    PlayerBonusCache.EnsureFresh(Player.m_localPlayer);
                    staminaMultiplier += PlayerBonusCache.CheatStaminaRegen;
                }
            }
        }

        // 19. Cheat Ability: Eitr Regen Multiplier
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
        public static class SEMan_ModifyEitrRegen_Patch
        {
            public static void Postfix(Character ___m_character, ref float eitrMultiplier)
            {
                if (!StatCalculator.IsCheatEnabled) return;
                if (___m_character == Player.m_localPlayer)
                {
                    PlayerBonusCache.EnsureFresh(Player.m_localPlayer);
                    eitrMultiplier += PlayerBonusCache.CheatEitrRegen;
                }
            }
        }

        // 20. Cheat Ability: Healing Received Multiplier
        [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
        public static class Character_Heal_Patch
        {
            public static void Prefix(Character __instance, ref float hp)
            {
                if (!StatCalculator.IsCheatEnabled || hp <= 0f) return;
                if (__instance == Player.m_localPlayer)
                {
                    PlayerBonusCache.EnsureFresh(Player.m_localPlayer);
                    hp *= (1f + PlayerBonusCache.CheatHealingBonus);
                }
            }
        }

        // 20. Prevent player actions while Enhancement UI is open
        [HarmonyPatch(typeof(Player), "TakeInput")]
        public static class Player_TakeInput_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (EnhancementGui.IsOpen)
                {
                    __result = false;
                }
            }
        }

        #endregion
    }
}
