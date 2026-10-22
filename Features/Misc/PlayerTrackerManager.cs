using System;
using System.IO;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using SteamworksNative;
using UnhollowerBaseLib;

namespace BOTMenuMod
{
    public static class PlayerTrackerManager
    {
        public static string BaseSavePath;

        private static ulong _lastLobbyId = 0;
        private static int _lastMemberCount = -1;
        private static float _checkTimer = 0f;

        public static void Init()
        {
            BaseSavePath = Path.Combine(Variables.mainFolderPath, "PlayerTrackerData");
            if (!Directory.Exists(BaseSavePath)) Directory.CreateDirectory(BaseSavePath);

            Harmony.CreateAndPatchAll(typeof(PlayerTrackerManager));
            Utility.Log("Passive Player Tracker Initialized (With Ban History).");
        }

        public static void Update()
        {
            _checkTimer += Time.deltaTime;

            if (_checkTimer >= 1.0f)
            {
                _checkTimer = 0f;
                CheckForChanges();
            }
        }

        private static void CheckForChanges()
        {
            if (SteamManager.Instance == null) return;

            try
            {
                CSteamID currentLobby = SteamManager.Instance.currentLobby;

                if (currentLobby.m_SteamID == 0)
                {
                    _lastLobbyId = 0;
                    _lastMemberCount = -1;
                    return;
                }

                ulong currentLobbyId = currentLobby.m_SteamID;
                int currentMemberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobby);

                bool isNewLobby = currentLobbyId != _lastLobbyId;
                bool isCountChanged = currentMemberCount != _lastMemberCount;

                if (isNewLobby || isCountChanged)
                {
                    _lastLobbyId = currentLobbyId;
                    _lastMemberCount = currentMemberCount;

                    ScanAndSavePlayers(currentLobby);
                }
            }
            catch { }
        }

        private static void ScanAndSavePlayers(CSteamID lobbyId)
        {
            try
            {
                string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "LobbyName");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

                string datePath = Path.Combine(BaseSavePath, dateFolder);
                if (!Directory.Exists(datePath)) Directory.CreateDirectory(datePath);

                string folderName = $"{SanitizeFileName(lobbyName)}_{lobbyId.m_SteamID}";
                string serverFolderPath = Path.Combine(datePath, folderName);

                if (!Directory.Exists(serverFolderPath)) Directory.CreateDirectory(serverFolderPath);

                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
                    if (memberId.m_SteamID == 0) continue;

                    string memberName = SteamFriends.GetFriendPersonaName(memberId);
                    if (string.IsNullOrEmpty(memberName)) continue;

                    SavePlayerData(serverFolderPath, memberName, memberId.m_SteamID, false);
                }
            }
            catch { }
        }

        private static void SavePlayerData(string folderPath, string playerName, ulong steamId, bool markAsBanned)
        {
            string fileName = $"{steamId}_{SanitizeFileName(playerName)}.json";
            string filePath = Path.Combine(folderPath, fileName);
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string firstSeen = currentTime;
            bool bannedMe = markAsBanned;

            try
            {

                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);

                    var matchFirst = Regex.Match(content, @"""FirstSeen"":\s*""([^""]+)""");
                    if (matchFirst.Success) firstSeen = matchFirst.Groups[1].Value;

                    var matchBan = Regex.Match(content, @"""BannedMe"":\s*(true|false)", RegexOptions.IgnoreCase);
                    if (matchBan.Success && matchBan.Groups[1].Value.ToLower() == "true") bannedMe = true;
                }

                if (markAsBanned) bannedMe = true;

                string json = "{\n" +
                              $"  \"CurrentName\": \"{playerName}\",\n" +
                              $"  \"SteamID\": {steamId},\n" +
                              $"  \"FirstSeen\": \"{firstSeen}\",\n" +
                              $"  \"LastSeen\": \"{currentTime}\",\n" +
                              $"  \"BannedMe\": {bannedMe.ToString().ToLower()}\n" +
                              "}";

                File.WriteAllText(filePath, json);
            }
            catch { }
        }

        private static string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string cleanName = Regex.Replace(Utility.StripColorTags(name), invalidRegStr, "_");
            return cleanName.Length > 0 ? cleanName : "Unnamed";
        }

        [HarmonyPatch(typeof(ClientHandleClass), "LobbyClosed")]
        [HarmonyPrefix]
        public static void Prefix_LobbyClosed(ObjectPublicIDisposableLi1ByInByBoUnique __0)
        {
            try
            {
                if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;

                int initPos = __0.get_readPos();
                int reason = __0.ReadInt(true);
                __0.set_readPos(initPos);

                if (reason == 2 || reason == 69)
                {
                    ulong hostId = SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID;
                    string hostName = SteamFriends.GetFriendPersonaName(new CSteamID(hostId));

                    if (hostId != 0)
                    {
                        Utility.Log($"[Tracker] We got BANNED! Recording host {hostName} ({hostId}) to blacklist...");

                        string lobbyName = SteamMatchmaking.GetLobbyData(SteamManager.Instance.currentLobby, "LobbyName");
                        string datePath = Path.Combine(BaseSavePath, DateTime.Now.ToString("yyyy-MM-dd"));
                        string folderName = $"{SanitizeFileName(lobbyName)}_{SteamManager.Instance.currentLobby.m_SteamID}";
                        string serverFolderPath = Path.Combine(datePath, folderName);

                        if (Directory.Exists(serverFolderPath))
                        {
                            SavePlayerData(serverFolderPath, hostName, hostId, true);
                        }

                        string globalBanList = Path.Combine(BaseSavePath, "BannedMe_Hosts.txt");
                        File.AppendAllText(globalBanList, $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {hostName} ({hostId}) banned you!\n");

                        Utility.ForceMessage($"<color=red>BANNED! Host {hostName} has been recorded.</color>");
                    }
                }
            }
            catch { }
        }
    }
}
