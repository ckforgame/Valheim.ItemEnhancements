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

        private Rect _windowRect = new Rect(Screen.width / 2f - 340f, Screen.height / 2f - 335f, 680f, 670f);
        private ItemDrop.ItemData _selectedItem;
        private Vector2 _scrollPosition = Vector2.zero;

        private readonly List<ItemDrop.ItemData> _cachedEnhanceableItems = new List<ItemDrop.ItemData>();
        private bool _isEnhanceablesDirty = true;
        private float _stationCheckTimer = 0f;

        // Category filter in picker list (0: All, 1: Weapons, 2: Armor, 3: Shields, 4: Accessories)
        private int _pickerCategory = 0;

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
        private GUIStyle _statArrowStyle;
        private GUIStyle _boxSectionStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _itemButtonStyle;
        private GUIStyle _rateNumberStyle;
        private GUIStyle _statusBoxStyle;
        private GUIStyle _statusTextStyle;
        private GUIStyle _meterTextStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _tabButtonActiveStyle;

        private Texture2D _texWindowBg;
        private Texture2D _texBoxBg;
        private Texture2D _texButtonNormal;
        private Texture2D _texButtonHover;
        private Texture2D _texButtonActive;
        private Texture2D _texButtonDisabled;
        private Texture2D _texScrollbarTrack;
        private Texture2D _texScrollbarThumb;
        private Texture2D _texScrollbarThumbHover;
        private Texture2D _texStatusBg;
        private Texture2D _texMeterBg;
        private Texture2D _texWhite;
        private Texture2D _texTabNormal;
        private Texture2D _texTabActive;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTextures();
        }

        private void UpdateWindowDimensions()
        {
            float maxWindowHeight = Mathf.Min(Screen.height - 40f, 760f);
            float winHeight = Mathf.Clamp(670f, 400f, maxWindowHeight);
            float winWidth = Mathf.Min(Screen.width - 40f, 680f);

            _windowRect.width = winWidth;
            _windowRect.height = winHeight;

            float maxX = Mathf.Max(10f, Screen.width - _windowRect.width - 10f);
            float maxY = Mathf.Max(10f, Screen.height - _windowRect.height - 10f);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, maxX);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, maxY);
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
                Instance._scrollPosition = Vector2.zero;
                Instance.UpdateWindowDimensions();
                // Auto-select equipped item if none selected
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
                if (Instance._selectedItem == item) return;
                Instance._selectedItem = item;
                Instance._lastResultMessage = "";
                Instance._isEnhanceablesDirty = true;
                Instance._scrollPosition = Vector2.zero;
            }
        }

        private bool CanEnhanceSelectedItem()
        {
            if (_selectedItem == null || Player.m_localPlayer == null) return false;
            if (ModConfig.RequireCraftingStation.Value && !IsNearCraftingStation()) return false;
            int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
            if (currentLvl >= ModConfig.MaxLevel) return false;
            int nextLvl = currentLvl + 1;
            return EnhancementManager.CanAfford(Player.m_localPlayer, nextLvl);
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

            // Keyboard shortcut to enhance: Space or Enter
            if (!_isForging && _selectedItem != null && CanEnhanceSelectedItem())
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    StartForging();
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
            UpdateWindowDimensions();

            _windowRect = GUI.Window(987654, _windowRect, DrawWindowContent, "", _windowStyle);
        }

        private void DrawWindowContent(int windowID)
        {
            // Handle mouse wheel scrolling anywhere inside the window
            if (Event.current.type == EventType.ScrollWheel)
            {
                _scrollPosition.y += Event.current.delta.y * 35f;
                if (_scrollPosition.y < 0f) _scrollPosition.y = 0f;
                Event.current.Use();
            }

            // Title Bar with clean Close Button (Exempt from drag window)
            GUILayout.BeginHorizontal();
            GUILayout.Label("✦  MMORPG ITEM ENHANCEMENT  ✦", _headerStyle);
            if (GUILayout.Button("✕", GUILayout.Width(32f), GUILayout.Height(26f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Item Selection Area Header
            DrawItemSelectionArea();

            GUILayout.Space(6f);

            if (_selectedItem != null && EnhancementManager.IsEnhanceable(_selectedItem))
            {
                // Scrollable Area: Stats Comparison + Rules & Cost
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                DrawStatsComparison();

                GUILayout.Space(8f);

                DrawRulesAndCostSection();

                GUILayout.EndScrollView();

                GUILayout.Space(8f);

                // Zero-Jitter Action & Status Panel (strictly fixed height)
                DrawActionButtonSection();
            }
            else
            {
                // List available equipment with category tabs
                DrawEquipmentPickerList();
            }

            // Drag window region leaves 45px margin on the right so close button click is never intercepted
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 45f, 35f));
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
                string equippedTag = _selectedItem.m_equipped ? " <color=#38bdf8>[Equipped]</color>" : "";
                GUILayout.Label($"<b>Target:</b> <color={hex}><b>{localizedName} +{currentLvl}</b></color> <color=#94a3b8>({rank})</color>{equippedTag}", _subHeaderStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Change Item", GUILayout.Width(130f), GUILayout.Height(26f)))
                {
                    _selectedItem = null;
                    _lastResultMessage = "";
                    _scrollPosition = Vector2.zero;
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=#ffd700>Select an item to enhance from the list below or click an item in your inventory.</color>", _subHeaderStyle);
                GUILayout.EndHorizontal();
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
            GUILayout.Label($"Current: <color={EnhancementManager.GetTierHex(currentLvl)}><b>+{currentLvl}</b></color>", _statValueCurrentStyle, GUILayout.Width(225f));
            GUILayout.Label("➔", _statArrowStyle, GUILayout.Width(25f));
            if (!isMax)
            {
                GUILayout.Label($"Next: <color={EnhancementManager.GetTierHex(nextLvl)}><b>+{nextLvl}</b></color>", _statValueNextStyle, GUILayout.Width(210f));
            }
            else
            {
                GUILayout.Label("<color=#ffd700><b>★ MAX LEVEL ★</b></color>", _statValueNextStyle, GUILayout.Width(210f));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 1. Weapons & Magic Staves
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

                // Attack Eitr Cost (Staves)
                float curEitr = StatCalculator.GetAttackEitrReduction(currentLvl) * 100f;
                float nxtEitr = StatCalculator.GetAttackEitrReduction(nextLvl) * 100f;
                if (curEitr > 0f || nxtEitr > 0f || (_selectedItem.m_shared.m_attack != null && _selectedItem.m_shared.m_attack.m_attackEitr > 0f))
                {
                    float diffEitr = nxtEitr - curEitr;
                    string diffEitrStr = curEitr <= 0f && nxtEitr > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(-{diffEitr:F0}%)</color>";
                    DrawStatRow("Attack Eitr Cost:",
                        $"-{curEitr:F0}%",
                        isMax ? "-" : $"-{nxtEitr:F0}% {diffEitrStr}");
                }

                // Backstab Bonus (Knives / Daggers)
                float curBackstab = StatCalculator.GetBackstabBonus(currentLvl);
                float nxtBackstab = StatCalculator.GetBackstabBonus(nextLvl);
                if (curBackstab > 0f || nxtBackstab > 0f || _selectedItem.m_shared.m_backstabBonus > 1f)
                {
                    float diffBackstab = nxtBackstab - curBackstab;
                    string diffBackstabStr = curBackstab <= 0f && nxtBackstab > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffBackstab:F2}x)</color>";
                    DrawStatRow("Backstab Multiplier:",
                        $"+{curBackstab:F2}x",
                        isMax ? "-" : $"+{nxtBackstab:F2}x {diffBackstabStr}");
                }
            }

            // 2. Armor pieces
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

                // Eitr Regen (Robes)
                float curEitrRegen = StatCalculator.GetEitrRegenBonus(currentLvl) * 100f;
                float nxtEitrRegen = StatCalculator.GetEitrRegenBonus(nextLvl) * 100f;
                if (curEitrRegen > 0f || nxtEitrRegen > 0f || _selectedItem.m_shared.m_eitrRegenModifier > 0f)
                {
                    float diffEitrRegen = nxtEitrRegen - curEitrRegen;
                    string diffEitrRegenStr = curEitrRegen <= 0f && nxtEitrRegen > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(+{diffEitrRegen:F0}%)</color>";
                    DrawStatRow("Eitr Regeneration:",
                        $"+{curEitrRegen:F0}%",
                        isMax ? "-" : $"+{nxtEitrRegen:F0}% {diffEitrRegenStr}");
                }
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

                float curParry = StatCalculator.GetTimedBlockBonus(currentLvl);
                float nxtParry = StatCalculator.GetTimedBlockBonus(nextLvl);
                if (curParry > 0f || nxtParry > 0f)
                {
                    float diffParry = nxtParry - curParry;
                    string diffParryStr = curParry <= 0f && nxtParry > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffParry:F2}x)</color>";
                    DrawStatRow("Parry Bonus:",
                        $"+{curParry:F2}x",
                        isMax ? "-" : $"+{nxtParry:F2}x {diffParryStr}");
                }
            }

            // 4. Utility Accessories (e.g. Megingjord belt)
            if (_selectedItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                float curCarry = StatCalculator.GetCarryWeightBonus(_selectedItem, currentLvl);
                float nxtCarry = StatCalculator.GetCarryWeightBonus(_selectedItem, nextLvl);
                if (curCarry > 0f || nxtCarry > 0f)
                {
                    float diffCarry = nxtCarry - curCarry;
                    string diffCarryStr = curCarry <= 0f && nxtCarry > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffCarry:F0})</color>";
                    DrawStatRow("Max Carry Weight:",
                        $"+{curCarry:F0}",
                        isMax ? "-" : $"+{nxtCarry:F0} {diffCarryStr}");
                }
            }

            // 5. Durability & Weight (All equipment)
            float curDur = _selectedItem.GetMaxDurability();
            float curDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_maxDurability);
            float nxtDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_maxDurability);
            float diffDur = nxtDurBonus - curDurBonus;
            string diffDurStr = curDurBonus <= 0f && nxtDurBonus > 0f ? "<color=#4ade80>(Unlocked!)</color>" : $"<color=#4ade80>(+{diffDur:F0})</color>";

            DrawStatRow("Max Durability:",
                $"{curDur:F0}",
                isMax ? "-" : $"{curDur + diffDur:F0} {diffDurStr}");

            float curWeightRed = (1f - StatCalculator.GetWeightMultiplier(currentLvl)) * 100f;
            float nxtWeightRed = (1f - StatCalculator.GetWeightMultiplier(nextLvl)) * 100f;
            if (curWeightRed > 0f || nxtWeightRed > 0f)
            {
                float diffWeight = nxtWeightRed - curWeightRed;
                string diffWeightStr = curWeightRed <= 0f && nxtWeightRed > 0f ? "<color=#38bdf8>(Unlocked!)</color>" : $"<color=#38bdf8>(-{diffWeight:F0}%)</color>";
                DrawStatRow("Weight Reduction:",
                    $"-{curWeightRed:F0}%",
                    isMax ? "-" : $"-{nxtWeightRed:F0}% {diffWeightStr}");
            }

            // 6. Cheat Abilities (If enabled in config)
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

                float curEitrCheat = StatCalculator.GetCheatItemMaxEitr(_selectedItem, currentLvl);
                float nxtEitrCheat = StatCalculator.GetCheatItemMaxEitr(_selectedItem, nextLvl);
                if (curEitrCheat > 0f || nxtEitrCheat > 0f)
                {
                    float diffEitrCheat = nxtEitrCheat - curEitrCheat;
                    string diffEitrCheatStr = curEitrCheat <= 0f && nxtEitrCheat > 0f ? "<color=#fb7185>(Unlocked!)</color>" : $"<color=#fb7185>(+{diffEitrCheat:F0})</color>";
                    DrawStatRow("<color=#fb7185>★ [Cheat] Max Eitr:</color>",
                        $"+{curEitrCheat:F0}",
                        isMax ? "-" : $"+{nxtEitrCheat:F0} {diffEitrCheatStr}");
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
            }

            GUILayout.EndVertical();
        }

        private void DrawStatRow(string label, string currentVal, string nextVal)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _statLabelStyle, GUILayout.Width(225f));
            GUILayout.Label(currentVal, _statValueCurrentStyle, GUILayout.Width(130f));
            GUILayout.Label("➔", _statArrowStyle, GUILayout.Width(25f));
            GUILayout.Label(nextVal, _statValueNextStyle, GUILayout.Width(210f));
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
                GUILayout.Label("<color=#ffd700><b>★ This item has reached maximum enhancement level (+20 MAX) ★</b></color>", _headerStyle);
                GUILayout.EndVertical();
                return;
            }

            float successRate = ModConfig.GetSuccessRate(nextLvl);
            Color rateBarColor = successRate >= 70f ? new Color(0.29f, 0.87f, 0.50f) : (successRate >= 40f ? new Color(0.96f, 0.62f, 0.04f) : new Color(0.94f, 0.27f, 0.27f));
            string rateColor = successRate >= 70f ? "#4ade80" : (successRate >= 40f ? "#f59e0b" : "#ef4444");

            // Success Rate & Risk
            GUILayout.BeginHorizontal();

            // Left Column: Success Chance with Visual Meter
            GUILayout.BeginVertical(GUILayout.Width(270f));
            GUILayout.Label("<b>Success Probability:</b>", _statLabelStyle);
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color={rateColor}><b>{successRate:F0}%</b></color>", _rateNumberStyle, GUILayout.Width(75f));
            GUILayout.BeginVertical();
            GUILayout.Space(8f);
            DrawProgressBar(successRate / 100f, rateBarColor, "", 12f);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(12f);

            // Right Column: Failure Consequence
            GUILayout.BeginVertical();
            GUILayout.Label("<b>Failure Penalty:</b>", _statLabelStyle);
            GUILayout.Space(2f);
            if (currentLvl < ModConfig.SafeLevel.Value)
            {
                GUILayout.Label("<color=#4ade80><b>🛡 100% Protected (Safe Level)</b></color>", _statLabelStyle);
                GUILayout.Label("<color=#94a3b8>Level will not decrease. Item cannot break.</color>", _statLabelStyle);
            }
            else
            {
                if (ModConfig.BreakOnFail.Value)
                {
                    GUILayout.Label($"<color=#ef4444><b>⚠ High Risk: Downgrade or Destroy</b></color>", _statLabelStyle);
                    GUILayout.Label($"<color=#f87171>Break Chance: {ModConfig.BreakChanceAboveSafeLevel.Value:F0}%  •  Otherwise: -1 Level</color>", _statLabelStyle);
                }
                else if (ModConfig.DowngradeOnFail.Value)
                {
                    GUILayout.Label("<color=#f59e0b><b>⚠ Moderate Risk: Downgrade on Fail</b></color>", _statLabelStyle);
                    GUILayout.Label($"<color=#fbbf24>Level will decrease: +{currentLvl} ➔ +{currentLvl - 1}</color>", _statLabelStyle);
                }
                else
                {
                    GUILayout.Label("<color=#4ade80><b>✔ Safe - Level remains unchanged</b></color>", _statLabelStyle);
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Requirements & Costs Cards
            GUILayout.BeginHorizontal();

            // Scroll card
            GUILayout.BeginVertical(_boxSectionStyle, GUILayout.ExpandWidth(true));
            if (ModConfig.RequireScrolls.Value)
            {
                int scrollTier = EnhancementManager.GetRequiredScrollTier(nextLvl);
                string scrollName = ScrollItemManager.GetScrollDisplayName(scrollTier);
                int neededScrolls = ModConfig.ScrollsRequiredPerAttempt.Value;
                int playerScrolls = EnhancementManager.GetPlayerScrollCount(Player.m_localPlayer, scrollTier);
                bool hasScrolls = playerScrolls >= neededScrolls;
                string scrollColor = hasScrolls ? "#4ade80" : "#ef4444";
                string tierHex = EnhancementManager.GetTierHex(nextLvl);

                GUILayout.Label($"<b>Required Scroll:</b> <color={tierHex}>{scrollName}</color>", _statLabelStyle);
                string missingText = hasScrolls ? "<color=#4ade80>✔ Ready</color>" : $"<color=#ef4444>(Need {neededScrolls - playerScrolls} more)</color>";
                GUILayout.Label($"<b>Inventory:</b> <color={scrollColor}><b>{playerScrolls}</b> / {neededScrolls}</color>  {missingText}", _statLabelStyle);
            }
            else
            {
                GUILayout.Label("<b>Required Scroll:</b> <color=#4ade80>None (Free)</color>", _statLabelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // Coin card
            GUILayout.BeginVertical(_boxSectionStyle, GUILayout.ExpandWidth(true));
            if (ModConfig.RequireCoins.Value)
            {
                int cost = ModConfig.GetCoinCost(nextLvl);
                int playerCoins = EnhancementManager.GetPlayerCoins(Player.m_localPlayer);
                bool canAffordCoins = playerCoins >= cost;
                string coinColor = canAffordCoins ? "#ffd700" : "#ef4444";
                string coinStatus = canAffordCoins ? "<color=#4ade80>✔ Ready</color>" : $"<color=#ef4444>(Need {cost - playerCoins} more)</color>";

                GUILayout.Label($"<b>Enhancement Fee:</b> <color={coinColor}>{cost} Coins</color>", _statLabelStyle);
                GUILayout.Label($"<b>Inventory:</b> <color={coinColor}><b>{playerCoins}</b> / {cost}</color>  {coinStatus}", _statLabelStyle);
            }
            else
            {
                GUILayout.Label("<b>Enhancement Fee:</b> <color=#4ade80>Free (No coins)</color>", _statLabelStyle);
                GUILayout.Label("<b>Status:</b> <color=#4ade80>✔ Ready</color>", _statLabelStyle);
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
            int playerCoins = EnhancementManager.GetPlayerCoins(Player.m_localPlayer);
            bool hasCoins = !ModConfig.RequireCoins.Value || (playerCoins >= cost);

            // STRICTLY FIXED HEIGHT CONTAINER (98px)
            // Ensures the Action Button NEVER shifts position or bounces during enhancement cycles!
            GUILayout.BeginVertical(GUILayout.Height(98f));

            // 1. Status & Result Card (Fixed Height: 40px)
            DrawStatusCard(currentLvl, nextLvl, isMax, hasScrolls, scrollTier, playerScrolls, neededScrolls, hasCoins, cost, playerCoins);

            GUILayout.Space(6f);

            // 2. Action Button (Fixed Height: 48px)
            DrawEnhanceButton(nextLvl, isMax, hasScrolls, scrollTier, playerScrolls, neededScrolls, hasCoins, cost, playerCoins);

            GUILayout.EndVertical();
        }

        private void DrawStatusCard(int currentLvl, int nextLvl, bool isMax, bool hasScrolls, int scrollTier, int playerScrolls, int neededScrolls, bool hasCoins, int cost, int playerCoins)
        {
            GUILayout.BeginVertical(_statusBoxStyle, GUILayout.Height(40f));

            if (_isForging)
            {
                float forgeProgress = Mathf.Clamp01(1f - (_forgeTimer / ForgeDuration));
                int pct = Mathf.RoundToInt(forgeProgress * 100f);
                GUILayout.Label($"<color=#ffd700><b>🔨  Forging on Anvil... ({pct}%)  🔨</b></color>", _statusTextStyle);
                GUILayout.Space(2f);
                DrawProgressBar(forgeProgress, new Color(1f, 0.85f, 0.25f, 0.9f), "", 6f);
            }
            else if (!string.IsNullOrEmpty(_lastResultMessage))
            {
                GUILayout.Label($"<color={_lastResultColor}><b>{_lastResultMessage}</b></color>", _statusTextStyle);
            }
            else
            {
                // Contextual readiness hints
                if (isMax)
                {
                    GUILayout.Label("<color=#ffd700><b>★ Item has reached maximum enhancement level (+20 DIVINE) ★</b></color>", _statusTextStyle);
                }
                else if (!hasScrolls)
                {
                    int missing = neededScrolls - playerScrolls;
                    string scrollName = ScrollItemManager.GetScrollDisplayName(scrollTier);
                    GUILayout.Label($"<color=#f87171><b>❌ Missing Materials: Need {missing} more {scrollName}</b></color>", _statusTextStyle);
                }
                else if (!hasCoins)
                {
                    int missing = cost - playerCoins;
                    GUILayout.Label($"<color=#fbbf24><b>❌ Insufficient Coins: Need {missing} more coins</b></color>", _statusTextStyle);
                }
                else
                {
                    GUILayout.Label($"<color=#38bdf8><b>⚡ Ready to enhance to +{nextLvl}  •  Click below or press [Space / Enter]</b></color>", _statusTextStyle);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawEnhanceButton(int nextLvl, bool isMax, bool hasScrolls, int scrollTier, int playerScrolls, int neededScrolls, bool hasCoins, int cost, int playerCoins)
        {
            if (_isForging)
            {
                GUI.enabled = false;
                float forgeProgress = Mathf.Clamp01(1f - (_forgeTimer / ForgeDuration));
                int pct = Mathf.RoundToInt(forgeProgress * 100f);
                GUILayout.Button($"🔨  FORGING... ({pct}%)  🔨", _actionButtonStyle, GUILayout.Height(48f));
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
                string scrollName = ScrollItemManager.GetScrollDisplayName(scrollTier);
                GUILayout.Button($"❌  Requires [{scrollName}] ({playerScrolls}/{neededScrolls})  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (!hasCoins)
            {
                GUI.enabled = false;
                GUILayout.Button($"❌  Insufficient Coins (Requires {cost}, Have {playerCoins})  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button($"⚡  ENHANCE TO +{nextLvl}  ⚡", _actionButtonStyle, GUILayout.Height(48f)))
                {
                    StartForging();
                }
            }
        }

        private void DrawProgressBar(float progress, Color fillColor, string overlayText = "", float height = 14f)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, _windowRect.width, height, height, GUILayout.ExpandWidth(true));

            // Background track
            if (_texMeterBg != null)
            {
                GUI.DrawTexture(rect, _texMeterBg);
            }

            // Fill
            float clampedProgress = Mathf.Clamp01(progress);
            if (clampedProgress > 0.001f && _texWhite != null)
            {
                Rect fillRect = new Rect(rect.x + 1f, rect.y + 1f, (rect.width - 2f) * clampedProgress, rect.height - 2f);
                Color oldColor = GUI.color;
                GUI.color = fillColor;
                GUI.DrawTexture(fillRect, _texWhite);
                GUI.color = oldColor;
            }

            // Overlay text
            if (!string.IsNullOrEmpty(overlayText) && _meterTextStyle != null)
            {
                GUI.Label(rect, overlayText, _meterTextStyle);
            }
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
                    Player.m_localPlayer.GetCurrentCraftingStation()?.m_repairItemDoneEffects?.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                    _lastResultColor = "#94a3b8";
                    _lastResultMessage = $"🛡 Safe Attempt: Level remained unchanged (+{newLevel})";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#94a3b8>🛡 Safe: Level Remained (+{newLevel})</color>");
                    break;

                case EnhanceResult.FailedDowngraded:
                    Player.m_localPlayer.GetCurrentCraftingStation()?.m_repairItemDoneEffects?.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                    _lastResultColor = "#f59e0b";
                    _lastResultMessage = $"⚠ Enhancement Failed! Item downgraded to +{newLevel}";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#f59e0b>⚠ Enhancement Failed! Downgraded to +{newLevel}</color>");
                    break;

                case EnhanceResult.FailedBroken:
                    Player.m_localPlayer.GetCurrentCraftingStation()?.m_repairItemDoneEffects?.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = $"☠ Enhancement Failed! {itemName} was destroyed! ☠";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#ef4444>☠ Enhancement Failed! {itemName} destroyed! ☠</color>");
                    _selectedItem = null;
                    break;

                case EnhanceResult.CannotAfford:
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = "Insufficient enhancement materials or coins!";
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

                    // Sort: Equipped first, then higher enhancement level, then alphabetical
                    _cachedEnhanceableItems.Sort((a, b) =>
                    {
                        if (a.m_equipped != b.m_equipped) return b.m_equipped.CompareTo(a.m_equipped);
                        int lvlA = EnhancementManager.GetEnhancementLevel(a);
                        int lvlB = EnhancementManager.GetEnhancementLevel(b);
                        if (lvlA != lvlB) return lvlB.CompareTo(lvlA);
                        string nameA = Localization.instance != null ? Localization.instance.Localize(a.m_shared.m_name) : a.m_shared.m_name;
                        string nameB = Localization.instance != null ? Localization.instance.Localize(b.m_shared.m_name) : b.m_shared.m_name;
                        return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                    });
                }
            }
            _isEnhanceablesDirty = false;
        }

        private bool MatchesCategory(ItemDrop.ItemData item, int category)
        {
            if (item?.m_shared == null) return false;
            switch (category)
            {
                case 1: return IsWeapon(item);
                case 2: return IsArmor(item);
                case 3: return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
                case 4: return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
                default: return true; // All
            }
        }

        private void DrawEquipmentPickerList()
        {
            GUILayout.BeginVertical(_boxSectionStyle, GUILayout.ExpandHeight(true));

            if (_isEnhanceablesDirty)
            {
                RefreshEnhanceablesList();
            }

            // Category Counts
            int countAll = _cachedEnhanceableItems.Count;
            int countWeapons = 0;
            int countArmor = 0;
            int countShields = 0;
            int countAcc = 0;
            for (int i = 0; i < _cachedEnhanceableItems.Count; i++)
            {
                var itm = _cachedEnhanceableItems[i];
                if (IsWeapon(itm)) countWeapons++;
                else if (IsArmor(itm)) countArmor++;
                else if (itm.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield) countShields++;
                else if (itm.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility) countAcc++;
            }

            // Category Filter Tabs
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"All ({countAll})", _pickerCategory == 0 ? _tabButtonActiveStyle : _tabButtonStyle, GUILayout.Height(28f)))
            {
                _pickerCategory = 0;
                _scrollPosition = Vector2.zero;
            }
            if (GUILayout.Button($"⚔ Weapons ({countWeapons})", _pickerCategory == 1 ? _tabButtonActiveStyle : _tabButtonStyle, GUILayout.Height(28f)))
            {
                _pickerCategory = 1;
                _scrollPosition = Vector2.zero;
            }
            if (GUILayout.Button($"🛡 Armor ({countArmor})", _pickerCategory == 2 ? _tabButtonActiveStyle : _tabButtonStyle, GUILayout.Height(28f)))
            {
                _pickerCategory = 2;
                _scrollPosition = Vector2.zero;
            }
            if (GUILayout.Button($"🔰 Shields ({countShields})", _pickerCategory == 3 ? _tabButtonActiveStyle : _tabButtonStyle, GUILayout.Height(28f)))
            {
                _pickerCategory = 3;
                _scrollPosition = Vector2.zero;
            }
            if (GUILayout.Button($"💍 Utility ({countAcc})", _pickerCategory == 4 ? _tabButtonActiveStyle : _tabButtonStyle, GUILayout.Height(28f)))
            {
                _pickerCategory = 4;
                _scrollPosition = Vector2.zero;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Filtered Item List
            List<ItemDrop.ItemData> filteredList = new List<ItemDrop.ItemData>();
            for (int i = 0; i < _cachedEnhanceableItems.Count; i++)
            {
                var item = _cachedEnhanceableItems[i];
                if (MatchesCategory(item, _pickerCategory))
                {
                    filteredList.Add(item);
                }
            }

            if (filteredList.Count == 0)
            {
                GUILayout.Space(20f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#94a3b8>No equipment found matching this category in your inventory.</color>", _statLabelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                for (int i = 0; i < filteredList.Count; i++)
                {
                    ItemDrop.ItemData item = filteredList[i];
                    if (item == null) continue;

                    int lvl = EnhancementManager.GetEnhancementLevel(item);
                    string hex = EnhancementManager.GetTierHex(lvl);
                    string rank = EnhancementManager.GetTierRankName(lvl);
                    string localizedName = Localization.instance != null ? Localization.instance.Localize(item.m_shared.m_name) : item.m_shared.m_name;

                    GUILayout.BeginHorizontal(_boxSectionStyle);
                    string equippedTag = item.m_equipped ? " <color=#38bdf8><b>[Equipped]</b></color>" : "";
                    GUILayout.Label($"<b>{localizedName}</b> <color={hex}>+{lvl}</color> <color=#94a3b8>({rank})</color>{equippedTag}", _statLabelStyle, GUILayout.Width(460f));

                    if (GUILayout.Button("⚡ Select", _itemButtonStyle, GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        _selectedItem = item;
                        _lastResultMessage = "";
                        _isEnhanceablesDirty = true;
                        _scrollPosition = Vector2.zero;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }

                GUILayout.EndScrollView();
            }

            // Helpful footer tip
            GUILayout.Space(4f);
            GUILayout.Label("<color=#64748b>💡 <i>Tip: You can also left-click or right-click any equipment in your inventory to select it directly.</i></color>", _statLabelStyle);

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

            _statArrowStyle = new GUIStyle(GUI.skin.label);
            _statArrowStyle.fontSize = 13;
            _statArrowStyle.alignment = TextAnchor.MiddleCenter;
            _statArrowStyle.normal.textColor = new Color(0.6f, 0.65f, 0.7f);

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

            _statusBoxStyle = new GUIStyle(GUI.skin.box);
            _statusBoxStyle.normal.background = _texStatusBg;
            _statusBoxStyle.padding = new RectOffset(10, 10, 8, 8);
            _statusBoxStyle.alignment = TextAnchor.MiddleCenter;

            _statusTextStyle = new GUIStyle(GUI.skin.label);
            _statusTextStyle.fontSize = 13;
            _statusTextStyle.fontStyle = FontStyle.Bold;
            _statusTextStyle.alignment = TextAnchor.MiddleCenter;
            _statusTextStyle.normal.textColor = Color.white;
            _statusTextStyle.wordWrap = false;
            _statusTextStyle.clipping = TextClipping.Clip;

            _meterTextStyle = new GUIStyle(GUI.skin.label);
            _meterTextStyle.fontSize = 10;
            _meterTextStyle.fontStyle = FontStyle.Bold;
            _meterTextStyle.alignment = TextAnchor.MiddleCenter;
            _meterTextStyle.normal.textColor = Color.white;

            _tabButtonStyle = new GUIStyle(GUI.skin.button);
            _tabButtonStyle.normal.background = _texTabNormal;
            _tabButtonStyle.fontSize = 12;
            _tabButtonStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);

            _tabButtonActiveStyle = new GUIStyle(GUI.skin.button);
            _tabButtonActiveStyle.normal.background = _texTabActive;
            _tabButtonActiveStyle.fontStyle = FontStyle.Bold;
            _tabButtonActiveStyle.fontSize = 12;
            _tabButtonActiveStyle.normal.textColor = new Color(1f, 0.9f, 0.35f);

            // Scrollbar Styles
            if (GUI.skin.verticalScrollbar != null)
            {
                GUI.skin.verticalScrollbar.normal.background = _texScrollbarTrack;
                GUI.skin.verticalScrollbar.fixedWidth = 12f;
            }
            if (GUI.skin.verticalScrollbarThumb != null)
            {
                GUI.skin.verticalScrollbarThumb.normal.background = _texScrollbarThumb;
                GUI.skin.verticalScrollbarThumb.hover.background = _texScrollbarThumbHover;
                GUI.skin.verticalScrollbarThumb.active.background = _texScrollbarThumbHover;
                GUI.skin.verticalScrollbarThumb.fixedWidth = 12f;
            }
        }

        private void CreateTextures()
        {
            _texWindowBg = MakeBorderedTexture(16, 16, new Color(0.10f, 0.11f, 0.14f, 0.96f), new Color(0.70f, 0.55f, 0.20f, 1f));
            _texBoxBg = MakeBorderedTexture(16, 16, new Color(0.15f, 0.17f, 0.22f, 0.90f), new Color(0.28f, 0.32f, 0.40f, 0.8f));
            _texButtonNormal = MakeBorderedTexture(16, 16, new Color(0.25f, 0.18f, 0.08f, 1f), new Color(0.85f, 0.65f, 0.15f, 1f));
            _texButtonHover = MakeBorderedTexture(16, 16, new Color(0.40f, 0.28f, 0.10f, 1f), new Color(1.00f, 0.85f, 0.30f, 1f));
            _texButtonActive = MakeBorderedTexture(16, 16, new Color(0.55f, 0.38f, 0.12f, 1f), new Color(1.00f, 0.95f, 0.50f, 1f));
            _texButtonDisabled = MakeBorderedTexture(16, 16, new Color(0.18f, 0.18f, 0.20f, 0.8f), new Color(0.30f, 0.30f, 0.35f, 0.6f));
            _texScrollbarTrack = MakeBorderedTexture(12, 12, new Color(0.08f, 0.09f, 0.12f, 0.90f), new Color(0.22f, 0.24f, 0.30f, 0.7f));
            _texScrollbarThumb = MakeBorderedTexture(12, 12, new Color(0.55f, 0.40f, 0.12f, 0.95f), new Color(0.85f, 0.65f, 0.20f, 1f));
            _texScrollbarThumbHover = MakeBorderedTexture(12, 12, new Color(0.75f, 0.55f, 0.18f, 1f), new Color(1.00f, 0.85f, 0.35f, 1f));
            _texStatusBg = MakeBorderedTexture(16, 16, new Color(0.12f, 0.14f, 0.19f, 0.95f), new Color(0.35f, 0.40f, 0.50f, 0.9f));
            _texMeterBg = MakeBorderedTexture(12, 12, new Color(0.08f, 0.09f, 0.12f, 0.95f), new Color(0.25f, 0.28f, 0.35f, 0.8f));
            _texWhite = MakeSolidTexture(2, 2, Color.white);
            _texTabNormal = MakeBorderedTexture(12, 12, new Color(0.14f, 0.16f, 0.20f, 0.90f), new Color(0.25f, 0.28f, 0.35f, 0.7f));
            _texTabActive = MakeBorderedTexture(12, 12, new Color(0.25f, 0.20f, 0.10f, 0.95f), new Color(0.85f, 0.65f, 0.15f, 1f));
        }

        private Texture2D MakeSolidTexture(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
