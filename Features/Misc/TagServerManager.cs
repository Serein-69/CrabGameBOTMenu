using UnityEngine;
using HarmonyLib;
using SteamworksNative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BOTMenuMod
{

    public static class TagServerManager
    {
        public static bool DisableSnowballs = true;
        public static bool canRespawnPlayers = true;
        public static bool canStartGame = true;

        public static bool WhitelistModeEnabled = false;
        public static HashSet<ulong> WhitelistIDs = new HashSet<ulong>();
        private static string WhitelistFilePath => Path.Combine(Variables.mainFolderPath, "Whitelist.txt");

        private static float _zombieCleanupTimer = 0f;

        public static Dictionary<ulong, Vector3> FrozenPlayers = new Dictionary<ulong, Vector3>();
        private static Dictionary<ulong, float> _unauthorizedPlayers = new Dictionary<ulong, float>();
        private static Dictionary<ulong, float> _lastKickTime = new Dictionary<ulong, float>();
        private static float _freezeTimer = 0f;

        public static void LoadWhitelist()
        {
            WhitelistIDs.Clear();
            try
            {
                if (!File.Exists(WhitelistFilePath)) File.WriteAllText(WhitelistFilePath, "");
                string[] lines = File.ReadAllLines(WhitelistFilePath);
                foreach (string line in lines)
                {
                    if (ulong.TryParse(line.Trim(), out ulong id))
                    {
                        WhitelistIDs.Add(id);
                    }
                }
                Utility.Log($"Loaded {WhitelistIDs.Count} whitelisted users.");
            }
            catch { }
        }

        public static void SaveWhitelist()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (ulong id in WhitelistIDs) lines.Add(id.ToString());
                File.WriteAllLines(WhitelistFilePath, lines);
            }
            catch { }
        }

        public static void AddToWhitelist(ulong id)
        {
            if (id == 0) return;
            if (WhitelistIDs.Contains(id))
            {
                Utility.ForceMessage("<color=yellow>Player already in Whitelist.</color>");
                return;
            }
            WhitelistIDs.Add(id);
            SaveWhitelist();
            Utility.ForceMessage($"<color=green>Added ID {id} to Whitelist.</color>");
        }

        public static void RemoveFromWhitelist(ulong id)
        {
            if (id == 0) return;
            if (WhitelistIDs.Contains(id))
            {
                WhitelistIDs.Remove(id);
                SaveWhitelist();
                Utility.ForceMessage($"<color=red>Removed ID {id} from Whitelist.</color>");
            }
            else Utility.ForceMessage("<color=yellow>ID not found in Whitelist.</color>");
        }

        public static void EnforceWhitelist()
        {
            if (!IsHost() || !WhitelistModeEnabled) return;
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;

            ulong hostId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);

            HashSet<ulong> currentUnauthorized = new HashSet<ulong>();

            for (int i = 0; i < memberCount; i++)
            {
                ulong targetId = SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i).m_SteamID;

                if (targetId != hostId && targetId != 0 && !WhitelistIDs.Contains(targetId))
                {
                    currentUnauthorized.Add(targetId);

                    if (!_unauthorizedPlayers.ContainsKey(targetId))
                    {
                        _unauthorizedPlayers[targetId] = 0f;
                        _lastKickTime[targetId] = 0f;
                    }
                }
            }

            var keys = _unauthorizedPlayers.Keys.ToList();
            foreach (var k in keys)
            {
                if (!currentUnauthorized.Contains(k))
                {
                    _unauthorizedPlayers.Remove(k);
                    _lastKickTime.Remove(k);
                }
            }

            foreach (var targetId in currentUnauthorized)
            {
                _unauthorizedPlayers[targetId] += Time.deltaTime;

                if (_unauthorizedPlayers[targetId] > 2.5f)
                {
                    if (Time.time - _lastKickTime[targetId] > 1.5f)
                    {
                        _lastKickTime[targetId] = Time.time;

                        if (LobbyManager.Instance != null)
                        {
                            var client = LobbyManager.Instance.GetClient(targetId);
                            if (client == null)
                            {
                                _unauthorizedPlayers.Remove(targetId);
                                _lastKickTime.Remove(targetId);
                                continue;
                            }
                        }

                        try
                        {
                            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(targetId));
                            LobbyManager.Instance.KickPlayer(targetId);
                            Utility.Log($"[Whitelist] Kicked unauthorized player {targetId}");
                        }
                        catch (System.Exception ex)
                        {
                            Utility.Log($"[Whitelist] Failed to kick {targetId}: {ex.Message}");
                        }

                        _unauthorizedPlayers.Remove(targetId);
                        _lastKickTime.Remove(targetId);
                    }
                }
            }
        }

        public static void ToggleFreezePlayer(ulong steamId)
        {
            if (steamId == 0) return;
            if (SteamManager.Instance != null && steamId == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID) return;

            if (FrozenPlayers.ContainsKey(steamId))
            {
                FrozenPlayers.Remove(steamId);
                Utility.ForceMessage($"<color=green>Player Unfrozen</color> {steamId}");
                SendPrivateServerMSG(steamId, "you have been unfrozen by host");
            }
            else
            {
                if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(steamId))
                {
                    FrozenPlayers[steamId] = GameManager.Instance.activePlayers[steamId].transform.position;
                    Utility.ForceMessage($"<color=red>Player Frozen:</color> {steamId}");
                    SendPrivateServerMSG(steamId, "you have been frozen by host");
                }
            }
        }

        public static void UpdateFrozenPlayers()
        {
            if (!IsHost() || FrozenPlayers.Count == 0) return;
            _freezeTimer += Time.deltaTime;
            if (_freezeTimer >= 0.5f)
            {
                _freezeTimer = 0f;
                List<ulong> toRemove = new List<ulong>();
                foreach (var kvp in FrozenPlayers)
                {
                    if (GameManager.Instance == null || !GameManager.Instance.activePlayers.ContainsKey(kvp.Key)) { toRemove.Add(kvp.Key); continue; }
                    if (GameManager.Instance.activePlayers[kvp.Key].dead) { toRemove.Add(kvp.Key); continue; }
                    MonoBehaviourPublicInInUnique.RespawnPlayer(kvp.Key, kvp.Value);
                }
                foreach (var id in toRemove) FrozenPlayers.Remove(id);
            }
        }

        public static void SendPrivateServerMSG(ulong toSteamid, string msg)
        {
            try
            {
                var packet = new ObjectPublicIDisposableLi1ByInByBoUnique(2);
                packet.Method_Public_Void_UInt64_0(1);
                packet.Method_Public_Void_String_0("");
                packet.Method_Public_Void_String_0(msg);
                MonoBehaviourPublicInInUnique.Method_Private_Static_Void_UInt64_ObjectPublicIDisposableLi1ByInByBoUnique_0(toSteamid, packet);
            }
            catch { }
        }

        public static bool IsHost()
        {
            if (SteamManager.Instance == null) return false;
            if (SteamManager.Instance.currentLobby.m_SteamID == 0) return true;
            return SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
        }

        public static void CleanUpZombiePlayers()
        {
            if (!IsHost() || SteamManager.Instance == null || GameManager.Instance == null) return;

            _zombieCleanupTimer += Time.deltaTime;
            if (_zombieCleanupTimer >= 10.0f)
            {
                _zombieCleanupTimer = 0f;

                HashSet<ulong> realSteamMembers = new HashSet<ulong>();
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);
                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i);
                    realSteamMembers.Add(memberId.m_SteamID);
                }

                List<ulong> zombiesToKick = new List<ulong>();
                if (GameManager.Instance.activePlayers != null)
                {
                    foreach (var id in GameManager.Instance.activePlayers.Keys)
                    {
                        if (!realSteamMembers.Contains(id))
                        {
                            zombiesToKick.Add(id);
                        }
                    }
                }

                foreach (ulong zombieId in zombiesToKick)
                {
                    if (LobbyManager.Instance != null)
                    {
                        LobbyManager.Instance.KickPlayer(zombieId);
                        Utility.Log($"[Zombie Cleanup] Purged ghost player: {zombieId}");
                    }
                }
            }
        }

    }

    public static class TagModePatches
    {
        [HarmonyPatch(typeof(SteamManager), "Update")]
        [HarmonyPostfix]
        public static void Postfix_SteamManagerUpdate()
        {
            TagServerManager.EnforceWhitelist();
        }

        [HarmonyPatch(typeof(GameMode), "Method_Private_Void_0")]
        [HarmonyPrefix]
        public static void Prefix_GameModeStartRound()
        {
            TagServerManager.canRespawnPlayers = false;
        }

        [HarmonyPatch(typeof(GameMode), "Update")]
        [HarmonyPostfix]
        public static void Postfix_GameModeUpdate()
        {
            if (!TagServerManager.IsHost()) return;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers != null)
            {
                List<ulong> fellPlayers = new List<ulong>();
                foreach (var kvp in GameManager.Instance.activePlayers)
                {
                    var player = kvp.Value;
                    if (player != null && !player.dead && player.transform.position.y <= -50f) fellPlayers.Add(kvp.Key);
                }
                foreach (ulong fellId in fellPlayers)
                {
                    if (GameManager.Instance.activePlayers == null || !GameManager.Instance.activePlayers.ContainsKey(fellId)) continue;
                    string name = SteamFriends.GetFriendPersonaName(new CSteamID(fellId));
                    ServerSend.SendChatMessage(1, $"{name} was killed due to falling under the map");
                    GameServer.PlayerDied(fellId, 1, Vector3.zero);
                }
            }

            TagServerManager.CleanUpZombiePlayers();
            TagServerManager.UpdateFrozenPlayers();
        }

        [HarmonyPatch(typeof(ServerHandle), "GameRequestToSpawn")]
        [HarmonyPrefix]
        public static void Prefix_GameRequestToSpawn(ulong __0)
        {
            if (!TagServerManager.IsHost()) return;

            if (TagServerManager.canRespawnPlayers && LobbyManager.Instance != null)
            {
                var client = LobbyManager.Instance.GetClient(__0);
                if (client != null) client.field_Public_Boolean_0 = true;
            }
        }
    }
}
