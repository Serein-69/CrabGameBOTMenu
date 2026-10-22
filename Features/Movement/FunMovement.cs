using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteamworksNative;
using UnhollowerBaseLib;

namespace BOTMenuMod
{
    public static class OrbitManager
    {
        public static bool enabled = false;
        public static bool autoCycle = false;
        public static float cycleInterval = 4.0f;
        private static float _timer = 0f;
        private static bool lastEnabled = false;
        private static int _resetForceFrames = 0;

        public static float speed = 3f;
        public static float radius = 3f;
        public static float height = 2.5f;
        public static ulong targetSteamId = 0;

        public static void Toggle(bool state)
        {
            enabled = state;
            if (!enabled)
            {
                _resetForceFrames = 15;
                ApplyBodyFix();
                Utility.ForceMessage("Orbit Walk: OFF");
            }
            else
            {
                Utility.ForceMessage("Orbit Walk: ON");
            }
        }

        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique GetLocalPlayer()
        {
            try { return UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>(); }
            catch { return null; }
        }

        public static void Update()
        {
            try
            {
                if (lastEnabled && !enabled)
                {
                    _resetForceFrames = 15;
                    Utility.ForceMessage("Fixing Controls...");
                }
                lastEnabled = enabled;

                if (_resetForceFrames > 0)
                {
                    ApplyBodyFix();
                    _resetForceFrames--;
                    return;
                }

                if (!enabled) return;

                var me = GetLocalPlayer();
                if (me == null || me.dead) return;

                if (autoCycle)
                {
                    _timer += Time.deltaTime;
                    ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
                    bool isSelf = targetSteamId == myId;
                    bool currentTargetInvalid = !isSelf && (targetSteamId == 0 || !IsPlayerAliveAndActive(targetSteamId));

                    if (_timer > cycleInterval || currentTargetInvalid)
                    {
                        _timer = 0f;
                        CycleToNextLivingTarget();
                    }
                }

                if (MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance == null) return;

                Vector3 centerPos = Vector3.zero;
                bool targetFound = false;
                float orbitRadius = radius;
                ulong mySteamID = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

                if (targetSteamId == mySteamID)
                {
                    centerPos = me.transform.position;
                    targetFound = true;
                    orbitRadius = 0f;
                }
                else if (targetSteamId != 0 && MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance.activePlayers.ContainsKey(targetSteamId))
                {
                    var targetPlayer = MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance.activePlayers[targetSteamId];
                    if (targetPlayer != null)
                    {
                        centerPos = targetPlayer.transform.position;
                        targetFound = true;
                    }
                }

                if (!targetFound) return;

                var rb = me.GetComponent<Rigidbody>();
                var col = me.GetComponent<Collider>();

                if (rb != null)
                {
                    if (!rb.isKinematic) rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                }
                if (targetSteamId != mySteamID)
                {
                    if (col != null && col.enabled) col.enabled = false;
                }
                else
                {
                    if (col != null && !col.enabled) col.enabled = true;
                }

                if (targetSteamId != mySteamID)
                {
                    float time = Time.time;
                    float x = Mathf.Cos(time * speed) * orbitRadius;
                    float z = Mathf.Sin(time * speed) * orbitRadius;
                    Vector3 finalPos = centerPos + new Vector3(x, height, z);
                    me.transform.position = finalPos;
                    me.transform.LookAt(centerPos);
                }
            }
            catch { }
        }

        private static void ApplyBodyFix()
        {
            try
            {
                var me = GetLocalPlayer();
                if (me == null) return;

                var rb = me.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                }

                var col = me.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                if (Camera.main != null)
                {
                    float cameraY = Camera.main.transform.rotation.eulerAngles.y;
                    me.transform.rotation = Quaternion.Euler(0f, cameraY, 0f);

                    if (PlayerInput.Instance != null)
                    {
                        PlayerInput.Instance.cameraRot = new Vector2(
                            PlayerInput.Instance.cameraRot.x,
                            cameraY
                        );
                    }

                    Transform orientation = me.transform.Find("Orientation");
                    if (orientation != null)
                    {
                        orientation.rotation = Quaternion.Euler(0f, cameraY, 0f);
                    }
                }
            }
            catch { }
        }

        private static bool IsPlayerAliveAndActive(ulong id)
        {
            try
            {
                if (MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance == null ||
                    MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance.activePlayers == null)
                    return false;

                if (!MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance.activePlayers.ContainsKey(id)) return false;
                var p = MonoBehaviourPublicDi2UIObacspDi2UIObUnique.Instance.activePlayers[id];
                return p != null && !p.dead;
            }
            catch { return false; }
        }

        public static void CycleToNextLivingTarget()
        {
            try
            {
                var allIds = PlayerInfoManager.GetPlayerIDs();
                if (allIds.Count == 0 && targetSteamId == 0) return;

                List<ulong> targets = new List<ulong>();
                ulong myId = 0;
                ulong hostId = 0;

                if (SteamManager.Instance != null)
                {
                    myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
                    hostId = Utility.GetHostID();
                }

                if (myId != 0) targets.Add(myId);

                foreach (var id in allIds)
                {
                    if (id == myId) continue;
                    if (IsPlayerAliveAndActive(id)) targets.Add(id);
                }

                if (targets.Count == 0)
                {
                    Utility.ForceMessage("No valid targets.");
                    return;
                }

                int currentIndex = targets.IndexOf(targetSteamId);
                if (currentIndex == -1) currentIndex = -1;

                int nextIndex = (currentIndex + 1) % targets.Count;
                targetSteamId = targets[nextIndex];

                string name = "Unknown";
                string tags = "";

                if (targetSteamId == myId) tags += " <color=green>[ME]</color>";
                if (targetSteamId == hostId) tags += " <color=red>[HOST]</color>";

                if (targetSteamId == myId)
                {
                    name = "ME (Anti-Knockback)";
                }
                else
                {
                    try
                    {
                        name = SteamFriends.GetFriendPersonaName(new CSteamID(targetSteamId));
                        name = Utility.StripColorTags(name);
                        if (name.Length > 20) name = name.Substring(0, 20) + "..";
                    }
                    catch { name = targetSteamId.ToString(); }
                }

                Utility.ForceMessage($"Orbit: {name}{tags}");
            }
            catch { }
        }

        public static string GetTargetName()
        {
            if (SteamManager.Instance != null && targetSteamId != 0 && targetSteamId == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID)
                return "<color=cyan>ME (Self)</color>";

            if (targetSteamId == 0) return autoCycle ? "Scanning..." : "None";

            try
            {
                if (SteamManager.Instance != null) return SteamFriends.GetFriendPersonaName(new CSteamID(targetSteamId));
                else return targetSteamId.ToString();
            }
            catch { return targetSteamId.ToString(); }
        }
    }

    public static class PlayerLauncherManager
    {
        public static bool enabled = false;
        public static float launchPower = 1200f;
        public static int bumpCount = 8;
        private static int _currentTargetIndex = 0;

        public static void Update()
        {
            if (!enabled) return;
            if (GameManager.Instance == null || SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            var players = GameManager.Instance.activePlayers;
            if (!players.ContainsKey(myId)) return;

            var me = players[myId];
            if (me == null || me.dead) return;

            Rigidbody rb = me.GetComponent<Rigidbody>();
            if (rb == null) return;

            List<MonoBehaviourPublicCSstReshTrheObplBojuUnique> targets = new List<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
            foreach (var kvp in players)
            {
                if (kvp.Key != myId && kvp.Value != null && !kvp.Value.dead) targets.Add(kvp.Value);
            }
            if (targets.Count == 0) return;

            rb.isKinematic = true;
            rb.useGravity = false;

            if (_currentTargetIndex >= targets.Count) _currentTargetIndex = 0;
            var target = targets[_currentTargetIndex];
            _currentTargetIndex++;

            Vector3 targetPos = target.transform.position;

            for (int i = 0; i < bumpCount; i++)
            {
                Vector3 offset = UnityEngine.Random.onUnitSphere * 0.05f;
                Vector3 glitchPos = targetPos + offset + Vector3.down * 0.1f;
                me.transform.position = glitchPos;

                ClientSendClass.PlayerPosition(glitchPos, myId);

                Vector3 pushDir = Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f;
                MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(target.steamProfile.m_SteamID, 0, pushDir * 80f, 4, 0);
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public static void Disable()
        {
            enabled = false;
            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me != null)
            {
                var rb = me.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }
    }

    public class FunnyScaleModule : MonoBehaviour
    {
        public static FunnyScaleModule Instance;
        private const string MSG_SCALE = "BOT:FunnyScale";

        public static bool _isLocalActive = false;
        public static int _resetFrameCount = 0;
        public static float LocalBaseScale = 0.4f;
        public static float LocalIntensity = 0.15f;
        public static float LocalSpeed = 25f;

        private Transform _localModel;
        private MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _localPlayerScript;

        private struct RemoteConfig { public float baseScale; public float intensity; public float speed; }
        private static Dictionary<ulong, RemoteConfig> _activeRemotePlayers = new Dictionary<ulong, RemoteConfig>();
        private Dictionary<Transform, float> _scaledItems = new Dictionary<Transform, float>();
        private float _itemScanTimer = 0f;
        private float _heartbeatTimer = 0f;
        private float _lastSyncTime = 0f;

        public FunnyScaleModule(IntPtr ptr) : base(ptr) { }
        void Awake() { Instance = this; }

        public static void InitNetwork()
        {
            if (BOTPlugin.HasModNet) ModNet.RegisterMessageHandler(MSG_SCALE, OnPacketReceived);
            SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if ((object)Instance == null || IL2CPP.Il2CppObjectBaseToPtr(Instance) == IntPtr.Zero) return;
                Instance._localModel = null;
                Instance._localPlayerScript = null;
                Instance._scaledItems.Clear();
            }
            catch { }
        }

        public void Toggle()
        {
            _isLocalActive = !_isLocalActive;
            if (_isLocalActive)
            {
                Utility.ForceMessage($"Funny Scale: <color=green>ON</color>");
                _resetFrameCount = 0;
                SendNetworkData(true);
            }
            else
            {
                Utility.ForceMessage("Funny Scale: <color=red>OFF</color>");
                _resetFrameCount = 20;
                SendNetworkData(false);
            }
        }

        public void SyncParameters()
        {
            if (_isLocalActive && Time.time - _lastSyncTime > 0.2f)
            {
                SendNetworkData(true);
                _lastSyncTime = Time.time;
            }
        }

        private void SendNetworkData(bool active)
        {
            if (BOTPlugin.HasModNet)
            {
                Packet p = new Packet();
                p.Write(active);
                if (active)
                {
                    p.Write(LocalBaseScale);
                    p.Write(LocalIntensity);
                    p.Write(LocalSpeed);
                }
                ModNet.SendMessage(MSG_SCALE, p, Utility.GetAllLobbyMembers());
            }
        }

        void Update()
        {
            if (GameManager.Instance == null) return;
            float dt = Time.deltaTime;

            if (_isLocalActive)
            {
                _heartbeatTimer += dt;
                if (_heartbeatTimer > 4f)
                {
                    _heartbeatTimer = 0f;
                    SendNetworkData(true);
                }
            }

            _itemScanTimer += dt;
            if (_itemScanTimer > 0.25f)
            {
                _itemScanTimer = 0f;
                ScanForDroppedItems();
            }
        }

        void LateUpdate()
        {
            float time = Time.time;

            if (_isLocalActive)
            {
                UpdatePlayerFull(GetLocalPlayerScript(), true, time, LocalBaseScale, LocalIntensity, LocalSpeed);
                GameObject tpDummy = GameObject.Find("BOT_ThirdPerson_Dummy");
                if (tpDummy != null) UpdatePlayerFull(tpDummy.transform, false, time, LocalBaseScale, LocalIntensity, LocalSpeed);
            }
            else if (_resetFrameCount > 0)
            {
                ResetLocalModel();
                GameObject tpDummy = GameObject.Find("BOT_ThirdPerson_Dummy");
                if (tpDummy != null) ResetFull(tpDummy.transform);
                _resetFrameCount--;
            }

            if (_activeRemotePlayers.Count > 0 && GameManager.Instance != null && GameManager.Instance.activePlayers != null)
            {
                var keys = _activeRemotePlayers.Keys.ToList();
                foreach (var id in keys)
                {
                    if (GameManager.Instance.activePlayers.ContainsKey(id))
                    {
                        var playerScript = GameManager.Instance.activePlayers[id];
                        if (playerScript != null)
                        {
                            RemoteConfig cfg = _activeRemotePlayers[id];
                            UpdatePlayerFull(playerScript, false, time, cfg.baseScale, cfg.intensity, cfg.speed);
                        }
                    }
                }
            }

            if (!_isLocalActive && _scaledItems.Count > 0)
            {
                foreach (var kvp in _scaledItems)
                {
                    Transform itemTrans = kvp.Key;
                    if (itemTrans == null || IL2CPP.Il2CppObjectBaseToPtr(itemTrans) == IntPtr.Zero) continue;
                    itemTrans.localScale = Vector3.one;
                }
                _scaledItems.Clear();
            }

            if (_scaledItems.Count > 0)
            {
                List<Transform> toRemove = new List<Transform>();
                foreach (var kvp in _scaledItems)
                {
                    Transform itemTrans = kvp.Key;
                    if (itemTrans == null || IL2CPP.Il2CppObjectBaseToPtr(itemTrans) == IntPtr.Zero)
                    {
                        toRemove.Add(itemTrans);
                        continue;
                    }
                    itemTrans.localScale = Vector3.one * kvp.Value;
                }
                foreach (var del in toRemove) _scaledItems.Remove(del);
            }
        }

        private MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique GetLocalPlayerScript()
        {
            if (_localPlayerScript == null)
            {
                _localPlayerScript = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                _localModel = null;
            }
            return _localPlayerScript;
        }

        private void ScanForDroppedItems()
        {
            var allRbs = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            Vector3 localPos = Vector3.zero;
            bool localValid = _isLocalActive && GetLocalPlayerScript() != null;
            if (localValid) localPos = _localPlayerScript.transform.position;

            foreach (var rb in allRbs)
            {
                if (rb == null) continue;
                GameObject go = rb.gameObject;
                if (!go.name.Contains("Clone") && !go.name.Contains("Pickup") && !go.name.Contains("Item")) continue;
                if (go.name.Contains("Player")) continue;

                Transform t = go.transform;
                if (_scaledItems.ContainsKey(t)) continue;

                if (localValid && Vector3.Distance(t.position, localPos) < 3.0f)
                {
                    _scaledItems.Add(t, LocalBaseScale);
                    continue;
                }

                foreach (var id in _activeRemotePlayers.Keys)
                {
                    if (GameManager.Instance.activePlayers.ContainsKey(id))
                    {
                        var player = GameManager.Instance.activePlayers[id];
                        if (player != null && Vector3.Distance(t.position, player.transform.position) < 3.0f)
                        {
                            _scaledItems.Add(t, _activeRemotePlayers[id].baseScale);
                            break;
                        }
                    }
                }
            }
        }

        private void UpdatePlayerFull(Component playerComponent, bool isLocal, float time, float baseScale, float intensity, float speed)
        {
            if (playerComponent == null) return;
            Transform root = playerComponent.transform;
            Transform model = isLocal ? (_localModel ?? (_localModel = root.Find("PlayerModel"))) : root.Find("PlayerModel");
            if (model == null) model = root;

            ApplyScale(model, time, baseScale, intensity, speed);
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                string name = r.gameObject.name;
                if (name.StartsWith("SantaHat") || name.StartsWith("RemoteAura") || name.StartsWith("JumpRing") || name.StartsWith("Moonwalk")) continue;
                ApplyScale(r.transform, time, baseScale, intensity, speed);
            }
        }

        private void ApplyScale(Transform target, float time, float baseScale, float intensity, float speed)
        {
            if (target == null) return;
            float scaleY = Mathf.Max(0.1f, baseScale + Mathf.Sin(time * speed) * intensity);
            float scaleXZ = Mathf.Max(0.1f, baseScale - Mathf.Sin(time * speed) * (intensity * 0.5f));
            target.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
        }

        private void ResetLocalModel()
        {
            _localPlayerScript = null;
            var script = GetLocalPlayerScript();
            if (script != null) ResetFull(script.transform);
        }

        private void ResetFull(Transform root)
        {
            if (root == null) return;
            Transform model = root.Find("PlayerModel");
            if (model != null) model.localScale = Vector3.one;
            else root.localScale = Vector3.one;

            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                string name = r.gameObject.name;
                if (name.StartsWith("SantaHat") || name.StartsWith("RemoteAura") || name.StartsWith("JumpRing") || name.StartsWith("Moonwalk")) continue;
                r.transform.localScale = Vector3.one;
            }
        }

        private static void OnPacketReceived(ulong senderId, Packet p)
        {
            if (GameManager.Instance == null) return;
            try
            {
                bool enable = p.UnreadLength() > 0 && p.ReadBool();
                if (enable)
                {
                    _activeRemotePlayers[senderId] = new RemoteConfig { baseScale = p.ReadFloat(), intensity = p.ReadFloat(), speed = p.ReadFloat() };
                }
                else
                {
                    _activeRemotePlayers.Remove(senderId);
                    if (Instance != null) Instance.ResetFull(GameManager.Instance.activePlayers.ContainsKey(senderId) ? GameManager.Instance.activePlayers[senderId].transform : null);
                }
            }
            catch { }
        }
    }
}
