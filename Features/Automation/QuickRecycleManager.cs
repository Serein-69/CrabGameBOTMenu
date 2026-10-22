using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnhollowerRuntimeLib;

namespace BOTMenuMod
{
    public static class QuickRecycleManager
    {
        public static void RecycleAllCommonItems()
        {
            try
            {
                var inventoryItems = MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.inventoryItems;
                if (inventoryItems == null || inventoryItems.Count == 0)
                {
                    Utility.ForceMessage("<color=yellow>背包数据为空！</color>");
                    return;
                }

                var clickUi = MonoBehaviourPublicGapaCacaTrCacawiReSiUnique.Instance;
                if (clickUi == null)
                {
                    var allClick = Resources.FindObjectsOfTypeAll(Il2CppType.Of<MonoBehaviourPublicGapaCacaTrCacawiReSiUnique>());
                    if (allClick != null && allClick.Count > 0) clickUi = allClick[0].TryCast<MonoBehaviourPublicGapaCacaTrCacawiReSiUnique>();
                }

                var recycleUi = MonoBehaviourPublicGareTecoObinpaGaIninUnique.Instance;
                if (recycleUi == null)
                {
                    var allRecycle = Resources.FindObjectsOfTypeAll(Il2CppType.Of<MonoBehaviourPublicGareTecoObinpaGaIninUnique>());
                    if (allRecycle != null && allRecycle.Count > 0) recycleUi = allRecycle[0].TryCast<MonoBehaviourPublicGareTecoObinpaGaIninUnique>();
                }

                if (clickUi == null || recycleUi == null)
                {
                    Utility.ForceMessage("<color=red>请先在游戏中打开一次背包！</color>");
                    return;
                }

                List<ulong> targetUniqueIds = new List<ulong>();

                foreach (var pair in inventoryItems)
                {
                    ulong uniqueId = pair.Key;
                    var cosmeticItem = pair.Value;
                    if (cosmeticItem == null) continue;

                    var tags = cosmeticItem.field_Public_Dictionary_2_String_String_0;

                    if (tags != null && tags.ContainsKey("shiny"))
                    {
                        string shinyStr = tags["shiny"];
                        if (!string.IsNullOrEmpty(shinyStr) && shinyStr != "0")
                        {
                            if (float.TryParse(shinyStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float sVal) && sVal > 0f) continue;
                        }
                    }

                    string rarity = "common";
                    if (tags != null && tags.ContainsKey("rarity")) rarity = tags["rarity"].ToLower().Trim();

                    if (rarity.Contains("uncommon") || rarity.Contains("rare") || rarity.Contains("extraordinaire") || rarity.Contains("legendary"))
                        continue;

                    if (rarity == "common" || rarity == "0" || string.IsNullOrEmpty(rarity))
                        targetUniqueIds.Add(uniqueId);
                }

                if (targetUniqueIds.Count == 0)
                {
                    Utility.ForceMessage("<color=yellow>未找到可分解的白色普通物品！</color>");
                    return;
                }

                int count = 0;
                foreach (ulong uniqueId in targetUniqueIds)
                {
                    try
                    {
                        clickUi.field_Private_UInt64_0 = uniqueId;
                        clickUi.Recycle();
                        recycleUi.ActuallyRecycle();
                        count++;
                    }
                    catch { }
                }

                Utility.ForceMessage($"<color=green>成功分解 <color=yellow>{count}</color> 件普通白装！</color>");
            }
            catch (Exception e)
            {
                Utility.ForceMessage("<color=red>分解失败: " + e.Message + "</color>");
            }
        }
    }
}
