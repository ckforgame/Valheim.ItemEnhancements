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
        private static GameObject _tabEnhanceObj;

        /// <summary>
        /// แทรกปุ่ม [⚡ ตีบวก] ไว้ข้างๆ แท็บ Craft และ Upgrade ในหน้าต่างโต๊ะคราฟต์
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (__instance?.m_tabUpgrade == null || __instance.m_tabUpgrade.transform.parent == null) return;

                try
                {
                    if (_tabEnhanceObj != null) return;

                    _tabEnhanceObj = UnityEngine.Object.Instantiate(__instance.m_tabUpgrade.gameObject, __instance.m_tabUpgrade.transform.parent);
                    _tabEnhanceObj.name = "TabEnhance";

                    RectTransform rt = _tabEnhanceObj.GetComponent<RectTransform>();
                    RectTransform upgradeRt = __instance.m_tabUpgrade.GetComponent<RectTransform>();

                    if (rt != null && upgradeRt != null)
                    {
                        float offsetX = upgradeRt.rect.width > 0 ? upgradeRt.rect.width + 10f : 110f;
                        rt.anchoredPosition = new Vector2(upgradeRt.anchoredPosition.x + offsetX, upgradeRt.anchoredPosition.y);
                    }

                    Button btn = _tabEnhanceObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick = new Button.ButtonClickedEvent();
                        btn.onClick.AddListener(() =>
                        {
                            EnhancementGui.Toggle();
                        });
                    }

                    TMP_Text tmp = _tabEnhanceObj.GetComponentInChildren<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = "⚡ ตีบวก";
                    }
                    else
                    {
                        Text txt = _tabEnhanceObj.GetComponentInChildren<Text>();
                        if (txt != null)
                        {
                            txt.text = "⚡ ตีบวก";
                        }
                    }

                    _tabEnhanceObj.SetActive(true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Valheim.ItemEnhancements] Could not create TabEnhance button: {ex.Message}");
                }
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
