using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{

    public static class SteamTrackerManager
    {
        public static bool enabled = true;
        public static HashSet<ulong> TrackedIDs = new HashSet<ulong>();
        private static string _filePath => Path.Combine(Variables.mainFolderPath, "BOT_SteamTracker.txt");
        private static Dictionary<ulong, string> _previousStates = new Dictionary<ulong, string>();
        private static float _pollTimer = 0f;

        public static void Init()
        {
            LoadList();
        }

        public static void Update()
        {
            if (!enabled || TrackedIDs.Count == 0 || SteamManager.Instance == null) return;

            _pollTimer += Time.deltaTime;
            if (_pollTimer >= 4.0f)
            {
                _pollTimer = 0f;
                CheckStatuses();
            }
        }

        private static void CheckStatuses()
        {
            List<ulong> snapshot;
            try { snapshot = new List<ulong>(TrackedIDs); }
            catch { return; }
            if (snapshot.Count == 0) return;

            foreach (var id in snapshot)
            {
                try
                {
                    CSteamID cId = new CSteamID(id);
                    string name = SteamFriends.GetFriendPersonaName(cId);
                    FriendGameInfo_t gameInfo;
                    if (SteamFriends.GetFriendGamePlayed(cId, out gameInfo) && gameInfo.m_gameID.m_GameID == 1782210)
                    {
                        if (!_previousStates.ContainsKey(id) || _previousStates[id] != "Playing")
                        {
                            _previousStates[id] = "Playing";
                            try { Utility.ForceMessage($"<color=#00FFFF>[Tracker]</color> <color=#FFFF00>{name}</color> 进入了 Crab Game！"); }
                            catch { }
                        }
                    }
                    else
                    {
                        _previousStates[id] = "Offline";
                    }
                }
                catch { }
            }
        }

        public static void LoadList()
        {
            TrackedIDs.Clear();
            if (File.Exists(_filePath))
            {
                foreach (var line in File.ReadAllLines(_filePath))
                {
                    if (ulong.TryParse(line.Trim(), out ulong id))
                        TrackedIDs.Add(id);
                }
            }
        }

        public static void SaveList()
        {
            try
            {
                if (!Directory.Exists(Variables.mainFolderPath))
                    Directory.CreateDirectory(Variables.mainFolderPath);

                File.WriteAllLines(_filePath, TrackedIDs.Select(id => id.ToString()).ToArray());
            }
            catch { }
        }

        public static void AddSelectedPlayer()
        {
            if (JoinGameManager.SelectedPlayerUid != 0)
            {
                TrackedIDs.Add(JoinGameManager.SelectedPlayerUid);
                SaveList();
                Utility.ForceMessage("已添加追踪");
            }
        }

        public static void AddFromClipboard()
        {
            if (ulong.TryParse(GUIUtility.systemCopyBuffer?.Trim(), out ulong id))
            {
                TrackedIDs.Add(id);
                SaveList();
                Utility.ForceMessage("已添加 ID: " + id);
            }
        }

        public static void RemoveSelectedPlayer()
        {
            if (JoinGameManager.SelectedPlayerUid != 0)
            {
                TrackedIDs.Remove(JoinGameManager.SelectedPlayerUid);
                SaveList();
                Utility.ForceMessage("已移除追踪");
            }
        }

        public static void OpenConfigFile()
        {
            try
            {
                if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "");
                System.Diagnostics.Process.Start("explorer.exe", _filePath);
            }
            catch { }
        }
    }
}
