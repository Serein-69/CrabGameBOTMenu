using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SteamworksNative;
using UnhollowerBaseLib;
using Il2CppSystem.Runtime.InteropServices;
using System.Linq;

namespace BOTMenuMod
{
    public static class InvalidHelpers
    {
        public static bool IsInvalid(this float value) => float.IsNaN(value) || float.IsInfinity(value) || value == float.MaxValue || value == float.MinValue;
        public static bool IsInvalid(this Vector3 value) => value.x.IsInvalid() || value.y.IsInvalid() || value.z.IsInvalid();
    }

    public static class AntiCrashManager
    {

        public static readonly HashSet<ulong> BuiltInWhitelist = new HashSet<ulong>
        {
        };

        public static bool IsWhitelisted(ulong steamId)
        {
            if (steamId == 0) return false;

            if (SteamManager.Instance != null && steamId == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID)
                return true;

            if (BuiltInWhitelist.Contains(steamId))
                return true;

            if (TagServerManager.WhitelistIDs != null && TagServerManager.WhitelistIDs.Contains(steamId))
                return true;

            return false;
        }

        public static HashSet<ulong> currentMembers = new HashSet<ulong>();
        public static HashSet<ulong> blockedMembers = new HashSet<ulong>();
        public static Dictionary<ulong, float> punishedAttackers = new Dictionary<ulong, float>();
        private const int MinPacketSize = 8;
        private static bool _packetCrashLogged = false;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(AntiCrashManager));
            Utility.Log("Passive Anti-Crash System Initialized (With Whitelist Bypass).");
        }

        public static void Flag(ulong attackerId, string reason, bool punishOffender = true)
        {
            if (SteamManager.Instance == null) return;

            if (IsWhitelisted(attackerId)) return;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (attackerId == myId) return;

            float currentTime = Time.realtimeSinceStartup;
            bool isSpamming = false;

            if (punishedAttackers.TryGetValue(attackerId, out float lastAttackTime))
            {
                if (currentTime - lastAttackTime < 5f) isSpamming = true;
            }

            punishedAttackers[attackerId] = currentTime;

            if (isSpamming)
            {
                if (punishOffender && Utility.IsHost() && GameManager.Instance != null)
                {
                    if (GameManager.Instance.activePlayers.ContainsKey(attackerId) && !GameManager.Instance.activePlayers[attackerId].dead)
                        GameServer.PlayerDied(attackerId, 1, Vector3.zero);
                }
                return;
            }

            string name = SteamFriends.GetFriendPersonaName(new CSteamID(attackerId));
            Utility.Log($"[Anti-Crash] Blocked malicious packet from {name} ({attackerId}): {reason}");
            Utility.ForceMessage($"<color=red>[Crash Blocked]</color> {name}: {reason}");

            if (!punishOffender)
            {

                blockedMembers.Add(attackerId);
                try { SteamManager.Instance.StopP2P(new CSteamID(attackerId)); } catch { }
                return;
            }

            if (Utility.IsHost())
            {
                try
                {
                    GameServer.PlayerDied(attackerId, 1, Vector3.zero);
                    Utility.ForceMessage($"<color=red>[Auto-Counter]</color> Exploded {name}!");
                }
                catch { }
            }
            else
            {
                ulong hostId = SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID;
                if (attackerId != hostId)
                {
                    blockedMembers.Add(attackerId);
                    SteamManager.Instance.StopP2P(new CSteamID(attackerId));
                }
            }
        }

        public static bool IsValidSession(ulong clientId)
        {
            if (SteamManager.Instance == null) return true;

            if (IsWhitelisted(clientId)) return true;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            return clientId == myId || (currentMembers.Contains(clientId) && !blockedMembers.Contains(clientId));
        }

        [HarmonyPatch(typeof(SteamManager), "Method_Private_Void_LobbyEnter_t_PDM_1")]
        [HarmonyPrefix]
        public static void PreLobbyEnter(LobbyEnter_t param_1)
        {
            currentMembers.Clear(); blockedMembers.Clear(); punishedAttackers.Clear();
            CSteamID lobbyId = new CSteamID(param_1.m_ulSteamIDLobby);
            int members = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            for (int i = 0; i < members; i++) currentMembers.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i).m_SteamID);
        }

        [HarmonyPatch(typeof(SteamManager), "Method_Private_Void_LobbyChatUpdate_t_PDM_3")]
        [HarmonyPrefix]
        public static void PrePlayerJoinOrLeave(LobbyChatUpdate_t param_1)
        {
            if (param_1.m_rgfChatMemberStateChange == 1u) currentMembers.Add(param_1.m_ulSteamIDUserChanged);
            else
            {
                currentMembers.Remove(param_1.m_ulSteamIDUserChanged);
                blockedMembers.Remove(param_1.m_ulSteamIDUserChanged);
                punishedAttackers.Remove(param_1.m_ulSteamIDUserChanged);
            }
        }

        [HarmonyPatch(typeof(SteamMatchmaking), "LeaveLobby")]
        [HarmonyPrefix]
        public static void PreLeaveLobby() { currentMembers.Clear(); blockedMembers.Clear(); punishedAttackers.Clear(); }

        [HarmonyPatch(typeof(SteamManager), "NewAcceptP2P", new Type[] { typeof(CSteamID) })]
        [HarmonyPrefix]
        public static bool PreAcceptP2P(CSteamID param_1) => IsValidSession(param_1.m_SteamID);

        [HarmonyPatch(typeof(SteamPacketManager), "Method_Private_Static_Void_SteamNetworkingMessage_t_Int32_0")]
        [HarmonyPrefix]
        public static bool PreHandlePacket(SteamNetworkingMessage_t param_0, int param_1)
        {
            if (SteamManager.Instance == null || param_0.m_pData == IntPtr.Zero)
                return false;

            ulong clientId = param_0.m_identityPeer.GetSteamID64();
            if (!IsValidSession(clientId))
            {
                SteamManager.Instance.StopP2P(new CSteamID(clientId));
                return false;
            }

            int size = param_0.m_cbSize;
            if (size < MinPacketSize)
            {

                return false;
            }

            byte[] rawData = new byte[size];
            try { Marshal.Copy(param_0.m_pData, rawData, 0, size); }
            catch { return false; }

            int type = BitConverter.ToInt32(rawData, 4);

            if (param_1 == 0 && Enum.IsDefined(typeof(ClientPackets), type))
            {

                if (type == 37 || type == 42 || type == 43 || type == 31 || type == 44)
                {
                    Flag(clientId, "Known Crash/Spam Packet", false);
                    return false;
                }

                if (type == 14 && size >= 32)
                {
                    float x = BitConverter.ToSingle(rawData, 20);
                    float y = BitConverter.ToSingle(rawData, 24);
                    float z = BitConverter.ToSingle(rawData, 28);
                    if (x.IsInvalid() || y.IsInvalid() || z.IsInvalid())
                    {
                        Flag(clientId, "Invalid Damage Vector", false);
                        return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(SteamPacketManager), "Method_Private_Static_Void_SteamNetworkingMessage_t_Int32_0")]
        [HarmonyFinalizer]
        public static Exception CatchPacketCrash(Exception __exception)
        {
            if (__exception != null && !_packetCrashLogged)
            {
                _packetCrashLogged = true;
                Utility.Log("[Anti-Crash] Suppressed packet exception: " + __exception.Message);
            }
            return null;
        }

        [HarmonyPatch(typeof(ServerHandle), "PlayerPosition")]
        [HarmonyPrefix]
        public static bool PreServerPlayerPos(ulong __0, ObjectPublicIDisposableLi1ByInByBoUnique __1)
        {
            if (IsWhitelisted(__0)) return true;
            try
            {
                byte[] data = __1.field_Private_ArrayOf_Byte_0.ToArray();
                if (data.Length < 20) return false;
                float x = BitConverter.ToSingle(data, 8); float y = BitConverter.ToSingle(data, 12); float z = BitConverter.ToSingle(data, 16);
                if (x.IsInvalid() || y.IsInvalid() || z.IsInvalid()) { Flag(__0, "Invalid Position (NaN/Inf)", false); return false; }
            }
            catch { return false; }
            return true;
        }

        [HarmonyPatch(typeof(ServerHandle), "PlayerRotation")]
        [HarmonyPrefix]
        public static bool PreServerPlayerRot(ulong __0, ObjectPublicIDisposableLi1ByInByBoUnique __1)
        {
            if (IsWhitelisted(__0)) return true;
            try
            {
                byte[] data = __1.field_Private_ArrayOf_Byte_0.ToArray();
                if (data.Length < 16) return false;
                float rotY = BitConverter.ToSingle(data, 8); float rotX = BitConverter.ToSingle(data, 12);
                if (rotX.IsInvalid() || rotY.IsInvalid()) { Flag(__0, "Invalid Rotation (NaN/Inf)", false); return false; }
            }
            catch { return false; }
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "PlayerPosition")]
        [HarmonyPrefix]
        public static bool PreClientPlayerPos(ObjectPublicIDisposableLi1ByInByBoUnique __0)
        {
            try
            {
                byte[] data = __0.field_Private_ArrayOf_Byte_0.ToArray();
                if (data.Length < 28) return false;
                ulong attackerId = BitConverter.ToUInt64(data, 8);
                if (IsWhitelisted(attackerId)) return true;

                float x = BitConverter.ToSingle(data, 16); float y = BitConverter.ToSingle(data, 20); float z = BitConverter.ToSingle(data, 24);
                if (x.IsInvalid() || y.IsInvalid() || z.IsInvalid()) { Flag(attackerId, "Invalid Position", false); return false; }
            }
            catch { return false; }
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "PlayerRotation")]
        [HarmonyPrefix]
        public static bool PreClientPlayerRot(ObjectPublicIDisposableLi1ByInByBoUnique __0)
        {
            try
            {
                byte[] data = __0.field_Private_ArrayOf_Byte_0.ToArray();
                if (data.Length < 24) return false;
                ulong attackerId = BitConverter.ToUInt64(data, 8);
                if (IsWhitelisted(attackerId)) return true;

                float rotY = BitConverter.ToSingle(data, 16); float rotX = BitConverter.ToSingle(data, 20);
                if (rotX.IsInvalid() || rotY.IsInvalid()) { Flag(attackerId, "Invalid Rotation", false); return false; }
            }
            catch { return false; }
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "PlayerDamage")]
        [HarmonyPrefix]
        public static bool PreClientPlayerDmg(ObjectPublicIDisposableLi1ByInByBoUnique __0)
        {
            try
            {
                byte[] data = __0.field_Private_ArrayOf_Byte_0.ToArray();
                if (data.Length < 40) return false;
                ulong attackerId = BitConverter.ToUInt64(data, 8);
                if (IsWhitelisted(attackerId)) return true;

                float x = BitConverter.ToSingle(data, 28); float y = BitConverter.ToSingle(data, 32); float z = BitConverter.ToSingle(data, 36);
                if (x.IsInvalid() || y.IsInvalid() || z.IsInvalid()) { Flag(attackerId, "Fatal Damage Vector", false); return false; }
            }
            catch { return false; }
            return true;
        }
    }
}
