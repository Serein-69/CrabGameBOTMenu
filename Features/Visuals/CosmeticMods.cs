using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class ItemSkinChangerConstants
    {
        public static readonly int[] MELEE_IDS = new int[] { 4, 5, 6, 7, 8, 10 };
        public const int SNOWBALL_ID = 9;
        public const float SKIN_APPLICATION_DELAY = 0.01f;
    }

    public enum ItemType { Default, Gun, Melee, Snowball }

    public class ItemSkinChangerManager : MonoBehaviour
    {
        public ItemSkinChangerManager(IntPtr ptr) : base(ptr) { }

        public static bool itemSkinChanger;
        public static bool isSkinnedItemUsable;
        public static int itemSkinId;

        private float skinApplicationTimer;
        private bool currentItemRecorded;
        private bool hasSwitchedAway;
        private int originalSlotIndex;
        private int originalItemId;

        private void Update()
        {
            if (!itemSkinChanger) return;
            this.skinApplicationTimer += Time.deltaTime;

            var invs = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            MonoBehaviourPublicItDi2ObIninInTrweGaUnique _PlayerInventory = null;
            foreach (var i in invs) if (i.enabled && i.gameObject.activeInHierarchy) _PlayerInventory = i;

            GameObject gameObject = (_PlayerInventory != null) ? _PlayerInventory.currentItem : null;
            bool flag = gameObject != null;

            if (flag && !this.currentItemRecorded)
            {
                this.currentItemRecorded = true;
                this.originalSlotIndex = _PlayerInventory.field_Private_Int32_0;
                this.originalItemId = _PlayerInventory.field_Private_Int32_2;
                this.skinApplicationTimer = 0f;
            }
            else if (!flag && isSkinnedItemUsable)
            {
                this.currentItemRecorded = false;
                isSkinnedItemUsable = false;
                this.skinApplicationTimer = 0f;
            }

            if (this.currentItemRecorded && !this.hasSwitchedAway && !isSkinnedItemUsable)
            {
                if (this.skinApplicationTimer < 0.01f && gameObject != null)
                {
                    ItemSkinChangerUtility.SetItemId(gameObject, this.originalItemId, itemSkinId);
                }
                else if (!this.hasSwitchedAway)
                {
                    _PlayerInventory.ScrollItem(-this.originalSlotIndex);
                    this.hasSwitchedAway = true;
                    this.skinApplicationTimer = 0f;
                    return;
                }
            }

            if (this.hasSwitchedAway)
            {
                _PlayerInventory.ScrollItem(this.originalSlotIndex);
                GameObject currentItem = _PlayerInventory.currentItem;
                if (currentItem != null)
                {
                    ItemSkinChangerUtility.SetItemId(currentItem, this.originalItemId, this.originalItemId);
                }
                this.hasSwitchedAway = false;
                isSkinnedItemUsable = true;
                this.skinApplicationTimer = 0f;
            }
        }
    }

    public static class ItemSkinChangerUtility
    {
        private static ItemType GetItemType(int itemId)
        {
            if (itemId == 9) return ItemType.Snowball;
            if (Array.IndexOf<int>(ItemSkinChangerConstants.MELEE_IDS, itemId) >= 0) return ItemType.Melee;
            return ItemType.Gun;
        }

        public static void SetItemId(GameObject item, int referenceId, int targetId)
        {
            ItemType itemType = GetItemType(referenceId);
            if (itemType != ItemType.Melee)
            {
                if (itemType == ItemType.Snowball)
                {
                    var component = item.GetComponent<MonoBehaviour2PublicGathObauTrgumuGaSiBoUnique>();
                    if (component != null) component.itemData.itemID = targetId;
                }
                else
                {
                    var component2 = item.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
                    if (component2 != null) component2.itemData.itemID = targetId;
                }
            }
            else
            {
                var component3 = item.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();
                if (component3 != null) component3.itemData.itemID = targetId;
            }
        }
    }

    public static class SkinManager
    {
        public static bool AutoApply = false;
        public static ulong LocalAutoTargetID = 0;
        private static HashSet<ulong> _networkBlacklist = new HashSet<ulong>();
        private static HashSet<ulong> _alreadyNotifiedBlack = new HashSet<ulong>();
        public static HashSet<ulong> SavedBlacklist = new HashSet<ulong>();
        private static string _fileName = "BOT_Skin_Blacklist.txt";

        private static float _timer = 0f;
        private static float _syncTimer = 0f;
        private static float _interval = 1.0f;
        private const string MSG_SKIN = "BOT:SkinSync";

        public static void Init()
        {
            if (BOTPlugin.HasModNet) ModNet.RegisterMessageHandler(MSG_SKIN, OnSkinPacket);
            LoadSavedList();
        }

        public static void Update()
        {
            if (GameManager.Instance == null) return;
            float dt = Time.deltaTime;
            _timer += dt;
            _syncTimer += dt;

            if (_timer >= _interval)
            {
                _timer = 0f;
                if (AutoApply && LocalAutoTargetID != 0)
                {
                    GameObject p = GetPlayerObject(LocalAutoTargetID);
                    if (p != null) ApplyBlackSkin(p);
                }
                foreach (var id in _networkBlacklist)
                {
                    GameObject p = GetPlayerObject(id);
                    if (p != null) ApplyBlackSkin(p);
                }
                foreach (var id in SavedBlacklist)
                {
                    if (IsPlayerInLobby(id))
                    {
                        GameObject p = GetPlayerObject(id);
                        if (p != null) ApplyBlackSkin(p);
                    }
                }
            }

            if (_syncTimer >= 4f)
            {
                _syncTimer = 0f;
                if (AutoApply && LocalAutoTargetID != 0) SendSkinSync(LocalAutoTargetID, true);
                foreach (var id in SavedBlacklist)
                {
                    if (IsPlayerInLobby(id)) SendSkinSync(id, true);
                }
            }
        }

        public static void LoadSavedList()
        {
            SavedBlacklist.Clear();
            if (File.Exists(_fileName))
            {
                var lines = File.ReadAllLines(_fileName);
                foreach (var line in lines)
                {
                    if (ulong.TryParse(line.Trim(), out ulong id)) SavedBlacklist.Add(id);
                }
            }
        }

        public static void AddCurrentToSavedList()
        {
            ulong targetId = JoinGameManager.SelectedPlayerUid;
            if (targetId == 0) return;
            if (!SavedBlacklist.Contains(targetId))
            {
                SavedBlacklist.Add(targetId);
                File.AppendAllText(_fileName, targetId + Environment.NewLine);
                Utility.ForceMessage($"Added to List: {JoinGameManager.GetSelectedPlayerName()}");
            }
        }

        public static void RemoveCurrentFromSavedList()
        {
            ulong targetId = JoinGameManager.SelectedPlayerUid;
            if (targetId == 0) return;
            if (SavedBlacklist.Contains(targetId))
            {
                SavedBlacklist.Remove(targetId);
                File.WriteAllLines(_fileName, SavedBlacklist.Select(id => id.ToString()).ToArray());
                ResetSelectedPlayer();
                Utility.ForceMessage($"Removed: {JoinGameManager.GetSelectedPlayerName()}");
            }
        }

        public static void ToggleAuto(bool state)
        {
            AutoApply = state;
            if (AutoApply)
            {
                ulong currentSel = JoinGameManager.SelectedPlayerUid;
                if (currentSel == 0) return;
                LocalAutoTargetID = currentSel;
                GameObject p = GetPlayerObject(LocalAutoTargetID);
                if (p != null) ApplyBlackSkin(p);
                SendSkinSync(LocalAutoTargetID, true);
            }
            else
            {
                if (LocalAutoTargetID != 0) SendSkinSync(LocalAutoTargetID, false);
                LocalAutoTargetID = 0;
            }
        }

        public static void MakeSelectedPlayerBlack()
        {
            ulong targetId = JoinGameManager.SelectedPlayerUid;
            if (targetId == 0) return;
            GameObject playerObj = GetPlayerObject(targetId);
            if (playerObj != null) ApplyBlackSkin(playerObj);
            SendSkinSync(targetId, true);
        }

        public static void ResetSelectedPlayer()
        {
            if (AutoApply) ToggleAuto(false);
            ulong targetId = JoinGameManager.SelectedPlayerUid;
            if (targetId == 0) return;
            ResetSkinLocal(targetId);
            SendSkinSync(targetId, false);
        }

        public static void ResetSpecificPlayer(ulong targetId)
        {
            if (targetId == 0) return;
            ResetSkinLocal(targetId);
            SendSkinSync(targetId, false);
        }

        private static void SendSkinSync(ulong targetId, bool enable)
        {
            if (!BOTPlugin.HasModNet) return;
            Packet p = new Packet();
            p.Write((long)targetId);
            p.Write(enable);
            ModNet.SendMessage(MSG_SKIN, p, Utility.GetAllLobbyMembers());
        }

        private static void OnSkinPacket(ulong senderId, Packet p)
        {
            if (GameManager.Instance == null) return;
            try
            {
                ulong targetId = (ulong)p.ReadLong();
                bool enable = p.ReadBool();
                if (enable)
                {
                    if (!_networkBlacklist.Contains(targetId)) _networkBlacklist.Add(targetId);
                    GameObject obj = GetPlayerObject(targetId);
                    if (obj != null) ApplyBlackSkin(obj);
                }
                else
                {
                    if (_networkBlacklist.Contains(targetId)) _networkBlacklist.Remove(targetId);
                    if (SavedBlacklist.Contains(targetId))
                    {
                        SavedBlacklist.Remove(targetId);
                        File.WriteAllLines(_fileName, SavedBlacklist.Select(id => id.ToString()).ToArray());
                    }
                    if (LocalAutoTargetID == targetId) LocalAutoTargetID = 0;
                    ResetSkinLocal(targetId);
                }
            }
            catch { }
        }

        private static void ResetSkinLocal(ulong targetId)
        {
            try
            {
                GameObject playerObj = GetPlayerObject(targetId);
                if (playerObj != null)
                {
                    var renderers = playerObj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r == null || r.material == null) continue;
                        if (r.name.Equals("PlayerModel") || r.name.StartsWith("LOD"))
                        {
                            r.material.color = Color.white;
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", Color.white);
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.white);
                        }
                        if (r.name.Equals("Pants") || r.name.Equals("Sweater")) r.enabled = true;
                    }
                }
            }
            catch { }
        }

        private static void ApplyBlackSkin(GameObject player)
        {
            if (player == null) return;
            var renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                try
                {
                    if (r == null) continue;
                    string objName = r.gameObject.name;
                    if (objName.Contains("Particle") || objName.Contains("Trail") || objName.Contains("Aura") || objName.Contains("Beam")) continue;
                    if (objName == "Pants" || objName == "Sweater") { r.enabled = false; continue; }
                    if (r.material == null) continue;

                    bool colorChanged = false;
                    if (r.material.HasProperty("_Color")) { r.material.SetColor("_Color", Color.black); colorChanged = true; }
                    if (r.material.HasProperty("_BaseColor")) { r.material.SetColor("_BaseColor", Color.black); colorChanged = true; }
                    if (!colorChanged) r.material.color = Color.black;
                }
                catch { continue; }
            }
        }

        private static GameObject GetPlayerObject(ulong steamId)
        {
            if (GameManager.Instance == null) return null;
            try
            {
                if (SteamManager.Instance != null && steamId == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID)
                {
                    var local = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                    if (local != null) return local.gameObject;
                }
                if (GameManager.Instance.activePlayers != null && GameManager.Instance.activePlayers.ContainsKey(steamId))
                {
                    var p = GameManager.Instance.activePlayers[steamId];
                    if (p != null) return p.gameObject;
                }
            }
            catch { }
            return null;
        }

        private static bool IsPlayerInLobby(ulong steamId)
        {
            if (GameManager.Instance == null) return false;
            if (SteamManager.Instance != null && steamId == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID) return true;
            return GameManager.Instance.activePlayers != null && GameManager.Instance.activePlayers.ContainsKey(steamId);
        }
    }

    public static class BotTabManager
    {
        public static bool enableTabColor = false;
        public static string tabColor = "#FFFF00";
        public static Dictionary<ulong, string[]> KnownBotUsers = new Dictionary<ulong, string[]>();
        private static float _syncTimer = 0f;

        public static void InitNetwork()
        {
            if (BOTPlugin.HasModNet)
            {
                ModNet.RegisterMessageHandler("BOT:TabNameSync", (senderId, packet) =>
                {
                    string remoteCustomName = packet.ReadString();
                    string remoteVersion = "Old";
                    string remoteColor = "None";
                    try
                    {
                        if (packet.UnreadLength() > 0)
                        {
                            remoteVersion = packet.ReadString();
                            if (packet.UnreadLength() > 0) remoteColor = packet.ReadString();
                        }
                    }
                    catch { }
                    if (!remoteColor.StartsWith("#") && remoteColor != "None") remoteColor = "None";
                    KnownBotUsers[senderId] = new string[] { remoteCustomName, remoteVersion, remoteColor };
                });
            }
        }

        public static void Update()
        {
            if (!BOTPlugin.HasModNet || SteamManager.Instance == null) return;
            _syncTimer += Time.deltaTime;
            if (_syncTimer >= 3.0f)
            {
                _syncTimer = 0f;
                try
                {
                    Packet p = new Packet();
                    p.Write(BOTPlugin.ConfigCustomName.Value);
                    p.Write(BOTPlugin.CurrentBotVersion);
                    p.Write(enableTabColor ? tabColor : "None");
                    ModNet.SendMessage("BOT:TabNameSync", p, Utility.GetAllLobbyMembers());
                }
                catch { }
            }
        }

        public static void ApplyBotTag(PlayerListingPrefab prefab, ulong steamId)
        {
            if (prefab == null || prefab.username == null || steamId == 0) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            bool isMe = (steamId == myId);
            bool isBotUser = KnownBotUsers.ContainsKey(steamId);
            bool isBlacklisted = (SkinManager.SavedBlacklist != null && SkinManager.SavedBlacklist.Contains(steamId)) ||
                                 (BlacklistManager.GlobalBlacklistedUsers != null && BlacklistManager.GlobalBlacklistedUsers.Contains(steamId));

            if (!isMe && !isBotUser && !isBlacklisted) return;

            try
            {
                var txt = prefab.username;
                txt.richText = true;
                string nativeText = txt.text;

                if (isBlacklisted)
                {
                    txt.text = $"{nativeText} <color=#FF3333>[Blacklist]</color>";
                    return;
                }

                if (isMe)
                {
                    if (enableTabColor) txt.text = $"<color={tabColor}>{nativeText}</color> <color=#00FFFF>[v{BOTPlugin.CurrentBotVersion}]</color>";
                    else txt.text = $"{nativeText} <color=#00FFFF>[v{BOTPlugin.CurrentBotVersion}]</color>";
                }
                else if (isBotUser)
                {
                    string botName = KnownBotUsers[steamId][0];
                    string version = KnownBotUsers[steamId][1];
                    string hisColor = KnownBotUsers[steamId][2];
                    string versionTag = (version == "Old") ? "<color=red>[vOld]</color>" : $"<color=#00FFFF>[v{version}]</color>";

                    if (hisColor != "None") txt.text = $"<color={hisColor}>{nativeText} [BOT: {botName}]</color> {versionTag}";
                    else txt.text = $"{nativeText} [BOT: {botName}] {versionTag}";
                }
            }
            catch { }
        }
    }

    public static class MarkerManager
    {
        public static bool enabled = true;
        public static HashSet<ulong> MarkedPlayers = new HashSet<ulong>();
        private static Dictionary<ulong, int> _playerTypes = new Dictionary<ulong, int>();
        private static Dictionary<ulong, GameObject> _markers = new Dictionary<ulong, GameObject>();
        public static int myMarkerColorIndex = 1;

        public static void Toggle(bool state)
        {
            enabled = state;
            if (!enabled)
            {
                foreach (var marker in _markers.Values) if (marker != null) GameObject.Destroy(marker);
                _markers.Clear();
            }

            if (BOTPlugin.HasModNet)
            {
                try
                {
                    Packet p = new Packet();
                    p.Write(enabled);
                    p.Write(myMarkerColorIndex);
                    ModNet.SendMessage("BOT:Marker", p, Utility.GetAllLobbyMembers());
                }
                catch { }
            }
        }

        public static void SetMarker(ulong steamId, bool active, int type = 1)
        {
            if (active)
            {
                if (!MarkedPlayers.Contains(steamId)) MarkedPlayers.Add(steamId);
                if (_playerTypes.ContainsKey(steamId)) _playerTypes[steamId] = type;
                else _playerTypes.Add(steamId, type);

                if (_markers.ContainsKey(steamId) && _markers[steamId] != null) RemoveMarkerObj(steamId, false);
            }
            else
            {
                if (MarkedPlayers.Contains(steamId)) MarkedPlayers.Remove(steamId);
                RemoveMarkerObj(steamId, true);
            }
        }

        public static void Update()
        {
            if (!enabled || GameManager.Instance == null || GameManager.Instance.activePlayers == null)
            {
                if (_markers.Count > 0)
                {
                    foreach (var marker in _markers.Values) if (marker != null) GameObject.Destroy(marker);
                    _markers.Clear();
                }
                return;
            }

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var keysToRemove = new List<ulong>();

            foreach (var id in MarkedPlayers)
            {
                if (id == myId) continue;
                if (!GameManager.Instance.activePlayers.ContainsKey(id) || GameManager.Instance.activePlayers[id].dead)
                {
                    if (_markers.ContainsKey(id) && _markers[id] != null)
                    {
                        GameObject.Destroy(_markers[id]);
                        _markers.Remove(id);
                    }
                    keysToRemove.Add(id);
                    continue;
                }

                var player = GameManager.Instance.activePlayers[id];
                if (!_markers.ContainsKey(id) || _markers[id] == null)
                {
                    int type = _playerTypes.ContainsKey(id) ? _playerTypes[id] : 1;
                    CreateMarkerObj(id, type);
                }

                if (_markers.TryGetValue(id, out GameObject marker))
                {
                    if (player.transform == null) continue;
                    marker.transform.position = player.transform.position + Vector3.up * 3.5f;
                    if (Camera.main) marker.transform.LookAt(Camera.main.transform);
                    marker.transform.Rotate(0, 180, 0);
                }
            }

            foreach (var k in keysToRemove) MarkedPlayers.Remove(k);
        }

        private static void CreateMarkerObj(ulong id, int type)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Marker_{id}";
            GameObject.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            var r = go.GetComponent<Renderer>();
            if (r)
            {
                Color c = Color.cyan;
                if (type == 1) c = Color.cyan;
                else if (type == 2) c = Color.red;
                else if (type == 3) c = Color.green;
                else if (type == 4) c = Color.blue;
                else if (type == 5) c = Color.yellow;
                else if (type == 6) c = Color.magenta;
                else if (type == 7) c = Color.white;
                else if (type == 8) c = new Color(1f, 0.84f, 0f);
                else if (type == 9) c = Color.black;

                r.material.color = c;
                if (type != 9)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", c);
                }
            }
            _markers[id] = go;
        }

        private static void RemoveMarkerObj(ulong id, bool fullRemove)
        {
            if (_markers.ContainsKey(id))
            {
                if (_markers[id] != null) GameObject.Destroy(_markers[id]);
                _markers.Remove(id);
            }
            if (fullRemove)
            {
                if (_playerTypes.ContainsKey(id)) _playerTypes.Remove(id);
            }
        }

        public static string GetColorName(int index)
        {
            switch (index)
            {
                case 1: return "<color=cyan>Cyan</color>";
                case 2: return "<color=red>Red</color>";
                case 3: return "<color=green>Green</color>";
                case 4: return "<color=blue>Blue</color>";
                case 5: return "<color=yellow>Yellow</color>";
                case 6: return "<color=magenta>Magenta</color>";
                case 7: return "<color=white>White</color>";
                case 8: return "<color=#FFD700>Gold</color>";
                case 9: return "<color=#444444>Black</color>";
                default: return "Unknown";
            }
        }
    }
}
