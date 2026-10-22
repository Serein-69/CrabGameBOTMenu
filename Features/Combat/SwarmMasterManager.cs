using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class SwarmMasterManager
    {
        public const string CHANNEL_CMD = "BOT:SwarmCmd";
        public const string CHANNEL_RESP = "BOT:SwarmResp";

        public static HashSet<ulong> OnlineSlaves = new HashSet<ulong>();
        public static bool IsAttackingActive = false;
        public static ulong CurrentTargetId = 0;

        private static float _pingTimer = 0f;
        private static ulong _lastLobbyId = 0;

        public static void Init()
        {
            if (!BOTPlugin.HasModNet) return;

            try
            {
                ModNet.RegisterMessageHandler(CHANNEL_RESP, (senderId, packet) =>
                {
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    if (senderId == myId || senderId == 0) return;

                    int op = packet.ReadInt();
                    if (op == 1)
                    {
                        if (!OnlineSlaves.Contains(senderId))
                        {
                            OnlineSlaves.Add(senderId);
                            string name = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(senderId)));
                            Utility.ForceMessage($"<color=#00FF44>[傀儡连接成功] 已捕获副号: {name}</color>");
                        }
                    }
                });

                ModNet.RegisterMessageHandler(CHANNEL_CMD, (sender, packet) => { });
                Utility.Log("[SwarmMaster] 主控端就绪！");
            }
            catch { }
        }

        public static void Update()
        {
            if (!BOTPlugin.HasModNet || SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;

            ulong currentLobby = SteamManager.Instance.currentLobby.m_SteamID;

            if (currentLobby != _lastLobbyId)
            {
                _lastLobbyId = currentLobby;
                OnlineSlaves.Clear();
                _pingTimer = 1.8f;
            }

            OnlineSlaves.RemoveWhere(id => !IsPlayerInLobby(id));

            _pingTimer += Time.deltaTime;
            if (_pingTimer >= 2.0f)
            {
                _pingTimer = 0f;
                BroadcastPing();
            }
        }

        private static bool IsPlayerInLobby(ulong id)
        {
            if (SteamManager.Instance == null) return false;
            int count = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);
            for (int i = 0; i < count; i++)
            {
                if (SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i).m_SteamID == id)
                    return true;
            }
            return false;
        }

        public static void BroadcastPing()
        {
            if (SteamManager.Instance == null) return;
            var lobby = SteamManager.Instance.currentLobby;
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);

            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (member.m_SteamID == 0 || member.m_SteamID == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID) continue;

                SteamNetworking.AcceptP2PSessionWithUser(member);

                Packet p = new Packet();
                p.Write(0);
                p.Write(OnlineSlaves.Count);
                foreach (ulong sId in OnlineSlaves)
                {
                    p.Write((long)sId);
                }

                ModNet.SendMessage(CHANNEL_CMD, p, member);
            }
        }

        private static void DispatchCommandToAll(Action<Packet> writePacketAction)
        {
            if (SteamManager.Instance == null) return;

            HashSet<ulong> allTargetIds = new HashSet<ulong>(OnlineSlaves);

            var lobby = SteamManager.Instance.currentLobby;
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (member.m_SteamID != 0 && member.m_SteamID != myId)
                {
                    allTargetIds.Add(member.m_SteamID);
                }
            }

            foreach (ulong targetId in allTargetIds)
            {
                CSteamID targetSteamId = new CSteamID(targetId);
                SteamNetworking.AcceptP2PSessionWithUser(targetSteamId);

                Packet freshPacket = new Packet();
                writePacketAction(freshPacket);

                ModNet.SendMessage(CHANNEL_CMD, freshPacket, targetSteamId);
            }
        }

        public static void CommandAttack(ulong targetId)
        {
            if (targetId == 0)
            {
                Utility.ForceMessage("<color=red>请先选择一个目标！</color>");
                return;
            }

            CurrentTargetId = targetId;
            IsAttackingActive = true;

            DispatchCommandToAll(p =>
            {
                p.Write(2);
                p.Write((long)targetId);
            });

            string targetName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(targetId)));
            Utility.ForceMessage($"<color=red>已命令全军集火: {targetName}！</color>");
        }

        public static void CommandStop()
        {
            IsAttackingActive = false;
            CurrentTargetId = 0;

            DispatchCommandToAll(p =>
            {
                p.Write(3);
            });

            Utility.ForceMessage("<color=yellow>已命令傀儡停手挂机。</color>");
        }

        public static void CommandFollow()
        {
            DispatchCommandToAll(p =>
            {
                p.Write(4);

                p.Write(OnlineSlaves.Count);
                foreach (ulong slaveId in OnlineSlaves)
                {
                    p.Write((long)slaveId);
                }
            });

            Utility.ForceMessage("<color=cyan>已命令全军护驾！(多傀儡同伴免打保护)</color>");
        }

        public static void CommandSuicide()
        {
            DispatchCommandToAll(p =>
            {
                p.Write(7);
            });

            Utility.ForceMessage("<color=red>已下达集体自爆指令！</color>");
        }
    }
}
