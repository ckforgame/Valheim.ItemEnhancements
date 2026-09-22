using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class ScrollDropManager
    {
        /// <summary>
        /// คำนวณและสุ่มดรอปม้วนคัมภีร์เมื่อมอนสเตอร์ตาย โดยอ้างอิงจาก Drops.yml
        /// </summary>
        public static void TryAddScrollDrop(CharacterDrop dropComponent, Character character, List<KeyValuePair<GameObject, int>> dropsList)
        {
            if (!ModConfig.EnableMonsterDrops.Value) return;
            if (character == null || character.IsPlayer() || character.IsTamed()) return;

            bool isBoss = character.IsBoss();
            Vector3 pos = dropComponent.transform.position;
            Heightmap.Biome biome = Heightmap.FindBiome(pos);
            string name = character.gameObject.name.ToLower();

            // 1. จัดการการดรอปของบอสโลก (World Bosses)
            if (isBoss)
            {
                var bossConfig = YamlConfigManager.Drops.BossDrops;
                int bossTier = bossConfig.GetBossTier(name);
                bool shouldDrop = bossConfig.GuaranteedDrop;

                if (!shouldDrop)
                {
                    float chance = YamlConfigManager.Drops.GetBiomeTierChance(biome, bossTier);
                    if (chance <= 0.0001f) chance = 50f; // Fallback หากไม่ได้ตั้งค่าไว้ใน Biome
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
                    }
                }
                return;
            }

            // 2. มอนสเตอร์ทั่วไป และ มอนสเตอร์ระดับสูง (Elite Monsters)
            bool isElite = IsEliteMonster(name);
            float eliteBonus = isElite ? YamlConfigManager.Drops.EliteDrops.BonusMultiplier : 1f;

            // ตัวคูณระดับดาว (1 = ปกติ, 2 = 1 ดาว, 3 = 2 ดาว)
            int level = character.GetLevel();
            float starMult = 1f;
            if (level > 1)
            {
                starMult = 1f + (level - 1) * (YamlConfigManager.Drops.StarLevelMultiplier - 1f);
            }

            // สุ่มตรวจโอกาสดรอปจาก Tier สูงสุดลงไปต่ำสุด (Tier 4 -> Tier 1) ตามค่า % ใน BiomeDrops
            for (int tier = 4; tier >= 1; tier--)
            {
                float baseChance = YamlConfigManager.Drops.GetBiomeTierChance(biome, tier);
                if (baseChance <= 0.0001f) continue;

                float finalChance = baseChance * starMult * eliteBonus;
                float roll = UnityEngine.Random.Range(0f, 100f);

                if (roll < finalChance)
                {
                    int dropTier = tier;

                    // มอนสเตอร์ระดับสูงมีโอกาสอัปเกรดเป็น Tier ถัดไป
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
                    }

                    // ดรอป 1 ม้วนต่อมอนสเตอร์ 1 ตัว
                    break;
                }
            }
        }

        /// <summary>
        /// ตรวจสอบว่ามอนสเตอร์เข้าข่ายมอนสเตอร์ชั้นสูง/ยักษ์ใหญ่/มินิบอส หรือไม่
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
