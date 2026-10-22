using System;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public class BodyRotationManager : MonoBehaviour
    {
        public static BodyRotationManager Instance;
        public static bool IsActive = false;
        public static float FixedAngle = 180f;

        public BodyRotationManager(IntPtr ptr) : base(ptr) { }
        private void Awake() { Instance = this; }

        private void Update()
        {
            if (!IsActive) return;
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (myId == 0) return;

            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
            if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;
            var myPlayer = GameManager.Instance.activePlayers[myId];
            if (myPlayer == null || myPlayer.dead) return;

            BodyInverseManager.SendRotationSafely(0f, FixedAngle, myId);
        }
    }

    public class BodyInverseManager : MonoBehaviour
    {
        public static BodyInverseManager Instance;
        public static bool IsActive = false;
        public static bool IsDynamic = false;
        public static bool Is360Spin = false;
        public static float targetPitch = 270f;
        public static float yawOffset = 0f;
        public static float DynamicSpeed = 10f;
        public static float DynamicRange = 180f;

        public BodyInverseManager(IntPtr ptr) : base(ptr) { }
        private void Awake() { Instance = this; }

        private void Update()
        {
            if (!IsActive && !IsDynamic && !Is360Spin) return;
            if (Camera.main == null) return;
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (myId == 0) return;

            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
            if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;
            var myPlayer = GameManager.Instance.activePlayers[myId];
            if (myPlayer == null || myPlayer.dead) return;

            float cameraYaw = Camera.main.transform.rotation.eulerAngles.y;
            float finalYaw = cameraYaw + yawOffset;
            float finalPitch = targetPitch;

            if (Is360Spin)
            {
                finalPitch = (targetPitch + Time.time * DynamicSpeed * 36f) % 360f;
            }
            else if (IsDynamic)
            {
                float pitchOffset = Mathf.Sin(Time.time * DynamicSpeed) * DynamicRange;
                finalPitch = targetPitch + pitchOffset;
            }

            SendRotationSafely(finalPitch, finalYaw, myId);
        }

        public static void SendRotationSafely(float pitch, float yaw, ulong myId)
        {
            try
            {
                if (Utility.IsHost())
                {
                    foreach (var member in Utility.GetAllLobbyMembers())
                    {
                        ulong targetId = member.m_SteamID;
                        if (targetId != 0 && targetId != myId)
                        {
                            ClientSendClass.PlayerRotation(pitch, yaw, targetId);
                        }
                    }
                }
                else
                {
                    ulong hostId = Utility.GetHostID();
                    if (hostId != 0 && hostId != myId)
                    {
                        ClientSendClass.PlayerRotation(pitch, yaw, hostId);
                    }
                }
            }
            catch { }
        }
    }
}
