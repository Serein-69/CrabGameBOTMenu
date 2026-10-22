using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class ThirdPersonManager
    {
        public static bool enabled = false;
        private static GameObject dummyPlayer;
        private static GameObject mainCameraObj;
        private static MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique dummyMovement;
        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique dummyCosmetic;

        public static float cameraDistance = 4.0f;
        public static float cameraHeight = -0.25f;
        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _localPlayer;

        public static void Update()
        {
            if (!enabled)
            {
                if (dummyPlayer != null) Cleanup();
                return;
            }

            try
            {
                if (_localPlayer == null)
                    _localPlayer = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();

                if (_localPlayer == null)
                {
                    if (dummyPlayer != null) Cleanup();
                    return;
                }

                if (dummyPlayer == null) CreateDummy();
                if (dummyPlayer != null && _localPlayer != null) UpdateCameraAndDummy();
            }
            catch { }
        }

        private static void CreateDummy()
        {
            try
            {
                var gameManager = MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance;
                if (gameManager == null || gameManager.onlinePlayer == null) return;

                dummyPlayer = UnityEngine.Object.Instantiate(gameManager.onlinePlayer, new Vector3(0f, -100f, 0f), Quaternion.identity);
                dummyPlayer.name = "BOT_ThirdPerson_Dummy";
                dummyPlayer.SetActive(false);

                dummyMovement = dummyPlayer.GetComponent<MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique>();
                if (dummyMovement != null) dummyMovement.enabled = false;

                var rb = dummyPlayer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.detectCollisions = false;
                }

                var colliders = dummyPlayer.GetComponentsInChildren<Collider>();
                foreach (var col in colliders) col.enabled = false;

                var audioListener = dummyPlayer.GetComponentInChildren<AudioListener>();
                if (audioListener != null) UnityEngine.Object.DestroyImmediate(audioListener);

                dummyCosmetic = dummyPlayer.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                if (dummyCosmetic != null && MonoBehaviourPublicLi1CoalDi2InitCoUIUnique.Instance != null)
                {
                    var cosmetics = MonoBehaviourPublicLi1CoalDi2InitCoUIUnique.Instance.GetDrip();
                    if (cosmetics != null) dummyCosmetic.SetCosmetics(cosmetics);

                    if (SteamManager.Instance != null)
                    {
                        ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
                        dummyCosmetic.SetPlayer(myId, 1, false);
                    }
                }

                dummyPlayer.SetActive(true);
                var cam = GameObject.Find("Main Camera");
                if (cam != null) mainCameraObj = cam;
            }
            catch
            {
                enabled = false;
                Cleanup();
            }
        }

        private static void UpdateCameraAndDummy()
        {
            try
            {
                if (mainCameraObj == null || dummyPlayer == null || _localPlayer == null) return;
                dummyPlayer.transform.position = _localPlayer.transform.position;
                float rotY = mainCameraObj.transform.rotation.eulerAngles.y;
                dummyPlayer.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                mainCameraObj.transform.localPosition = new Vector3(0f, cameraHeight, -cameraDistance);
            }
            catch { }
        }

        private static void Cleanup()
        {
            try
            {
                if (dummyPlayer != null) { UnityEngine.Object.Destroy(dummyPlayer); dummyPlayer = null; }
                if (mainCameraObj != null) mainCameraObj.transform.localPosition = Vector3.zero;
            }
            catch { }
        }
    }

    public static class SpectatorPOVManager
    {
        public static bool enabled = false;
        public static Text SpectatorText;
        public static CamBob camBobInstance;

        private static GameObject currentSpectatedPlayer;
        private static GameObject lastSpectatedPlayer;
        private static GameObject scoreboardObj;
        private static bool isFirstPerson = false;
        private static float defaultScale = 0.3411f;
        private static float smoothSpeed = 20f;
        private static Vector3 thirdPersonOffset = new Vector3(0, 1.5f, -4.0f);

        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _localMovement;
        private static bool _wasEnabled = false;

        private static bool IsLocalPlayerAlive()
        {
            if (_localMovement == null)
                _localMovement = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            var movement = _localMovement;
            return movement != null && movement.enabled && movement.gameObject.activeInHierarchy;
        }

        public static void Update()
        {
            if (!enabled || IsLocalPlayerAlive())
            {
                if (_wasEnabled)
                {
                    _wasEnabled = false;
                    Cleanup();
                    camBobInstance = null;
                }
                return;
            }
            _wasEnabled = true;

            if (camBobInstance == null)
            {
                camBobInstance = UnityEngine.Object.FindObjectOfType<CamBob>();
                if (camBobInstance == null) return;
            }

            HandleScoreboard();
            HandleInput();

            if (camBobInstance.field_Private_Transform_1 != null)
            {
                currentSpectatedPlayer = camBobInstance.field_Private_Transform_1.gameObject;
            }

            if (currentSpectatedPlayer != null)
            {
                if (lastSpectatedPlayer != currentSpectatedPlayer)
                {
                    if (lastSpectatedPlayer != null) RestorePlayerModel(lastSpectatedPlayer);
                    UpdateSpectatorInfo(currentSpectatedPlayer);
                }
                lastSpectatedPlayer = currentSpectatedPlayer;

                if (isFirstPerson)
                {
                    if (SpectatorText != null && !SpectatorText.gameObject.activeSelf) SpectatorText.gameObject.SetActive(true);
                    HidePlayerModel(currentSpectatedPlayer);
                    UpdateCameraFirstPerson(camBobInstance.transform, currentSpectatedPlayer);
                }
                else
                {
                    if (SpectatorText != null && SpectatorText.gameObject.activeSelf) SpectatorText.gameObject.SetActive(false);
                    RestorePlayerModel(currentSpectatedPlayer);
                    UpdateCameraThirdPerson(camBobInstance.transform, currentSpectatedPlayer);
                }
            }
            else
            {
                if (SpectatorText != null && SpectatorText.gameObject.activeSelf) SpectatorText.gameObject.SetActive(false);
            }
        }

        private static void UpdateSpectatorInfo(GameObject playerObj)
        {
            if (SpectatorText == null) return;
            string displayText = "Unknown";
            ulong steamId = 0;

            try
            {
                if (GameManager.Instance != null)
                {
                    foreach (var pair in GameManager.Instance.activePlayers)
                    {
                        if (pair.Value.gameObject == playerObj) { steamId = pair.Key; break; }
                    }
                    if (steamId == 0)
                    {
                        foreach (var pair in GameManager.Instance.spectators)
                        {
                            if (pair.Value.gameObject == playerObj) { steamId = pair.Key; break; }
                        }
                    }
                }

                if (steamId != 0)
                {
                    string name = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));
                    displayText = $"{name} <size=18><color=grey>[{steamId}]</color></size>";
                }
            }
            catch { }

            SpectatorText.text = $"Spectating: {displayText}";
        }

        private static void HandleInput()
        {
            if (camBobInstance == null) return;
            if (Input.GetMouseButtonDown(1))
            {
                camBobInstance.Method_Private_Void_Int32_0(1);
                if (currentSpectatedPlayer != null) RestorePlayerModel(currentSpectatedPlayer);
            }
            if (Input.GetMouseButtonDown(0))
            {
                isFirstPerson = !isFirstPerson;
                if (!isFirstPerson && currentSpectatedPlayer != null) RestorePlayerModel(currentSpectatedPlayer);
                if (currentSpectatedPlayer != null) UpdateSpectatorInfo(currentSpectatedPlayer);
            }
        }

        private static void HandleScoreboard()
        {
            if (scoreboardObj == null) scoreboardObj = GameObject.Find("Scoreboard") ?? GameObject.Find("GameUI/Scoreboard");
            if (scoreboardObj != null)
            {
                bool shouldShow = Input.GetKey(KeyCode.Tab);
                if (scoreboardObj.activeSelf != shouldShow) scoreboardObj.SetActive(shouldShow);
            }
        }

        private static void UpdateCameraFirstPerson(Transform camInfo, GameObject target)
        {
            Vector3 targetPos = target.transform.position + new Vector3(0, 1.5f, 0);
            Quaternion targetRot = GetTargetRotation(target);
            camInfo.position = Vector3.Lerp(camInfo.position, targetPos, smoothSpeed * Time.deltaTime);
            camInfo.rotation = Quaternion.Slerp(camInfo.rotation, targetRot, smoothSpeed * Time.deltaTime);
        }

        private static void UpdateCameraThirdPerson(Transform camInfo, GameObject target)
        {
            Quaternion targetRot = GetTargetRotation(target);
            Vector3 targetPos = target.transform.position + (targetRot * thirdPersonOffset);
            camInfo.position = Vector3.Lerp(camInfo.position, targetPos, smoothSpeed * Time.deltaTime);

            Vector3 lookAtPos = target.transform.position + new Vector3(0, 1.0f, 0);
            if ((lookAtPos - camInfo.position) != Vector3.zero)
            {
                var lookRot = Quaternion.LookRotation(lookAtPos - camInfo.position);
                camInfo.rotation = Quaternion.Slerp(camInfo.rotation, lookRot, smoothSpeed * Time.deltaTime);
            }
        }

        private static Quaternion GetTargetRotation(GameObject target)
        {
            var pm = target.GetComponent<PlayerManager>();
            if (pm != null) return pm.transform.rotation;
            return target.transform.rotation;
        }

        private static void HidePlayerModel(GameObject player)
        {
            if (player == null) return;
            Transform model = player.transform.Find("PlayerModel");
            if (model != null && model.localScale.x > 0.01f) model.localScale = Vector3.one * 0.0001f;
        }

        private static void RestorePlayerModel(GameObject player)
        {
            if (player == null) return;
            Transform model = player.transform.Find("PlayerModel");
            if (model != null && model.localScale.x < 0.1f) model.localScale = Vector3.one * defaultScale;
        }

        private static void Cleanup()
        {
            if (lastSpectatedPlayer != null) RestorePlayerModel(lastSpectatedPlayer);
            if (currentSpectatedPlayer != null) RestorePlayerModel(currentSpectatedPlayer);
            if (SpectatorText != null && SpectatorText.gameObject.activeSelf) SpectatorText.gameObject.SetActive(false);

            lastSpectatedPlayer = null;
            currentSpectatedPlayer = null;
            isFirstPerson = false;
        }

        public static class CamBobPatch
        {
            [HarmonyPatch(typeof(CamBob), "Awake")]
            [HarmonyPostfix]
            public static void AwakePostfix(CamBob __instance)
            {
                SpectatorPOVManager.camBobInstance = __instance;
                SpectatorPOVManager.isFirstPerson = false;
            }

            [HarmonyPatch(typeof(CamBob), "Update")]
            [HarmonyPrefix]
            public static bool UpdatePrefix(CamBob __instance)
            {
                if (!SpectatorPOVManager.enabled || IsLocalPlayerAlive()) return true;
                SpectatorPOVManager.camBobInstance = __instance;
                return __instance.field_Private_Transform_1 == null;
            }
        }
    }

    public static class FullbrightManager
    {
        public static bool enabled = false;
        private static bool _lastEnabled = false;
        private static Color _backupAmbientColor = Color.black;
        private static float _backupIntensity = 1.0f;
        private static bool _hasBackup = false;

        public static void Update()
        {
            if (enabled && !_lastEnabled)
            {
                try
                {
                    _backupAmbientColor = UnityEngine.RenderSettings.ambientLight;
                    _backupIntensity = UnityEngine.RenderSettings.ambientIntensity;
                    _hasBackup = true;
                    Utility.ForceMessage("Fullbright: Backup Created");
                }
                catch { }
            }

            if (!enabled && _lastEnabled)
            {
                try
                {
                    if (_hasBackup)
                    {
                        UnityEngine.RenderSettings.ambientLight = _backupAmbientColor;
                        UnityEngine.RenderSettings.ambientIntensity = _backupIntensity;
                    }
                    else
                    {
                        UnityEngine.RenderSettings.ambientLight = Color.black;
                        UnityEngine.RenderSettings.ambientIntensity = 1.0f;
                    }
                    UnityEngine.RenderSettings.fog = true;
                }
                catch { }
            }

            _lastEnabled = enabled;

            if (enabled)
            {
                try
                {
                    UnityEngine.RenderSettings.ambientLight = Color.white;
                    UnityEngine.RenderSettings.ambientIntensity = 1.0f;
                    UnityEngine.RenderSettings.fog = false;
                }
                catch { }
            }
        }
    }

    public static class MirrorCamManager
    {
        public static bool enabled = false;
        private static GameObject _camObj;
        private static RawImage _displayUI;

        public static void Toggle()
        {
            enabled = !enabled;
            if (enabled) CreateMirror();
            else Cleanup();
        }

        private static void CreateMirror()
        {
            if (Camera.main == null) return;
            _camObj = new GameObject("BOT_MirrorCam");
            var cam = _camObj.AddComponent<Camera>();
            cam.CopyFrom(Camera.main);
            cam.transform.SetParent(Camera.main.transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.Euler(0, 180, 0);

            RenderTexture rt = new RenderTexture(400, 200, 16);
            cam.targetTexture = rt;

            var canvas = UnityEngine.Object.FindObjectOfType<GameUIClass>().transform;
            GameObject uiObj = new GameObject("MirrorUI");
            uiObj.transform.SetParent(canvas, false);
            _displayUI = uiObj.AddComponent<RawImage>();
            _displayUI.texture = rt;
            _displayUI.color = new Color(1, 1, 1, 0.8f);

            RectTransform rect = uiObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -150f);
            rect.sizeDelta = new Vector2(300, 120);
        }

        public static void Cleanup()
        {
            if (_camObj != null) UnityEngine.Object.Destroy(_camObj);
            if (_displayUI != null) UnityEngine.Object.Destroy(_displayUI.gameObject);
        }
    }

    public static class CinematicGraphicsManager
    {
        public static bool isEnabled = false;
        private static bool _wasEnabled = false;
        private static float _ogAmbientIntensity;
        private static Color _ogAmbientColor;
        private static UnityEngine.Rendering.AmbientMode _ogAmbientMode;
        private static Color _ogSkyColor;
        private static Color _ogEquatorColor;
        private static Color _ogGroundColor;
        private static bool _ogAllowHDR;
        private static Light[] _lights;
        private static readonly Dictionary<Light, float> _ogLightIntensity = new Dictionary<Light, float>();
        private static readonly Dictionary<Light, Color> _ogLightColor = new Dictionary<Light, Color>();
        private static readonly Dictionary<Light, float> _ogShadowStrength = new Dictionary<Light, float>();
        private static readonly Dictionary<UnityEngine.Rendering.PostProcessing.AmbientOcclusion, AoState> _ogAo = new Dictionary<UnityEngine.Rendering.PostProcessing.AmbientOcclusion, AoState>();
        private static readonly Dictionary<UnityEngine.Rendering.PostProcessing.Bloom, BloomState> _ogBloom = new Dictionary<UnityEngine.Rendering.PostProcessing.Bloom, BloomState>();
        private static readonly Dictionary<UnityEngine.Rendering.PostProcessing.Vignette, VignetteState> _ogVignette = new Dictionary<UnityEngine.Rendering.PostProcessing.Vignette, VignetteState>();

        private struct AoState
        {
            public bool active;
            public float intensity;
        }

        private struct BloomState
        {
            public bool active;
            public float intensity;
            public float threshold;
        }

        private struct VignetteState
        {
            public bool active;
            public float intensity;
            public float smoothness;
        }

        public static void Update()
        {
            if (Camera.main == null) return;

            if (isEnabled && !_wasEnabled)
            {
                _ogAmbientIntensity = RenderSettings.ambientIntensity;
                _ogAmbientColor = RenderSettings.ambientLight;
                _ogAmbientMode = RenderSettings.ambientMode;
                _ogSkyColor = RenderSettings.ambientSkyColor;
                _ogEquatorColor = RenderSettings.ambientEquatorColor;
                _ogGroundColor = RenderSettings.ambientGroundColor;
                _ogAllowHDR = Camera.main.allowHDR;
                CaptureOriginals();
                _wasEnabled = true;
            }
            else if (!isEnabled && _wasEnabled)
            {
                RestoreOriginals();
                _wasEnabled = false;
                return;
            }

            if (!isEnabled) return;

            Camera.main.allowHDR = true;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

            int hour = DateTime.Now.Hour;
            Color skyColor, equatorColor, groundColor;

            if (hour >= 6 && hour < 11)
            {
                skyColor = new Color(0.7f, 0.8f, 1f);
                equatorColor = new Color(0.9f, 0.85f, 0.8f);
                groundColor = new Color(0.3f, 0.4f, 0.3f);
            }
            else if (hour >= 11 && hour < 16)
            {
                skyColor = new Color(0.9f, 0.95f, 1f);
                equatorColor = new Color(1f, 1f, 0.9f);
                groundColor = new Color(0.4f, 0.4f, 0.35f);
            }
            else if (hour >= 16 && hour < 19)
            {
                skyColor = new Color(1f, 0.6f, 0.3f);
                equatorColor = new Color(0.8f, 0.3f, 0.1f);
                groundColor = new Color(0.2f, 0.1f, 0.15f);
            }
            else
            {
                skyColor = new Color(0.05f, 0.1f, 0.2f);
                equatorColor = new Color(0.02f, 0.05f, 0.1f);
                groundColor = new Color(0.01f, 0.01f, 0.02f);
            }

            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = equatorColor;
            RenderSettings.ambientGroundColor = groundColor;

            if (_lights != null)
            {
                foreach (var l in _lights)
                {
                    if (l == null) continue;
                    if (l.type == LightType.Directional)
                    {
                        if (hour >= 19 || hour < 6)
                        {
                            l.intensity = 0.2f;
                            l.color = new Color(0.2f, 0.3f, 0.5f);
                        }
                        else if (hour >= 16 && hour < 19)
                        {
                            l.intensity = 1.0f;
                            l.color = new Color(1f, 0.5f, 0.2f);
                        }
                        else
                        {
                            l.intensity = 1.5f;
                            l.color = new Color(1f, 0.95f, 0.9f);
                        }
                        l.shadowStrength = 0.8f;
                    }
                }
            }

            foreach (var kv in _ogAo)
            {
                var ao = kv.Key;
                if (ao == null) continue;
                ao.active = true;
                ao.intensity.value = 1.5f;
            }

            foreach (var kv in _ogBloom)
            {
                var bloom = kv.Key;
                if (bloom == null) continue;
                bloom.active = true;
                bloom.intensity.value = 2.0f;
                bloom.threshold.value = 0.8f;
            }

            foreach (var kv in _ogVignette)
            {
                var vignette = kv.Key;
                if (vignette == null) continue;
                vignette.active = true;
                vignette.intensity.value = 0.35f;
                vignette.smoothness.value = 0.8f;
            }
        }

        private static void CaptureOriginals()
        {
            _ogLightIntensity.Clear();
            _ogLightColor.Clear();
            _ogShadowStrength.Clear();
            _ogAo.Clear();
            _ogBloom.Clear();
            _ogVignette.Clear();

            _lights = UnityEngine.Object.FindObjectsOfType<Light>();
            foreach (var l in _lights)
            {
                if (l == null) continue;
                _ogLightIntensity[l] = l.intensity;
                _ogLightColor[l] = l.color;
                _ogShadowStrength[l] = l.shadowStrength;
            }

            var volumes = UnityEngine.Object.FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            foreach (var vol in volumes)
            {
                if (vol == null || vol.profile == null) continue;

                var ao = vol.profile.GetSetting<UnityEngine.Rendering.PostProcessing.AmbientOcclusion>();
                if (ao != null)
                {
                    AoState state;
                    state.active = ao.active;
                    state.intensity = ao.intensity.value;
                    _ogAo[ao] = state;
                }

                var bloom = vol.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Bloom>();
                if (bloom != null)
                {
                    BloomState state;
                    state.active = bloom.active;
                    state.intensity = bloom.intensity.value;
                    state.threshold = bloom.threshold.value;
                    _ogBloom[bloom] = state;
                }

                var vignette = vol.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Vignette>();
                if (vignette != null)
                {
                    VignetteState state;
                    state.active = vignette.active;
                    state.intensity = vignette.intensity.value;
                    state.smoothness = vignette.smoothness.value;
                    _ogVignette[vignette] = state;
                }
            }
        }

        private static void RestoreOriginals()
        {
            foreach (var kv in _ogLightIntensity)
            {
                var l = kv.Key;
                if (l == null) continue;
                l.intensity = kv.Value;
                Color c;
                if (_ogLightColor.TryGetValue(l, out c)) l.color = c;
                float s;
                if (_ogShadowStrength.TryGetValue(l, out s)) l.shadowStrength = s;
            }

            foreach (var kv in _ogAo)
            {
                var ao = kv.Key;
                if (ao == null) continue;
                ao.active = kv.Value.active;
                ao.intensity.value = kv.Value.intensity;
            }

            foreach (var kv in _ogBloom)
            {
                var bloom = kv.Key;
                if (bloom == null) continue;
                bloom.active = kv.Value.active;
                bloom.intensity.value = kv.Value.intensity;
                bloom.threshold.value = kv.Value.threshold;
            }

            foreach (var kv in _ogVignette)
            {
                var vignette = kv.Key;
                if (vignette == null) continue;
                vignette.active = kv.Value.active;
                vignette.intensity.value = kv.Value.intensity;
                vignette.smoothness.value = kv.Value.smoothness;
            }

            RenderSettings.ambientIntensity = _ogAmbientIntensity;
            RenderSettings.ambientLight = _ogAmbientColor;
            RenderSettings.ambientMode = _ogAmbientMode;
            RenderSettings.ambientSkyColor = _ogSkyColor;
            RenderSettings.ambientEquatorColor = _ogEquatorColor;
            RenderSettings.ambientGroundColor = _ogGroundColor;
            if (Camera.main != null) Camera.main.allowHDR = _ogAllowHDR;

            _lights = null;
            _ogLightIntensity.Clear();
            _ogLightColor.Clear();
            _ogShadowStrength.Clear();
            _ogAo.Clear();
            _ogBloom.Clear();
            _ogVignette.Clear();
        }
    }

    public static class EnemyScaleManager
    {
        public static bool enabled = false;
        public static float scaleMultiplier = 3.0f;
        private static bool _lastEnabled = false;

        public static void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;

            if (_lastEnabled && !enabled) ResetAll(myId);
            _lastEnabled = enabled;

            if (!enabled) return;

            if (Time.frameCount % 10 == 0)
            {
                foreach (var kvp in GameManager.Instance.activePlayers)
                {
                    if (kvp.Key == myId) continue;
                    var enemy = kvp.Value;
                    if (enemy != null && !enemy.dead)
                    {
                        Transform model = enemy.transform.Find("PlayerModel");
                        if (model != null) model.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
                    }
                }
            }
        }

        private static void ResetAll(ulong myId)
        {
            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy != null)
                {
                    Transform model = enemy.transform.Find("PlayerModel");
                    if (model != null) model.localScale = Vector3.one;
                }
            }
        }
    }
}
