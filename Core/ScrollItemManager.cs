using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;

namespace Valheim.ItemEnhancements.Core
{
    public static class ScrollItemManager
    {
        public const string PrefabTier1 = "ScrollEnhance_Tier1";
        public const string PrefabTier2 = "ScrollEnhance_Tier2";
        public const string PrefabTier3 = "ScrollEnhance_Tier3";
        public const string PrefabTier4 = "ScrollEnhance_Tier4";

        private static readonly Dictionary<int, GameObject> _scrollPrefabs = new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, Sprite> _scrollSprites = new Dictionary<int, Sprite>();

        public static bool IsCloningCustomPrefab { get; private set; }
        private static GameObject _prefabContainer;

        public static GameObject GetPrefabContainer()
        {
            if (_prefabContainer == null)
            {
                _prefabContainer = new GameObject("_ItemEnhancements_PrefabContainer");
                _prefabContainer.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(_prefabContainer);
            }
            return _prefabContainer;
        }

        /// <summary>
        /// Cleans up orphaned or corrupted scroll instances in the scene lacking valid ZDO/ZNetView
        /// </summary>
        public static void CleanupOrphanScrolls()
        {
            try
            {
                var items = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
                if (items == null) return;

                foreach (var item in items)
                {
                    if (item == null || item.gameObject == null) continue;

                    string name = item.gameObject.name;
                    if (!name.StartsWith(PrefabTier1) && !name.StartsWith(PrefabTier2) && 
                        !name.StartsWith(PrefabTier3) && !name.StartsWith(PrefabTier4))
                    {
                        continue;
                    }

                    // Preserve valid prefab templates in container
                    if (_prefabContainer != null && item.transform.IsChildOf(_prefabContainer.transform))
                    {
                        continue;
                    }

                    // Destroy corrupted ground instance lacking ZNetView or ZDO to prevent NRE
                    ZNetView znv = item.GetComponent<ZNetView>();
                    if (znv == null || znv.GetZDO() == null)
                    {
                        Plugin.Log.LogWarning($"[ScrollItemManager] Cleaning up orphaned/corrupted scroll instance '{name}' from scene.");
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[ScrollItemManager] Error during CleanupOrphanScrolls: {ex.Message}");
            }
        }

        public static GameObject GetScrollPrefab(int tier)
        {
            if (_scrollPrefabs.TryGetValue(tier, out GameObject prefab) && prefab != null)
            {
                if (!prefab.activeSelf) prefab.SetActive(true);
                return prefab;
            }

            string prefabName = GetScrollPrefabName(tier);
            if (ObjectDB.instance != null)
            {
                GameObject existing = ObjectDB.instance.GetItemPrefab(prefabName);
                if (existing != null)
                {
                    if (!existing.activeSelf) existing.SetActive(true);
                    _scrollPrefabs[tier] = existing;
                    return existing;
                }
            }

            if (ZNetScene.instance != null)
            {
                GameObject existing = ZNetScene.instance.GetPrefab(prefabName);
                if (existing != null)
                {
                    if (!existing.activeSelf) existing.SetActive(true);
                    _scrollPrefabs[tier] = existing;
                    return existing;
                }
            }

            return null;
        }

        public static string GetScrollPrefabName(int tier)
        {
            switch (tier)
            {
                case 1: return PrefabTier1;
                case 2: return PrefabTier2;
                case 3: return PrefabTier3;
                case 4: return PrefabTier4;
                default: return PrefabTier1;
            }
        }

        public static string GetScrollDisplayName(int tier)
        {
            switch (tier)
            {
                case 1: return "Enhancement Scroll Tier 1 (Basic +1 to +5)";
                case 2: return "Enhancement Scroll Tier 2 (Refined +6 to +10)";
                case 3: return "Enhancement Scroll Tier 3 (Exquisite +11 to +15)";
                case 4: return "Enhancement Scroll Tier 4 (Divine +16 to +20)";
                default: return $"Enhancement Scroll Tier {tier}";
            }
        }

        public static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 1: return new Color(0.29f, 0.87f, 0.50f, 1f); // #4ade80 Green
                case 2: return new Color(0.13f, 0.83f, 0.93f, 1f); // #22d3ee Cyan
                case 3: return new Color(0.66f, 0.33f, 0.97f, 1f); // #a855f7 Purple
                case 4: return new Color(0.96f, 0.62f, 0.04f, 1f); // #f59e0b Gold
                default: return Color.white;
            }
        }

        /// <summary>
        /// Registers scroll item prefabs into ObjectDB when ready
        /// </summary>
        public static void InitCustomItems(ObjectDB objectDb)
        {
            if (objectDb == null || objectDb.m_items == null || objectDb.m_items.Count == 0) return;

            // Look up base item with 3D physics and ItemDrop components (e.g. Amber, Ruby, or Coins)
            GameObject baseItem = objectDb.GetItemPrefab("Amber") ?? objectDb.GetItemPrefab("Ruby") ?? objectDb.GetItemPrefab("Coins");
            if (baseItem == null)
            {
                Plugin.Log.LogWarning("[ScrollItemManager] Could not find base prefab (Amber/Ruby/Coins) to clone scrolls.");
                return;
            }

            var container = GetPrefabContainer();

            for (int tier = 1; tier <= 4; tier++)
            {
                string prefabName = GetScrollPrefabName(tier);
                GameObject existing = objectDb.GetItemPrefab(prefabName);

                if (existing != null)
                {
                    if (!existing.activeSelf) existing.SetActive(true);
                    _scrollPrefabs[tier] = existing;
                    continue;
                }

                if (_scrollPrefabs.TryGetValue(tier, out GameObject cached) && cached != null)
                {
                    if (!cached.activeSelf) cached.SetActive(true);
                    if (!objectDb.m_items.Contains(cached))
                    {
                        objectDb.m_items.Add(cached);
                        int h = prefabName.GetStableHashCode();
                        try
                        {
                            var itemByHash = HarmonyLib.AccessTools.FieldRefAccess<ObjectDB, Dictionary<int, GameObject>>("m_itemByHash")(objectDb);
                            if (itemByHash != null) itemByHash[h] = cached;
                        }
                        catch { }
                    }
                    continue;
                }

                GameObject scrollObj;
                IsCloningCustomPrefab = true;
                try
                {
                    scrollObj = UnityEngine.Object.Instantiate(baseItem, container.transform);
                    scrollObj.name = prefabName;
                    // Ensure activeSelf is true so Object.Instantiate creates active ground objects
                    scrollObj.SetActive(true);
                }
                finally
                {
                    IsCloningCustomPrefab = false;
                }

                ItemDrop itemDrop = scrollObj.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    itemDrop.m_autoPickup = true;
                    itemDrop.m_itemData.m_dropPrefab = scrollObj;

                    ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
                    shared.m_name = $"$item_scroll_enhance_t{tier}";
                    shared.m_description = $"$item_scroll_enhance_t{tier}_desc";
                    shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                    shared.m_maxStackSize = 50;
                    shared.m_weight = 0.1f;

                    // Tier scroll icon sprite
                    Sprite icon = GetOrCreateScrollSprite(tier);
                    if (icon != null)
                    {
                        shared.m_icons = new Sprite[] { icon };
                    }

                    // Tint ground 3D model
                    MeshRenderer renderer = scrollObj.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material = UnityEngine.Object.Instantiate(renderer.material);
                        renderer.material.color = GetTierColor(tier);
                    }
                }

                // Register into ObjectDB
                objectDb.m_items.Add(scrollObj);
                int hash = prefabName.GetStableHashCode();
                try
                {
                    var itemByHash = HarmonyLib.AccessTools.FieldRefAccess<ObjectDB, Dictionary<int, GameObject>>("m_itemByHash")(objectDb);
                    if (itemByHash != null) itemByHash[hash] = scrollObj;
                }
                catch
                {
                    // Fallback
                }

                _scrollPrefabs[tier] = scrollObj;
            }

            RegisterLocalization();
            RegisterFusionRecipes(objectDb);

            if (ZNetScene.instance != null)
            {
                RegisterZNetScenePrefabs(ZNetScene.instance);
                CleanupOrphanScrolls();
            }

            Plugin.Log.LogInfo("[ScrollItemManager] Successfully registered 4 Tiers of Enhancement Scrolls into ObjectDB!");
        }

        /// <summary>
        /// Registers prefabs into ZNetScene for 3D world drops and multiplayer synchronization
        /// </summary>
        public static void RegisterZNetScenePrefabs(ZNetScene znetScene)
        {
            if (znetScene == null || znetScene.m_prefabs == null) return;

            Dictionary<int, GameObject> namedPrefabs = null;
            try
            {
                namedPrefabs = HarmonyLib.AccessTools.FieldRefAccess<ZNetScene, Dictionary<int, GameObject>>("m_namedPrefabs")(znetScene);
            }
            catch
            {
                // Fallback
            }

            for (int tier = 1; tier <= 4; tier++)
            {
                GameObject scrollObj = GetScrollPrefab(tier);
                if (scrollObj != null)
                {
                    int hash = scrollObj.name.GetStableHashCode();
                    if (!znetScene.m_prefabs.Contains(scrollObj))
                    {
                        znetScene.m_prefabs.Add(scrollObj);
                    }
                    if (namedPrefabs != null)
                    {
                        namedPrefabs[hash] = scrollObj;
                    }
                }
            }
        }

        private static readonly Action<Localization, string, string> _addWord =
            HarmonyLib.AccessTools.MethodDelegate<Action<Localization, string, string>>(
                HarmonyLib.AccessTools.Method(typeof(Localization), "AddWord", new Type[] { typeof(string), typeof(string) })
            );

        public static void AddWord(string key, string value)
        {
            if (Localization.instance == null) return;
            try
            {
                _addWord?.Invoke(Localization.instance, key, value);
            }
            catch
            {
                try
                {
                    var translations = HarmonyLib.AccessTools.FieldRefAccess<Localization, Dictionary<string, string>>("m_translations")(Localization.instance);
                    if (translations != null) translations[key] = value;
                }
                catch
                {
                    // Fallback
                }
            }
        }

        /// <summary>
        /// Registers localization words for enhancement scrolls. English is the primary language.
        /// </summary>
        public static void RegisterLocalization()
        {
            if (Localization.instance == null) return;

            string language = Localization.instance.GetSelectedLanguage();
            if (string.Equals(language, "Thai", StringComparison.OrdinalIgnoreCase))
            {
                AddWord("item_scroll_enhance_t1", "ใบตีบวกระดับ 1 (พื้นฐาน)");
                AddWord("item_scroll_enhance_t1_desc", "ม้วนคัมภีร์เวทมนตร์โบราณ ใช้สำหรับตีบวกอุปกรณ์ระดับ +1 ถึง +5\nดรอปจากมอนสเตอร์ในทุ่งหญ้าและป่าดำ");

                AddWord("item_scroll_enhance_t2", "ใบตีบวกระดับ 2 (ขัดเกลา)");
                AddWord("item_scroll_enhance_t2_desc", "ม้วนคัมภีร์เวทมนตร์โบราณ ใช้สำหรับตีบวกอุปกรณ์ระดับ +6 ถึง +10\nดรอปจากมอนสเตอร์ในหนองน้ำและภูเขา");

                AddWord("item_scroll_enhance_t3", "ใบตีบวกระดับ 3 (ประณีต)");
                AddWord("item_scroll_enhance_t3_desc", "ม้วนคัมภีร์เวทมนตร์โบราณ ใช้สำหรับตีบวกอุปกรณ์ระดับ +11 ถึง +15\nดรอปจากมอนสเตอร์ในที่ราบและม่านหมอก");

                AddWord("item_scroll_enhance_t4", "ใบตีบวกระดับ 4 (เทวะ)");
                AddWord("item_scroll_enhance_t4_desc", "ม้วนคัมภีร์เวทมนตร์โบราณ ใช้สำหรับตีบวกอุปกรณ์ระดับ +16 ถึง +20\nดรอปจากมอนสเตอร์ในแดนเถ้าถ่านและบอสโลก");
            }
            else
            {
                AddWord("item_scroll_enhance_t1", "Enhancement Scroll Tier 1 (Basic)");
                AddWord("item_scroll_enhance_t1_desc", "An ancient magical scroll used for enhancing equipment from +1 to +5.\nDrops from monsters in the Meadows and Black Forest.");

                AddWord("item_scroll_enhance_t2", "Enhancement Scroll Tier 2 (Refined)");
                AddWord("item_scroll_enhance_t2_desc", "An ancient magical scroll used for enhancing equipment from +6 to +10.\nDrops from monsters in the Swamp and Mountain.");

                AddWord("item_scroll_enhance_t3", "Enhancement Scroll Tier 3 (Exquisite)");
                AddWord("item_scroll_enhance_t3_desc", "An ancient magical scroll used for enhancing equipment from +11 to +15.\nDrops from monsters in the Plains and Mistlands.");

                AddWord("item_scroll_enhance_t4", "Enhancement Scroll Tier 4 (Divine)");
                AddWord("item_scroll_enhance_t4_desc", "An ancient magical scroll used for enhancing equipment from +16 to +20.\nDrops from monsters in the Ashlands and World Bosses.");
            }
        }

        /// <summary>
        /// Registers fusion recipes at workbench: fuse 3 lower-tier scrolls into 1 next-tier scroll
        /// </summary>
        private static void RegisterFusionRecipes(ObjectDB objectDb)
        {
            if (objectDb?.m_recipes == null) return;

            for (int tier = 1; tier <= 3; tier++)
            {
                int nextTier = tier + 1;
                string recipeName = $"Recipe_Fusion_ScrollTier{nextTier}";

                // Check if recipe already exists
                if (objectDb.m_recipes.Exists(r => r != null && r.name == recipeName)) continue;

                GameObject sourcePrefab = GetScrollPrefab(tier);
                GameObject targetPrefab = GetScrollPrefab(nextTier);

                if (sourcePrefab == null || targetPrefab == null) continue;

                ItemDrop targetItemDrop = targetPrefab.GetComponent<ItemDrop>();
                ItemDrop sourceItemDrop = sourcePrefab.GetComponent<ItemDrop>();

                if (targetItemDrop == null || sourceItemDrop == null) continue;

                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = recipeName;
                recipe.m_item = targetItemDrop;
                recipe.m_amount = 1;
                recipe.m_enabled = true;
                recipe.m_minStationLevel = 1;

                CraftingStation workbench = objectDb.GetItemPrefab("piece_workbench")?.GetComponent<CraftingStation>();
                recipe.m_craftingStation = workbench;

                recipe.m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement
                    {
                        m_resItem = sourceItemDrop,
                        m_amount = 3,
                        m_recover = false
                    }
                };

                objectDb.m_recipes.Add(recipe);
            }
        }

        /// <summary>
        /// Generates vintage parchment scroll sprite with tier-specific wax seal
        /// </summary>
        public static Sprite GetOrCreateScrollSprite(int tier)
        {
            if (_scrollSprites.TryGetValue(tier, out Sprite s) && s != null)
            {
                return s;
            }

            int width = 64;
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color transparent = new Color(0, 0, 0, 0);
            Color parchmentBody = new Color(0.95f, 0.90f, 0.78f, 1f); // Aged parchment tone
            Color parchmentEdge = new Color(0.76f, 0.65f, 0.48f, 1f); // Border shadow
            Color parchmentShade = new Color(0.85f, 0.77f, 0.62f, 1f);
            Color woodCap = new Color(0.40f, 0.22f, 0.10f, 1f);       // Wooden roller caps
            Color sealColor = GetTierColor(tier);                     // Tier wax seal color
            Color ribbonColor = new Color(sealColor.r * 0.8f, sealColor.g * 0.8f, sealColor.b * 0.8f, 1f);

            // Clear transparent background
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Draw scroll parchment body (vertical cylinder)
            int minX = 14;
            int maxX = 49;
            int minY = 10;
            int maxY = 53;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // Rounded corner edges
                    if ((x == minX || x == maxX) && (y == minY || y == maxY)) continue;

                    // Gradient shading for cylinder volume
                    float horizontalFactor = (float)(x - minX) / (maxX - minX);
                    Color pixel = Color.Lerp(parchmentShade, parchmentBody, Mathf.Sin(horizontalFactor * Mathf.PI));

                    if (x <= minX + 2 || x >= maxX - 2 || y <= minY + 1 || y >= maxY - 1)
                    {
                        pixel = Color.Lerp(pixel, parchmentEdge, 0.65f);
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            // Draw wooden rollers (Top & Bottom Wooden Rollers)
            for (int x = minX - 4; x <= maxX + 4; x++)
            {
                for (int y = maxY; y <= maxY + 3; y++)
                {
                    tex.SetPixel(x, y, woodCap);
                }
                for (int y = minY - 3; y <= minY; y++)
                {
                    tex.SetPixel(x, y, woodCap);
                }
            }

            // Draw middle binding ribbon
            int midY = 32;
            for (int y = midY - 3; y <= midY + 3; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, y, ribbonColor);
                }
            }

            // Draw wax seal (round seal in center)
            int centerX = 32;
            int centerY = 32;
            int radius = 9;

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (dist <= radius)
                    {
                        Color c = sealColor;
                        if (dist > radius - 1.5f)
                        {
                            c = Color.Lerp(sealColor, Color.black, 0.35f);
                        }
                        else if (dist < 3.5f)
                        {
                            // Center highlight glow
                            c = Color.Lerp(sealColor, Color.white, 0.6f);
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            _scrollSprites[tier] = sprite;
            return sprite;
        }
    }
}
