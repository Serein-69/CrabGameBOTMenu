using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Dissonance.Config;
using UnhollowerBaseLib;

namespace BOTMenuMod
{

    public static class PerfectMicManager
    {
        public static float BgNoiseRemoval = 0f;
        public static float VoiceDuckLevel = 0.4f;

        private static float _timer = 0f;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(PerfectMicManager));
            Harmony.CreateAndPatchAll(typeof(LipSyncPatch));
            Utility.Log("PerfectMic + LipSync + HeadSwing Initialized.");
        }

        public static void UpdateVoiceSettings()
        {
            try
            {
                VoiceSettings voice = VoiceSettings.Instance;
                if (voice != null && IL2CPP.Il2CppObjectBaseToPtr(voice) != IntPtr.Zero)
                {
                    voice.BackgroundSoundRemovalAmount = BgNoiseRemoval;
                    voice.VoiceDuckLevel = VoiceDuckLevel;
                    voice._vadSensitivity = 100;
                    voice._quality = EnumPublicSealedvaStPoToLoMeHi7vUnique.High;
                    voice.Quality = EnumPublicSealedvaStPoToLoMeHi7vUnique.High;
                    voice.FrameSize = EnumPublicSealedvaTiSmMeLa5vUnique.Large;
                    voice._frameSize = EnumPublicSealedvaTiSmMeLa5vUnique.Large;
                    voice.AecmRoutingMode = EnumPublicSealedvaDiQuEaLoSp7vLoUnique.Disabled;
                    voice.VadSensitivity = EnumPublicSealedvaLoMeHiVe5vUnique.VeryHighSensitivity;
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(SteamManager), "Awake")]
        [HarmonyPostfix]
        public static void OnSteamManagerAwake()
        {
            UpdateVoiceSettings();
        }

        [HarmonyPatch(typeof(OnlinePlayerDissonanceTracker), "Setup")]
        [HarmonyPostfix]
        public static void Postfix_TrackerSetup(OnlinePlayerDissonanceTracker __instance)
        {
            try
            {
                if (__instance != null && IL2CPP.Il2CppObjectBaseToPtr(__instance) != IntPtr.Zero)
                {
                    if (__instance.sphereCollider != null && IL2CPP.Il2CppObjectBaseToPtr(__instance.sphereCollider) != IntPtr.Zero)
                    {
                        __instance.sphereCollider.radius = 99999f;
                    }
                }
            }
            catch { }
        }

        public static void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 1.0f)
            {
                _timer = 0f;
                try
                {
                    var trackers = UnityEngine.Object.FindObjectsOfType<OnlinePlayerDissonanceTracker>();
                    if (trackers == null) return;
                    foreach (var tracker in trackers)
                    {
                        if (tracker == null || IL2CPP.Il2CppObjectBaseToPtr(tracker) == IntPtr.Zero) continue;
                        if (tracker.sphereCollider != null && IL2CPP.Il2CppObjectBaseToPtr(tracker.sphereCollider) != IntPtr.Zero)
                        {
                            if (tracker.sphereCollider.radius < 99999f)
                                tracker.sphereCollider.radius = 99999f;
                        }
                    }
                }
                catch { }
            }
        }
    }

    public static class LipSyncPatch
    {
        private static Dictionary<IntPtr, Transform> headCache = new Dictionary<IntPtr, Transform>();
        private static Dictionary<IntPtr, Quaternion> headRestPose = new Dictionary<IntPtr, Quaternion>();
        public static bool Enabled = true;
        private static float _pruneTimer = 0f;

        public static void ClearCache()
        {
            try { headCache.Clear(); } catch { }
            try { headRestPose.Clear(); } catch { }
            _pruneTimer = 0f;
        }

        private static void PruneCache()
        {
            _pruneTimer += Time.deltaTime;
            if (_pruneTimer < 2f) return;
            _pruneTimer = 0f;

            try
            {
                List<IntPtr> dead = null;
                foreach (var kv in headCache)
                {
                    bool invalid = false;
                    try
                    {
                        Transform cached = kv.Value;
                        if (cached == null || IL2CPP.Il2CppObjectBaseToPtr(cached) == IntPtr.Zero) invalid = true;
                    }
                    catch { invalid = true; }

                    if (invalid)
                    {
                        if (dead == null) dead = new List<IntPtr>();
                        dead.Add(kv.Key);
                    }
                }

                if (dead != null)
                {
                    foreach (var key in dead)
                    {
                        headCache.Remove(key);
                        headRestPose.Remove(key);
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicTrjaObpmObBoObUnique), "LateUpdate")]
        [HarmonyPostfix]
        public static void Postfix(MonoBehaviourPublicTrjaObpmObBoObUnique __instance)
        {
            if (!Enabled) return;

            PruneCache();

            try
            {
                if (__instance == null || IL2CPP.Il2CppObjectBaseToPtr(__instance) == IntPtr.Zero)
                    return;

                float currentVolume = GetSpeakingVolume(__instance);

                Transform head = GetOrFindHead(__instance);
                if (head != null && IL2CPP.Il2CppObjectBaseToPtr(head) != IntPtr.Zero)
                {
                    IntPtr key = IL2CPP.Il2CppObjectBaseToPtr(__instance);
                    if (!headRestPose.ContainsKey(key))
                        headRestPose[key] = head.localRotation;

                    if (currentVolume > 0.001f)
                    {
                        float yaw = Mathf.Sin(Time.time * 20f) * 30f;
                        float pitch = Mathf.Cos(Time.time * 25f) * 25f;
                        head.localRotation = headRestPose[key] * Quaternion.Euler(pitch, yaw, 0f);
                    }
                    else
                    {
                        head.localRotation = headRestPose[key];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LipSync] Error: {e}");
            }
        }

        private static float GetSpeakingVolume(MonoBehaviourPublicTrjaObpmObBoObUnique jawComponent)
        {
            try
            {
                var tracker = jawComponent.GetComponent<MonoBehaviourPublicBoloStBoTrorBoObpmSpUnique>();
                if (tracker == null) return 0f;

                string playerIdStr = tracker.prop_String_0;
                if (string.IsNullOrEmpty(playerIdStr)) return 0f;

                if (DissonanceManager.Instance == null || DissonanceManager.Instance.comms == null) return 0f;
                var state = DissonanceManager.Instance.comms.FindPlayer(playerIdStr);
                if (state != null && state.prop_Boolean_1)
                    return 0.5f;

                return 0f;
            }
            catch { return 0f; }
        }

        private static Transform GetOrFindHead(MonoBehaviourPublicTrjaObpmObBoObUnique instance)
        {
            IntPtr ptr = IL2CPP.Il2CppObjectBaseToPtr(instance);
            if (headCache.TryGetValue(ptr, out Transform cachedHead) && cachedHead != null)
                return cachedHead;

            Transform found = null;

            if (instance.jaw != null && IL2CPP.Il2CppObjectBaseToPtr(instance.jaw) != IntPtr.Zero)
            {
                Transform current = instance.jaw.parent;
                while (current != null)
                {
                    string name = current.name;
                    if (name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Neck", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = current;
                        break;
                    }
                    current = current.parent;
                }
            }

            if (found == null)
            {
                Transform root = instance.transform.root;
                Transform playerModel = root.Find("PlayerModel");
                if (playerModel != null) root = playerModel;

                found = FindDeepChild(root, "Head") ??
                        FindDeepChild(root, "head") ??
                        FindDeepChild(root, "Neck") ??
                        root;
            }

            headCache[ptr] = found;
            return found;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
                Transform deep = FindDeepChild(child, name);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
