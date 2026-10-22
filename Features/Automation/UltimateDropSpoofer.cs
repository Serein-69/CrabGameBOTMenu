using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;
using BepInEx.IL2CPP.Utils.Collections;

namespace BOTMenuMod
{
    public class UltimateDropSpoofer : MonoBehaviour
    {
        public static UltimateDropSpoofer Instance;
        public UltimateDropSpoofer(IntPtr ptr) : base(ptr) { }
        void Awake() { Instance = this; }

        public static bool isRunning = false;

        public static void TriggerJackpot()
        {
            if (isRunning) return;
            if (SteamManager.Instance == null || GameManager.Instance == null) return;
            if (ItemBrowserManager.SelectedItemIDs.Count == 0)
            {
                Utility.ForceMessage("<color=red>请先在饰品浏览器中至少选择 1 个物品！</color>");
                return;
            }
            if (Instance == null) return;
            isRunning = true;
            try
            {
                Instance.StartCoroutine(Instance.JackpotRoutine().WrapToIl2Cpp());
            }
            catch
            {
                isRunning = false;
            }
        }

        private System.Collections.IEnumerator JackpotRoutine()
        {
            try
            {
                Utility.ForceMessage("<color=orange>Jackpot Triggered! 偷梁换柱模式启动！</color>");
                var inventoryDict = MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.inventoryItems;
                if (inventoryDict == null || inventoryDict.Count == 0)
                {
                    Utility.ForceMessage("<color=red>Error: 你的本地库存为空！需要至少拥有一件饰品才能作为模板！</color>");
                    yield break;
                }

                var iterator = inventoryDict.Values.GetEnumerator();
                if (!iterator.MoveNext())
                {
                    Utility.ForceMessage("<color=red>Error: 无法读取库存枚举器！</color>");
                    yield break;
                }
                var templateItem = iterator.Current;
                if (templateItem == null) yield break;

                for (int i = 0; i < 15; i++)
                {
                    if (ItemBrowserManager.SelectedItemIDs.Count == 0) break;

                    try
                    {
                        SteamworksNative.SteamItemDetails_t originalDetails = templateItem.field_Public_SteamItemDetails_t_0;
                        try
                        {
                            SteamworksNative.SteamItemDetails_t spoofedDetails = originalDetails;

                            int randomItemId = ItemBrowserManager.SelectedItemIDs[UnityEngine.Random.Range(0, ItemBrowserManager.SelectedItemIDs.Count)];
                            spoofedDetails.m_iDefinition = (SteamItemDef_t)randomItemId;
                            templateItem.field_Public_SteamItemDetails_t_0 = spoofedDetails;

                            MonoBehaviourPublicInpabyInInInUnique.ReceivedDrop(templateItem);
                        }
                        finally
                        {
                            templateItem.field_Public_SteamItemDetails_t_0 = originalDetails;
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.Log("Jackpot Error: " + ex.Message);
                    }
                    yield return new WaitForSeconds(0.2f);
                }
                Utility.ForceMessage("<color=green>Jackpot Finished! 绝版狂潮已结束！</color>");
            }
            finally
            {
                isRunning = false;
            }
        }
    }

    public class ItemBrowserManager : MonoBehaviour
    {
        public static ItemBrowserManager Instance;
        public static bool ShowGUI = false;

        private Rect _windowRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 250, 500, 500);
        private Vector2 _scrollPos;
        private string _searchQuery = "";
        private bool _isDragging = false;
        private Vector2 _dragOffset;

        private Texture2D _bgTex;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _btnStyle;

        private struct ItemCache { public int ID; public string Name; }
        private List<ItemCache> _cachedItems = new List<ItemCache>();
        private bool _hasCached = false;

        public static List<int> SelectedItemIDs = new List<int> { 416, 801, 217 };

        public ItemBrowserManager(IntPtr ptr) : base(ptr) { }
        void Awake() { Instance = this; }

        public void LoadItemsFromGame()
        {
            _cachedItems.Clear();
            try
            {
                var dict = MonoBehaviourPublicLi1CoalDi2InitCoUIUnique.itemIdToItem;
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        int id = kvp.Key;
                        var itemObj = kvp.Value;
                        string itemName = "Unknown";
                        if (itemObj != null)
                        {
                            try { itemName = itemObj.name; } catch { itemName = itemObj.ToString(); }
                        }
                        itemName = itemName.Replace("(Clone)", "").Trim();
                        _cachedItems.Add(new ItemCache { ID = id, Name = itemName });
                    }
                    _cachedItems.Sort((a, b) => a.ID.CompareTo(b.ID));
                    _hasCached = true;
                    Utility.ForceMessage($"<color=green>成功加载 {_cachedItems.Count} 个饰品数据！</color>");
                }
            }
            catch (Exception e) { Utility.ForceMessage($"<color=red>读取饰品失败: {e.Message}</color>"); }
        }

        public static void DumpItemsToFile()
        {
            if (Instance == null || !Instance._hasCached) return;
            try
            {
                string path = Path.Combine(Variables.mainFolderPath, "ItemIDs_Dump.txt");
                List<string> lines = new List<string> { "=== Crab Game 饰品 ID 列表 ===", $"导出时间: {DateTime.Now}", "-----------------------------" };
                foreach (var item in Instance._cachedItems)
                {
                    lines.Add($"ID: {item.ID,-5} | Name: {item.Name}");
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        void OnGUI()
        {
            if (!ShowGUI) return;

            if (_bgTex == null)
            {
                _bgTex = new Texture2D(1, 1);
                _bgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.95f));
                _bgTex.Apply();
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
                _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            }

            HandleDrag();
            GUI.DrawTexture(_windowRect, _bgTex);
            GUI.BeginGroup(_windowRect);

            GUI.Label(new Rect(10, 5, _windowRect.width - 20, 30), "<color=#00FFFF>✦ ITEM ID BROWSER ✦</color>", _titleStyle);

            if (GUI.Button(new Rect(_windowRect.width - 40, 5, 30, 30), "<color=red>X</color>", _btnStyle))
            {
                ShowGUI = false;
                if (!Variables.menuTrigger) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            }

            GUI.Label(new Rect(20, 40, 200, 25), $"<b>Jackpot Pool: <color=magenta>{SelectedItemIDs.Count} Items</color></b>", _labelStyle);
            if (GUI.Button(new Rect(_windowRect.width - 120, 40, 100, 25), "Clear All (清空)")) SelectedItemIDs.Clear();

            GUI.Label(new Rect(20, 70, 60, 25), "<b>Search:</b>", _labelStyle);
            _searchQuery = GUI.TextField(new Rect(80, 70, _windowRect.width - 100, 25), _searchQuery);

            if (!_hasCached)
            {
                if (GUI.Button(new Rect(20, 110, _windowRect.width - 40, 40), "Load Items Database (读取底层数据)", _btnStyle))
                {
                    LoadItemsFromGame();
                }
            }
            else
            {
                GUI.Label(new Rect(20, 105, 50, 20), "<color=yellow><b>ID</b></color>", _labelStyle);
                GUI.Label(new Rect(80, 105, 200, 20), "<color=yellow><b>Item Name</b></color>", _labelStyle);

                GUILayout.BeginArea(new Rect(15, 130, _windowRect.width - 30, _windowRect.height - 180));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, "box");

                string lowerQuery = _searchQuery.ToLower();
                foreach (var item in _cachedItems)
                {
                    if (!string.IsNullOrEmpty(lowerQuery) && !item.Name.ToLower().Contains(lowerQuery) && !item.ID.ToString().Contains(lowerQuery))
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<color=cyan>{item.ID}</color>", _labelStyle, GUILayout.Width(50));
                    GUILayout.Label(item.Name, _labelStyle);

                    bool isSelected = SelectedItemIDs.Contains(item.ID);
                    if (GUILayout.Button(isSelected ? "<color=green>[ Added ]</color>" : "[   +   ]", _labelStyle, GUILayout.Width(70)))
                    {
                        if (isSelected) SelectedItemIDs.Remove(item.ID);
                        else SelectedItemIDs.Add(item.ID);
                    }

                    if (GUILayout.Button("Copy", GUILayout.Width(50)))
                    {
                        GUIUtility.systemCopyBuffer = item.ID.ToString();
                        Utility.ForceMessage($"<color=green>Copied ID: {item.ID}</color>");
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();

                if (GUI.Button(new Rect(20, _windowRect.height - 40, _windowRect.width - 40, 30), "Dump All to TXT (导出所有ID到本地TXT)", _btnStyle))
                {
                    DumpItemsToFile();
                }
            }

            GUI.EndGroup();
        }

        private void HandleDrag()
        {
            Event e = Event.current;
            Rect dragRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width - 40, 40);
            if (e.type == EventType.MouseDown && dragRect.Contains(e.mousePosition))
            {
                _isDragging = true;
                _dragOffset = e.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
            }
            else if (e.type == EventType.MouseUp) _isDragging = false;

            if (_isDragging && e.type == EventType.MouseDrag)
            {
                _windowRect.x = e.mousePosition.x - _dragOffset.x;
                _windowRect.y = e.mousePosition.y - _dragOffset.y;
            }
        }
    }
}
