using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valheim.ItemEnhancements.Configuration;
using Valheim.ItemEnhancements.Core;

namespace Valheim.ItemEnhancements.UI
{
    public class EnhancementGui : MonoBehaviour
    {
        public static EnhancementGui Instance { get; private set; }
        public static bool IsOpen { get; set; } = false;

        private Rect _windowRect = new Rect(Screen.width / 2f - 340f, Screen.height / 2f - 290f, 680f, 580f);
        private ItemDrop.ItemData _selectedItem;
        private Vector2 _scrollPosition = Vector2.zero;

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

            if (ModConfig.RequireCraftingStation.Value)
            {
                CraftingStation station = Player.m_localPlayer.GetCurrentCraftingStation();
                if (station == null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "<color=#f59e0b>ต้องอยู่ใกล้โต๊ะคราฟต์หรือเตาตีเหล็กเพื่อตีบวก! (Must be near a crafting station)</color>");
                    return;
                }
            }

            IsOpen = true;
            if (Instance != null)
            {
                Instance._lastResultMessage = "";
                // Auto-select equipped item if empty
                if (Instance._selectedItem == null)
                {
                    Instance._selectedItem = Player.m_localPlayer.GetInventory()?.GetEquippedItems()?.Find(EnhancementManager.IsEnhanceable);
                }
            }
        }

        public static void Close()
        {
            IsOpen = false;
        }

        public static void SetSelectedItem(ItemDrop.ItemData item)
        {
            if (Instance != null)
            {
                Instance._selectedItem = item;
                Instance._lastResultMessage = "";
            }
        }

        private void Update()
        {
            if (!IsOpen) return;

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

            // Close if player moves away from station
            if (ModConfig.RequireCraftingStation.Value && Player.m_localPlayer != null)
            {
                if (Player.m_localPlayer.GetCurrentCraftingStation() == null)
                {
                    Close();
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
            GUILayout.Label("✦  MMORPG ITEM REFINEMENT (ระบบตีบวกอุปกรณ์)  ✦", _headerStyle);
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

                // Action Button (ตีบวก) & Result Banner
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
                GUILayout.Label($"<b>ไอเทมเป้าหมาย:</b> <color={hex}><b>{localizedName} +{currentLvl}</b></color> <color=#94a3b8>({rank})</color>", _subHeaderStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("เลือกไอเทมอื่น (Change)", GUILayout.Width(150f), GUILayout.Height(26f)))
                {
                    _selectedItem = null;
                    _lastResultMessage = "";
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<color=#ffd700>กรุณาคลิกเลือกไอเทมที่ต้องการตีบวกจากรายการด้านล่าง หรือคลิกไอเทมในกระเป๋าของคุณ</color>", _subHeaderStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawStatsComparison()
        {
            int currentLvl = EnhancementManager.GetEnhancementLevel(_selectedItem);
            int nextLvl = currentLvl + 1;
            bool isMax = currentLvl >= ModConfig.MaxLevel;

            GUILayout.BeginVertical(_boxSectionStyle);
            GUILayout.Label("<b>เปรียบเทียบสเตตัส (Stat Progression):</b>", _statLabelStyle);
            GUILayout.Space(4f);

            // Columns Header
            GUILayout.BeginHorizontal();
            GUILayout.Label($"ระดับปัจจุบัน: <color={EnhancementManager.GetTierHex(currentLvl)}><b>+{currentLvl}</b></color>", _statValueCurrentStyle, GUILayout.Width(270f));
            GUILayout.Label("➔", _headerStyle, GUILayout.Width(60f));
            if (!isMax)
            {
                GUILayout.Label($"ระดับถัดไป: <color={EnhancementManager.GetTierHex(nextLvl)}><b>+{nextLvl}</b></color>", _statValueNextStyle, GUILayout.Width(270f));
            }
            else
            {
                GUILayout.Label("<color=#ffd700><b>★ MAX LEVEL ★</b></color>", _statValueNextStyle, GUILayout.Width(270f));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Stats Rows
            // 1. Damage (สำหรับอาวุธ)
            if (IsWeapon(_selectedItem))
            {
                float curDmgMult = 1f + (currentLvl * ModConfig.WeaponDamageBonusPerLevel.Value);
                float nxtDmgMult = 1f + (nextLvl * ModConfig.WeaponDamageBonusPerLevel.Value);
                float diffPct = ModConfig.WeaponDamageBonusPerLevel.Value * 100f;

                DrawStatRow("พลังโจมตีทุกธาตุ (All Damage):",
                    $"+{(curDmgMult - 1f) * 100f:F0}%",
                    isMax ? "-" : $"+{(nxtDmgMult - 1f) * 100f:F0}% <color=#4ade80>(+{diffPct:F0}%)</color>");

                float curStam = StatCalculator.GetAttackStaminaReduction(currentLvl) * 100f;
                float nxtStam = StatCalculator.GetAttackStaminaReduction(nextLvl) * 100f;
                DrawStatRow("ลด Stamina โจมตี:",
                    $"-{curStam:F0}%",
                    isMax ? "-" : $"-{nxtStam:F0}% <color=#38bdf8>(-{ModConfig.WeaponStaminaReductionPerLevel.Value * 100f:F0}%)</color>");
            }

            // 2. Armor (สำหรับเกราะ)
            if (IsArmor(_selectedItem))
            {
                float curArmor = _selectedItem.GetArmor();
                float nextArmorBonus = StatCalculator.GetArmorBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_armor);
                float curArmorBonus = StatCalculator.GetArmorBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_armor);
                float diff = nextArmorBonus - curArmorBonus;

                DrawStatRow("พลังป้องกันเกราะ (Armor):",
                    $"{curArmor:F1}",
                    isMax ? "-" : $"{curArmor + diff:F1} <color=#4ade80>(+{diff:F1})</color>");

                float curMove = StatCalculator.GetMovementModifierDelta(_selectedItem, currentLvl) * 100f;
                float nxtMove = StatCalculator.GetMovementModifierDelta(_selectedItem, nextLvl) * 100f;
                DrawStatRow("ลดโทษความเร็วเดิน / โบนัส:",
                    $"+{curMove:F1}%",
                    isMax ? "-" : $"+{nxtMove:F1}% <color=#38bdf8>(+{nxtMove - curMove:F1}%)</color>");
            }

            // 3. Shield (สำหรับโล่)
            if (_selectedItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                float curBlk = _selectedItem.GetBlockPower(Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking));
                float curBlkBonus = StatCalculator.GetBlockPowerBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_blockPower);
                float nxtBlkBonus = StatCalculator.GetBlockPowerBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_blockPower);
                float diffBlk = nxtBlkBonus - curBlkBonus;

                DrawStatRow("พลังบล็อก (Block Power):",
                    $"{curBlk:F1}",
                    isMax ? "-" : $"{curBlk + diffBlk:F1} <color=#4ade80>(+{diffBlk:F1})</color>");

                float curBlkStam = StatCalculator.GetBlockStaminaReduction(currentLvl) * 100f;
                float nxtBlkStam = StatCalculator.GetBlockStaminaReduction(nextLvl) * 100f;
                DrawStatRow("ลด Stamina การบล็อก:",
                    $"-{curBlkStam:F0}%",
                    isMax ? "-" : $"-{nxtBlkStam:F0}% <color=#38bdf8>(-{ModConfig.ShieldBlockStaminaReductionPerLevel.Value * 100f:F1}%)</color>");
            }

            // 4. Durability & Weight
            float curDur = _selectedItem.GetMaxDurability();
            float curDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, currentLvl, _selectedItem.m_shared.m_maxDurability);
            float nxtDurBonus = StatCalculator.GetDurabilityBonus(_selectedItem, nextLvl, _selectedItem.m_shared.m_maxDurability);
            float diffDur = nxtDurBonus - curDurBonus;

            DrawStatRow("ความทนทานสูงสุด (Max Durability):",
                $"{curDur:F0}",
                isMax ? "-" : $"{curDur + diffDur:F0} <color=#4ade80>(+{diffDur:F0})</color>");

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
                GUILayout.Label("<color=#ffd700><b>ไอเทมนี้ได้รับการตีบวกถึงระดับสูงสุดแล้ว (+20 MAX)</b></color>", _headerStyle);
                GUILayout.EndVertical();
                return;
            }

            float successRate = ModConfig.GetSuccessRate(nextLvl);
            string rateColor = successRate >= 70f ? "#4ade80" : (successRate >= 40f ? "#f59e0b" : "#ef4444");

            // Success Rate & Risk Bar
            GUILayout.BeginHorizontal();

            // Left: Success Chance
            GUILayout.BeginVertical(GUILayout.Width(280f));
            GUILayout.Label("<b>โอกาสสำเร็จ (Success Rate):</b>", _statLabelStyle);
            GUILayout.Label($"<color={rateColor}><b>{successRate:F0}%</b></color>", _rateNumberStyle);
            GUILayout.EndVertical();

            // Right: Failure Consequence
            GUILayout.BeginVertical();
            GUILayout.Label("<b>ผลลัพธ์หากล้มเหลว (Failure Risk):</b>", _statLabelStyle);
            if (currentLvl < ModConfig.SafeLevel.Value)
            {
                GUILayout.Label("<color=#4ade80>✔ ปลอดภัย (Safe) - ระดับไม่ลดลง ไม่แตก</color>", _statLabelStyle);
            }
            else
            {
                if (ModConfig.BreakOnFail.Value)
                {
                    GUILayout.Label($"<color=#ef4444>⚠ เสี่ยงลดลง 1 ขั้น (เหลือ +{currentLvl - 1}) หรือแตก ({ModConfig.BreakChanceAboveSafeLevel.Value:F0}%)</color>", _statLabelStyle);
                }
                else if (ModConfig.DowngradeOnFail.Value)
                {
                    GUILayout.Label($"<color=#f59e0b>⚠ ระดับจะลดลง 1 ขั้น (เหลือ +{currentLvl - 1})</color>", _statLabelStyle);
                }
                else
                {
                    GUILayout.Label("<color=#4ade80>✔ ปลอดภัย (Safe) - ระดับคงเดิม</color>", _statLabelStyle);
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

                GUILayout.Label($"<b>วัตถุดิบที่ต้องใช้:</b> <color={tierHex}>{scrollName}</color> x{neededScrolls}", _statLabelStyle);
                GUILayout.Label($"<b>จำนวนที่คุณมี:</b> <color={scrollColor}><b>{playerScrolls} ใบ</b></color>" + (hasScrolls ? "" : " <color=#ef4444>(ไม่เพียงพอ)</color>"), _statLabelStyle);
            }

            if (ModConfig.RequireCoins.Value)
            {
                int cost = ModConfig.GetCoinCost(nextLvl);
                int playerCoins = EnhancementManager.GetPlayerCoins(Player.m_localPlayer);
                bool canAffordCoins = playerCoins >= cost;
                string coinColor = canAffordCoins ? "#ffd700" : "#ef4444";
                GUILayout.Label($"<b>ค่าธรรมเนียม:</b> <color={coinColor}>{cost} เหรียญทอง</color> <color=#94a3b8>(คุณมี: {playerCoins})</color>", _statLabelStyle);
            }
            else
            {
                GUILayout.Label("<b>ค่าธรรมเนียม:</b> <color=#4ade80>ฟรี (ไม่มีค่าธรรมเนียมเหรียญ)</color>", _statLabelStyle);
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
                GUILayout.Button("🔨  กำลังหลอมตีเหล็ก... (FORGING...)  🔨", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (isMax)
            {
                GUI.enabled = false;
                GUILayout.Button("★  ระดับสูงสุดแล้ว (MAX LEVEL +20)  ★", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (!hasScrolls)
            {
                GUI.enabled = false;
                GUILayout.Button($"❌  ต้องการ [ใบตีบวกระดับ {scrollTier}] (คุณมี {playerScrolls}/{neededScrolls} ใบ)  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else if (!hasCoins)
            {
                GUI.enabled = false;
                GUILayout.Button($"❌  เหรียญทองไม่เพียงพอ (ต้องการ {cost} Coins)  ❌", _actionButtonStyle, GUILayout.Height(48f));
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("⚡  เริ่มตีบวก (ENHANCE ITEM)  ⚡", _actionButtonStyle, GUILayout.Height(48f)))
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
                    _lastResultMessage = $"★ ตีบวกสำเร็จ! {itemName} กลายเป็น +{newLevel} ★";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#ffd700>★ ตีบวกสำเร็จ! {itemName} (+{newLevel}) ★</color>");
                    break;

                case EnhanceResult.FailedSafe:
                    _lastResultColor = "#94a3b8";
                    _lastResultMessage = $"✖ ตีบวกล้มเหลว... โชคดีที่ไอเทมปลอดภัย ไม่ลดระดับ (คงที่ +{newLevel}) ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#94a3b8>✖ ตีบวกล้มเหลว... ไอเทมปลอดภัย (+{newLevel}) ✖</color>");
                    break;

                case EnhanceResult.FailedDowngraded:
                    _lastResultColor = "#f59e0b";
                    _lastResultMessage = $"✖ ตีบวกล้มเหลว! ระดับลดลงเหลือ +{newLevel} ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#f59e0b>✖ ตีบวกล้มเหลว! ระดับลดลงเหลือ +{newLevel} ✖</color>");
                    break;

                case EnhanceResult.FailedBroken:
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = $"✖ ตีบวกล้มเหลว! {itemName} แตกสลายไปแล้ว... ✖";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#ef4444>✖ ตีบวกล้มเหลว! {itemName} แตกสลาย... ✖</color>");
                    _selectedItem = null;
                    break;

                case EnhanceResult.CannotAfford:
                    _lastResultColor = "#ef4444";
                    _lastResultMessage = "วัตถุดิบคัมภีร์ตีบวกไม่เพียงพอ!";
                    break;
            }

            // Sync Inventory
            if (InventoryGui.instance != null)
            {
                HarmonyLib.AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel")?.Invoke(InventoryGui.instance, new object[] { false });
            }
        }

        private void DrawEquipmentPickerList()
        {
            GUILayout.BeginVertical(_boxSectionStyle);
            GUILayout.Label("<b>อุปกรณ์ในตัวที่สามารถตีบวกได้:</b>", _statLabelStyle);
            GUILayout.Space(4f);

            List<ItemDrop.ItemData> allItems = Player.m_localPlayer.GetInventory().GetAllItems();

            List<ItemDrop.ItemData> enhanceables = allItems.FindAll(EnhancementManager.IsEnhanceable);

            if (enhanceables.Count == 0)
            {
                GUILayout.Label("<color=#94a3b8>ไม่มีอุปกรณ์ที่สามารถตีบวกได้ในกระเป๋าของคุณ</color>", _statLabelStyle);
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300f));

                foreach (var item in enhanceables)
                {
                    int lvl = EnhancementManager.GetEnhancementLevel(item);
                    string hex = EnhancementManager.GetTierHex(lvl);
                    string localizedName = Localization.instance != null ? Localization.instance.Localize(item.m_shared.m_name) : item.m_shared.m_name;

                    GUILayout.BeginHorizontal();
                    string equippedTag = item.m_equipped ? " <color=#38bdf8>[สวมใส่อยู่]</color>" : "";
                    GUILayout.Label($"<b>{localizedName}</b> <color={hex}>+{lvl}</color>{equippedTag}", _statLabelStyle, GUILayout.Width(450f));

                    if (GUILayout.Button("เลือก (Select)", _itemButtonStyle, GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        _selectedItem = item;
                        _lastResultMessage = "";
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
