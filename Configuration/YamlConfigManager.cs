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
        public static AbilitiesConfigData Abilities { get; private set; } = new AbilitiesConfigData();

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
            Abilities = LoadWithOverride<AbilitiesConfigData>("Abilities.yml", "Abilities.override.yml");

            LogInfo($"[Valheim.ItemEnhancements] YAML Configurations loaded successfully from '{ConfigDirectory}'.");
            OnConfigReloaded?.Invoke();
        }

        public static float GetCumulativeAbilityValue(string abilityName, int level)
        {
            if (level <= 0 || Abilities?.Levels == null) return 0f;
            float total = 0f;
            for (int l = 1; l <= level; l++)
            {
                if (Abilities.Levels.TryGetValue(l, out var list) && list != null)
                {
                    foreach (var entry in list)
                    {
                        if (string.Equals(entry.Ability, abilityName, StringComparison.OrdinalIgnoreCase))
                        {
                            total += entry.EffectiveValue;
                        }
                    }
                }
            }
            return total;
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
                    string baseYaml = File.ReadAllText(basePath);
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
                    string overrideYaml = File.ReadAllText(overridePath);
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

        private static void ApplyOverrides<T>(T target, Dictionary<string, object> overrides)
        {
            if (target == null || overrides == null) return;

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;

                // Case-insensitive lookup in dictionary
                foreach (var kvp in overrides)
                {
                    if (string.Equals(kvp.Key, prop.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(Dictionary<int, List<AbilityEntry>>) && kvp.Value is Dictionary<object, object> levelsDict)
                            {
                                var currentLevels = prop.GetValue(target) as Dictionary<int, List<AbilityEntry>> ?? new Dictionary<int, List<AbilityEntry>>();
                                foreach (var lvlEntry in levelsDict)
                                {
                                    if (int.TryParse(lvlEntry.Key?.ToString(), out int lvlNum))
                                    {
                                        if (lvlEntry.Value is List<object> entryList)
                                        {
                                            var newAbilityList = new List<AbilityEntry>();
                                            foreach (var itemObj in entryList)
                                            {
                                                if (itemObj is Dictionary<object, object> itemDict)
                                                {
                                                    var entry = new AbilityEntry();
                                                    foreach (var f in itemDict)
                                                    {
                                                        string fName = f.Key?.ToString();
                                                        if (string.Equals(fName, "Ability", StringComparison.OrdinalIgnoreCase))
                                                            entry.Ability = f.Value?.ToString();
                                                        else if (string.Equals(fName, "Value", StringComparison.OrdinalIgnoreCase) && float.TryParse(f.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float v))
                                                            entry.Value = v;
                                                        else if (string.Equals(fName, "Percent", StringComparison.OrdinalIgnoreCase) && float.TryParse(f.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float p))
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
                            else if (prop.PropertyType == typeof(Dictionary<string, float>) && kvp.Value is Dictionary<object, object> dictObj)
                            {
                                var currentDict = prop.GetValue(target) as Dictionary<string, float> ?? new Dictionary<string, float>();
                                foreach (var entry in dictObj)
                                {
                                    string key = entry.Key?.ToString();
                                    if (!string.IsNullOrEmpty(key) && float.TryParse(entry.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
                                    {
                                        currentDict[key] = val;
                                    }
                                }
                                prop.SetValue(target, currentDict);
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

            string strVal = value.ToString();
            if (targetType == typeof(float))
            {
                return float.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : 0f;
            }
            if (targetType == typeof(int))
            {
                return int.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out int i) ? i : 0;
            }
            if (targetType == typeof(bool))
            {
                return bool.TryParse(strVal, out bool b) ? b : false;
            }
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, strVal, true);
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
            WriteDefaultIfMissing("Abilities.yml", DefaultAbilitiesYaml);
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
# หมวดหมู่อัตราความสำเร็จ, กฎการล้มเหลว, ค่าธรรมเนียม และการดรอปคัมภีร์
# ====================================================================
# หากต้องการปรับแต่งค่าโดยไม่ให้การอัปเดต Mod ส่งผลกระทบ
# ให้สร้างไฟล์ ""Success.override.yml"" ในโฟลเดอร์เดียวกันนี้
# และใส่เฉพาะค่าที่ต้องการแก้ไข ระบบจะนำค่า override มาทับค่าเดิมอัตโนมัติ
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

RequireScrolls: true
ScrollsRequiredPerAttempt: 1

EnableMonsterDrops: true
Tier1_DropChance: 15.0
Tier2_DropChance: 10.0
Tier3_DropChance: 6.0
Tier4_DropChance: 3.0
StarLevelMultiplier: 1.5
BossGuaranteedDrop: true
BossMinDrop: 1
BossMaxDrop: 3
";

        private const string DefaultItemYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - ITEM & WEAPON ABILITIES CONFIGURATION
# หมวดหมู่อาวุธ, คุณสมบัติทั่วไป (ความทนทาน, น้ำหนัก) และเครื่องประดับ
# ====================================================================
# หากต้องการปรับแต่งค่าโดยไม่ให้การอัปเดต Mod ส่งผลกระทบ
# ให้สร้างไฟล์ ""Item.override.yml"" ในโฟลเดอร์เดียวกันนี้
# และใส่เฉพาะค่าที่ต้องการแก้ไข ระบบจะนำค่า override มาทับค่าเดิมอัตโนมัติ
# ====================================================================

WeaponDamageBonusPerLevel: 0.05
WeaponBackstabBonusPerLevel: 0.05
WeaponStaminaReductionPerLevel: 0.01
WeaponEitrReductionPerLevel: 0.01

MaxDurabilityBonusPerLevel: 0.05
WeightReductionPerLevel: 0.015

CarryWeightBonusPerLevel: 5.0
";

        private const string DefaultArmorYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - ARMOR & SHIELD ABILITIES CONFIGURATION
# หมวดหมู่ชุดเกราะ, โล่ และความคล่องตัว (Mobility)
# ====================================================================
# หากต้องการปรับแต่งค่าโดยไม่ให้การอัปเดต Mod ส่งผลกระทบ
# ให้สร้างไฟล์ ""Armor.override.yml"" ในโฟลเดอร์เดียวกันนี้
# และใส่เฉพาะค่าที่ต้องการแก้ไข ระบบจะนำค่า override มาทับค่าเดิมอัตโนมัติ
# ====================================================================

ArmorFlatBonusPerLevel: 1.5
ArmorPercentBonusPerLevel: 0.02
ArmorMovementPenaltyReductionPerLevel: 0.05
EitrRegenBonusPerLevel: 0.02

ShieldBlockPowerBonusPerLevel: 0.05
ShieldDeflectionBonusPerLevel: 0.05
ShieldTimedBlockBonusPerLevel: 0.02
ShieldBlockStaminaReductionPerLevel: 0.015

DodgeStaminaReductionPerLevel: 0.01
RunStaminaReductionPerLevel: 0.005
JumpStaminaReductionPerLevel: 0.005
";

        private const string DefaultCheatYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - CHEAT ABILITIES CONFIGURATION (โหมดโกงเสริม)
# หมวดหมู่ความสามารถพิเศษระดับโกง (เพิ่ม Capacity เลือด, สเตมินา, พลังเวท และรีเจนต่างๆ)
# ====================================================================
# หากต้องการปรับแต่งค่าโดยไม่ให้การอัปเดต Mod ส่งผลกระทบ
# ให้สร้างไฟล์ ""Cheat.override.yml"" ในโฟลเดอร์เดียวกันนี้
# และใส่เฉพาะค่าที่ต้องการแก้ไข ระบบจะนำค่า override มาทับค่าเดิมอัตโนมัติ
# ====================================================================

EnableCheatAbilities: false

CheatMaxHealthPerLevel: 5.0
CheatMaxStaminaPerLevel: 5.0
CheatMaxEitrPerLevel: 5.0

CheatHealthRegenPerLevel: 0.05
CheatStaminaRegenPerLevel: 0.05
CheatEitrRegenPerLevel: 0.05
CheatHealingMultiplierPerLevel: 0.05
";

        private const string DefaultAbilitiesYaml = @"# ====================================================================
# VALHEIM ITEM ENHANCEMENTS - ABILITIES BY LEVEL CONFIGURATION
# กำหนด Abilities และเปอร์เซ็นต์/ค่าพลัง ที่จะได้รับในแต่ละระดับ (Level 1 - 20)
# ====================================================================

Levels:
  1:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeaponAttackEitr
      Value: 2.0
    - Ability: MaxDurability
      Value: 5.0
    - Ability: DodgeStaminaReduction
      Value: 1.0
  2:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorFlat
      Value: 1.5
    - Ability: ShieldBlockPower
      Value: 5.0
  3:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: RunStaminaReduction
      Value: 1.0
    - Ability: ShieldBlockStaminaReduction
      Value: 2.0
    - Ability: WeightReduction
      Value: 3.0
  4:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorPercent
      Value: 2.0
    - Ability: JumpStaminaReduction
      Value: 1.0
    - Ability: WeaponBackstab
      Value: 5.0
  5:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: ShieldDeflectionForce
      Value: 5.0
    - Ability: ShieldTimedBlock
      Value: 2.0
    - Ability: CarryWeightBonus
      Value: 10.0
    - Ability: CheatMaxHealth
      Value: 10.0
    - Ability: CheatMaxStamina
      Value: 10.0
    - Ability: CheatMaxEitr
      Value: 10.0
  6:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorFlat
      Value: 1.5
    - Ability: ShieldBlockPower
      Value: 5.0
  7:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: WeaponAttackEitr
      Value: 2.0
    - Ability: DodgeStaminaReduction
      Value: 1.0
    - Ability: WeightReduction
      Value: 3.0
  8:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorPercent
      Value: 2.0
    - Ability: WeaponBackstab
      Value: 5.0
  9:
    - Ability: ShieldBlockPower
      Value: 5.0
    - Ability: ShieldBlockStaminaReduction
      Value: 2.0
    - Ability: RunStaminaReduction
      Value: 1.0
  10:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: ArmorMovementPenaltyReduction
      Value: 15.0
    - Ability: EitrRegen
      Value: 10.0
    - Ability: CarryWeightBonus
      Value: 15.0
    - Ability: CheatHealthRegen
      Value: 10.0
    - Ability: CheatStaminaRegen
      Value: 10.0
    - Ability: CheatEitrRegen
      Value: 10.0
  11:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorFlat
      Value: 2.0
  12:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: ShieldBlockPower
      Value: 5.0
    - Ability: WeightReduction
      Value: 3.0
  13:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorPercent
      Value: 2.0
    - Ability: JumpStaminaReduction
      Value: 1.0
  14:
    - Ability: WeaponBackstab
      Value: 5.0
    - Ability: ShieldTimedBlock
      Value: 2.0
    - Ability: DodgeStaminaReduction
      Value: 1.0
  15:
    - Ability: MaxDurability
      Value: 5.0
    - Ability: ArmorMovementPenaltyReduction
      Value: 20.0
    - Ability: ShieldDeflectionForce
      Value: 5.0
    - Ability: CarryWeightBonus
      Value: 25.0
    - Ability: CheatHealingMultiplier
      Value: 15.0
  16:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorFlat
      Value: 2.0
  17:
    - Ability: WeaponAttackStamina
      Value: 2.0
    - Ability: ShieldBlockPower
      Value: 5.0
    - Ability: WeightReduction
      Value: 3.0
  18:
    - Ability: WeaponDamage
      Value: 5.0
    - Ability: ArmorPercent
      Value: 2.0
  19:
    - Ability: WeaponBackstab
      Value: 5.0
    - Ability: ShieldTimedBlock
      Value: 2.0
    - Ability: EitrRegen
      Value: 10.0
  20:
    - Ability: WeaponDamage
      Value: 10.0
    - Ability: ArmorFlat
      Value: 3.0
    - Ability: MaxDurability
      Value: 10.0
    - Ability: ArmorMovementPenaltyReduction
      Value: 25.0
    - Ability: CarryWeightBonus
      Value: 50.0
";

        #endregion
    }

    #region Data Models

    public class SuccessConfigData
    {
        public Dictionary<string, float> SuccessRates { get; set; } = new Dictionary<string, float>
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

        public bool RequireScrolls { get; set; } = true;
        public int ScrollsRequiredPerAttempt { get; set; } = 1;

        public bool EnableMonsterDrops { get; set; } = true;
        public float Tier1_DropChance { get; set; } = 15.0f;
        public float Tier2_DropChance { get; set; } = 10.0f;
        public float Tier3_DropChance { get; set; } = 6.0f;
        public float Tier4_DropChance { get; set; } = 3.0f;
        public float StarLevelMultiplier { get; set; } = 1.5f;
        public bool BossGuaranteedDrop { get; set; } = true;
        public int BossMinDrop { get; set; } = 1;
        public int BossMaxDrop { get; set; } = 3;

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
        public float WeaponDamageBonusPerLevel { get; set; } = 0.05f;
        public float WeaponBackstabBonusPerLevel { get; set; } = 0.05f;
        public float WeaponStaminaReductionPerLevel { get; set; } = 0.01f;
        public float WeaponEitrReductionPerLevel { get; set; } = 0.01f;

        public float MaxDurabilityBonusPerLevel { get; set; } = 0.05f;
        public float WeightReductionPerLevel { get; set; } = 0.015f;

        public float CarryWeightBonusPerLevel { get; set; } = 5.0f;
    }

    public class ArmorConfigData
    {
        public float ArmorFlatBonusPerLevel { get; set; } = 1.5f;
        public float ArmorPercentBonusPerLevel { get; set; } = 0.02f;
        public float ArmorMovementPenaltyReductionPerLevel { get; set; } = 0.05f;
        public float EitrRegenBonusPerLevel { get; set; } = 0.02f;

        public float ShieldBlockPowerBonusPerLevel { get; set; } = 0.05f;
        public float ShieldDeflectionBonusPerLevel { get; set; } = 0.05f;
        public float ShieldTimedBlockBonusPerLevel { get; set; } = 0.02f;
        public float ShieldBlockStaminaReductionPerLevel { get; set; } = 0.015f;

        public float DodgeStaminaReductionPerLevel { get; set; } = 0.01f;
        public float RunStaminaReductionPerLevel { get; set; } = 0.005f;
        public float JumpStaminaReductionPerLevel { get; set; } = 0.005f;
    }

    public class CheatConfigData
    {
        public bool EnableCheatAbilities { get; set; } = false;

        public float CheatMaxHealthPerLevel { get; set; } = 5.0f;
        public float CheatMaxStaminaPerLevel { get; set; } = 5.0f;
        public float CheatMaxEitrPerLevel { get; set; } = 5.0f;

        public float CheatHealthRegenPerLevel { get; set; } = 0.05f;
        public float CheatStaminaRegenPerLevel { get; set; } = 0.05f;
        public float CheatEitrRegenPerLevel { get; set; } = 0.05f;
        public float CheatHealingMultiplierPerLevel { get; set; } = 0.05f;
    }

    public class AbilityEntry
    {
        public string Ability { get; set; } = string.Empty;
        public float Value { get; set; } = 0f;
        public float Percent { get; set; } = 0f;

        public float EffectiveValue => Math.Abs(Value) > 0.00001f ? Value : Percent;
    }

    public class AbilitiesConfigData
    {
        public Dictionary<int, List<AbilityEntry>> Levels { get; set; } = new Dictionary<int, List<AbilityEntry>>();
    }

    #endregion
}
