using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class GodModeManager
    {
        public static bool enabled = false;
        public static bool autoRescue = true;
        public static void Update() { }
    }

    public static class HillGodManager
    {
        public static bool enabled = false;
        private static bool _wasEnabled = false;

        public static void Update()
        {
            if (_wasEnabled && !enabled) UnfreezePlayer();
            _wasEnabled = enabled;

            if (!enabled || GameManager.Instance == null || SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (myId == 0 || !GameManager.Instance.activePlayers.ContainsKey(myId)) return;

            var me = GameManager.Instance.activePlayers[myId];
            if (me == null) return;

            GameObject zone = GameObject.Find("ScoreZone");
            if (zone != null)
            {
                me.transform.position = zone.transform.position + Vector3.up * 1f;
                Rigidbody rb = me.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic) rb.isKinematic = true;
            }
        }

        private static void UnfreezePlayer()
        {
            if (GameManager.Instance == null || SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (myId == 0 || !GameManager.Instance.activePlayers.ContainsKey(myId)) return;
            var me = GameManager.Instance.activePlayers[myId];
            if (me == null) return;
            Rigidbody rb = me.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic) rb.isKinematic = false;
        }
    }

    public static class AntiBoundManager
    {
        public static bool enabled = false;
        public static void Update()
        {
            if (!enabled || Utility.IsHost()) return;
            if (Time.frameCount % 60 == 0)
            {
                var killBounds = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicSikiUnique>();
                if (killBounds != null)
                {
                    foreach (var bound in killBounds)
                        if (bound != null) UnityEngine.Object.Destroy(bound);
                }
            }
        }
    }

    public static class MiniGameTrollManager
    {
        public static bool rlglGodMode = false;
        public static bool rlglBot = false;
        public static bool trollGlassBreak = false;
        public static Vector3 rlglSafePos = Vector3.zero;
        private static float _glassTimer = 0f;
        private static float _glassScanTime = 0f;
        private static bool _glassScanned = false;
        private static readonly List<GameObject> _glassCache = new List<GameObject>();
        private static readonly Dictionary<int, Material> _glassMats = new Dictionary<int, Material>();

        public static void Update()
        {
            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me == null || me.dead) return;

            if (rlglBot)
            {
                if (IsGreenLight()) me.transform.position += new Vector3(0, 0, 1f) * 5f * Time.deltaTime;
                else
                {
                    var rb = me.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = Vector3.zero;
                }
            }

            if (BridgeHelperManager.noGlassBreak || trollGlassBreak)
            {
                _glassTimer += Time.deltaTime;
                if (_glassTimer > 0.2f)
                {
                    _glassTimer = 0f;
                    ProcessGlass();
                }
            }
        }

        public static bool IsGreenLight()
        {
            var rlglMode = UnityEngine.Object.FindObjectOfType<GameModeRedLight>();
            if (rlglMode != null)
            {
                var type = rlglMode.GetIl2CppType();
                var timeField = type.GetField("currentTime", Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Public);
                if (timeField != null) return timeField.GetValue(rlglMode).Unbox<float>() == 0.5f;
            }
            var audioObj = GameObject.Find("StatueFinal/GreenLight") ?? GameObject.Find("Statue/GreenLight");
            if (audioObj != null)
            {
                var src = audioObj.GetComponent<AudioSource>();
                if (src != null && src.isPlaying) return true;
            }
            return false;
        }

        public static void ProcessGlass()
        {
            if (!_glassScanned || Time.time - _glassScanTime > 0.75f)
            {
                _glassScanned = true;
                _glassScanTime = Time.time;
                _glassCache.Clear();
                _glassCache.AddRange(UnityEngine.Object.FindObjectsOfType<GameObject>());
            }

            foreach (var obj in _glassCache)
            {
                if (obj == null) continue;
                string name = obj.name;
                if (name.Contains("Glass") || name.Contains("Piece") || name.Contains("Pice"))
                {
                    bool isOriginallyFake = obj.GetComponent<Rigidbody>() != null || (obj.GetComponent<Collider>() != null && obj.GetComponent<Collider>().isTrigger);

                    if (BridgeHelperManager.noGlassBreak && isOriginallyFake)
                    {

                        var rb = obj.GetComponent<Rigidbody>();
                        if (rb != null) UnityEngine.Object.Destroy(rb);

                        var col = obj.GetComponent<Collider>();
                        if (col != null) col.isTrigger = false;

                        var scripts = obj.GetComponents<MonoBehaviour>();
                        foreach (var s in scripts)
                        {
                            if (s == null) continue;
                            string sName = s.GetIl2CppType().Name;
                            if (sName.Contains("Break") || sName.Contains("Gasogl") || sName.Length > 20)
                            {
                                UnityEngine.Object.Destroy(s);
                            }
                        }

                        var rnd = obj.GetComponent<Renderer>();
                        if (rnd != null)
                        {
                            var mat = GetGlassMaterial(rnd);
                            if (mat != null) mat.color = new Color(0f, 1f, 1f, 0.4f);
                        }
                    }

                    if (trollGlassBreak && !isOriginallyFake)
                    {
                        if (obj.GetComponent<Rigidbody>() == null)
                        {
                            var rb = obj.AddComponent<Rigidbody>();
                            rb.isKinematic = false;
                            rb.useGravity = true;
                            rb.mass = 50f;
                        }
                        var col = obj.GetComponent<Collider>();
                        if (col != null) col.isTrigger = true;
                        var rnd = obj.GetComponent<Renderer>();
                        if (rnd != null)
                        {
                            var mat = GetGlassMaterial(rnd);
                            if (mat != null) mat.color = new Color(1f, 0f, 0f, 0.4f);
                        }
                    }
                }
            }
        }

        private static Material GetGlassMaterial(Renderer rnd)
        {
            int id = rnd.GetInstanceID();
            Material mat;
            if (_glassMats.TryGetValue(id, out mat) && mat != null) return mat;
            mat = rnd.material;
            _glassMats[id] = mat;
            return mat;
        }
    }

    public static class BridgeHelperManager
    {
        public static bool noGlassBreak = false;
        private static List<Vector3> _targetStones = new List<Vector3>();
        private static Vector3 _returnPos;
        private static bool _isProcessingTP = false;
        private static int _currentTPIndex = 0;
        private static float _tpTimer = 0f;
        private static List<int> _remoteTargetIds = new List<int>();
        private static bool _isRemoteProcessing = false;
        private static int _remoteCurrentIndex = 0;
        private static float _remoteTimer = 0f;

        public static void Update()
        {

            if (noGlassBreak && Time.frameCount % 15 == 0)
            {
                MiniGameTrollManager.ProcessGlass();
            }

            if (_isProcessingTP)
            {
                var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                if (player == null || player.dead) { _isProcessingTP = false; return; }
                _tpTimer += Time.deltaTime;
                if (_tpTimer >= 0.05f)
                {
                    _tpTimer = 0f;
                    if (_currentTPIndex < _targetStones.Count)
                    {
                        player.transform.position = _targetStones[_currentTPIndex];
                        _currentTPIndex++;
                    }
                    else
                    {
                        player.transform.position = _returnPos;
                        _isProcessingTP = false;
                        Utility.ForceMessage("<color=green>Bridge Cleared!</color>");
                    }
                }
            }

            if (_isRemoteProcessing)
            {
                _remoteTimer += Time.deltaTime;
                if (_remoteTimer >= 0.05f)
                {
                    _remoteTimer = 0f;
                    if (_remoteCurrentIndex < _remoteTargetIds.Count)
                    {
                        try { MonoBehaviourPublicInpabyInInInUnique.TryInteract(_remoteTargetIds[_remoteCurrentIndex]); } catch { }
                        _remoteCurrentIndex++;
                    }
                    else
                    {
                        _isRemoteProcessing = false;
                        Utility.ForceMessage("<color=green>All fake glasses shattered remotely!</color>");
                    }
                }
            }
        }

        public static void ExecuteRemoteBreak()
        {
            if (_isRemoteProcessing) return;
            var dict = MonoBehaviourPublicDi2InObInObInUnique.Instance?.field_Private_Dictionary_2_Int32_MonoBehaviourPublicInidBoskUnique_0;
            if (dict == null) return;
            _remoteTargetIds.Clear();
            foreach (var kvp in dict)
            {
                if (kvp.Value == null || kvp.Value.gameObject == null) continue;
                string name = kvp.Value.gameObject.name;
                if (name.Contains("Glass") || name.Contains("Piece") || name.Contains("Pice"))
                {
                    bool isBad = kvp.Value.gameObject.GetComponent<Rigidbody>() != null || (kvp.Value.gameObject.GetComponent<Collider>()?.isTrigger == true);
                    if (isBad) _remoteTargetIds.Add(kvp.Key);
                }
            }
            if (_remoteTargetIds.Count > 0)
            {
                _isRemoteProcessing = true;
                _remoteCurrentIndex = 0;
                _remoteTimer = 0f;
                Utility.ForceMessage($"<color=cyan>Remote Breaking {_remoteTargetIds.Count} glasses...</color>");
            }
        }

        public static void ExecuteTeleportBreak()
        {
            if (_isProcessingTP) return;
            var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (player == null || player.dead) return;
            _targetStones.Clear();
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (obj.name.Contains("Glass") || obj.name.Contains("Piece") || obj.name.Contains("Pice"))
                {
                    bool isBad = obj.GetComponent<Rigidbody>() != null || obj.GetComponent<Collider>()?.isTrigger == true;
                    if (isBad) _targetStones.Add(obj.transform.position + Vector3.up * 1.5f);
                }
            }
            if (_targetStones.Count > 0)
            {
                _returnPos = player.transform.position;
                _isProcessingTP = true;
                _currentTPIndex = 0;
                Utility.ForceMessage($"<color=cyan>TP Breaking {_targetStones.Count} Fake Stones...</color>");
            }
        }
    }

    public static class BridgeTPManager
    {
        public static void TeleportToNextSafe()
        {
            var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (player == null || player.dead) return;
            GameObject nextSafeStone = null;
            float minDistance = float.MaxValue;
            Vector3 myPos = player.transform.position;

            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (obj.name.Contains("Glass") || obj.name.Contains("Piece") || obj.name.Contains("Pice"))
                {
                    bool isBad = obj.GetComponent<Rigidbody>() != null || obj.GetComponent<Collider>()?.isTrigger == true;
                    if (!isBad)
                    {
                        float dist = Vector3.Distance(myPos, obj.transform.position);
                        if (dist > 2f && dist < minDistance)
                        {
                            minDistance = dist;
                            nextSafeStone = obj;
                        }
                    }
                }
            }
            if (nextSafeStone != null)
            {
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = Vector3.zero;
                player.transform.position = nextSafeStone.transform.position + Vector3.up * 1.5f;
                Utility.ForceMessage("Teleported to Safe Stone!");
            }
        }
    }

    public static class BridgeESPManager
    {
        public static bool enabled = false;
        private static bool lastEnabled = false;

        public static void Update()
        {
            if (lastEnabled && !enabled) RevertColors();
            lastEnabled = enabled;
            if (!enabled || Time.frameCount % 60 != 0) return;
            ScanAndColor();
        }

        private static void ScanAndColor()
        {
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.name.Contains("Glass") || obj.name.Contains("Piece") || obj.name.Contains("Pice"))
                {
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer == null) continue;
                    bool isBad = obj.GetComponent<Rigidbody>() != null || obj.GetComponent<Collider>()?.isTrigger == true;
                    renderer.material.color = isBad ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
                }
            }
        }

        private static void RevertColors()
        {
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.name.Contains("Glass") || obj.name.Contains("Piece") || obj.name.Contains("Pice"))
                {
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = Color.white;
                }
            }
        }
    }

    public static class ButtonESPManager
    {
        public static bool enabled = false;
        private static bool lastEnabled = false;
        private static float updateTimer = 0f;

        public static void Update()
        {
            if (lastEnabled && !enabled) RevertVisuals();
            lastEnabled = enabled;
            if (!enabled) return;

            updateTimer += Time.deltaTime;
            if (updateTimer < 0.1f) return;
            updateTimer = 0f;

            var gameMode = UnityEngine.Object.FindObjectOfType<GameModeBustlingButtons>();
            if (gameMode == null) return;

            int badId = -1;
            var gmType = gameMode.GetIl2CppType();
            var flags = Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic;
            foreach (var f in gmType.GetFields(flags))
            {
                if (f.FieldType.Name == "Int32")
                {
                    try { badId = f.GetValue(gameMode).Unbox<int>(); break; } catch { }
                }
            }

            var allRenderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;
                ColorButton(renderer, badId, true);
            }
        }

        private static void RevertVisuals()
        {
            var allRenderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;
                ColorButton(renderer, -1, false);
            }
        }

        private static void ColorButton(MeshRenderer renderer, int badId, bool apply)
        {
            var btnScript = renderer.GetComponent<BustlingButtonsButton>() ?? renderer.GetComponentInParent<BustlingButtonsButton>();
            if (btnScript != null)
            {
                if (apply)
                {
                    renderer.material.color = (btnScript.buttonId == badId) ? Color.black : Color.white;
                }
                else
                {
                    renderer.material.color = Color.white;
                }
            }
        }
    }
}
