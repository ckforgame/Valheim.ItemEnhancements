using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.ItemEnhancements.UI;

namespace Valheim.ItemEnhancements.Patches
{
    public static class InventoryGuiPatches
    {
        private static GameObject _enhanceButtonObj;

        /// <summary>
        /// Creates [⚡ Enhance] button next to the Repair Button on crafting stations.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (__instance?.m_repairButton == null || __instance.m_repairButton.transform.parent == null) return;

                try
                {
                    if (_enhanceButtonObj != null) return;

                    // Clone button from m_repairButton inside the same parent (m_repairPanel)
                    _enhanceButtonObj = UnityEngine.Object.Instantiate(__instance.m_repairButton.gameObject, __instance.m_repairButton.transform.parent);
                    _enhanceButtonObj.name = "EnhanceButton";

                    // Unlock mask to prevent clipping the additional button
                    var mask = __instance.m_repairButton.transform.parent.GetComponent<Mask>();
                    if (mask != null) mask.enabled = false;
                    var mask2d = __instance.m_repairButton.transform.parent.GetComponent<RectMask2D>();
                    if (mask2d != null) mask2d.enabled = false;

                    RectTransform repairRt = __instance.m_repairButton.GetComponent<RectTransform>();
                    RectTransform rt = _enhanceButtonObj.GetComponent<RectTransform>();

                    if (rt != null && repairRt != null)
                    {
                        rt.anchorMin = repairRt.anchorMin;
                        rt.anchorMax = repairRt.anchorMax;
                        rt.pivot = repairRt.pivot;
                        rt.sizeDelta = repairRt.sizeDelta;
                        rt.localScale = repairRt.localScale;

                        float btnHeight = repairRt.rect.height > 0 ? repairRt.rect.height : 44f;
                        float offsetY = btnHeight + 6f;
                        rt.anchoredPosition = new Vector2(repairRt.anchoredPosition.x, repairRt.anchoredPosition.y - offsetY);
                    }

                    // Disable glow effect cloned from repair button
                    foreach (Transform child in _enhanceButtonObj.transform)
                    {
                        if (child.name.ToLower().Contains("glow"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }

                    // Setup button click event
                    Button btn = _enhanceButtonObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick = new Button.ButtonClickedEvent();
                        btn.onClick.AddListener(() =>
                        {
                            EnhancementGui.Toggle();
                        });
                        btn.interactable = true;
                    }

                    // Setup tooltip for enhancement system
                    UITooltip tooltip = _enhanceButtonObj.GetComponent<UITooltip>();
                    if (tooltip == null)
                    {
                        tooltip = _enhanceButtonObj.AddComponent<UITooltip>();
                    }
                    tooltip.m_text = "Open Item Enhancement Window";
                    tooltip.m_topic = "⚡ Enhance Item";

                    // Tint icon to golden color
                    Image[] images = _enhanceButtonObj.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (img.gameObject != _enhanceButtonObj && !img.name.ToLower().Contains("glow"))
                        {
                            img.color = new Color(1f, 0.85f, 0.25f, 1f);
                        }
                    }

                    // Add lightning badge icon ⚡ in center of button
                    GameObject labelObj = new GameObject("EnhanceIconBadge");
                    labelObj.transform.SetParent(_enhanceButtonObj.transform, false);
                    RectTransform labelRt = labelObj.AddComponent<RectTransform>();
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.offsetMin = Vector2.zero;
                    labelRt.offsetMax = Vector2.zero;

                    if (__instance.m_craftingStationName != null)
                    {
                        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
                        tmp.font = __instance.m_craftingStationName.font;
                        tmp.text = "<color=#ffd700><b>⚡</b></color>";
                        tmp.fontSize = 24;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                    }
                    else
                    {
                        Text txt = labelObj.AddComponent<Text>();
                        txt.text = "⚡";
                        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                        txt.fontSize = 20;
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.color = new Color(1f, 0.85f, 0.25f, 1f);
                        txt.raycastTarget = false;
                    }

                    _enhanceButtonObj.SetActive(true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Valheim.ItemEnhancements] Could not create EnhanceButton: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates enhancement button visibility and state according to crafting station status.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "UpdateRepair")]
        public static class UpdateRepair_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (_enhanceButtonObj == null) return;

                // Enhancement button stays active whenever crafting station is open (does not gray out with repair button)
                Button btn = _enhanceButtonObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                }

                bool shouldShow = __instance.m_repairButton != null && __instance.m_repairButton.gameObject.activeInHierarchy;
                _enhanceButtonObj.SetActive(shouldShow);

                UITooltip tooltip = _enhanceButtonObj.GetComponent<UITooltip>();
                if (tooltip != null)
                {
                    tooltip.m_text = EnhancementGui.IsOpen ? "Close Item Enhancement Window [F8]" : "Open Item Enhancement Window [F8]";
                }
            }
        }

        /// <summary>
        /// Closes enhancement window when inventory window is hidden.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class Hide_Patch
        {
            public static void Postfix()
            {
                EnhancementGui.Close(forceLockCursorIfNoMenu: true);
            }
        }
    }
}
