using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Timers;
using BepInEx;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Valheim.ItemEnhancements.Configuration
{
    public static class YamlConfigManager
    {
        public static string ConfigDirectory { get; private set; }

        public static SuccessConfigData Success { get; private set; } = new SuccessConfigData();
        public static ItemConfigData Item { get; private set; } = new ItemConfigData();
        public static ArmorConfigData Armor { get; private set; } = new ArmorConfigData();
        public static CheatConfigData Cheat { get; private set; } = new CheatConfigData();
        public static DropsConfigData Drops { get; private set; } = new DropsConfigData();


        private static FileSystemWatcher _watcher;
        private static System.Threading.Timer _reloadDebounceTimer;
        private static readonly IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public static event Action OnConfigReloaded;

        public static void Initialize(string customConfigDir = null)
        {
            if (!string.IsNullOrEmpty(customConfigDir))
            {
                ConfigDirectory = customConfigDir;
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(Paths.ConfigPath))
                    {
                        ConfigDirectory = Path.Combine(Paths.ConfigPath, "ckforgame.ItemEnhancements");
                    }
                }
                catch
                {
                    // ignored
                }

                if (string.IsNullOrEmpty(ConfigDirectory))
                {
                    ConfigDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "Yaml");
                }
            }

            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            EnsureDefaultFiles();
            LoadAll();
            SetupWatcher();
        }

        public static void LoadAll()
        {
            Success = LoadWithOverride<SuccessConfigData>("Success.yml", "Success.override.yml");
            Item = LoadWithOverride<ItemConfigData>("Item.yml", "Item.override.yml");
            Armor = LoadWithOverride<ArmorConfigData>("Armor.yml", "Armor.override.yml");
            Cheat = LoadWithOverride<CheatConfigData>("Cheat.yml", "Cheat.override.yml");
            Drops = LoadWithOverride<DropsConfigData>("Drops.yml", "Drops.override.yml");


            RebuildCumulativeCache();

            LogInfo($"[Valheim.ItemEnhancements] YAML Configurations loaded successfully from '{ConfigDirectory}'.");
            OnConfigReloaded?.Invoke();
        }

        private static readonly Dictionary<string, float[]> _cumulativeCache = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

        private static void RebuildCumulativeCache()
        {
            _cumulativeCache.Clear();

            HashSet<string> abilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectAbilities(Item?.Levels, abilities);
            CollectAbilities(Armor?.Levels, abilities);
            CollectAbilities(Cheat?.Levels, abilities);

            int maxLevel = ModConfig.MaxLevel;
            foreach (var ability in abilities)
            {
                float[] values = new float[maxLevel + 1];
                float runningSum = 0f;

                for (int l = 1; l <= maxLevel; l++)
                {
                    runningSum += GetLevelAddition(Item?.Levels, ability, l);
                    runningSum += GetLevelAddition(Armor?.Levels, ability, l);
                    if (Cheat?.EnableCheatAbilities == true)
                    {
                        runningSum += GetLevelAddition(Cheat?.Levels, ability, l);
                    }
                    values[l] = runningSum;
                }

                _cumulativeCache[ability] = values;
            }
        }

        private static void CollectAbilities(Dictionary<int, List<AbilityEntry>> levels, HashSet<string> set)
        {
            if (levels == null) return;
            foreach (var kvp in levels)
            {
                if (kvp.Value == null) continue;
                foreach (var entry in kvp.Value)
                {
                    if (!string.IsNullOrEmpty(entry?.Ability))
                    {
                        set.Add(entry.Ability);
                    }
                }
            }
        }

        private static float GetLevelAddition(Dictionary<int, List<AbilityEntry>> levels, string abilityName, int level)
        {
            if (levels == null) return 0f;
            if (levels.TryGetValue(level, out var list) && list != null)
            {
                float sum = 0f;
                foreach (var entry in list)
                {
                    if (string.Equals(entry.Ability, abilityName, StringComparison.OrdinalIgnoreCase))
                    {
                        sum += entry.EffectiveValue;
                    }
                }
                return sum;
            }
            return 0f;
        }

        public static float GetCumulativeAbilityValue(string abilityName, int level)
        {
            if (level <= 0 || string.IsNullOrEmpty(abilityName)) return 0f;

            if (_cumulativeCache.TryGetValue(abilityName, out float[] values))
            {
                int clampedLevel = Mathf.Clamp(level, 1, ModConfig.MaxLevel);
                return values[clampedLevel];
            }

            return 0f;
        }

        public static bool HasAbilityAtLevel(string abilityName, int level)
        {
            return GetCumulativeAbilityValue(abilityName, level) > 0.0001f;
        }

        private static void LogInfo(string msg)
        {
            try { Debug.Log(msg); } catch { System.Console.WriteLine(msg); }
        }

        private static void LogWarning(string msg)
        {
            try { Debug.LogWarning(msg); } catch { System.Console.WriteLine(msg); }
        }

        private static void LogError(string msg)
        {
            try { Debug.LogError(msg); } catch { System.Console.WriteLine(msg); }
        }

        private static string ReadAllTextSafe(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static bool TryParseFloat(object value, out float result)
        {
            result = 0f;
            if (value == null) return false;
            try
            {
                result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return float.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
        }

        private static bool TryParseInt(object value, out int result)
        {
            result = 0;
            if (value == null) return false;
            try
            {
                result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
        }

        private static T LoadWithOverride<T>(string baseFileName, string overrideFileName) where T : new()
        {
            T result = new T();
            string basePath = Path.Combine(ConfigDirectory, baseFileName);
            string overridePath = Path.Combine(ConfigDirectory, overrideFileName);

            // 1. Load Base File
            if (File.Exists(basePath))
            {
                try
                {
                    string baseYaml = ReadAllTextSafe(basePath);
                    var loadedBase = _deserializer.Deserialize<T>(baseYaml);
                    if (loadedBase != null)
                    {
                        result = loadedBase;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Valheim.ItemEnhancements] Failed to parse base config '{baseFileName}': {ex.Message}");
                }
            }

            // 2. Load & Merge Override File
            if (File.Exists(overridePath))
            {
                try
                {
                    string overrideYaml = ReadAllTextSafe(overridePath);
                    var rawDict = _deserializer.Deserialize<Dictionary<string, object>>(overrideYaml);
                    if (rawDict != null)
                    {
                        ApplyOverrides(result, rawDict);
                        LogInfo($"[Valheim.ItemEnhancements] Applied custom overrides from '{overrideFileName}'.");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Valheim.ItemEnhancements] Failed to apply overrides from '{overrideFileName}': {ex.Message}");
                }
            }

            return result;
        }

        private static void ApplyOverrides(object target, System.Collections.IDictionary overrides)
        {
            if (target == null || overrides == null) return;

            PropertyInfo[] props = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;

                // Case-insensitive lookup in dictionary
                foreach (System.Collections.DictionaryEntry kvp in overrides)
                {
                    string keyStr = kvp.Key?.ToString();
                    if (string.IsNullOrEmpty(keyStr)) continue;

                    if (string.Equals(keyStr, prop.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(Dictionary<int, List<AbilityEntry>>) && kvp.Value is System.Collections.IDictionary levelsDict)
                            {
                                var currentLevels = prop.GetValue(target) as Dictionary<int, List<AbilityEntry>> ?? new Dictionary<int, List<AbilityEntry>>();
                                foreach (System.Collections.DictionaryEntry lvlEntry in levelsDict)
                                {
                                    if (int.TryParse(lvlEntry.Key?.ToString(), out int lvlNum))
                                    {
                                        if (lvlEntry.Value is System.Collections.IEnumerable entryList)
                                        {
                                            var newAbilityList = new List<AbilityEntry>();
                                            foreach (var itemObj in entryList)
                                            {
                                                if (itemObj is System.Collections.IDictionary itemDict)
                                                {
                                                    var entry = new AbilityEntry();
                                                    foreach (System.Collections.DictionaryEntry f in itemDict)
                                                    {
                                                        string fName = f.Key?.ToString();
                                                        if (string.Equals(fName, "Ability", StringComparison.OrdinalIgnoreCase))
                                                            entry.Ability = f.Value?.ToString();
                                                        else if (string.Equals(fName, "Value", StringComparison.OrdinalIgnoreCase) && TryParseFloat(f.Value, out float v))
                                                            entry.Value = v;
                                                        else if (string.Equals(fName, "Percent", StringComparison.OrdinalIgnoreCase) && TryParseFloat(f.Value, out float p))
                                                            entry.Percent = p;
                                                    }
                                                    newAbilityList.Add(entry);
                                                }
                                            }
                                            currentLevels[lvlNum] = newAbilityList;
                                        }
                                    }
                                }
                                prop.SetValue(target, currentLevels);
                            }
                            else if (prop.PropertyType == typeof(Dictionary<string, float>) && kvp.Value is System.Collections.IDictionary dictObj)
                            {
                                var currentDict = prop.GetValue(target) as Dictionary<string, float>;
                                var newDict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                                if (currentDict != null)
                                {
                                    foreach (var kv in currentDict) newDict[kv.Key] = kv.Value;
                                }
                                foreach (System.Collections.DictionaryEntry entry in dictObj)
                                {
                                    string key = entry.Key?.ToString();
                                    if (!string.IsNullOrEmpty(key) && TryParseFloat(entry.Value, out float val))
                                    {
                                        newDict[key] = val;
                                    }
                                }
                                prop.SetValue(target, newDict);
                            }
                            else if (prop.PropertyType == typeof(Dictionary<string, Dictionary<string, float>>) && kvp.Value is System.Collections.IDictionary outerDict)
                            {
                                var currentOuter = prop.GetValue(target) as Dictionary<string, Dictionary<string, float>>;
                                var newOuter = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
                                if (currentOuter != null)
                                {
                                    foreach (var okv in currentOuter)
                                    {
                                        var innerCopy = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                                        if (okv.Value != null)
                                        {
                                            foreach (var ikv in okv.Value) innerCopy[ikv.Key] = ikv.Value;
                                        }
                                        newOuter[okv.Key] = innerCopy;
                                    }
                                }

                                foreach (System.Collections.DictionaryEntry outerEntry in outerDict)
                                {
                                    string outerKey = outerEntry.Key?.ToString();
                                    if (string.IsNullOrEmpty(outerKey)) continue;

                                    if (outerEntry.Value is System.Collections.IDictionary innerDict)
                                    {
                                        if (!newOuter.TryGetValue(outerKey, out var currentInner) || currentInner == null)
                                        {
                                            currentInner = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                                            newOuter[outerKey] = currentInner;
                                        }
                                        foreach (System.Collections.DictionaryEntry innerEntry in innerDict)
                                        {
                                            string innerKey = innerEntry.Key?.ToString();
                                            if (!string.IsNullOrEmpty(innerKey) && TryParseFloat(innerEntry.Value, out float val))
                                            {
                                                currentInner[innerKey] = val;
                                            }
                                        }
                                    }
                                }
                                prop.SetValue(target, newOuter);
                            }
                            else if (prop.PropertyType == typeof(Dictionary<string, int>) && kvp.Value is System.Collections.IDictionary dictIntObj)
                            {
                                var currentDict = prop.GetValue(target) as Dictionary<string, int>;
                                var newDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                if (currentDict != null)
                                {
                                    foreach (var kv in currentDict) newDict[kv.Key] = kv.Value;
                                }
                                foreach (System.Collections.DictionaryEntry entry in dictIntObj)
                                {
                                    string key = entry.Key?.ToString();
                                    if (!string.IsNullOrEmpty(key) && TryParseInt(entry.Value, out int val))
                                    {
                                        newDict[key] = val;
                                    }
                                }
                                prop.SetValue(target, newDict);
                            }
                            else if (kvp.Value is System.Collections.IDictionary childDict && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string) && !prop.PropertyType.IsEnum)
                            {
                                object childTarget = prop.GetValue(target);
                                if (childTarget == null)
                                {
                                    childTarget = Activator.CreateInstance(prop.PropertyType);
                                    prop.SetValue(target, childTarget);
                                }
                                ApplyOverrides(childTarget, childDict);
                            }
                            else
                            {
                                object converted = ConvertValue(kvp.Value, prop.PropertyType);
                                if (converted != null)
                                {
                                    prop.SetValue(target, converted);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"[Valheim.ItemEnhancements] Error overriding property '{prop.Name}': {ex.Message}");
                        }
                        break;
                    }
                }
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            if (targetType == typeof(float))
            {
                return TryParseFloat(value, out float f) ? f : 0f;
            }
            if (targetType == typeof(int))
            {
                return TryParseInt(value, out int i) ? i : 0;
            }
            if (targetType == typeof(bool))
            {
                if (value is bool b) return b;
                return bool.TryParse(value.ToString(), out bool pb) ? pb : false;
            }
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value.ToString(), true);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static void SetupWatcher()
        {
            if (_watcher != null) return;

            try
            {
                _watcher = new FileSystemWatcher(ConfigDirectory, "*.yml")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileChanged;
                _watcher.Deleted += OnFileChanged;
                _watcher.Renamed += (s, e) => OnFileChanged(s, e);

                _reloadDebounceTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        LoadAll();
                    }
                    catch (Exception ex)
                    {
                        LogError($"[Valheim.ItemEnhancements] Error during config hot-reload: {ex.Message}");
                    }
                }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LogWarning($"[Valheim.ItemEnhancements] Could not setup file watcher for YAML configs: {ex.Message}");
            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                _reloadDebounceTimer?.Change(300, System.Threading.Timeout.Infinite);
            }
            catch
            {
                // ignored
            }
        }

        private static void EnsureDefaultFiles()
        {
            WriteDefaultIfMissing("Success.yml", DefaultSuccessYaml);
            WriteDefaultIfMissing("Item.yml", DefaultItemYaml);
            WriteDefaultIfMissing("Armor.yml", DefaultArmorYaml);
            WriteDefaultIfMissing("Cheat.yml", DefaultCheatYaml);
            WriteDefaultIfMissing("Drops.yml", DefaultDropsYaml);
        }

        private static void WriteDefaultIfMissing(string fileName, string content)
        {
            string path = Path.Combine(ConfigDirectory, fileName);
            if (!File.Exists(path))
            {
                try
                {
                    File.WriteAllText(path, content);
                }
                catch (Exception ex)
                {
                    LogWarning($"[Valheim.ItemEnhancements] Could not write default file '{fileName}': {ex.Message}");
                }
            }
        }

        #region Embedded Default YAML Templates

        private const string DefaultSuccessYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - SUCCESS & RULES CONFIGURATION
# Success rates, failure penalties, coin fees, and scroll requirements
# ====================================================================
# To customize values without having mod updates overwrite your changes,
# create a file named ""Success.override.yml"" in this same folder
# and specify only the values you wish to override.
# ====================================================================

SuccessRates:
  Level_01: 100.0
  Level_02: 100.0
  Level_03: 95.0
  Level_04: 90.0
  Level_05: 80.0
  Level_06: 70.0
  Level_07: 60.0
  Level_08: 50.0
  Level_09: 40.0
  Level_10: 35.0
  Level_11: 30.0
  Level_12: 25.0
  Level_13: 20.0
  Level_14: 15.0
  Level_15: 12.0
  Level_16: 10.0
  Level_17: 8.0
  Level_18: 5.0
  Level_19: 3.0
  Level_20: 1.0

SafeLevel: 3
DowngradeOnFail: true
BreakOnFail: false
BreakChanceAboveSafeLevel: 10.0

RequireCoins: false
CoinsBaseCost: 0
CoinsPerLevelIncrement: 0.0
RequireCraftingStation: true
CraftingStationRange: 5.0

RequireScrolls: true
ScrollsRequiredPerAttempt: 1
";

        private const string DefaultItemYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - ITEM & WEAPON ABILITIES CONFIGURATION
# ====================================================================

Levels:
  1:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeaponAttackEitr
      Value: 2.0
    - Ability: MaxDurability
      Value: 5.0
  2:
    - Ability: WeaponDamage
      Value: 5.0
  3:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeightReduction
      Value: 3.0
  4:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: WeaponBackstab
      Value: 5.0
  5:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: CarryWeightBonus
      Value: 10.0
  6:
    - Ability: WeaponDamage
      Value: 5.0
  7:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeaponAttackEitr
      Value: 2.0
    - Ability: WeightReduction
      Value: 3.0
  8:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: WeaponBackstab
      Value: 5.0
  10:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: CarryWeightBonus
      Value: 15.0
  11:
    - Ability: WeaponDamage
      Value: 5.0
  12:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeightReduction
      Value: 3.0
  13:
    - Ability: WeaponDamage
      Value: 5.0
  14:
    - Ability: WeaponBackstab
      Value: 5.0
  15:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: CarryWeightBonus
      Value: 25.0
  16:
    - Ability: WeaponDamage
      Value: 5.0
  17:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeightReduction
      Value: 3.0
  18:
    - Ability: WeaponDamage
      Value: 5.0
  19:
    - Ability: WeaponBackstab
      Value: 5.0
  20:
    - Ability: WeaponDamage
      Value: 10.0
    - Ability: MaxDurability
      Value: 10.0
    - Ability: CarryWeightBonus
      Value: 50.0
";

        private const string DefaultArmorYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - ARMOR & SHIELD ABILITIES CONFIGURATION
# ====================================================================

Levels:
  1:
    - Ability: DodgeStaminaReduction
      Value: 1.0
  2:
    - Ability: ArmorFlat
      Value: 1.5
    - Ability: ShieldBlockPower
      Value: 5.0
  3:
    - Ability: RunStaminaReduction
      Value: 1.0
    - Ability: ShieldBlockStaminaReduction
      Value: 2.0
  4:
    - Ability: ArmorPercent
      Value: 2.0
    - Ability: JumpStaminaReduction
      Value: 1.0
  5:
    - Ability: ShieldDeflectionForce
      Value: 5.0
    - Ability: ShieldTimedBlock
      Value: 2.0
  6:
    - Ability: ArmorFlat
      Value: 1.5
    - Ability: ShieldBlockPower
      Value: 5.0
  7:
    - Ability: DodgeStaminaReduction
      Value: 1.0
  8:
    - Ability: ArmorPercent
      Value: 2.0
  9:
    - Ability: ShieldBlockPower
      Value: 5.0
    - Ability: ShieldBlockStaminaReduction
      Value: 2.0
    - Ability: RunStaminaReduction
      Value: 1.0
  10:
    - Ability: ArmorMovementPenaltyReduction
      Value: 15.0
    - Ability: EitrRegen
      Value: 10.0
  11:
    - Ability: ArmorFlat
      Value: 2.0
  12:
    - Ability: ShieldBlockPower
      Value: 5.0
  13:
    - Ability: ArmorPercent
      Value: 2.0
    - Ability: JumpStaminaReduction
      Value: 1.0
  14:
    - Ability: ShieldTimedBlock
      Value: 2.0
    - Ability: DodgeStaminaReduction
      Value: 1.0
  15:
    - Ability: ArmorMovementPenaltyReduction
      Value: 20.0
    - Ability: ShieldDeflectionForce
      Value: 5.0
  16:
    - Ability: ArmorFlat
      Value: 2.0
  17:
    - Ability: ShieldBlockPower
      Value: 5.0
  18:
    - Ability: ArmorPercent
      Value: 2.0
  19:
    - Ability: ShieldTimedBlock
      Value: 2.0
    - Ability: EitrRegen
      Value: 10.0
  20:
    - Ability: ArmorFlat
      Value: 3.0
    - Ability: ArmorMovementPenaltyReduction
      Value: 25.0
";

        private const string DefaultCheatYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - CHEAT ABILITIES CONFIGURATION
# ====================================================================

EnableCheatAbilities: false

Levels:
  5:
    - Ability: MaxHealth
      Value: 10.0
    - Ability: MaxStamina
      Value: 10.0
    - Ability: MaxEitr
      Value: 10.0
  10:
    - Ability: HealthRegen
      Value: 10.0
    - Ability: StaminaRegen
      Value: 10.0
  15:
    - Ability: HealingMultiplier
      Value: 15.0
";

        private const string DefaultDropsYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - MONSTER SCROLL DROPS CONFIGURATION
# Detailed drop rates for enhancement scrolls from monsters and bosses
# ====================================================================
# To customize values without having mod updates overwrite your changes,
# create a file named ""Drops.override.yml"" in this same folder
# and specify only the values you wish to override.
# ====================================================================

EnableMonsterDrops: true
StarLevelMultiplier: 1.5

BiomeDrops:
  Meadows:
    Tier1: 15.0
    Tier2: 0.0
    Tier3: 0.0
    Tier4: 0.0
  BlackForest:
    Tier1: 15.0
    Tier2: 3.0
    Tier3: 0.0
    Tier4: 0.0
  Swamp:
    Tier1: 5.0
    Tier2: 10.0
    Tier3: 0.0
    Tier4: 0.0
  Mountain:
    Tier1: 0.0
    Tier2: 10.0
    Tier3: 3.0
    Tier4: 0.0
  Plains:
    Tier1: 0.0
    Tier2: 2.0
    Tier3: 6.0
    Tier4: 0.0
  Mistlands:
    Tier1: 0.0
    Tier2: 0.0
    Tier3: 6.0
    Tier4: 2.0
  AshLands:
    Tier1: 0.0
    Tier2: 0.0
    Tier3: 1.0
    Tier4: 3.0
  DeepNorth:
    Tier1: 0.0
    Tier2: 0.0
    Tier3: 0.0
    Tier4: 5.0
  Ocean:
    Tier1: 5.0
    Tier2: 10.0
    Tier3: 0.0
    Tier4: 0.0

EliteDrops:
  BonusMultiplier: 2.0
  TierUpgradeChance: 25.0

BossDrops:
  GuaranteedDrop: true
  MinAmount: 1
  MaxAmount: 3
  BossTiers:
    Eikthyr: 1
    Elder: 2
    Bonemass: 2
    Moder: 3
    Yagluth: 3
    Queen: 4
    Fader: 4
";

        #endregion
    }

    #region Data Models

    public class SuccessConfigData
    {
        public Dictionary<string, float> SuccessRates { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            { "Level_01", 100f }, { "Level_02", 100f }, { "Level_03", 95f }, { "Level_04", 90f }, { "Level_05", 80f },
            { "Level_06", 70f },  { "Level_07", 60f },  { "Level_08", 50f }, { "Level_09", 40f }, { "Level_10", 35f },
            { "Level_11", 30f },  { "Level_12", 25f },  { "Level_13", 20f }, { "Level_14", 15f }, { "Level_15", 12f },
            { "Level_16", 10f },  { "Level_17", 8f },   { "Level_18", 5f },  { "Level_19", 3f },  { "Level_20", 1f }
        };

        public int SafeLevel { get; set; } = 3;
        public bool DowngradeOnFail { get; set; } = true;
        public bool BreakOnFail { get; set; } = false;
        public float BreakChanceAboveSafeLevel { get; set; } = 10.0f;

        public bool RequireCoins { get; set; } = false;
        public int CoinsBaseCost { get; set; } = 0;
        public float CoinsPerLevelIncrement { get; set; } = 0.0f;
        public bool RequireCraftingStation { get; set; } = true;
        public float CraftingStationRange { get; set; } = 5.0f;

        public bool RequireScrolls { get; set; } = true;
        public int ScrollsRequiredPerAttempt { get; set; } = 1;

        public float GetSuccessRate(int level)
        {
            if (level < 1) return 100f;
            if (level > 20) return 0f;
            string key = $"Level_{level:D2}";
            if (SuccessRates != null && SuccessRates.TryGetValue(key, out float rate))
            {
                return rate;
            }
            return 0f;
        }
    }

    public class ItemConfigData
    {
        public Dictionary<int, List<AbilityEntry>> Levels { get; set; } = new Dictionary<int, List<AbilityEntry>>();
    }

    public class ArmorConfigData
    {
        public Dictionary<int, List<AbilityEntry>> Levels { get; set; } = new Dictionary<int, List<AbilityEntry>>();
    }

    public class CheatConfigData
    {
        public bool EnableCheatAbilities { get; set; } = false;
        public Dictionary<int, List<AbilityEntry>> Levels { get; set; } = new Dictionary<int, List<AbilityEntry>>();
    }

    public class AbilityEntry
    {
        public string Ability { get; set; } = string.Empty;
        public float Value { get; set; } = 0f;
        public float Percent { get; set; } = 0f;

        public float EffectiveValue => Math.Abs(Value) > 0.00001f ? Value : Percent;
    }

    public class DropsConfigData
    {
        public bool EnableMonsterDrops { get; set; } = true;
        public float StarLevelMultiplier { get; set; } = 1.5f;

        public Dictionary<string, Dictionary<string, float>> BiomeDrops { get; set; } = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Meadows", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 15.0f }, { "Tier2", 0.0f }, { "Tier3", 0.0f }, { "Tier4", 0.0f } } },
            { "BlackForest", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 15.0f }, { "Tier2", 3.0f }, { "Tier3", 0.0f }, { "Tier4", 0.0f } } },
            { "Swamp", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 5.0f }, { "Tier2", 10.0f }, { "Tier3", 0.0f }, { "Tier4", 0.0f } } },
            { "Mountain", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 0.0f }, { "Tier2", 10.0f }, { "Tier3", 3.0f }, { "Tier4", 0.0f } } },
            { "Plains", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 0.0f }, { "Tier2", 2.0f }, { "Tier3", 6.0f }, { "Tier4", 0.0f } } },
            { "Mistlands", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 0.0f }, { "Tier2", 0.0f }, { "Tier3", 6.0f }, { "Tier4", 2.0f } } },
            { "AshLands", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 0.0f }, { "Tier2", 0.0f }, { "Tier3", 1.0f }, { "Tier4", 3.0f } } },
            { "DeepNorth", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 0.0f }, { "Tier2", 0.0f }, { "Tier3", 0.0f }, { "Tier4", 5.0f } } },
            { "Ocean", new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "Tier1", 5.0f }, { "Tier2", 10.0f }, { "Tier3", 0.0f }, { "Tier4", 0.0f } } }
        };

        public EliteDropConfig EliteDrops { get; set; } = new EliteDropConfig();
        public BossDropConfig BossDrops { get; set; } = new BossDropConfig();

        public float GetBiomeTierChance(Heightmap.Biome biome, int tier)
        {
            if (BiomeDrops == null) return 0f;

            string tierKey = $"Tier{tier}";
            string biomeKey = biome.ToString();

            if (BiomeDrops.TryGetValue(biomeKey, out var tierRates) && tierRates != null)
            {
                if (tierRates.TryGetValue(tierKey, out float chance))
                {
                    return chance;
                }
            }

            // Fallback for flag masks or biome variations
            foreach (var kvp in BiomeDrops)
            {
                if (Enum.TryParse<Heightmap.Biome>(kvp.Key, true, out var parsedBiome))
                {
                    if ((biome & parsedBiome) != 0 && kvp.Value != null)
                    {
                        if (kvp.Value.TryGetValue(tierKey, out float chance))
                        {
                            return chance;
                        }
                    }
                }
            }

            return 0f;
        }
    }

    public class EliteDropConfig
    {
        public float BonusMultiplier { get; set; } = 2.0f;
        public float TierUpgradeChance { get; set; } = 25.0f;
    }

    public class BossDropConfig
    {
        public bool GuaranteedDrop { get; set; } = true;
        public int MinAmount { get; set; } = 1;
        public int MaxAmount { get; set; } = 3;
        public Dictionary<string, int> BossTiers { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Eikthyr", 1 },
            { "Elder", 2 },
            { "Bonemass", 2 },
            { "Moder", 3 },
            { "Yagluth", 3 },
            { "Queen", 4 },
            { "Fader", 4 }
        };

        public int GetBossTier(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return 1;
            string lowerName = characterName.ToLower();

            foreach (var kvp in BossTiers)
            {
                if (lowerName.Contains(kvp.Key.ToLower()))
                {
                    return kvp.Value;
                }
            }

            // Fallback default mappings
            if (lowerName.Contains("eikthyr")) return 1;
            if (lowerName.Contains("gd_king") || lowerName.Contains("elder")) return 2;
            if (lowerName.Contains("bonemass")) return 2;
            if (lowerName.Contains("dragon") || lowerName.Contains("moder")) return 3;
            if (lowerName.Contains("goblinking") || lowerName.Contains("yagluth")) return 3;
            if (lowerName.Contains("seekerqueen") || lowerName.Contains("queen")) return 4;
            if (lowerName.Contains("fader")) return 4;

            return 1;
        }
    }

    #endregion
}
