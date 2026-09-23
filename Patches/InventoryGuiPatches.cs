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
        /// สร้างปุ่ม [⚡ ตีบวก] วางข้างๆ ปุ่มซ่อมแซม (Repair Button) บนโต๊ะคราฟต์
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

                    // โคลนปุ่มจาก m_repairButton ให้อยู่ใน parent เดียวกัน (m_repairPanel)
                    _enhanceButtonObj = UnityEngine.Object.Instantiate(__instance.m_repairButton.gameObject, __instance.m_repairButton.transform.parent);
                    _enhanceButtonObj.name = "EnhanceButton";

                    // ปลดล็อก Mask เพื่อไม่ให้ปุ่มที่วางเพิ่มถูก Clip
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

                    // ปิดเอฟเฟกต์เรืองแสงของปุ่มซ่อมแซมที่ถูกโคลนมา
                    foreach (Transform child in _enhanceButtonObj.transform)
                    {
                        if (child.name.ToLower().Contains("glow"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }

                    // ตั้งค่า Event เมื่อคลิกปุ่ม
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

                    // ตั้งค่า Tooltip แสดงชื่อระบบตีบวก
                    UITooltip tooltip = _enhanceButtonObj.GetComponent<UITooltip>();
                    if (tooltip == null)
                    {
                        tooltip = _enhanceButtonObj.AddComponent<UITooltip>();
                    }
                    tooltip.m_text = "เปิดหน้าต่างตีบวกอุปกรณ์ (Item Enhancement)";
                    tooltip.m_topic = "⚡ ตีบวกอุปกรณ์";

                    // ปรับสีไอคอนให้เป็นสีทอง
                    Image[] images = _enhanceButtonObj.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (img.gameObject != _enhanceButtonObj && !img.name.ToLower().Contains("glow"))
                        {
                            img.color = new Color(1f, 0.85f, 0.25f, 1f);
                        }
                    }

                    // เพิ่มป้ายสัญลักษณ์สายฟ้า ⚡ ตรงกลางปุ่ม
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
        /// อัปเดตสถานะการแสดงผลและความพร้อมใช้งานของปุ่มตีบวกให้ตรงกับการเปิดโต๊ะคราฟต์
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "UpdateRepair")]
        public static class UpdateRepair_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (_enhanceButtonObj == null) return;

                // ปุ่มตีบวกพร้อมใช้งานตลอดเมื่ออยู่ที่โต๊ะคราฟต์ (ไม่ต้อง Gray out ตามปุ่มซ่อม)
                Button btn = _enhanceButtonObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                }

                bool shouldShow = __instance.m_repairButton != null && __instance.m_repairButton.gameObject.activeInHierarchy;
                _enhanceButtonObj.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// เมื่อปิดหน้าต่าง Inventory ให้ปิดหน้าต่างตีบวกด้วย
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class Hide_Patch
        {
            public static void Postfix()
            {
                EnhancementGui.Close();
            }
        }
    }
}
