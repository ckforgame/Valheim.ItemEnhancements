using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class ScrollDropManager
    {
        /// <summary>
        /// คำนวณและสุ่มดรอปม้วนคัมภีร์เมื่อมอนสเตอร์ตาย
        /// </summary>
        public static void TryAddScrollDrop(CharacterDrop dropComponent, Character character, List<KeyValuePair<GameObject, int>> dropsList)
        {
            if (!ModConfig.EnableMonsterDrops.Value) return;
            if (character == null || character.IsPlayer() || character.IsTamed()) return;

            bool isBoss = character.IsBoss();
            Vector3 pos = dropComponent.transform.position;
            Heightmap.Biome biome = Heightmap.FindBiome(pos);

            // 1. กำหนด Tier ของคัมภีร์ที่จะดรอป
            int tier = DetermineScrollTier(character, isBoss, biome);

            // 2. คำนวณโอกาสดรอปและจำนวน
            int amount = 0;
            if (isBoss && ModConfig.BossGuaranteedDrop.Value)
            {
                // บอสการันตีการดรอป
                int minDrop = Mathf.Max(1, ModConfig.BossMinDrop.Value);
                int maxDrop = Mathf.Max(minDrop, ModConfig.BossMaxDrop.Value);
                amount = UnityEngine.Random.Range(minDrop, maxDrop + 1);
            }
            else
            {
                float baseChance = ModConfig.GetTierDropChance(tier);
                
                // ตัวคูณระดับดาวของมอนสเตอร์
                int level = character.GetLevel(); // 1 = ปกติ, 2 = 1 ดาว, 3 = 2 ดาว
                float starMult = 1f;
                if (level > 1)
                {
                    starMult = 1f + (level - 1) * (ModConfig.StarLevelMultiplier.Value - 1f);
                }

                float finalChance = baseChance * starMult;
                float roll = UnityEngine.Random.Range(0f, 100f);

                if (roll < finalChance)
                {
                    amount = 1;
                }
            }

            if (amount > 0)
            {
                GameObject prefab = ScrollItemManager.GetScrollPrefab(tier);
                if (prefab != null)
                {
                    dropsList.Add(new KeyValuePair<GameObject, int>(prefab, amount));
                }
            }
        }

        /// <summary>
        /// ตัดสินระดับคัมภีร์ (Tier 1-4) ตามประเภทมอนสเตอร์, บอส, และ Biome
        /// </summary>
        private static int DetermineScrollTier(Character character, bool isBoss, Heightmap.Biome biome)
        {
            string name = character.gameObject.name.ToLower();

            // ตรวจสอบบอสหลัก
            if (isBoss)
            {
                if (name.Contains("eikthyr")) return 1;
                if (name.Contains("gd_king") || name.Contains("elder")) return 2;
                if (name.Contains("bonemass")) return 2;
                if (name.Contains("dragon") || name.Contains("moder")) return 3;
                if (name.Contains("goblinking") || name.Contains("yagluth")) return 3;
                if (name.Contains("seekerqueen") || name.Contains("queen")) return 4;
                if (name.Contains("fader")) return 4;
            }

            // มอนสเตอร์ชั้นสูง/ยักษ์ใหญ่พิเศษ (Elite)
            if (name.Contains("troll")) return UnityEngine.Random.value < 0.25f ? 2 : 1;
            if (name.Contains("stonegolem") || name.Contains("fenring")) return UnityEngine.Random.value < 0.25f ? 3 : 2;
            if (name.Contains("gjall")) return UnityEngine.Random.value < 0.25f ? 4 : 3;

            // คัดตาม Biome
            switch (biome)
            {
                case Heightmap.Biome.Meadows:
                    return 1;

                case Heightmap.Biome.BlackForest:
                    return 1;

                case Heightmap.Biome.Swamp:
                    return 2;

                case Heightmap.Biome.Mountain:
                    return 2;

                case Heightmap.Biome.Plains:
                    return 3;

                case Heightmap.Biome.Mistlands:
                    return UnityEngine.Random.value < 0.20f ? 4 : 3;

                case Heightmap.Biome.AshLands:
                case Heightmap.Biome.DeepNorth:
                    return 4;

                case Heightmap.Biome.Ocean:
                    return 2;

                default:
                    return 1;
            }
        }
    }
}
