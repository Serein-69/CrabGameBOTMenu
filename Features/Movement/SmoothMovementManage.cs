using System;
using HarmonyLib;
using UnityEngine;

namespace BOTMenuMod
{

    public static class SmoothMovementManager
    {
        public static bool enabled = false;
        private static float _elapsed = 0f;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(SmoothMovementManager));
            Utility.Log("Passive Smooth Movement (High-Tickrate Sync) Initialized.");
        }

        public static void Update()
        {
            if (!enabled || Utility.IsHost() || SteamManager.Instance == null) return;

            ulong hostId = Utility.GetHostID();
            if (hostId == 0) return;

            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me == null || me.dead) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= 1f / 30f)
            {
                _elapsed = 0f;
                if (Camera.main != null)
                {
                    Vector3 rot = Camera.main.transform.rotation.eulerAngles;
                    try
                    {
                        ClientSendClass.PlayerRotation(rot.x, rot.y, hostId);
                        ClientSendClass.PlayerPosition(me.transform.position, hostId);
                    }
                    catch { }
                }
            }
        }

        [HarmonyPatch(typeof(ClientSendClass), "PlayerRotation")]
        [HarmonyPrefix]
        public static void Prefix_PlayerRotation(ref float __0, ref float __1)
        {
            if (!enabled) return;

            if (__0 > 180f) __0 -= 360f;
            if (__0 < -180f) __0 += 360f;
            if (__1 > 180f) __1 -= 360f;
            if (__1 < -180f) __1 += 360f;
        }
    }
}
