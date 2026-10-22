using System;
using System.IO;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using SteamworksNative;
using UnhollowerBaseLib;
using BepInEx.IL2CPP.Utils.Collections;

namespace BOTMenuMod
{
    public class ShadowModePatches
    {
        public static bool crashPlayerOnHit = false;
        public static bool enableNoWeaponPush = false;
        public static bool hideFromPlayerList = true;
        public static bool preventKickOnBan = true;
        public static bool bypassFreeze = false;
        public static bool enableFakePing = false;
        public static bool enablePingJitter = false;
        public static bool enableAutoBox = false;
        public static bool enableUncensoredChat = false;
        public static bool showMarker = true;
        public static bool enableSpinbot = false;
        public static bool respawnOnDeath = false;
        public static bool enableStrongCrash = false;
        public static bool disableChat = false;
        private static bool _isRetryingLoading = false;

        public static Vector3 spawnPosition;
        public static ShadowPlayer ShadowPlayerData;

        public static float spinSpeed = 2000f;
        public static int spinMode = 1;
        private static bool _isSendingCrash = false;
        public static int crashMode = 0;
        private static float _serverRotationY = 0f;
        private static float _lastRecoilTime = 0f;

        public static bool noShake = false;
        public static bool enableAntiSpectate = false;
        public static bool enableAutoReady = false;
        public static bool blockLoadMap = false;

        public static int fakePingValue = 0;
        public static int pingMin = 10;
        public static int pingMax = 100;
        public static int pingInterval = 200;
        public static int autoBoxCount = 0;
        public static int autoBoxDelayMs = 500;

        public static CameraControllerClass cameraInstance;

        private static System.Random _rng = new System.Random();
        private static float _lastJitterUpdate = 0f;
        private static int _cachedJitterPing = 0;

        public static void Update() { }

        [HarmonyPatch(typeof(ClientSendClass), "PlayerPosition")]
        [HarmonyPrefix]
        public static bool PrefixPlayerPosition(ref Vector3 __0, ulong __1)
        {
            if (enableAntiSpectate)
            {
                __0 = new Vector3(float.NaN, float.NaN, float.NaN);
                return true;
            }

            if (FreecamManager.enabled) return false;

            if (MiniGameTrollManager.rlglGodMode && SteamManager.Instance != null)
            {
                ulong hostId = Utility.GetHostID();
                if (MiniGameTrollManager.IsGreenLight())
                {
                    MiniGameTrollManager.rlglSafePos = __0;
                }
                else
                {
                    if (__1 == hostId)
                    {
                        __0 = MiniGameTrollManager.rlglSafePos;
                        return true;
                    }
                }
            }

            if (BacktrackingManager.enabled)
            {
                if (Time.time - BacktrackingManager.lastSendTime < BacktrackingManager.interval) return false;
                BacktrackingManager.lastSendTime = Time.time;
                BacktrackingManager.lastSentPos = __0;
            }

            return true;
        }

        [HarmonyPatch(typeof(ClientSendClass), "PlayerRotation")]
        [HarmonyPrefix]
        public static bool PrefixPlayerRotation(ref float __0, ref float __1)
        {
            if (enableAntiSpectate)
            {
                __0 = float.NaN;
                __1 = float.NaN;
                return true;
            }

            if (enableSpinbot)
            {
                switch (spinMode)
                {
                    case 0:
                        _serverRotationY += (spinSpeed * Time.deltaTime);
                        if (_serverRotationY > 360f) _serverRotationY -= 360f;
                        __1 = _serverRotationY;
                        break;
                    case 1:
                        float currentLook = 0f;
                        if (Camera.main != null) currentLook = Camera.main.transform.rotation.eulerAngles.y;
                        __1 = (Time.frameCount % 2 == 0) ? currentLook + 90f : currentLook - 90f;
                        break;
                    case 2:
                        __1 = UnityEngine.Random.Range(0f, 360f);
                        break;
                }
            }

            if (FreecamManager.enabled) return false;

            if (BacktrackingManager.enabled)
            {
                if (Time.time - BacktrackingManager.lastSendTime < BacktrackingManager.interval) return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CameraControllerClass), "BobOnce")]
        [HarmonyPrefix]
        public static bool PrefixNoShake()
        {
            if (noShake) return false;
            return true;
        }

        [HarmonyPatch(typeof(RecoilClass), "AddRecoil")]
        [HarmonyPrefix]
        public static bool PrefixNoRecoil()
        {
            if (noShake) return false;
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "LoadMap")]
        [HarmonyPrefix]
        public static bool OnClientHandleLoadMap(PacketClass __0)
        {
            if (blockLoadMap)
            {
                int mapId = BitConverter.ToInt32(__0.field_Private_ArrayOf_Byte_0.ToArray(), 12);
                Utility.ForceMessage($"Blocked Map Load (ID: {mapId})");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "ShowWinner")]
        [HarmonyPrefix]
        public static bool OnClientHandleShowWinner()
        {
            return !blockLoadMap;
        }

        [HarmonyPatch(typeof(ClientSendClass), "StartedLoading")]
        [HarmonyPrefix]
        public static bool OnClientSendStartedLoading()
        {
            if (!ShadowModePatches.hideFromPlayerList) return true;
            if (_isRetryingLoading)
            {
                _isRetryingLoading = false;
                return true;
            }
            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.StartCoroutine(DelayedSendStartedLoading().WrapToIl2Cpp());
            }
            return false;
        }

        private static System.Collections.IEnumerator DelayedSendStartedLoading()
        {
            yield return new UnityEngine.WaitForSeconds(1.0f);
            _isRetryingLoading = true;
            try { ClientSendClass.StartedLoading(); } catch { }
        }

        [HarmonyPatch(typeof(CameraControllerClass), "Awake")]
        [HarmonyPostfix]
        public static void Postfix_CameraAwake(CameraControllerClass __instance)
        {
            cameraInstance = __instance;
        }

        [HarmonyPatch(typeof(GameManagerClass), "SpawnPlayer")]
        [HarmonyPrefix]
        public static bool Prefix_SpawnPlayer(GameManagerClass __instance, ulong param_1, Vector3 param_2, int param_3, bool param_4, Il2CppStructArray<byte> param_5, int param_6)
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (param_1 == myId)
            {
                spawnPosition = param_2;
                ShadowPlayerData = new ShadowPlayer(param_1, param_2, param_3, param_4, param_5, param_6);
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerMovement), "Start")]
        [HarmonyPostfix]
        public static void Postfix_PlayerStart()
        {
            if (bypassFreeze)
            {
                PersistentPlayerData.frozen = false;
                PersistentPlayerData.hnsFrozen = false;
            }
        }

        [HarmonyPatch(typeof(ClientHandleClass), "LobbyClosed")]
        [HarmonyPrefix]
        static bool OnClientHandleLobbyClosed()
        {
            if (!preventKickOnBan) return true;
            Utility.ForceMessage("Blocked Kick/Ban.");
            return false;
        }

        [HarmonyPatch(typeof(PlayerManager), "Awake")]
        [HarmonyPostfix]
        static void OnPlayerManagerAwake()
        {
            ServerConfig.field_Public_Static_Single_0 = 999999f;
            if (bypassFreeze) PersistentPlayerData.frozen = false;
        }

        [HarmonyPatch(typeof(NetStatus), "GetPing")]
        [HarmonyPrefix]
        public static bool PrefixGetPing(ref int __result)
        {
            if (enableFakePing)
            {
                if (enablePingJitter)
                {
                    if (Time.unscaledTime - _lastJitterUpdate > (pingInterval / 1000f))
                    {
                        _cachedJitterPing = _rng.Next(Math.Min(pingMin, pingMax), Math.Max(pingMin, pingMax) + 1);
                        _lastJitterUpdate = Time.unscaledTime;
                    }
                    __result = _cachedJitterPing;
                }
                else { __result = fakePingValue; }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RecoilClass), "AddRecoil")]
        [HarmonyPostfix]
        public static void PostAddRecoil()
        {
            if (enableNoWeaponPush) _lastRecoilTime = Time.time;
        }

        [HarmonyPatch(typeof(PlayerMovement), "PushPlayer")]
        [HarmonyPrefix]
        public static bool PrefixPushPlayer(Vector3 __0)
        {
            if (!enableNoWeaponPush) return true;
            if (Time.time - _lastRecoilTime < 0.15f) return false;
            return true;
        }

        [HarmonyPatch(typeof(ClientSendClass), "PlayerDied")]
        [HarmonyPrefix]
        public static bool PrefixPlayerDied()
        {
            if (GodModeManager.enabled) return false;
            return true;
        }

        [HarmonyPatch(typeof(PlayerListingPrefab), "SetPlayer")]
        [HarmonyPostfix]
        public static void Postfix_SetPlayer(PlayerListingPrefab __instance, PlayerManager __0)
        {
            if (__0 != null && __0.steamProfile != null) BotTabManager.ApplyBotTag(__instance, __0.steamProfile.m_SteamID);
        }

        [HarmonyPatch(typeof(PlayerListingPrefab), "SetSpectator")]
        [HarmonyPostfix]
        public static void Postfix_SetSpectator(PlayerListingPrefab __instance, ulong __0)
        {
            BotTabManager.ApplyBotTag(__instance, __0);
        }

        [HarmonyPatch(typeof(ClientSendClass), "DamagePlayer")]
        [HarmonyPrefix]
        public static bool PrefixDamagePlayer(ulong __0, int __1, ref Vector3 __2, int __3, int __4)
        {
            if (_isSendingCrash) return true;

            ulong myId = SteamManager.Instance?.field_Private_CSteamID_0.m_SteamID ?? 0;
            if (__0 == myId && GodModeManager.enabled) return false;

            if (__0 != myId && __0 > 1)
            {
                if (WeaponAttackManager.enableKnockbackMult && WeaponAttackManager.knockbackMult != 1.0f) __2 *= WeaponAttackManager.knockbackMult;
                if (WeaponAttackManager.enableKnockbackDeflect && __2 != Vector3.zero && __2.magnitude > 0.1f)
                    __2 = Quaternion.Euler(0, WeaponAttackManager.knockbackDeflectAngle, 0) * __2;

                if (crashPlayerOnHit)
                {
                    _isSendingCrash = true;
                    Vector3 extremePos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    for (int i = 0; i < 100; i++) ClientSendClass.DamagePlayer(__0, 0, extremePos, __3, __4);

                    Vector3 skyPos = new Vector3(0, 99999f, 0);
                    for (int i = 0; i < 50; i++) ClientSendClass.DamagePlayer(__0, 99999, skyPos, __3, __4);

                    if (enableStrongCrash)
                    {
                        for (int i = 0; i < 80; i++) ClientSend.PlayerReload(float.NaN);
                    }
                    _isSendingCrash = false;
                    return false;
                }

                if (GameManager.Instance?.activePlayers?.ContainsKey(__0) == true)
                {
                    HitEffectManager.SpawnHeart(__0);
                    if (DamageNotificationManager.enabled && __3 != 9)
                    {
                        string targetName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(__0)));
                        DamageNotificationManager.AddDamageLog(targetName, __1 > 0 ? __1 : -520f);
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientSendClass), "PunchPlayer")]
        [HarmonyPrefix]
        public static bool Prefix_ClientSend_PunchPlayer(ulong __0, ref Vector3 __1)
        {
            if (_isSendingCrash) return true;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;

            if (__0 != myId && __0 > 1)
            {
                if (WeaponAttackManager.enableKnockbackMult && WeaponAttackManager.knockbackMult != 1.0f) __1 *= WeaponAttackManager.knockbackMult;
                if (WeaponAttackManager.enableKnockbackDeflect && __1 != Vector3.zero && __1.magnitude > 0.1f)
                    __1 = Quaternion.Euler(0, WeaponAttackManager.knockbackDeflectAngle, 0) * __1;

                if (crashPlayerOnHit)
                {
                    _isSendingCrash = true;
                    if (crashMode == 0)
                    {
                        Vector3 crashVector = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        for (int i = 0; i < 100; i++) ClientSendClass.PunchPlayer(__0, crashVector);
                    }
                    else
                    {
                        Vector3 crashVector = new Vector3(0, 99999f, 0);
                        for (int i = 0; i < 500; i++) ClientSendClass.PunchPlayer(__0, crashVector);
                    }
                    _isSendingCrash = false;
                    return false;
                }

                if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(__0))
                {
                    HitEffectManager.SpawnHeart(__0);
                    if (DamageNotificationManager.enabled)
                    {
                        string targetName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(__0)));
                        DamageNotificationManager.AddDamageLog(targetName, -520f);
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicRaovTMinTemeColoonCoUnique), "SendMessage", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool Prefix_SendMessage(MonoBehaviourPublicRaovTMinTemeColoonCoUnique __instance, ref string __0)
        {
            if (string.IsNullOrEmpty(__0)) return false;
            if (!enableUncensoredChat) return true;

            __instance.prop_Boolean_0 = false;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            string myName = "Player";
            if (myId != 0) myName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(myId)));

            ClientSendClass.SendChatMessage(__0);
            __instance.AppendMessage(myId, __0, myName);

            if (__instance.inputField != null)
            {
                __instance.inputField.text = "";
                __instance.inputField.interactable = false;
            }
            return false;
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicUIInUIByUIUnique), "ReceiveChatMessage")]
        [HarmonyPrefix]
        private static bool OnClientHandleReceiveChatMessagePre()
        {
            if (disableChat)
            {
                string clearLines = string.Concat(Enumerable.Repeat("?\n", 20));
                MonoBehaviourPublicInpabyInInInUnique.SendChatMessage(clearLines);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientHandleClass), "PlayerDied")]
        [HarmonyPrefix]
        public static bool Prefix_ClientHandlePlayerDied(PacketClass __0)
        {
            try
            {
                ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                ulong diedId = BitConverter.ToUInt64(__0.field_Private_ArrayOf_Byte_0.ToArray(), 8);

                if (respawnOnDeath && diedId == myId)
                {
                    if (GameManager.Instance != null) GameManager.Instance.RespawnPlayer(myId, spawnPosition);
                    Utility.ForceMessage("<color=magenta>Shadow Respawned (影子复活生效)</color>");
                    return false;
                }
            }
            catch { }
            return true;
        }

        [HarmonyPatch(typeof(GameManagerClass), "SpawnSpectator")]
        [HarmonyPrefix]
        public static bool Prefix_SpawnSpectator(GameManagerClass __instance, ulong __0)
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (!respawnOnDeath || __0 != myId) return true;

            Vector3 spawnPos = Vector3.zero;
            try
            {
                if (SpawnManager.Instance != null) spawnPos = SpawnManager.Instance.FindGroundedSpawnPosition(myId);
            }
            catch { }

            if (spawnPos == Vector3.zero) spawnPos = spawnPosition != Vector3.zero ? spawnPosition : Vector3.zero;

            if (__instance.activePlayers.ContainsKey(myId)) __instance.activePlayers.Remove(myId);

            int pNum = ShadowPlayerData != null ? ShadowPlayerData.playerNumber : 0;
            bool unkBool = ShadowPlayerData != null ? ShadowPlayerData.unknowBool : false;
            Il2CppStructArray<byte> cData = ShadowPlayerData != null ? ShadowPlayerData.clientData : new Il2CppStructArray<byte>(0L);
            int unkInt = ShadowPlayerData != null ? ShadowPlayerData.unknownInt : 0;

            __instance.SpawnPlayer(myId, spawnPos, pNum, unkBool, cData, unkInt);
            Utility.ForceMessage("<color=magenta>Shadow Respawned (影子复活生效)</color>");
            return false;
        }

        public class ShadowPlayer
        {
            public ulong steamId;
            public Vector3 position;
            public int playerNumber;
            public bool unknowBool;
            public Il2CppStructArray<byte> clientData;
            public int unknownInt;
            public ShadowPlayer(ulong id, Vector3 pos, int num, bool b, Il2CppStructArray<byte> data, int i)
            {
                steamId = id; position = pos; playerNumber = num; unknowBool = b; clientData = data; unknownInt = i;
            }
        }
    }
}
