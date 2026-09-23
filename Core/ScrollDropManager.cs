using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class ScrollDropManager
    {
        /// <summary>
        /// Calculates and rolls for enhancement scroll drops upon monster death based on Drops.yml
        /// </summary>
        public static void TryAddScrollDrop(CharacterDrop dropComponent, Character character, List<KeyValuePair<GameObject, int>> dropsList)
        {
            if (!ModConfig.EnableMonsterDrops.Value) return;
            if (character == null || character.IsPlayer() || character.IsTamed()) return;

            bool isBoss = character.IsBoss();
            Vector3 pos = dropComponent != null ? dropComponent.transform.position : character.transform.position;
            Heightmap.Biome biome = Heightmap.FindBiome(pos);
            string name = character.gameObject.name.ToLower();

            // 1. Handle World Boss Drops
            if (isBoss)
            {
                var bossConfig = YamlConfigManager.Drops.BossDrops;
                int bossTier = bossConfig.GetBossTier(name);
                bool shouldDrop = bossConfig.GuaranteedDrop;

                if (!shouldDrop)
                {
                    float chance = YamlConfigManager.Drops.GetBiomeTierChance(biome, bossTier);
                    if (chance <= 0.0001f) chance = 50f; // Fallback if unconfigured in Biome
                    shouldDrop = UnityEngine.Random.Range(0f, 100f) < chance;
                }

                if (shouldDrop)
                {
                    int minDrop = Mathf.Max(1, bossConfig.MinAmount);
                    int maxDrop = Mathf.Max(minDrop, bossConfig.MaxAmount);
                    int amount = UnityEngine.Random.Range(minDrop, maxDrop + 1);

                    GameObject prefab = ScrollItemManager.GetScrollPrefab(bossTier);
                    if (prefab != null && amount > 0)
                    {
                        dropsList.Add(new KeyValuePair<GameObject, int>(prefab, amount));
                        Plugin.Log.LogInfo($"[ScrollDrop] Boss '{character.m_name}' dropped {prefab.name} x{amount} (Tier {bossTier}) at {pos}");
                    }
                    else if (prefab == null)
                    {
                        Plugin.Log.LogWarning($"[ScrollDrop] Boss drop succeeded for Tier {bossTier} but prefab was null!");
                    }
                }
                return;
            }

            // 2. Normal and Elite Monsters
            bool isElite = IsEliteMonster(name);
            float eliteBonus = isElite ? YamlConfigManager.Drops.EliteDrops.BonusMultiplier : 1f;

            // Star level multiplier (1 = normal, 2 = 1 star, 3 = 2 stars)
            int level = character.GetLevel();
            float starMult = 1f;
            if (level > 1)
            {
                starMult = 1f + (level - 1) * (YamlConfigManager.Drops.StarLevelMultiplier - 1f);
            }

            // Roll from highest tier down to lowest (Tier 4 -> Tier 1) according to BiomeDrops
            for (int tier = 4; tier >= 1; tier--)
            {
                float baseChance = YamlConfigManager.Drops.GetBiomeTierChance(biome, tier);
                if (baseChance <= 0.0001f) continue;

                float finalChance = baseChance * starMult * eliteBonus;
                float roll = UnityEngine.Random.Range(0f, 100f);

                if (roll < finalChance)
                {
                    int dropTier = tier;

                    // Elite monsters have a chance to upgrade to the next tier
                    if (isElite && dropTier < 4)
                    {
                        float upgradeRoll = UnityEngine.Random.Range(0f, 100f);
                        if (upgradeRoll < YamlConfigManager.Drops.EliteDrops.TierUpgradeChance)
                        {
                            dropTier = Mathf.Min(4, dropTier + 1);
                        }
                    }

                    GameObject prefab = ScrollItemManager.GetScrollPrefab(dropTier);
                    if (prefab != null)
                    {
                        dropsList.Add(new KeyValuePair<GameObject, int>(prefab, 1));
                        Plugin.Log.LogInfo($"[ScrollDrop] Monster '{character.m_name}' dropped {prefab.name} (Tier {dropTier}) at {pos} (Biome: {biome}, Roll: {roll:F1}/{finalChance:F1}%)");
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[ScrollDrop] Roll succeeded for Tier {dropTier} ({roll:F1}/{finalChance:F1}%) but prefab was null!");
                    }

                    // Maximum 1 scroll drop per monster
                    break;
                }
            }
        }

        /// <summary>
        /// Checks whether the monster qualifies as an elite/giant/miniboss
        /// </summary>
        private static bool IsEliteMonster(string lowerName)
        {
            return lowerName.Contains("troll")
                || lowerName.Contains("stonegolem")
                || lowerName.Contains("fenring")
                || lowerName.Contains("abomination")
                || lowerName.Contains("gjall")
                || lowerName.Contains("berserker")
                || lowerName.Contains("morgen")
                || lowerName.Contains("valkyrie")
                || lowerName.Contains("charred_warlock");
        }
    }
}
