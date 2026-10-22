using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SteamworksNative;
using UnhollowerBaseLib;
using UnityEngine;

namespace BOTMenuMod.Net
{
    public static class PacketExtensions
    {
        public static byte[] CloneBytes(this Packet self) => self.Method_Public_ArrayOf_Byte_1();

        public static byte[] GetReadableBuffer(this Packet self) => self.field_Private_ArrayOf_Byte_0;

        public static void SetBuffer(this Packet self, byte[] buffer) => self.field_Private_ArrayOf_Byte_0 = buffer;

        public static int get_readPos(this Packet self) => self.field_Private_Int32_0;

        public static void set_readPos(this Packet self, int readPos) => self.field_Private_Int32_0 = readPos;

        public static void Reset(this Packet self, bool reset) => self.Method_Public_Void_Boolean_PDM_1(reset);

        public static byte[] ToArray(this Packet self) => self.Method_Public_ArrayOf_Byte_PDM_0();

        public static void Dispose(this Packet self, bool reset) => self.Method_Protected_Virtual_New_Void_Boolean_1(reset);

        public static void SetBytes(this Packet self, byte[] data) => self.Method_Public_Void_ArrayOf_Byte_1(data);

        public static int Length(this Packet self) => self.Method_Public_Int32_2();

        public static int UnreadLength(this Packet self) => self.Method_Public_Int32_3();

        public static bool ReadBool(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Boolean_Boolean_0(moveReadPos);

        public static byte ReadByte(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Byte_Boolean_PDM_0(moveReadPos);

        public static byte[] ReadBytes(this Packet self, int length, bool moveReadPos = true)
            => self.Method_Public_ArrayOf_Byte_Int32_Boolean_0(length, moveReadPos);

        public static float ReadFloat(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Single_Boolean_0(moveReadPos);

        public static int ReadInt(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Int32_Boolean_1(moveReadPos);

        public static string ReadString(this Packet self, bool moveReadPos = true)
            => self.Method_Public_String_Boolean_0(moveReadPos);

        public static short ReadShort(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Int16_Boolean_PDM_1(moveReadPos);

        public static long ReadLong(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Int64_Boolean_PDM_0(moveReadPos);

        public static ulong ReadUlong(this Packet self, bool moveReadPos = true)
            => self.Method_Public_UInt64_Boolean_0(moveReadPos);

        public static Vector3 ReadVector3(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Vector3_Boolean_0(moveReadPos);

        public static Quaternion ReadQuaternion(this Packet self, bool moveReadPos = true)
            => self.Method_Public_Quaternion_Boolean_0(moveReadPos);

        public static void Write(this Packet self, bool value) => self.Method_Public_Void_Boolean_0(value);

        public static void Write(this Packet self, byte value) => self.Method_Public_Void_Byte_PDM_1(value);

        public static void Write(this Packet self, byte[] value) => self.Method_Public_Void_ArrayOf_Byte_0(value);

        public static void Write(this Packet self, float value) => self.Method_Public_Void_Single_0(value);

        public static void Write(this Packet self, int value) => self.Method_Public_Void_Int32_0(value);

        public static void Write(this Packet self, string value) => self.Method_Public_Void_String_0(value);

        public static void Write(this Packet self, short value) => self.Method_Public_Void_Int16_PDM_1(value);

        public static void Write(this Packet self, long value) => self.Method_Public_Void_Int64_PDM_2(value);

        public static void Write(this Packet self, ulong value) => self.Method_Public_Void_UInt64_1(value);

        public static void Write(this Packet self, Vector3 value) => self.Method_Public_Void_Vector3_1(value);

        public static void Write(this Packet self, Quaternion value) => self.Method_Public_Void_Quaternion_0(value);

        public static void WriteLength(this Packet self) => self.Method_Public_Void_0();

        public static void InsertInt(this Packet self, int value) => self.Method_Public_Void_Int32_PDM_0(value);
    }

    public static class ModNet
    {
        public delegate void MessageHandler(ulong clientId, Packet packet);

        private const int NetChannel = 123;
        private const int SendFlag = 8;
        private const int MaxMessagesPerFrame = 70;

        private const string VersionKey = "CrabNetVersion";
        private const string HandlerKeyPrefix = "CrabNet:Message.";

        private static readonly Dictionary<string, uint> _nameToId = new Dictionary<string, uint>();
        private static readonly Dictionary<uint, MessageHandler> _idToHandler = new Dictionary<uint, MessageHandler>();
        private static readonly Dictionary<string, string> _remoteHandlerCache = new Dictionary<string, string>();
        private static readonly Il2CppStructArray<IntPtr> _messageBuffer = new Il2CppStructArray<IntPtr>(MaxMessagesPerFrame);

        private static uint _nextId = 0;
        private static ulong _registeredLobby = 0;
        private static bool _disabled = false;
        private static int _consecutiveErrors = 0;

        public static bool Enabled => !_disabled;

        public static void RegisterMessageHandler(string messageId, MessageHandler handler)
        {
            if (string.IsNullOrEmpty(messageId) || handler == null) return;
            if (_nameToId.ContainsKey(messageId)) return;

            uint id = _nextId;
            _nextId++;
            _nameToId.Add(messageId, id);
            _idToHandler.Add(id, handler);

            if (_registeredLobby != 0)
            {
                try { SteamMatchmaking.SetLobbyMemberData(SteamManager.Instance.currentLobby, HandlerKeyPrefix + id, messageId); }
                catch { }
            }
        }

        public static bool SendMessage(string messageId, Packet packet, CSteamID target)
            => SendToIds(messageId, packet, new ulong[] { target.m_SteamID });

        public static bool SendMessage(string messageId, Packet packet, ulong target)
            => SendToIds(messageId, packet, new ulong[] { target });

        public static bool SendMessage(string messageId, Packet packet, IEnumerable<ulong> targets)
            => SendToIds(messageId, packet, targets);

        public static bool SendMessage(string messageId, Packet packet, IEnumerable<CSteamID> targets)
        {
            List<ulong> ids = new List<ulong>();
            if (targets != null)
            {
                foreach (CSteamID id in targets)
                {
                    if (id.m_SteamID != 0) ids.Add(id.m_SteamID);
                }
            }
            return SendToIds(messageId, packet, ids);
        }

        private static bool SendToIds(string messageId, Packet packet, IEnumerable<ulong> targets)
        {
            if (packet == null) return false;
            if (string.IsNullOrEmpty(messageId) || !_nameToId.TryGetValue(messageId, out uint id))
            {
                packet.Dispose();
                return false;
            }

            if (SteamManager.Instance == null || targets == null)
            {
                packet.Dispose();
                return false;
            }

            CSteamID lobby = SteamManager.Instance.currentLobby;
            if (lobby.m_SteamID == 0)
            {
                packet.Dispose();
                return false;
            }

            bool sentAny = false;
            ulong myId = SteamUser.GetSteamID().m_SteamID;

            try
            {
                Packet outgoing = new Packet();
                outgoing.Write((int)id);
                outgoing.Write(packet.CloneBytes());

                foreach (ulong target in targets)
                {
                    if (target == 0) continue;

                    if (target == myId)
                    {
                        Packet localCopy = new Packet();
                        localCopy.SetBytes(outgoing.CloneBytes());
                        Dispatch(target, localCopy);
                        sentAny = true;
                        continue;
                    }

                    string version;
                    try { version = SteamMatchmaking.GetLobbyMemberData(lobby, new CSteamID(target), VersionKey); }
                    catch { version = null; }

                    if (string.IsNullOrEmpty(version)) continue;

                    try
                    {
                        Packet remoteCopy = new Packet();
                        remoteCopy.SetBytes(outgoing.CloneBytes());
                        SteamPacketManager.SendPacket(new CSteamID(target), remoteCopy, SendFlag, (SteamPacketManager.EnumNPublicSealedvaTo3vToUnique)NetChannel);
                        sentAny = true;
                    }
                    catch (Exception e)
                    {
                        Utility.Log("[Net] send failed to " + target + ": " + e.Message);
                    }
                }

                outgoing.Dispose();
            }
            catch (Exception e)
            {
                Utility.Log("[Net] send error: " + e.Message);
            }
            finally
            {
                packet.Dispose();
            }

            return sentAny;
        }

        public static void Pump()
        {
            if (_disabled) return;

            try
            {
                RegisterLobbyIfNeeded();

                SteamManager manager = SteamManager.Instance;
                if (manager == null || manager.currentLobby.m_SteamID == 0) return;

                int received;
                int guard = 0;
                do
                {
                    received = SteamNetworkingMessages.ReceiveMessagesOnChannel(NetChannel, _messageBuffer, MaxMessagesPerFrame);
                    if (received <= 0) break;

                    for (int i = 0; i < received; i++)
                    {
                        try
                        {
                            IntPtr raw = _messageBuffer[i];
                            if (raw == IntPtr.Zero) continue;

                            SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(raw);
                            int size = message.m_cbSize;
                            if (size <= 0) continue;

                            byte[] buffer = new byte[size];
                            Marshal.Copy(message.m_pData, buffer, 0, size);

                            ulong sender = message.m_identityPeer.GetSteamID64();
                            Packet incoming = new Packet();
                            incoming.SetBytes(buffer);
                            Dispatch(sender, incoming);
                        }
                        catch (Exception e)
                        {
                            _consecutiveErrors++;
                            if (_consecutiveErrors > 200)
                            {
                                _disabled = true;
                                Utility.Log("[Net] receive disabled: " + e.Message);
                                return;
                            }
                        }
                    }

                    guard++;
                }
                while (received == MaxMessagesPerFrame && guard < 4);

                _consecutiveErrors = 0;
            }
            catch (Exception e)
            {
                _consecutiveErrors++;
                if (_consecutiveErrors > 200)
                {
                    _disabled = true;
                    Utility.Log("[Net] pump disabled: " + e.Message);
                }
            }
        }

        private static void RegisterLobbyIfNeeded()
        {
            SteamManager manager = SteamManager.Instance;
            if (manager == null) return;

            CSteamID lobby = manager.currentLobby;
            ulong lobbyId = lobby.m_SteamID;

            if (lobbyId == 0)
            {
                if (_registeredLobby != 0)
                {
                    _registeredLobby = 0;
                    _remoteHandlerCache.Clear();
                }
                return;
            }

            if (lobbyId == _registeredLobby) return;

            try
            {
                SteamMatchmaking.SetLobbyMemberData(lobby, VersionKey, BOTPlugin.CurrentBotVersion);
                foreach (KeyValuePair<string, uint> pair in _nameToId)
                    SteamMatchmaking.SetLobbyMemberData(lobby, HandlerKeyPrefix + pair.Value, pair.Key);

                _remoteHandlerCache.Clear();
                _registeredLobby = lobbyId;
            }
            catch (Exception e)
            {
                Utility.Log("[Net] lobby register failed: " + e.Message);
            }
        }

        private static void Dispatch(ulong senderId, Packet packet)
        {
            if (packet == null) return;

            try
            {
                if (packet.UnreadLength() < 4)
                {
                    packet.Dispose();
                    return;
                }

                uint id = (uint)packet.ReadInt();
                string messageId = ResolveMessageId(senderId, id);
                if (string.IsNullOrEmpty(messageId))
                {
                    packet.Dispose();
                    return;
                }

                if (!_nameToId.TryGetValue(messageId, out uint localId) || !_idToHandler.TryGetValue(localId, out MessageHandler handler))
                {
                    packet.Dispose();
                    return;
                }

                handler(senderId, packet);
            }
            catch (Exception e)
            {
                Utility.Log("[Net] handler error: " + e.Message);
            }
            finally
            {
                try { packet.Dispose(); } catch { }
            }
        }

        private static string ResolveMessageId(ulong senderId, uint id)
        {
            string cacheKey = senderId + ":" + id;
            if (_remoteHandlerCache.TryGetValue(cacheKey, out string cached)) return cached;

            string resolved = null;
            try
            {
                SteamManager manager = SteamManager.Instance;
                if (manager != null && manager.currentLobby.m_SteamID != 0)
                    resolved = SteamMatchmaking.GetLobbyMemberData(manager.currentLobby, new CSteamID(senderId), HandlerKeyPrefix + id);
            }
            catch { }

            if (!string.IsNullOrEmpty(resolved)) _remoteHandlerCache[cacheKey] = resolved;
            return resolved;
        }
    }

    public class ModNetPump : MonoBehaviour
    {
        public ModNetPump(IntPtr ptr) : base(ptr) { }
        private void Update() { ModNet.Pump(); }
    }
}
