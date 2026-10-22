using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class JoinGameManager
    {
        public static List<string> savedIds = new List<string>();
        public static int currentIndex = 0;
        private static string playerFilePath = Path.Combine(Variables.mainFolderPath, "sniper_target.txt");
        public static List<string> savedLobbies = new List<string>();
        public static int lobbyIndex = 0;
        private static string lobbyFilePath = Path.Combine(Variables.mainFolderPath, "saved_lobbies.txt");
        public static string statusText = "等待指令 (Ready)";
        private static ulong lastJoinedTargetId = 0;
        private static ulong lastJoinedLobbyId = 0;
        private static int _playerSelectorIndex = 0;
        public static ulong SelectedPlayerUid = 0;
        public static string LobbyCodeInput = "";

        public static void Init()
        {
            MigrateLegacyFile("sniper_target.txt", playerFilePath);
            MigrateLegacyFile("saved_lobbies.txt", lobbyFilePath);
            LoadIdFromFile();
            LoadLobbiesFromFile();
        }

        private static void MigrateLegacyFile(string fileName, string newPath)
        {
            try
            {
                if (File.Exists(newPath)) return;
                string legacyPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                if (!File.Exists(legacyPath)) return;
                if (!Directory.Exists(Variables.mainFolderPath)) Directory.CreateDirectory(Variables.mainFolderPath);
                File.Copy(legacyPath, newPath, false);
            }
            catch { }
        }

        public static void CheckIfJoined()
        {
            if (SteamManager.Instance == null) return;
            ulong currentLobbyId = SteamManager.Instance.currentLobby.m_SteamID;

            if (lastJoinedTargetId != 0 && currentLobbyId != 0 && statusText.Contains("Joining"))
            {
                statusText = "加入玩家成功! (Success)";
                lastJoinedTargetId = 0;
            }
            if (lastJoinedLobbyId != 0 && currentLobbyId == lastJoinedLobbyId)
            {
                statusText = "加入房间成功! (Success)";
                lastJoinedLobbyId = 0;
            }
        }

        public static void FetchCurrentRoomCode()
        {
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0)
            {
                statusText = "你不在房间里";
                return;
            }
            LobbyCodeInput = SteamManager.Instance.currentLobby.m_SteamID.ToString();
            GUIUtility.systemCopyBuffer = LobbyCodeInput;
            statusText = "已获取并复制!";
        }

        public static void SaveCurrentLobbyCode()
        {
            if (string.IsNullOrEmpty(LobbyCodeInput)) return;
            if (savedLobbies.Contains(LobbyCodeInput)) { statusText = "已存在"; return; }
            savedLobbies.Add(LobbyCodeInput);
            lobbyIndex = savedLobbies.Count - 1;
            SaveLobbiesToFile();
            statusText = "房间号已保存";
        }

        public static void CycleSavedLobbies()
        {
            if (savedLobbies.Count == 0) { statusText = "无保存记录"; return; }
            lobbyIndex = (lobbyIndex + 1) % savedLobbies.Count;
            LobbyCodeInput = savedLobbies[lobbyIndex];
            statusText = $"选中房间 {lobbyIndex + 1}/{savedLobbies.Count}";
        }

        public static void DeleteCurrentLobby()
        {
            if (savedLobbies.Count == 0 || lobbyIndex >= savedLobbies.Count) return;
            savedLobbies.RemoveAt(lobbyIndex);
            if (lobbyIndex >= savedLobbies.Count) lobbyIndex = 0;
            LobbyCodeInput = savedLobbies.Count > 0 ? savedLobbies[lobbyIndex] : "";
            SaveLobbiesToFile();
            statusText = "已删除";
        }

        private static void LoadLobbiesFromFile()
        {
            try
            {
                if (!File.Exists(lobbyFilePath)) File.WriteAllText(lobbyFilePath, "");
                savedLobbies = File.ReadAllLines(lobbyFilePath).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).Distinct().ToList();
            }
            catch { }
        }

        private static void SaveLobbiesToFile()
        {
            try { File.WriteAllLines(lobbyFilePath, savedLobbies.ToArray()); } catch { }
        }

        public static void JoinByLobbyCode()
        {
            if (string.IsNullOrEmpty(LobbyCodeInput)) { statusText = "房间号为空"; return; }
            if (ulong.TryParse(LobbyCodeInput, out ulong lobbyId) && SteamManager.Instance != null)
            {
                lastJoinedLobbyId = lobbyId;
                statusText = "正在加入...";
                if (SteamManager.Instance.currentLobby.m_SteamID != 0)
                {
                    SteamMatchmaking.LeaveLobby(SteamManager.Instance.currentLobby);
                    SteamManager.Instance.currentLobby = new CSteamID(0);
                }
                SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
            }
            else statusText = "ID无效";
        }

        public static string GetCurrentId() => (savedIds.Count == 0 || currentIndex >= savedIds.Count) ? "" : savedIds[currentIndex];

        public static void LoadIdFromFile()
        {
            try
            {
                if (!File.Exists(playerFilePath)) File.WriteAllText(playerFilePath, "");
                savedIds = File.ReadAllLines(playerFilePath).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).Distinct().ToList();
                currentIndex = 0;
                statusText = savedIds.Count > 0 ? $"已载入 {savedIds.Count} ID" : "文件为空";
            }
            catch { statusText = "读取失败"; }
        }

        public static void OpenConfigFile()
        {
            new Thread(() => {
                try
                {
                    if (!File.Exists(playerFilePath)) File.WriteAllText(playerFilePath, "");
                    Process.Start(new ProcessStartInfo { FileName = playerFilePath, UseShellExecute = true });
                }
                catch { }
            })
            { IsBackground = true }.Start();
            statusText = "打开中...";
        }

        private static void SaveListToTxt()
        {
            try { File.WriteAllLines(playerFilePath, savedIds.ToArray()); } catch { }
        }

        public static void AddId(string newId)
        {
            if (string.IsNullOrEmpty(newId)) return;
            if (savedIds.Contains(newId)) { currentIndex = savedIds.IndexOf(newId); statusText = "已存在"; return; }
            savedIds.Add(newId);
            currentIndex = savedIds.Count - 1;
            SaveListToTxt();
            statusText = "已保存";
        }

        public static void DeleteCurrentTarget()
        {
            if (savedIds.Count == 0) return;
            savedIds.RemoveAt(currentIndex);
            if (currentIndex >= savedIds.Count) currentIndex = 0;
            SaveListToTxt();
            statusText = "已删除";
        }

        public static void CycleTargetId()
        {
            if (savedIds.Count <= 1) return;
            currentIndex = (currentIndex + 1) % savedIds.Count;
            statusText = $"选中 {currentIndex + 1}/{savedIds.Count}";
        }

        public static void TryJoin()
        {
            statusText = "查询中...";
            string targetIdStr = GetCurrentId();
            if (!ulong.TryParse(targetIdStr, out ulong targetId) || SteamManager.Instance == null) { statusText = "无有效目标"; return; }

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (targetId == myId) { statusText = "不能加入自己"; return; }

            FriendGameInfo_t gameInfo;
            if (SteamFriends.GetFriendGamePlayed(new CSteamID(targetId), out gameInfo) && gameInfo.m_gameID.m_GameID == 1782210)
            {
                if (gameInfo.m_steamIDLobby.m_SteamID != 0)
                {
                    lastJoinedTargetId = targetId;
                    statusText = "Joining...";
                    if (SteamManager.Instance.currentLobby.m_SteamID != 0)
                    {
                        SteamMatchmaking.LeaveLobby(SteamManager.Instance.currentLobby);
                        SteamManager.Instance.currentLobby = new CSteamID(0);
                    }
                    SteamMatchmaking.JoinLobby(gameInfo.m_steamIDLobby);
                }
                else statusText = "目标无房间";
            }
            else statusText = "离线/隐私";
        }

        public static void PasteAndSave()
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clip)) AddId(clip);
        }

        public static void CyclePlayerInLobby()
        {
            var ids = PlayerInfoManager.GetPlayerIDs();
            if (ids.Count == 0) return;
            _playerSelectorIndex = (_playerSelectorIndex + 1) % ids.Count;
            SelectedPlayerUid = ids[_playerSelectorIndex];
        }

        public static string GetSelectedPlayerName()
        {
            if (SelectedPlayerUid == 0) return "None";
            try
            {
                string name = SelectedPlayerUid.ToString();
                string idStr = "?";
                string tags = "";

                if (SteamManager.Instance != null)
                {
                    try { name = SteamFriends.GetFriendPersonaName(new CSteamID(SelectedPlayerUid)); } catch { }
                    name = Utility.StripColorTags(name);
                    if (name.Length > 20) name = name.Substring(0, 20) + "..";
                    if (Utility.GetHostID() == SelectedPlayerUid) tags += " <color=red>[HOST]</color>";
                    if (SteamManager.Instance.field_Private_CSteamID_0.m_SteamID == SelectedPlayerUid) tags += " <color=green>[ME]</color>";
                }

                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.activePlayers != null && GameManager.Instance.activePlayers.ContainsKey(SelectedPlayerUid))
                        idStr = $"#{GameManager.Instance.activePlayers[SelectedPlayerUid].playerNumber}";
                    else if (GameManager.Instance.spectators != null && GameManager.Instance.spectators.ContainsKey(SelectedPlayerUid))
                        idStr = $"Spec#{GameManager.Instance.spectators[SelectedPlayerUid].playerNumber}";
                }
                return $"{name} <color=yellow>[{idStr}]</color>{tags}";
            }
            catch { return "Error"; }
        }

        public static void SaveSelectedPlayer()
        {
            if (SelectedPlayerUid != 0) AddId(SelectedPlayerUid.ToString());
        }

        public static string GetTargetName(string idStr)
        {
            if (string.IsNullOrEmpty(idStr)) return "None";
            try
            {
                if (ulong.TryParse(idStr, out ulong id))
                    return Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(id)));
            }
            catch { }
            return "Unknown";
        }
    }

    public static class PlayerInfoManager
    {
        public static int currentIndex = 0;
        public static bool showSteamID = false;

        public static List<ulong> GetPlayerIDs()
        {
            List<ulong> list = new List<ulong>();
            try
            {
                if (SteamManager.Instance != null && SteamManager.Instance.currentLobby.m_SteamID != 0)
                {
                    int count = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);
                    for (int i = 0; i < count; i++)
                    {
                        CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i);
                        if (memberId.m_SteamID != 0 && !list.Contains(memberId.m_SteamID)) list.Add(memberId.m_SteamID);
                    }
                }
                if (list.Count == 0 && GameManager.Instance != null)
                {
                    if (GameManager.Instance.activePlayers != null) foreach (var key in GameManager.Instance.activePlayers.Keys) if (!list.Contains(key)) list.Add(key);
                    if (GameManager.Instance.spectators != null) foreach (var key in GameManager.Instance.spectators.Keys) if (!list.Contains(key)) list.Add(key);
                }
            }
            catch { }
            return list;
        }

        public static void CycleNext()
        {
            var ids = GetPlayerIDs();
            if (ids.Count == 0) return;
            currentIndex = (currentIndex + 1) % ids.Count;
            showSteamID = false;
        }

        public static string GetCurrentStatus()
        {
            try
            {
                var ids = GetPlayerIDs();
                if (ids.Count == 0) return "<color=grey>[ No Players ]</color>";
                if (currentIndex >= ids.Count) currentIndex = 0;

                ulong targetID = ids[currentIndex];
                ulong ownerId = SteamManager.Instance != null ? SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID : 0;

                if (showSteamID) return $"<color=yellow>{targetID}</color>";

                string name = "Unknown";
                if (SteamManager.Instance != null)
                {
                    try { name = SteamFriends.GetFriendPersonaName(new CSteamID(targetID)); } catch { }
                }
                name = Utility.StripColorTags(name);
                if (name.Length > 12) name = name.Substring(0, 12) + "..";

                string numStr = "?";
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.activePlayers != null && GameManager.Instance.activePlayers.ContainsKey(targetID))
                        numStr = $"#{GameManager.Instance.activePlayers[targetID].playerNumber}";
                    else if (GameManager.Instance.spectators != null && GameManager.Instance.spectators.ContainsKey(targetID))
                        numStr = $"Spec#{GameManager.Instance.spectators[targetID].playerNumber}";
                }
                string hostTag = (targetID == ownerId) ? " <color=red>[HOST]</color>" : "";
                return $"<color=white>{name}</color> <color=cyan>[ {numStr} ]</color>{hostTag}";
            }
            catch { return "Error"; }
        }

        public static void OnClick()
        {
            var ids = GetPlayerIDs();
            if (ids.Count == 0) return;
            if (currentIndex >= ids.Count) currentIndex = 0;
            showSteamID = !showSteamID;
            if (showSteamID)
            {
                GUIUtility.systemCopyBuffer = ids[currentIndex].ToString();
                Utility.ForceMessage($"<color=green>Copied ID: {ids[currentIndex]}</color>");
            }
        }
    }
}
