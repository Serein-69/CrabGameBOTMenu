using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SteamworksNative;
using UnhollowerRuntimeLib;

namespace BOTMenuMod
{
    public class BlacklistManager : MonoBehaviour
    {
        public BlacklistManager(IntPtr ptr) : base(ptr) { }

        private const string GIST_URL = "";
        private volatile bool _hasStartedCheck = false;
        private volatile bool _checkInFlight = false;
        private float _retryTimer = 0f;

        public static HashSet<ulong> GlobalBlacklistedUsers = new HashSet<ulong>();

        private void Update()
        {
            if (_hasStartedCheck) return;
            if (_checkInFlight) return;

            _retryTimer -= Time.deltaTime;
            if (_retryTimer > 0f) return;

            if (SteamManager.Instance != null && SteamManager.Instance.field_Private_CSteamID_0.m_SteamID != 0)
            {
                _checkInFlight = true;
                ulong mySteamID = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
                var worker = new Thread(() => { CheckThread(mySteamID); }) { IsBackground = true };
                worker.Start();
            }
        }

        private void CheckThread(ulong myId)
        {
            bool succeeded = false;
            if (string.IsNullOrEmpty(GIST_URL))
            {
                _hasStartedCheck = true;
                _checkInFlight = false;
                return;
            }
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)4032;

                using (TimeoutWebClient client = new TimeoutWebClient())
                {
                    client.Timeout = 8000;
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    string finalUrl = GIST_URL + "?t=" + DateTime.Now.Ticks.ToString();
                    string remoteData = client.DownloadString(finalUrl);

                    if (!string.IsNullOrEmpty(remoteData))
                    {
                        ParseBlacklist(remoteData);
                        succeeded = true;
                        if (GlobalBlacklistedUsers.Contains(myId))
                        {
                            Utility.Log($"[SECURITY] User {myId} detected in Blacklist!");
                            SelfDestruct();
                        }
                    }
                }
            }
            catch { succeeded = false; }
            finally
            {
                _hasStartedCheck = succeeded;
                _retryTimer = succeeded ? 0f : 2f;
                _checkInFlight = false;
            }
        }

        private void ParseBlacklist(string content)
        {
            GlobalBlacklistedUsers.Clear();
            string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (ulong.TryParse(line.Trim(), out ulong badId))
                {
                    GlobalBlacklistedUsers.Add(badId);
                }
            }
        }

        private void SelfDestruct()
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string batPath = Path.Combine(Path.GetDirectoryName(dllPath), "cleanup.bat");
                int currentPid = Process.GetCurrentProcess().Id;

                using (StreamWriter sw = File.CreateText(batPath))
                {
                    sw.WriteLine("@echo off");
                    sw.WriteLine("timeout /t 2 > NUL");
                    sw.WriteLine($"taskkill /F /PID {currentPid} > NUL 2>&1");
                    sw.WriteLine($"del /F /Q \"{dllPath}\"");
                    sw.WriteLine("(goto) 2>nul & del \"%~f0\"");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch { }
            Process.GetCurrentProcess().Kill();
        }
    }

    public class TimeoutWebClient : WebClient
    {
        public int Timeout = 8000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request != null) request.Timeout = Timeout;
            return request;
        }
    }

    public static class AutoAdvertiser
    {
        public static bool enabled = false;
        private static float _adTimer = 5f;

        public static void Update()
        {
            if (!enabled || GameManager.Instance == null || SteamManager.Instance == null) return;
            string adText = BOTPlugin.ConfigAutoAdMessage != null ? BOTPlugin.ConfigAutoAdMessage.Value : "";
            if (string.IsNullOrEmpty(adText))
            {
                enabled = false;
                return;
            }
            _adTimer -= Time.deltaTime;
            if (_adTimer <= 0f)
            {
                try
                {
                    ClientSendClass.SendChatMessage(adText);
                }
                catch { }
                _adTimer = UnityEngine.Random.Range(45f, 70f);
            }
        }
    }

    public static class AutoReadyManager
    {
        private static bool hasReadied = false;
        private static float timer = 0f;
        private static float retryAt = 0f;

        public static void Update()
        {
            if (!ShadowModePatches.enableAutoReady) return;
            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null) return;
            try
            {
                if (lobbyManager.gameMode.id == 0)
                {
                    timer += Time.deltaTime;
                    if (!hasReadied && timer > 2.0f && Time.time >= retryAt)
                    {
                        if (PressReady()) hasReadied = true;
                        else retryAt = Time.time + 2f;
                    }
                }
                else { hasReadied = false; timer = 0f; retryAt = 0f; }
            }
            catch { }
        }

        private static bool PressReady()
        {
            try
            {
                var btnObj = GameObject.Find("Button/Button");
                if (btnObj == null) return false;
                var readyScript = btnObj.GetComponent<ReadyButtonClass>();
                if (readyScript == null) return false;
                readyScript.field_Private_Boolean_0 = true;
                readyScript.TryInteract();
                Utility.ForceMessage("Auto Readied!");
                return true;
            }
            catch { return false; }
        }
    }

    public static class BoxOpenerManager
    {
        private static float lastBoxOpenTime = 0f;
        private static int openedCount = 0;
        private static bool isRunning = false;
        private static float itemScanTimer = 0f;
        private static readonly List<GameObject> cachedItems = new List<GameObject>();

        public static void Update()
        {
            if (!ShadowModePatches.enableAutoBox)
            {
                if (isRunning) isRunning = false;
                return;
            }

            if (!isRunning)
            {
                isRunning = true;
                openedCount = 0;
                lastBoxOpenTime = Time.time;
            }

            itemScanTimer -= Time.deltaTime;

            if (ShadowModePatches.autoBoxCount > 0 && openedCount >= ShadowModePatches.autoBoxCount)
            {
                ShadowModePatches.enableAutoBox = false;
                Utility.ForceMessage($"Target reached ({openedCount}). Stopping.");
                return;
            }

            float delaySec = ShadowModePatches.autoBoxDelayMs / 1000f;
            if (Time.time - lastBoxOpenTime >= delaySec)
            {
                if (TryOpenNextBox())
                {
                    lastBoxOpenTime = Time.time;
                    openedCount++;
                }
            }
        }

        private static bool TryOpenNextBox()
        {
            if (itemScanTimer <= 0f)
            {
                itemScanTimer = 1f;
                cachedItems.Clear();
                var foundItems = GameObject.FindObjectsOfType<GameObject>();
                foreach (var go in foundItems)
                {
                    if (go != null && go.name == "CosmeticItemUi(Clone)") cachedItems.Add(go);
                }
            }
            if (cachedItems.Count == 0) return false;

            foreach (var item in cachedItems)
            {
                if (item == null || !item.activeInHierarchy) continue;
                var itemText = item.GetComponentInChildren<TextMeshProUGUI>();
                if (itemText == null || !itemText.text.Contains("Crab Box")) continue;
                var itemButton = item.GetComponent<Button>();
                if (itemButton == null) continue;

                SimulateButtonClick(itemButton);
                if (OpenBox()) return true;
                Utility.Log("[BoxOpener] OpenBox returned no result for Crab Box");
                return false;
            }
            return false;
        }

        private static bool OpenBox()
        {
            bool invoked = false;
            try
            {
                var allObjects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<UnityEngine.Object>());
                if (allObjects == null) return false;
                var targetScripts = allObjects.Where(obj => obj?.TryCast<GeneralUiInventoryItemClick>() != null).Select(obj => obj.TryCast<GeneralUiInventoryItemClick>()).ToList();
                foreach (var script in targetScripts)
                {
                    if (script == null) continue;
                    try
                    {
                        var equipMethod = script.GetType().GetMethod("Equip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (equipMethod == null) continue;
                        equipMethod.Invoke(script, null);
                        invoked = true;
                    }
                    catch { }
                }
            }
            catch { return invoked; }
            return invoked;
        }

        private static void SimulateButtonClick(Button button)
        {
            if (!button) return;
            try
            {
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                if (eventSystem != null)
                {
                    eventSystem.SetSelectedGameObject(button.gameObject);
                    var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject, pointerData, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                }
                else button.onClick.Invoke();
            }
            catch { button.onClick.Invoke(); }
        }
    }

    public static class DailyQuestManager
    {
        public static void CompleteDaily()
        {
            try
            {
                var questManager = MonoBehaviourPublicLi1QudaDi2InquQuacUnique.Instance;
                var saveManager = SaveManager.Instance;
                if (questManager != null && saveManager != null)
                {
                    questManager.CompleteQuest();
                    saveManager.state.AddQuestProgress(187f);
                    saveManager.Save();
                    MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.TryDailyQuest();
                    Utility.ForceMessage("Daily Quest: Completed!");
                }
            }
            catch { }

            try
            {
                MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.TryTriggerDrop();
                MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.TryTriggerDrop();
                MonoBehaviourPublicStCaSt1ObSthaUIStmaUnique.TryTriggerDrop();
            }
            catch { }
        }
    }

    public static class TilePainterManager
    {
        public static bool isPainting = false;
        private static List<GameObject> _tiles = new List<GameObject>();

        public static void Start()
        {
            _tiles.Clear();
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var o in all)
            {
                if (o.activeInHierarchy && (o.name.Contains("Tile") || o.name.Contains("NewTile")))
                    _tiles.Add(o);
            }
            if (_tiles.Count > 0)
            {
                isPainting = true;
                Utility.ForceMessage($"Painting {_tiles.Count} Tiles...");
            }
        }

        public static void Update()
        {
            if (!isPainting || _tiles.Count == 0 || GameManager.Instance == null) { isPainting = false; return; }
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;
            var me = GameManager.Instance.activePlayers[myId];

            for (int i = 0; i < 3; i++)
            {
                if (_tiles.Count == 0) break;
                var t = _tiles[0];
                _tiles.RemoveAt(0);
                if (t != null) me.transform.position = t.transform.position + Vector3.up * 0.5f;
            }
        }
    }
}
