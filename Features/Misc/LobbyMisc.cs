using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class CustomKickBanManager
    {
        public static bool enabled = false;
        public static int kickReason = 1488;
        public static int banReason = 69;
        public static int leaveReason = 420;
        public static int lostConnectionReason = -300;

        public static void Init() { Harmony.CreateAndPatchAll(typeof(CustomKickBanManager)); }

        [HarmonyPatch(typeof(MonoBehaviourPublicInInUnique), "LobbyClosed")]
        [HarmonyPrefix]
        public static bool Prefix_LobbyClosed(ulong param_0, MonoBehaviourPublicCSDi2UIInstObUIloDiUnique.EnumNPublicSealedvaSeKiBaLo5vUnique param_1)
        {
            if (!enabled || !Utility.IsHost()) return true;
            try
            {
                ObjectPublicIDisposableLi1ByInByBoUnique packet = new ObjectPublicIDisposableLi1ByInByBoUnique(8);
                int customReasonValue = 0;
                switch (param_1)
                {
                    case MonoBehaviourPublicCSDi2UIInstObUIloDiUnique.EnumNPublicSealedvaSeKiBaLo5vUnique.ServerClosed: customReasonValue = leaveReason; break;
                    case MonoBehaviourPublicCSDi2UIInstObUIloDiUnique.EnumNPublicSealedvaSeKiBaLo5vUnique.Kicked: customReasonValue = kickReason; break;
                    case MonoBehaviourPublicCSDi2UIInstObUIloDiUnique.EnumNPublicSealedvaSeKiBaLo5vUnique.Banned: customReasonValue = banReason; break;
                    case MonoBehaviourPublicCSDi2UIInstObUIloDiUnique.EnumNPublicSealedvaSeKiBaLo5vUnique.LostConnection: customReasonValue = lostConnectionReason; break;
                }
                packet.Method_Public_Void_Int32_0(customReasonValue);
                MonoBehaviourPublicInInUnique.Method_Private_Static_Void_UInt64_ObjectPublicIDisposableLi1ByInByBoUnique_0(param_0, packet);
            }
            catch { }
            return false;
        }
    }

    public static class CustomServerListManager
    {
        public static string CustomName = "";
        public static string CustomPrefix = "";
        public static string CustomIcon = "";
        public static string CustomVersion = "";
        private static ulong _lastHostedLobbyId = 0;
        private static bool _isConfigLoaded = false;
        private static string _originalLobbyName = "";

        public static void LoadConfig()
        {
            if (_isConfigLoaded) return;
            CustomName = PlayerPrefs.GetString("BOT_SvrName", "");
            CustomPrefix = PlayerPrefs.GetString("BOT_SvrPrefix", "");
            CustomIcon = PlayerPrefs.GetString("BOT_SvrIcon", "");
            CustomVersion = PlayerPrefs.GetString("BOT_SvrVer", "");
            _isConfigLoaded = true;
        }

        public static void SaveConfig()
        {
            PlayerPrefs.SetString("BOT_SvrName", CustomName);
            PlayerPrefs.SetString("BOT_SvrPrefix", CustomPrefix);
            PlayerPrefs.SetString("BOT_SvrIcon", CustomIcon);
            PlayerPrefs.SetString("BOT_SvrVer", CustomVersion);
            PlayerPrefs.Save();
        }

        public static void Update()
        {
            LoadConfig();
            if (Utility.IsHost() && SteamManager.Instance != null && SteamManager.Instance.currentLobby.m_SteamID != 0)
            {
                ulong currentId = SteamManager.Instance.currentLobby.m_SteamID;
                if (currentId != _lastHostedLobbyId)
                {
                    _lastHostedLobbyId = currentId;
                    _originalLobbyName = SteamMatchmaking.GetLobbyData(SteamManager.Instance.currentLobby, "LobbyName");
                    ApplyServerSettings();
                }
            }
            else if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0)
            {
                _lastHostedLobbyId = 0;
                _originalLobbyName = "";
            }
        }

        public static void ApplyServerSettings()
        {
            if (!Utility.IsHost() || SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;
            var lobby = SteamManager.Instance.currentLobby;
            string nameToApply = string.IsNullOrEmpty(CustomName) ? $"<color=#FF0000>{(string.IsNullOrEmpty(_originalLobbyName) ? "Server" : _originalLobbyName)}</color>" : CustomName;
            string prefixToApply = string.IsNullOrEmpty(CustomPrefix) ? $"<color=#00FFFF>[{(string.IsNullOrEmpty(BOTPlugin.ConfigCustomName.Value) ? "BOT" : BOTPlugin.ConfigCustomName.Value)}]</color>" : CustomPrefix;

            SteamMatchmaking.SetLobbyData(lobby, "LobbyName", Utility.StripColorTags(nameToApply));
            SteamMatchmaking.SetLobbyData(lobby, "LobbyNameColor", nameToApply);
            SteamMatchmaking.SetLobbyData(lobby, "ServerType", prefixToApply);
            SteamMatchmaking.SetLobbyData(lobby, "LobbyIcon", CustomIcon);
            SteamMatchmaking.SetLobbyData(lobby, "FakeVersionUI", CustomVersion);
        }

        [HarmonyPatch(typeof(SerevrUIPrefab), "SetUI")]
        public static class SetUIPatch
        {
            [HarmonyPostfix]
            public static void Postfix(SerevrUIPrefab __instance, CSteamID param_1, int param_2)
            {
                try
                {
                    var allTexts = __instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    foreach (var txt in allTexts) if (txt != null) txt.richText = true;

                    string titleColor = SteamMatchmaking.GetLobbyData(param_1, "LobbyNameColor");
                    if (!string.IsNullOrEmpty(titleColor) && __instance.title != null) __instance.title.text = titleColor;

                    if (__instance.versionNumber != null)
                    {
                        string fakeVer = SteamMatchmaking.GetLobbyData(param_1, "FakeVersionUI");
                        string prefix = SteamMatchmaking.GetLobbyData(param_1, "ServerType");
                        string finalVersionText = __instance.versionNumber.text;
                        if (!string.IsNullOrEmpty(fakeVer)) finalVersionText = fakeVer;
                        if (!string.IsNullOrEmpty(prefix)) finalVersionText = prefix + " " + finalVersionText;
                        __instance.versionNumber.text = finalVersionText;
                    }
                }
                catch { }
            }
        }
    }

    public static class ServerListViewerManager
    {

        public static KeyCode ToggleKey = KeyCode.F2;

        private static GameObject _serverListObj = null;
        private static bool _initialized = false;
        private static float _initRetryTimer = 0f;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(ServerListViewerManager));
            Utility.Log("Passive Server List Viewer Initialized.");
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicGalomeGacrsemiGaupObUnique), "Start")]
        [HarmonyPostfix]
        public static void Postfix_MenuStart()
        {
            if (!_initialized)
            {
                InitializeServerList();
            }
        }

        private static void InitializeServerList()
        {
            if (_serverListObj != null)
            {
                _initialized = true;
                return;
            }

            try
            {
                GameObject originalUI = GameObject.Find("UI");
                if (originalUI != null)
                {

                    var serverListComponent = originalUI.GetComponentInChildren<MonoBehaviourPublicGaseliGaVibuToclmiToUnique>(true);
                    if (serverListComponent != null)
                    {

                        GameObject standaloneUI = new GameObject("BOT_ServerList_Standalone");
                        Canvas canvas = standaloneUI.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.sortingOrder = 1000;

                        CanvasScaler scaler = standaloneUI.AddComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920f, 1080f);
                        standaloneUI.AddComponent<GraphicRaycaster>();

                        Transform serverListTransform = serverListComponent.transform;
                        while (serverListTransform.parent != originalUI.transform && serverListTransform.parent != null)
                        {
                            serverListTransform = serverListTransform.parent;
                        }

                        GameObject serverListClone = UnityEngine.Object.Instantiate(serverListTransform.gameObject, standaloneUI.transform);
                        serverListClone.transform.localPosition = Vector3.zero;

                        RectTransform rect = serverListClone.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            rect.anchorMin = new Vector2(0f, 0f);
                            rect.anchorMax = new Vector2(1f, 1f);
                            rect.pivot = new Vector2(0.5f, 0.5f);
                            rect.sizeDelta = Vector2.zero;
                            rect.localPosition = Vector3.zero;
                        }

                        UnityEngine.Object.DontDestroyOnLoad(standaloneUI);
                        _serverListObj = standaloneUI;

                        for (int i = 0; i < serverListClone.transform.childCount; i++)
                        {
                            serverListClone.transform.GetChild(i).gameObject.SetActive(true);
                        }

                        _serverListObj.SetActive(false);
                        _initialized = true;
                        Utility.Log("Server List Duplicated Successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log("Server List Init Error: " + ex.Message);
                _serverListObj = null;
                _initialized = false;
            }
        }

        public static void Update()
        {
            if (!_initialized)
            {
                _initRetryTimer += Time.unscaledDeltaTime;
                if (_initRetryTimer >= 1f)
                {
                    _initRetryTimer = 0f;
                    InitializeServerList();
                }
                return;
            }

            if (_serverListObj == null || ToggleKey == KeyCode.None) return;

            if (Input.GetKeyDown(ToggleKey))
            {
                bool newActiveState = !_serverListObj.activeSelf;
                _serverListObj.SetActive(newActiveState);

                if (newActiveState)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    Transform child = _serverListObj.transform.GetChild(0);
                    child.gameObject.SetActive(true);

                    for (int i = 0; i < child.childCount; i++)
                    {
                        child.GetChild(i).gameObject.SetActive(true);
                    }
                }
                else
                {

                    if (!Variables.menuTrigger)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            if (_serverListObj.activeSelf)
            {
                Transform child = _serverListObj.transform.GetChild(0);
                if (!child.gameObject.activeSelf)
                {
                    _serverListObj.SetActive(false);
                    if (!Variables.menuTrigger)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicObRaovInObSiSiUnique), "StartTransition")]
        [HarmonyPostfix]
        public static void Postfix_OnStartTransition()
        {
            if (_initialized && _serverListObj != null && _serverListObj.activeSelf)
            {
                _serverListObj.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        [HarmonyPatch(typeof(SteamManager), "LeaveLobby")]
        [HarmonyPostfix]
        public static void Postfix_LeaveLobby()
        {
            if (_serverListObj != null) _serverListObj.SetActive(false);
        }

        [HarmonyPatch(typeof(SteamManager), "JoinLobby")]
        [HarmonyPrefix]
        public static void Prefix_JoinLobby()
        {
            if (SteamManager.Instance != null && SteamManager.Instance.currentLobby != (CSteamID)0UL)
            {
                SteamManager.Instance.LeaveLobby();
                if (_serverListObj != null) _serverListObj.SetActive(false);
            }
        }
    }

    public static class TeamRadarManager
    {
        public static bool IsEnemy(Vector3 myPos, Vector3 targetPos)
        {
            string mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            if (!mapName.Contains("dodgy") && !mapName.Contains("snowbrawl") && !mapName.Contains("team")) return true;
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return true;

            float maxX = -9999f, minX = 9999f, maxZ = -9999f, minZ = 9999f;
            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                var pl = kvp.Value;
                if (pl == null || pl.dead) continue;
                Vector3 pos = pl.transform.position;
                if (pos.x > maxX) maxX = pos.x; if (pos.x < minX) minX = pos.x;
                if (pos.z > maxZ) maxZ = pos.z; if (pos.z < minZ) minZ = pos.z;
            }
            return (maxZ - minZ) > (maxX - minX) ? (myPos.z > 0) != (targetPos.z > 0) : (myPos.x > 0) != (targetPos.x > 0);
        }
    }

    public static class SharedObjectDestroyer
    {
        public static bool enabled = false;
        public static bool destroyAll = false;
        public static int selectedObjectId = -1;
        private static float _timer = 0f;
        private static List<int> _availableIds = new List<int>();

        public static void Update()
        {
            if (!enabled || GameManager.Instance == null) return;
            _timer += Time.deltaTime;
            if (_timer >= 0.01f)
            {
                _timer = 0f;
                ulong proxyId = GetAliveProxyId();
                if (proxyId == 0) return;

                var dict = MonoBehaviourPublicDi2InObInObInUnique.Instance?.field_Private_Dictionary_2_Int32_MonoBehaviourPublicInidBoskUnique_0;
                if (dict == null) return;

                if (destroyAll)
                {
                    foreach (var kvp in dict) TryDestroy(proxyId, kvp.Key);
                }
                else if (selectedObjectId != -1)
                {
                    TryDestroy(proxyId, selectedObjectId);
                }
            }
        }

        private static ulong GetAliveProxyId()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (JoinGameManager.SelectedPlayerUid != 0 && IsAlive(JoinGameManager.SelectedPlayerUid)) return JoinGameManager.SelectedPlayerUid;
            if (IsAlive(myId)) return myId;
            if (GameManager.Instance?.activePlayers != null)
            {
                foreach (var kvp in GameManager.Instance.activePlayers)
                    if (kvp.Value != null && !kvp.Value.dead) return kvp.Key;
            }
            return 0;
        }

        private static bool IsAlive(ulong id) => GameManager.Instance?.activePlayers != null && GameManager.Instance.activePlayers.ContainsKey(id) && !GameManager.Instance.activePlayers[id].dead;

        private static void TryDestroy(ulong proxyId, int objectId)
        {
            for (int i = 0; i < 200; i++)
            {
                try { MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(proxyId, 0, Vector3.up, 9, objectId); } catch { }
            }
        }

        public static void CycleObject()
        {
            var dict = MonoBehaviourPublicDi2InObInObInUnique.Instance?.field_Private_Dictionary_2_Int32_MonoBehaviourPublicInidBoskUnique_0;
            if (dict == null || dict.Count == 0) { selectedObjectId = -1; return; }
            _availableIds.Clear();
            foreach (var key in dict.Keys) _availableIds.Add(key);
            int nextIndex = (_availableIds.IndexOf(selectedObjectId) + 1) % _availableIds.Count;
            selectedObjectId = _availableIds[nextIndex];
        }

        public static string GetCurrentObjectName()
        {
            if (destroyAll) return "<color=yellow>ALL OBJECTS</color>";
            if (selectedObjectId == -1) return "Unknown";
            var dict = MonoBehaviourPublicDi2InObInObInUnique.Instance?.field_Private_Dictionary_2_Int32_MonoBehaviourPublicInidBoskUnique_0;
            if (dict != null && dict.ContainsKey(selectedObjectId))
            {
                return $"{dict[selectedObjectId].name.Replace("(Clone)", "").Trim()} (ID: {selectedObjectId})";
            }
            return $"ID: {selectedObjectId}";
        }
    }

    public static class SuicideManager
    {
        public static void Execute()
        {
            if (SteamManager.Instance != null && SteamManager.Instance.field_Private_CSteamID_0.m_SteamID != 0)
            {
                ClientSendClass.PlayerDied(SteamManager.Instance.field_Private_CSteamID_0.m_SteamID, Vector3.zero);
                Utility.ForceMessage("You committed suicide.");
            }
        }
    }

    public static class GlobalStatsManager
    {
        private const string SERVER_URL = "";
        public static int OnlineUsers = 1;
        public static bool hiddenMode = false;
        private static float _heartbeatTimer = 0f;
        private static string _processInstanceId = "";

        public static void Init()
        {
            hiddenMode = PlayerPrefs.GetInt("BOT_HiddenMode", 0) == 1;
            try
            {
                _processInstanceId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            }
            catch
            {
                _processInstanceId = UnityEngine.Random.Range(10000, 99999).ToString();
            }
            SendHeartbeat();
        }

        public static void Update()
        {
            _heartbeatTimer += Time.unscaledDeltaTime;
            if (_heartbeatTimer >= 8.0f)
            {
                _heartbeatTimer = 0f;
                SendHeartbeat();
            }
        }

        public static void SendHeartbeat()
        {

            ulong myId = 0;
            try
            {
                if (SteamManager.Instance != null && SteamManager.Instance.field_Private_CSteamID_0 != null)
                {
                    myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
                }
                else if (SteamUser.GetSteamID().m_SteamID != 0)
                {
                    myId = SteamUser.GetSteamID().m_SteamID;
                }
            }
            catch { }

            string myName = "User";
            try
            {
                if (BOTPlugin.ConfigCustomName != null && !string.IsNullOrEmpty(BOTPlugin.ConfigCustomName.Value))
                    myName = BOTPlugin.ConfigCustomName.Value;
            }
            catch { }

            bool isHidden = hiddenMode;
            string procId = !string.IsNullOrEmpty(_processInstanceId) ? _processInstanceId : "1";

            if (string.IsNullOrEmpty(SERVER_URL)) return;

            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | (System.Net.SecurityProtocolType)4032 | System.Net.SecurityProtocolType.Tls12;
                    System.Net.ServicePointManager.Expect100Continue = false;

                    string uniqueKey = myId > 0 ? $"{myId}_{procId}" : $"Anon_{procId}";
                    string fullUrl = $"{SERVER_URL}?id={myId}&clientId={uniqueKey}&name={System.Uri.EscapeDataString(myName)}&hidden={(isHidden ? "1" : "0")}";

                    using (var client = new System.Net.WebClient())
                    {
                        client.Headers[System.Net.HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = System.Text.Encoding.UTF8;

                        string result = client.DownloadString(fullUrl);
                        if (int.TryParse(result.Trim(), out int count))
                        {
                            OnlineUsers = Mathf.Max(1, count);
                        }
                    }
                }
                catch { }
            });
        }

        public static void ToggleHiddenMode()
        {
            hiddenMode = !hiddenMode;
            PlayerPrefs.SetInt("BOT_HiddenMode", hiddenMode ? 1 : 0);
            PlayerPrefs.Save();
            SendHeartbeat();
        }
    }

    public class BOTNameSetup : MonoBehaviour
    {
        public static BOTNameSetup Instance;
        private GameObject _setupPanel;
        private InputField _inputField;

        public BOTNameSetup(IntPtr ptr) : base(ptr) { }

        public static void Create(Transform parent)
        {
            GameObject go = new GameObject("BOT_NameSetupUI");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<BOTNameSetup>();
            Instance.Init();
        }

        private void Init()
        {
            _setupPanel = this.gameObject;
            RectTransform rt = _setupPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 200);

            Image bg = _setupPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);
            _setupPanel.AddComponent<Outline>().effectColor = Color.magenta;

            GameObject titleObj = new GameObject("T");
            titleObj.transform.SetParent(_setupPanel.transform, false);
            Text t = titleObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = "SET YOUR BOT IDENTITY\n<size=12>(Press Enter to Confirm)</size>";
            t.alignment = TextAnchor.UpperCenter;
            t.supportRichText = true;
            t.rectTransform.anchoredPosition = new Vector2(0, 70);
            t.rectTransform.sizeDelta = new Vector2(380, 50);

            GameObject inputObj = new GameObject("Input");
            inputObj.transform.SetParent(_setupPanel.transform, false);
            _inputField = inputObj.AddComponent<InputField>();
            Image ibg = inputObj.AddComponent<Image>();
            ibg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            GameObject inputText = new GameObject("Text");
            inputText.transform.SetParent(inputObj.transform, false);
            Text it = inputText.AddComponent<Text>();
            it.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            it.alignment = TextAnchor.MiddleCenter;
            _inputField.textComponent = it;

            RectTransform irt = inputObj.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(300, 40);
            irt.anchoredPosition = new Vector2(0, -10);
            _setupPanel.SetActive(false);
        }

        public void Show()
        {
            _setupPanel.SetActive(true);
            _inputField.ActivateInputField();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!_setupPanel.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string name = _inputField.text.Trim();
                if (name.Length >= 2)
                {
                    BOTPlugin.ConfigCustomName.Value = name;
                    BOTPlugin.ConfigIsNameSet.Value = true;
                    BOTPlugin.SaveConfigDebounced();
                    _setupPanel.SetActive(false);
                    Variables.menuTrigger = true;
                    BOTPlugin.BroadcastIdentity();
                    Utility.ForceMessage($"Welcome, {name}!");
                }
            }
        }
    }
}
