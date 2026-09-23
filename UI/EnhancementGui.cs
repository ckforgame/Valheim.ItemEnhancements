using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.Core;

namespace Valheim.ItemEnhancements.UI
{
    public class EnhancementGui : MonoBehaviour
    {
        public static EnhancementGui Instance { get; private set; }
        public static bool IsOpen { get; set; } = false;

        private static readonly Func<List<CraftingStation>> s_getAllStations = CreateStaticFieldGetter<List<CraftingStation>>(typeof(CraftingStation), "m_allStations");

        private static Func<T> CreateStaticFieldGetter<T>(Type type, string fieldName)
        {
            try
            {
                FieldInfo field = AccessTools.Field(type, fieldName);
                if (field == null) return () => default(T);
                var expr = System.Linq.Expressions.Expression.Field(null, field);
                return System.Linq.Expressions.Expression.Lambda<Func<T>>(expr).Compile();
            }
            catch
            {
                return () => default(T);
            }
        }

        private static readonly AccessTools.FieldRef<GameCamera, bool> s_mouseCaptureRef =
            AccessTools.FieldRefAccess<GameCamera, bool>("m_mouseCapture");

        private Rect _windowRect = new Rect(Screen.width / 2f - 340f, Screen.height / 2f - 310f, 680f, 620f);
        private ItemDrop.ItemData _selectedItem;
        private Vector2 _scrollPosition = Vector2.zero;

        private readonly List<ItemDrop.ItemData> _cachedEnhanceableItems = new List<ItemDrop.ItemData>();
        private bool _isEnhanceablesDirty = true;
        private float _stationCheckTimer = 0f;

        private bool _isForging = false;
        private float _forgeTimer = 0f;
        private const float ForgeDuration = 0.75f;

        private string _lastResultMessage = "";
        private string _lastResultColor = "#ffffff";

        // GUI Styles
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _statLabelStyle;
        private GUIStyle _statValueCurrentStyle;
        private GUIStyle _statValueNextStyle;
        private GUIStyle _boxSectionStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _itemButtonStyle;
        private GUIStyle _rateNumberStyle;

        private Texture2D _texWindowBg;
        private Texture2D _texBoxBg;
        private Texture2D _texButtonNormal;
        private Texture2D _texButtonHover;
        private Texture2D _texButtonActive;
        private Texture2D _texButtonDisabled;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTextures();
        }

        private void OnDisable()
        {
            if (IsOpen)
            {
                Close();
            }
        }

        public static bool IsNearCraftingStation()
        {
            if (Player.m_localPlayer == null) return false;
            if (!ModConfig.RequireCraftingStation.Value) return true;

            if (Player.m_localPlayer.GetCurrentCraftingStation() != null)
            {
                return true;
            }

            List<CraftingStation> stations = s_getAllStations?.Invoke();
            if (stations != null)
            {
                Vector3 playerPos = Player.m_localPlayer.transform.position;
                float maxRange = ModConfig.CraftingStationRange.Value;
                float maxRangeSqr = maxRange * maxRange;
                for (int i = 0; i < stations.Count; i++)
                {
                    CraftingStation station = stations[i];
                    if (station != null && (station.transform.position - playerPos).sqrMagnitude <= maxRangeSqr)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public static void Open()
        {
            if (Player.m_localPlayer == null) return;

            if (ModConfig.RequireCraftingStation.Value && !IsNearCraftingStation())
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "<color=#f59e0b>Must be near a crafting station or forge to enhance items!</color>");
                return;
            }

            IsOpen = true;
            if (Instance != null)
            {
                Instance._lastResultMessage = "";
                Instance._isEnhanceablesDirty = true;
                // Auto-select equipped item if empty
                if (Instance._selectedItem == null)
                {
                    Instance._selectedItem = Player.m_localPlayer.GetInventory()?.GetEquippedItems()?.Find(EnhancementManager.IsEnhanceable);
                }
            }

            // Unlock cursor for GUI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();

            // Cancel any active drag item in Inventory if present
            if (InventoryGui.instance != null)
            {
                var setupDrag = AccessTools.Method(typeof(InventoryGui), "SetupDragItem", new[] { typeof(ItemDrop.ItemData), typeof(Inventory), typeof(int) });
                setupDrag?.Invoke(InventoryGui.instance, new object[] { null, null, 0 });
            }
        }

        public static void Close(bool forceLockCursorIfNoMenu = false)
        {
            IsOpen = false;
            if (Instance != null)
            {
                Instance._isForging = false;
                Instance._forgeTimer = 0f;
            }

            // Restore GameCamera mouse capture if it was disabled
            if (GameCamera.instance != null && s_mouseCaptureRef != null)
            {
                s_mouseCaptureRef(GameCamera.instance) = true;
            }

            // Restore cursor lock if no other UI is open
            bool otherGuiVisible = false;
            if (!forceLockCursorIfNoMenu)
            {
                otherGuiVisible = (InventoryGui.instance != null && InventoryGui.IsVisible())
                               || (StoreGui.instance != null && StoreGui.IsVisible());
            }

            if (Menu.instance != null && (Menu.IsVisible() || Menu.IsActive()))
            {
                otherGuiVisible = true;
            }
            if (TextInput.IsVisible())
            {
                otherGuiVisible = true;
            }

            if (!otherGuiVisible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ZCursor.LockState = CursorLockMode.Locked;
                ZCursor.Hide();
            }
        }

        public static void SetSelectedItem(ItemDrop.ItemData item)
        {
            if (Instance != null)
            {
                Instance._selectedItem = item;
                Instance._lastResultMessage = "";
                Instance._isEnhanceablesDirty = true;
            }
        }

        private void Update()
        {
            if (!IsOpen) return;

            // Keep cursor unlocked while UI is open
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (ZCursor.LockState != CursorLockMode.None)
            {
                ZCursor.LockState = CursorLockMode.None;
                ZCursor.Show();
            }

            // Handle forging timer
            if (_isForging)
            {
                _forgeTimer -= Time.deltaTime;
                if (_forgeTimer <= 0f)
                {
                    _isForging = false;
                    ExecuteEnhancement();
                }
            }

            // Close if player moves away from station (throttled check every 0.25s)
            _stationCheckTimer -= Time.deltaTime;
            if (_stationCheckTimer <= 0f)
            {
                _stationCheckTimer = 0.25f;
                if (ModConfig.RequireCraftingStation.Value && Player.m_localPlayer != null)
                {
                    if (!IsNearCraftingStation())
                    {
                        Close();
                    }
                }
            }

            // ESC to close
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || Player.m_localPlayer == null) return;

            InitStyles();

            // Center window if resolution changed
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Screen.height - _windowRect.height);

            _windowRect = GUI.Window(987654, _windowRect, DrawWindowContent, "", _windowStyle);
        }

        private void DrawWindowContent(int windowID)
        {
            // Title Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("✦  MMORPG ITEM ENHANCEMENT  ✦", _headerStyle);
            if (GUILayout.Button("✕", GUILayout.Width(30f), GUILayout.Height(26f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // Item Selection Area
            DrawItemSelectionArea();

            GUILayout.Space(10f);

            if (_selectedItem != null && EnhancementManager.IsEnhanceable(_selectedItem))
            {
                // Stats Comparison (Current vs Next)
                DrawStatsComparison();

                GUILayout.Space(10f);

                // Chances, Rules, and Cost Section
                DrawRulesAndCostSection();

                GUILayout.Space(10f);

                // Action Button & Result Banner
                DrawActionButtonSection();
            }
            else
            {
                // List available equipment to pick
                DrawEquipmentPickerList();
            }

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 35f));
        }

        private void DrawItemSelectionArea()
        {
            GUILayout.BeginVertical(_boxSectionStyle);

            if (_selectedItem != null)
            {
                int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
                string hex = EnhancementManager.GetTierHex(currentLvl);
                string rank = EnhancementManager.GetTierRankName(currentLvl);

                GUILayout.BeginHorizontal();
                string localizedName = Localization.instance != null ? Localization.instance.Localize(_selectedItem.m_shared.m_name) : _selectedItem.m_shared.m_name;
                GUILayout.Label($"<b>Target Item:</b> <color={hex}><b>{localizedName} +{currentLvl}</b></color> <color=#94a3b8>({rank})</color>", _subHeaderStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Change Item", GUILayout.Width(150f), GUILayout.Height(26f)))
                {
                    _selectedItem = null;
                    _lastResultMessage = "";
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<color=#ffd700>Select an item to enhance from the list below or click an item in your inventory.</color>", _subHeaderStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawStatsComparison()
        {
            int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
            int nextLvl = currentLvl + 1;
            bool isMax = currentLvl >= ModConfig.MaxLevel;

            GUILayout.BeginVertical(_boxSectionStyle);
            GUILayout.Label("<b>Stat Progression:</b>", _statLabelStyle);
            GUILayout.Space(4f);

            // Columns Header
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current: <color={EnhancementManager.GetTierHex(currentLvl)}><b>+{currentLvl}</b></color>", _statValueCurrentStyle, GUILayout.Width(270f));
            GUILayout.Label("➔", _headerStyle, GUILayout.Width(60f));
            if (!isMax)
            {
                GUILayout.Label($"Next: <color={EnhancementManager.GetTierHex(nextLvl)}><b>+{nextLvl}</b></color>", _statValueNextStyle, GUILayout.Width(270f));
            }
            else
            {
                GUILayout.Label("<color=#ffd700><b>★ MAX LEVEL ★</b></color>", _statValueNextStyle, GUILayout.Width(270f));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Stats Rows
            // 1. Damage (Weapons)
            if (IsWeapon(_selectedItem))
            {
                float curDmgPct = YamlConfigManager.GetCumulativeAbilityValue("WeaponDamage", currentLvl);
                float nxtDmgPct = YamlConfigManager.GetCumulativeAbilityValue("WeaponDamage", nextLvl);
                float diffDmg = nxtDmgPct - curDmgPct;
                string diffDmgStr = curDmgPct <= 0f && nxtDmgPct > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffDmg:F0}%)</color>";

                DrawStatRow("All Damage:",
                    $"+{curDmgPct:F0}%",
                    isMax ? "-" : $"+{nxtDmgPct:F0}% {diffDmgStr}");

                float curStam = StatCalculator.GetAttackStaminaReduction(currentLvl) * 100f;
                float nxtStam = StatCalculator.GetAttackStaminaReduction(nextLvl) * 100f;
                float diffStam = nxtStam - curStam;
                string diffStamStr = curStam <= 0f && nxtStam > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(-{diffStam:F0}%)</color>";

                DrawStatRow("Attack Stamina Cost:",
                    $"-{curStam:F0}%",
                    isMax ? "-" : $"-{nxtStam:F0}% {diffStamStr}");
            }

            // 2. Armor (Armor pieces)
            if (IsArmor(_selectedItem))
            {
                float curArmor = _selectedItem.GetArmor();
                float nextArmorBonus = StatCalculator.GetArmorBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_armor);
                float curArmorBonus = StatCalculator.GetArmorBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_armor);
                float diff = nextArmorBonus - curArmorBonus;
                string diffArmorStr = curArmorBonus <= 0f && nextArmorBonus > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diff:F1})</color>";

                DrawStatRow("Armor:",
                    $"{curArmor:F1}",
                    isMax ? "-" : $"{curArmor + diff:F1} {diffArmorStr}");

                float curMove = StatCalculator.GetMovementModifierDelta(_selectedItem, currentLvl) * 100f;
                float nxtMove = StatCalculator.GetMovementModifierDelta(_selectedItem, nextLvl) * 100f;
                float diffMove = nxtMove - curMove;
                string diffMoveStr = curMove <= 0f && nxtMove > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(+{diffMove:F1}%)</color>";

                DrawStatRow("Movement Speed Bonus:",
                    $"+{curMove:F1}%",
                    isMax ? "-" : $"+{nxtMove:F1}% {diffMoveStr}");
            }

            // 3. Shield
            if (_selectedItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                float curBlk = _selectedItem.GetBlockPower(Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking));
                float curBlkBonus = StatCalculator.GetBlockPowerBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_blockPower);
                float nxtBlkBonus = StatCalculator.GetBlockPowerBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_blockPower);
                float diffBlk = nxtBlkBonus - curBlkBonus;
                string diffBlkStr = curBlkBonus <= 0f && nxtBlkBonus > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffBlk:F1})</color>";

                DrawStatRow("Block Power:",
                    $"{curBlk:F1}",
                    isMax ? "-" : $"{curBlk + diffBlk:F1} {diffBlkStr}");

                float curBlkStam = StatCalculator.GetBlockStaminaReduction(currentLvl) * 100f;
                float nxtBlkStam = StatCalculator.GetBlockStaminaReduction(nextLvl) * 100f;
                float diffBlkStam = nxtBlkStam - curBlkStam;
                string diffBlkStamStr = curBlkStam <= 0f && nxtBlkStam > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(-{diffBlkStam:F1}%)</color>";

                DrawStatRow("Block Stamina Cost:",
                    $"-{curBlkStam:F0}%",
                    isMax ? "-" : $"-{nxtBlkStam:F0}% {diffBlkStamStr}");
            }

            // 4. Durability & Weight
            float curDur = _selectedItem.GetMaxDurability();
            float curDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_maxDurability);
            float nxtDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_maxDurability);
            float diffDur = nxtDurBonus - curDurBonus;
            string diffDurStr = curDurBonus <= 0f && nxtDurBonus > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffDur:F0})</color>";

            DrawStatRow("Max Durability:",
                $"{curDur:F0}",
                isMax ? "-" : $"{curDur + diffDur:F0} {diffDurStr}");

            // 5. Cheat Abilities (If enabled in config)
            if (StatCalculator.IsCheatEnabled)
            {
                float curHp = StatCalculator.GetCheatItemMaxHealth(_selectedItem, currentLvl);
                float nxtHp = StatCalculator.GetCheatItemMaxHealth(_selectedItem, nextLvl);
                if (curHp > 0f || nxtHp > 0f)
                {
                    float diffHp = nxtHp - curHp;
                    string diffHpStr = curHp <= 0f && nxtHp > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffHp:F0})</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Max HP:</color>",
                        $"+{curHp:F0}",
                        isMax ? "-" : $"+{nxtHp:F0} {diffHpStr}");
                }

                float curStam = StatCalculator.GetCheatItemMaxStamina(_selectedItem, currentLvl);
                float nxtStam = StatCalculator.GetCheatItemMaxStamina(_selectedItem, nextLvl);
                if (curStam > 0f || nxtStam > 0f)
                {
                    float diffStam = nxtStam - curStam;
                    string diffStamStr = curStam <= 0f && nxtStam > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffStam:F0})</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Max Stamina:</color>",
                        $"+{curStam:F0}",
                        isMax ? "-" : $"+{nxtStam:F0} {diffStamStr}");
                }

                float curEitr = StatCalculator.GetCheatItemMaxEitr(_selectedItem, currentLvl);
                float nxtEitr = StatCalculator.GetCheatItemMaxEitr(_selectedItem, nextLvl);
                if (curEitr > 0f || nxtEitr > 0f)
                {
                    float diffEitr = nxtEitr - curEitr;
                    string diffEitrStr = curEitr <= 0f && nxtEitr > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffEitr:F0})</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Max Eitr:</color>",
                        $"+{curEitr:F0}",
                        isMax ? "-" : $"+{nxtEitr:F0} {diffEitrStr}");
                }

                float curHpRegen = StatCalculator.GetCheatItemHealthRegen(_selectedItem, currentLvl) * 100f;
                float nxtHpRegen = StatCalculator.GetCheatItemHealthRegen(_selectedItem, nextLvl) * 100f;
                if (curHpRegen > 0f || nxtHpRegen > 0f)
                {
                    float diffHpRegen = nxtHpRegen - curHpRegen;
                    string diffHpRegenStr = curHpRegen <= 0f && nxtHpRegen > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffHpRegen:F0}%)</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] HP Regen:</color>",
                        $"+{curHpRegen:F0}%",
                        isMax ? "-" : $"+{nxtHpRegen:F0}% {diffHpRegenStr}");
                }

                float curStamRegen = StatCalculator.GetCheatItemStaminaRegen(_selectedItem, currentLvl) * 100f;
                float nxtStamRegen = StatCalculator.GetCheatItemStaminaRegen(_selectedItem, nextLvl) * 100f;
                if (curStamRegen > 0f || nxtStamRegen > 0f)
                {
                    float diffStamRegen = nxtStamRegen - curStamRegen;
                    string diffStamRegenStr = curStamRegen <= 0f && nxtStamRegen > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffStamRegen:F0}%)</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Stamina Regen:</color>",
                        $"+{curStamRegen:F0}%",
                        isMax ? "-" : $"+{nxtStamRegen:F0}% {diffStamRegenStr}");
                }

                float curEitrRegen = StatCalculator.GetCheatItemEitrRegen(_selectedItem, currentLvl) * 100f;
                float nxtEitrRegen = StatCalculator.GetCheatItemEitrRegen(_selectedItem, nextLvl) * 100f;
                if (curEitrRegen > 0f || nxtEitrRegen > 0f)
                {
                    float diffEitrRegen = nxtEitrRegen - curEitrRegen;
                    string diffEitrRegenStr = curEitrRegen <= 0f && nxtEitrRegen > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffEitrRegen:F0}%)</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Eitr Regen:</color>",
                        $"+{curEitrRegen:F0}%",
                        isMax ? "-" : $"+{nxtEitrRegen:F0}% {diffEitrRegenStr}");
                }

                float curHeal = StatCalculator.GetCheatItemHealingBonus(_selectedItem, currentLvl) * 100f;
                float nxtHeal = StatCalculator.GetCheatItemHealingBonus(_selectedItem, nextLvl) * 100f;
                if (curHeal > 0f || nxtHeal > 0f)
                {
                    float diffHeal = nxtHeal - curHeal;
                    string diffHealStr = curHeal <= 0f && nxtHeal > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffHeal:F0}%)</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Healing Bonus:</color>",
                        $"+{curHeal:F0}%",
                        isMax ? "-" : $"+{nxtHeal:F0}% {diffHealStr}");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawStatRow(string label, string currentVal, string nextVal)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _statLabelStyle, GUILayout.Width(230f));
            GUILayout.Label(currentVal, _statValueCurrentStyle, GUILayout.Width(130f));
            GUILayout.Label("➔", _statLabelStyle, GUILayout.Width(25f));
            GUILayout.Label(nextVal, _statValueNextStyle, GUILayout.Width(200f));
            GUILayout.EndHorizontal();
        }

        private void DrawRulesAndCostSection()
        {
            int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
            int nextLvl = currentLvl + 1;
            bool isMax = currentLvl >= ModConfig.MaxLevel;

            GUILayout.BeginVertical(_boxSectionStyle);

            if (isMax)
            {
                GUILayout.Label("<color=#ffd700><b>This item has reached maximum enhancement level (+20 MAX)</b></color>", _headerStyle);
                GUILayout.EndVertical();
                return;
            }

            float successRate = ModConfig.GetSuccessRate(nextLvl);
            string rateColor = successRate >= 70f ? "#4ade80" : (successRate >= 40f ? "#f59e0b" : "#ef4444");

            // Success Rate & Risk Bar
            GUILayout.BeginHorizontal();

            // Left: Success Chance
            GUILayout.BeginVertical(GUILayout.Width(280f));
            GUILayout.Label("<b>Success Rate:</b>", _statLabelStyle);
            GUILayout.Label($"<color={rateColor}><b>{successRate:F0}%</b></color>", _rateNumberStyle);
            GUILayout.EndVertical();

            // Right: Failure Consequence
            GUILayout.BeginVertical();
            GUILayout.Label("<b>Failure Consequence:</b>", _statLabelStyle);
            if (currentLvl < ModConfig.SafeLevel.Value)
            {
                GUILayout.Label("<color=#4ade80>✔ Safe - Level will not decrease and item will not break</color>", _statLabelStyle);
            }
            else
            {
                if (ModConfig.BreakOnFail.Value)
                {
                    GUILayout.Label($"<color=#ef4444>⚠ Risk: Downgrade by 1 level (+{currentLvl - 1}) or Break ({ModConfig.BreakChanceAboveSafeLevel.Value:F0}%)</color>", _statLabelStyle);
                }
                else if (ModConfig.DowngradeOnFail.Value)
                {
                    GUILayout.Label($"<color=#f59e0b>⚠ Risk: Downgrade by 1 level (to +{currentLvl - 1})</color>", _statLabelStyle);
                }
                else
                {
                    GUILayout.Label("<color=#4ade80>✔ Safe - Level remains unchanged</color>", _statLabelStyle);
                }
            }

            // Materials & Fee
            if (ModConfig.RequireScrolls.Value)
            {
                int scrollTier = EnhancementManager.GetRequiredScrollTier(nextLvl);
                string scrollName = ScrollItemManager.GetScrollDisplayName(scrollTier);
                int neededScrolls = ModConfig.ScrollsRequiredPerAttempt.Value;
                int playerScrolls = EnhancementManager.GetPlayerScrollCount(Player.m_localPlayer, scrollTier);
                bool hasScrolls = playerScrolls >= neededScrolls;
                string scrollColor = hasScrolls ? "#4ade80" : "#ef4444";
                string tierHex = EnhancementManager.GetTierHex(nextLvl);

                GUILayout.Label($"<b>Required Materials:</b> <color={tierHex}>{scrollName}</color> x{neededScrolls}", _statLabelStyle);
                GUILayout.Label($"<b>You Have:</b> <color={scrollColor}><b>{playerScrolls}</b></color>" + (hasScrolls ? "" : " <color=#ef4444>(Insufficient)</color>"), _statLabelStyle);
            }

            if (ModConfig.RequireCoins.Value)
            {
                int cost = ModConfig.GetCoinCost(nextLvl);
                int playerCoins = EnhancementManager.GetPlayerCoins(Player.m_localPlayer);
                bool canAffordCoins = playerCoins >= cost;
                string coinColor = canAffordCoins ? "#ffd700" : "#ef4444";
                GUILayout.Label($"<b>Required Coins:</b> <color={coinColor}>{cost} Coins</color> <color=#94a3b8>(You have: {playerCoins})</color>", _statLabelStyle);
            }
            else
            {
                GUILayout.Label("<b>Fee:</b> <color=#4ade80>Free (No coins required)</color>", _statLabelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawActionButtonSection()
        {
            int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
            int nextLvl = currentLvl + 1;
            bool isMax = currentLvl >= ModConfig.MaxLevel;
            int scrollTier = EnhancementManager.GetRequiredScrollTier(nextLvl);
            int neededScrolls = ModConfig.ScrollsRequiredPerAttempt.Value;
            int playerScrolls = EnhancementManager.GetPlayerScrollCount(Player.m_localPlayer, scrollTier);
            bool hasScrolls = !ModConfig.RequireScrolls.Value || (playerScrolls >= neededScrolls);
            int cost = ModConfig.GetCoinCost(nextLvl);
            bool hasCoins = !ModConfig.RequireCoins.Value || (EnhancementManager.GetPlayerCoins(Player.m_localPlayer) >= cost);

            GUILayout.BeginVertical();

            if (_isForging)
            {
                GUI.enabled = false;
                GUILayout.Button("🔨  FORGING...  🔨", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (isMax)
            {
                GUI.enabled = false;
                GUILayout.Button("★  MAX LEVEL REACHED (+20)  ★", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (!hasScrolls)
            {
                GUI.enabled = false;
                GUILayout.Button($"❌  Requires [Scroll Tier {scrollTier}] ({playerScrolls}/{neededScrolls})  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (!hasCoins)
            {
                GUI.enabled = false;
                GUILayout.Button($"❌  Insufficient Coins (Requires {cost} Coins)  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("⚡  ENHANCE ITEM  ⚡", _actionButtonStyle, GUILayout.Height(48f)))
                {
                    StartForging();
                }
            }

            // Result Message Banner
            if (!string.IsNullOrEmpty(_lastResultMessage))
            {
                GUILayout.Space(6f);
                GUILayout.Label($"<color={_lastResultColor}><b>{_lastResultMessage}</b></color>", _headerStyle);
            }

            GUILayout.EndVertical();
        }

        private void StartForging()
        {
            if (_selectedItem == null || Player.m_localPlayer == null) return;

            Inventory playerInv = Player.m_localPlayer.GetInventory();
            Inventory containerInv = (InventoryGui.instance != null && InventoryGui.instance.IsContainerOpen())
                ? InventoryGui.instance.ContainerGrid?.GetInventory()
                : null;
            bool itemExists = (playerInv != null && playerInv.ContainsItem(_selectedItem))
                           || (containerInv != null && containerInv.ContainsItem(_selectedItem));

            if (!itemExists)
            {
                _selectedItem = null;
                _lastResultColor = "#ef4444";
                _lastResultMessage = "Item not found in inventory!";
                return;
            }

            _isForging = true;
            _forgeTimer = ForgeDuration;
            _lastResultMessage = "";

            // Play hammer audio and spark effect
            if (InventoryGui.instance != null && InventoryGui.instance.m_craftItemEffects != null)
            {
                InventoryGui.instance.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
            }
        }

        private void ExecuteEnhancement()
        {
            if (_selectedItem == null || Player.m_localPlayer == null) return;

            Inventory playerInv = Player.m_localPlayer.GetInventory();
            Inventory containerInv = (InventoryGui.instance != null && InventoryGui.instance.IsContainerOpen())
                ? InventoryGui.instance.ContainerGrid?.GetInventory()
                : null;
            bool itemExists = (playerInv != null && playerInv.ContainsItem(_selectedItem))
                           || (containerInv != null && containerInv.ContainsItem(_selectedItem));

            if (!itemExists)
            {
                _selectedItem = null;
                _lastResultColor = "#ef4444";
                _lastResultMessage = "Item not found in inventory!";
                return;
            }

            string itemName = Localization.instance != null ? Localization.instance.Localize(_selectedItem.m_shared.m_name) : _selectedItem.m_shared.m_name;
            EnhanceResult result = EnhancementManager.TryEnhance(Player.m_localPlayer, _selectedItem, out int newLevel);

            switch (result)
            {
                case EnhanceResult.Success:
                    if (InventoryGui.instance?.m_craftItemDoneEffects != null)
                    {
                        InventoryGui.instance.m_craftItemDoneEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                    }
                    _lastResultColor = "#ffd700";
                    _lastResultMessage = $"★ Enhancement Successful! {itemName} is now +{newLevel} ★";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#ffd700>★ Enhancement Successful! {itemName} (+{newLevel}) ★</color>");
                    break;

                case EnhanceResult.FailedSafe:
                    _lastResultColor = "#94a3b8";
                    _lastResultMessage = $"✖ Enhancement Failed! Safe: Level remained unchanged (+{newLevel}) ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#94a3b8>✖ Enhancement Failed! Safe (+{newLevel}) ✖</color>");
                    break;

                case EnhanceResult.FailedDowngraded:
                    _lastResultColor = "#f59e0b";
                    _lastResultMessage = $"✖ Enhancement Failed! Item downgraded to +{newLevel} ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#f59e0b>✖ Enhancement Failed! Downgraded to +{newLevel} ✖</color>");
                    break;

                case EnhanceResult.FailedBroken:
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = $"✖ Enhancement Failed! {itemName} was destroyed! ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#ef4444>✖ Enhancement Failed! {itemName} destroyed! ✖</color>");
                    _selectedItem = null;
                    break;

                case EnhanceResult.CannotAfford:
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = "Insufficient enhancement materials!";
                    break;
            }

            _isEnhanceablesDirty = true;

            // Sync Inventory
            if (InventoryGui.instance != null)
            {
                HarmonyLib.AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel")?.Invoke(InventoryGui.instance, new object[] { false });
            }
        }

        private void RefreshEnhanceablesList()
        {
            _cachedEnhanceableItems.Clear();
            if (Player.m_localPlayer != null && Player.m_localPlayer.GetInventory() != null)
            {
                List<ItemDrop.ItemData> allItems = Player.m_localPlayer.GetInventory().GetAllItems();
                if (allItems != null)
                {
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        ItemDrop.ItemData item = allItems[i];
                        if (item != null && EnhancementManager.IsEnhanceable(item))
                        {
                            _cachedEnhanceableItems.Add(item);
                        }
                    }
                }
            }
            _isEnhanceablesDirty = false;
        }

        private void DrawEquipmentPickerList()
        {
            GUILayout.BeginVertical(_boxSectionStyle);
            GUILayout.Label("<b>Enhanceable Equipment:</b>", _statLabelStyle);
            GUILayout.Space(4f);

            if (_isEnhanceablesDirty)
            {
                RefreshEnhanceablesList();
            }

            if (_cachedEnhanceableItems.Count == 0)
            {
                GUILayout.Label("<color=#94a3b8>No enhanceable equipment found in your inventory.</color>", _statLabelStyle);
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300f));

                for (int i = 0; i < _cachedEnhanceableItems.Count; i++)
                {
                    ItemDrop.ItemData item = _cachedEnhanceableItems[i];
                    if (item == null) continue;

                    int lvl = EnhancementManager.GetEnhancementLevel(item);
                    string hex = EnhancementManager.GetTierHex(lvl);
                    string localizedName = Localization.instance != null ? Localization.instance.Localize(item.m_shared.m_name) : item.m_shared.m_name;

                    GUILayout.BeginHorizontal();
                    string equippedTag = item.m_equipped ? " <color=#38bdf8>[Equipped]</color>" : "";
                    GUILayout.Label($"<b>{localizedName}</b> <color={hex}>+{lvl}</color>{equippedTag}", _statLabelStyle, GUILayout.Width(450f));

                    if (GUILayout.Button("Select", _itemButtonStyle, GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        _selectedItem = item;
                        _lastResultMessage = "";
                        _isEnhanceablesDirty = true;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private bool IsWeapon(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        }

        private bool IsArmor(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder;
        }

        private void InitStyles()
        {
            if (_windowStyle != null) return;

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.normal.background = _texWindowBg;
            _windowStyle.padding = new RectOffset(16, 16, 14, 16);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 17;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            _subHeaderStyle = new GUIStyle(GUI.skin.label);
            _subHeaderStyle.fontSize = 14;
            _subHeaderStyle.normal.textColor = Color.white;

            _statLabelStyle = new GUIStyle(GUI.skin.label);
            _statLabelStyle.fontSize = 13;
            _statLabelStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);

            _statValueCurrentStyle = new GUIStyle(GUI.skin.label);
            _statValueCurrentStyle.fontSize = 13;
            _statValueCurrentStyle.normal.textColor = new Color(0.7f, 0.75f, 0.8f);

            _statValueNextStyle = new GUIStyle(GUI.skin.label);
            _statValueNextStyle.fontSize = 13;
            _statValueNextStyle.fontStyle = FontStyle.Bold;
            _statValueNextStyle.normal.textColor = new Color(0.3f, 0.9f, 0.5f);

            _boxSectionStyle = new GUIStyle(GUI.skin.box);
            _boxSectionStyle.normal.background = _texBoxBg;
            _boxSectionStyle.padding = new RectOffset(12, 12, 10, 10);

            _actionButtonStyle = new GUIStyle(GUI.skin.button);
            _actionButtonStyle.normal.background = _texButtonNormal;
            _actionButtonStyle.hover.background = _texButtonHover;
            _actionButtonStyle.active.background = _texButtonActive;
            _actionButtonStyle.fontSize = 16;
            _actionButtonStyle.fontStyle = FontStyle.Bold;
            _actionButtonStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);
            _actionButtonStyle.hover.textColor = Color.white;

            _itemButtonStyle = new GUIStyle(GUI.skin.button);
            _itemButtonStyle.fontSize = 12;

            _rateNumberStyle = new GUIStyle(GUI.skin.label);
            _rateNumberStyle.fontSize = 28;
            _rateNumberStyle.fontStyle = FontStyle.Bold;
            _rateNumberStyle.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateTextures()
        {
            _texWindowBg = MakeBorderedTexture(16, 16, new Color(0.10f, 0.11f, 0.14f, 0.96f), new Color(0.70f, 0.55f, 0.20f, 1f));
            _texBoxBg = MakeBorderedTexture(16, 16, new Color(0.15f, 0.17f, 0.22f, 0.90f), new Color(0.28f, 0.32f, 0.40f, 0.8f));
            _texButtonNormal = MakeBorderedTexture(16, 16, new Color(0.25f, 0.18f, 0.08f, 1f), new Color(0.85f, 0.65f, 0.15f, 1f));
            _texButtonHover = MakeBorderedTexture(16, 16, new Color(0.40f, 0.28f, 0.10f, 1f), new Color(1.00f, 0.85f, 0.30f, 1f));
            _texButtonActive = MakeBorderedTexture(16, 16, new Color(0.55f, 0.38f, 0.12f, 1f), new Color(1.00f, 0.95f, 0.50f, 1f));
            _texButtonDisabled = MakeBorderedTexture(16, 16, new Color(0.18f, 0.18f, 0.20f, 0.8f), new Color(0.30f, 0.30f, 0.35f, 0.6f));
        }

        private Texture2D MakeBorderedTexture(int width, int height, Color background, Color border)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        pixels[y * width + x] = border;
                    }
                    else
                    {
                        pixels[y * width + x] = background;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
