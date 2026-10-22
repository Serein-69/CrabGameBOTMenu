using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using UnhollowerRuntimeLib;
using BepInEx.IL2CPP.Utils.Collections;

namespace BOTMenuMod
{
    public static class VisualsManager
    {
        public static bool enableTrail = false;
        public static bool enableJumpRing = false;
        public static bool enableWalkRing = false;
        private static Material _cachedRingMat;
        public static Color TrailColor = Color.cyan;
        public static Color JumpRingColor = Color.black;
        public static Color WalkRingColor = Color.white;

        private static TrailRenderer _currentTrail;
        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _localPlayer;
        private static Texture2D _jumpRingTexture;
        private static Color _lastJumpRingColor;
        private static Texture2D _walkRingTexture;
        private static Color _lastWalkRingColor;
        private static Vector3 _lastMovementPosition;
        private static bool _hasMovementSample;
        private static float _walkRingTimer;
        private const float WalkRingInterval = 0.28f;

        public static void Update()
        {
            if (_localPlayer == null)
            {
                _localPlayer = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                return;
            }
            HandleTrail();
            HandleWalkRing();
            AuraManager.Update();
        }

        public static void SpawnJumpEffect(Vector3 position)
        {
            if (!enableJumpRing || _localPlayer == null) return;

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "JumpRing";
            ring.transform.position = position + new Vector3(0, 0.05f, 0);
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            ring.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            var renderer = ring.GetComponent<Renderer>();
            if (_cachedRingMat == null)
            {
                _cachedRingMat = new Material(Shader.Find("Sprites/Default"));
                _cachedRingMat.enableInstancing = true;
            }

            renderer.material = new Material(_cachedRingMat);
            renderer.material.mainTexture = GetJumpRingTexture();
            renderer.material.color = Color.white;

            _localPlayer.StartCoroutine(AnimateRing(ring, renderer.material).WrapToIl2Cpp());
        }

        private static IEnumerator AnimateRing(GameObject ring, Material mat)
        {
            float timer = 0f;
            float duration = 2f;
            Vector3 startScale = ring.transform.localScale;
            Vector3 endScale = new Vector3(12f, 12f, 1f);

            while (timer < duration)
            {
                if (ring == null)
                {
                    if (mat != null) UnityEngine.Object.Destroy(mat);
                    yield break;
                }
                float t = timer / duration;
                ring.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                mat.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t));
                timer += Time.deltaTime;
                yield return null;
            }

            if (ring != null) UnityEngine.Object.Destroy(ring);
            if (mat != null) UnityEngine.Object.Destroy(mat);
        }

        private static Texture2D GetJumpRingTexture()
        {
            if (_jumpRingTexture == null || _lastJumpRingColor != JumpRingColor)
            {
                if (_jumpRingTexture != null) UnityEngine.Object.Destroy(_jumpRingTexture);
                _jumpRingTexture = CreateSoftRingTexture(512, JumpRingColor, 0.2f);
                _lastJumpRingColor = JumpRingColor;
            }
            return _jumpRingTexture;
        }

        private static Texture2D CreateSoftRingTexture(int size, Color color, float thickness)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;

            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;
            float midRadius = radius * (1 - thickness / 2f);
            float halfWidth = radius * thickness / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float distToMid = Mathf.Abs(dist - midRadius);
                    float alpha = 0f;

                    if (distToMid < halfWidth)
                    {
                        alpha = 1f - (distToMid / halfWidth);
                        alpha = Mathf.SmoothStep(0f, 1f, alpha);
                        alpha = alpha * alpha;
                    }
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private static void HandleWalkRing()
        {
            if (_localPlayer == null) return;

            Vector3 currentPosition = _localPlayer.transform.position;
            if (!_hasMovementSample)
            {
                _lastMovementPosition = currentPosition;
                _hasMovementSample = true;
                return;
            }

            Vector3 horizontalDelta = currentPosition - _lastMovementPosition;
            horizontalDelta.y = 0f;
            _lastMovementPosition = currentPosition;

            if (!enableWalkRing)
            {
                _walkRingTimer = 0f;
                return;
            }

            _walkRingTimer -= Time.deltaTime;
            if (horizontalDelta.sqrMagnitude > 0.00002f && _walkRingTimer <= 0f)
            {
                _walkRingTimer = WalkRingInterval;
                SpawnWalkRing(currentPosition);
            }
        }

        private static void SpawnWalkRing(Vector3 position)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "WalkRing";
            ring.transform.position = position + new Vector3(0f, 0.05f, 0f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            Renderer renderer = ring.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = GetWalkRingTexture();
            material.color = Color.white;
            renderer.material = material;
            _localPlayer.StartCoroutine(AnimateWalkRing(ring, material).WrapToIl2Cpp());
        }

        private static IEnumerator AnimateWalkRing(GameObject ring, Material material)
        {
            const float duration = 0.85f;
            float timer = 0f;
            Vector3 startScale = ring.transform.localScale;
            Vector3 endScale = new Vector3(2.6f, 2.6f, 1f);

            while (timer < duration)
            {
                if (ring == null) yield break;
                float t = timer / duration;
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                ring.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                material.color = new Color(1f, 1f, 1f, 1f - t);
                timer += Time.deltaTime;
                yield return null;
            }

            if (ring != null) UnityEngine.Object.Destroy(ring);
            if (material != null) UnityEngine.Object.Destroy(material);
        }

        private static Texture2D GetWalkRingTexture()
        {
            if (_walkRingTexture == null || _lastWalkRingColor != WalkRingColor)
            {
                if (_walkRingTexture != null) UnityEngine.Object.Destroy(_walkRingTexture);
                _walkRingTexture = CreateSoftRingTexture(256, WalkRingColor, 0.18f);
                _lastWalkRingColor = WalkRingColor;
            }
            return _walkRingTexture;
        }

        private static void HandleTrail()
        {
            if (_localPlayer == null) return;

            if (_currentTrail == null)
            {
                _currentTrail = _localPlayer.GetComponent<TrailRenderer>();
                if (_currentTrail == null && enableTrail)
                {
                    _currentTrail = _localPlayer.gameObject.AddComponent<TrailRenderer>();
                    _currentTrail.material = new Material(Shader.Find("Sprites/Default"));
                    _currentTrail.time = 0.5f;
                    _currentTrail.startWidth = 0.4f;
                    _currentTrail.endWidth = 0.0f;
                    _currentTrail.autodestruct = false;
                    _currentTrail.minVertexDistance = 0.1f;
                    _currentTrail.numCornerVertices = 6;
                    _currentTrail.numCapVertices = 6;
                    _currentTrail.alignment = LineAlignment.View;
                }
            }

            if (_currentTrail != null)
            {
                if (enableTrail && !_currentTrail.enabled) _currentTrail.enabled = true;
                else if (!enableTrail && _currentTrail.enabled) _currentTrail.enabled = false;

                if (enableTrail)
                {
                    _currentTrail.startColor = TrailColor;
                    _currentTrail.endColor = new Color(TrailColor.r, TrailColor.g, TrailColor.b, 0f);
                }
            }
        }

        public static void SpawnRemoteJumpEffect(Vector3 position, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "RemoteJumpRing";
            ring.transform.position = position + new Vector3(0, 0.05f, 0);
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            ring.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            var renderer = ring.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.mainTexture = CreateSoftRingTexture(256, color, 0.2f);
            renderer.material.color = Color.white;

            if (_localPlayer != null)
            {
                _localPlayer.StartCoroutine(AnimateRing(ring, renderer.material).WrapToIl2Cpp());
            }
            else
            {
                UnityEngine.Object.Destroy(ring);
            }
        }
    }

    [HarmonyPatch(typeof(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique), "Jump")]
    public static class JumpVisualPatch
    {
        private static float _lastJumpTime = 0f;
        private const float COOLDOWN = 0.25f;

        [HarmonyPostfix]
        public static void PostJump(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique __instance)
        {
            if (Time.time - _lastJumpTime < COOLDOWN) return;
            _lastJumpTime = Time.time;
            VisualsManager.SpawnJumpEffect(__instance.transform.position);
            NetworkVisualsManager.SendJump(__instance.transform.position);
        }
    }

    public static class AuraManager
    {
        public static bool enabled = false;
        public static bool enableDrag = true;
        public static Color AuraColor = Color.cyan;

        private static int particleCount = 70;
        private static float swarmRadius = 2.2f;
        private static Vector3 _currentCenter;
        private static bool _isFirstFrame = true;

        private static List<GameObject> _particles = new List<GameObject>();
        private static List<Vector4> _particleProperties = new List<Vector4>();
        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _localPlayer;

        public static void Update()
        {
            if (_localPlayer == null)
            {
                _localPlayer = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                if (_localPlayer != null) _currentCenter = _localPlayer.transform.position;
            }

            if (!enabled) { ClearParticles(); return; }

            if (_particles.Count > 0 && _particles[0] == null)
            {
                _particles.Clear();
                _particleProperties.Clear();
                _isFirstFrame = true;
            }

            if (_particles.Count != particleCount && _localPlayer != null) SpawnParticles();
            if (_localPlayer != null) AnimateParticles();
        }

        private static void SpawnParticles()
        {
            ClearParticles();
            _isFirstFrame = true;

            for (int i = 0; i < particleCount; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(p.GetComponent<Collider>());

                float size = UnityEngine.Random.Range(0.01f, 0.030f);
                float width = size * UnityEngine.Random.Range(0.5f, 1.5f);
                float height = size * UnityEngine.Random.Range(0.5f, 1.5f);

                p.transform.localScale = new Vector3(width, height, 0.0002f);

                var renderer = p.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = AuraColor;

                _particles.Add(p);
                _particleProperties.Add(new Vector4(
                    UnityEngine.Random.Range(0f, 100f),
                    UnityEngine.Random.Range(0f, 100f),
                    UnityEngine.Random.Range(0f, 100f),
                    UnityEngine.Random.Range(150f, 400f)
                ));
            }
        }

        private static void AnimateParticles()
        {
            Vector3 targetPos = _localPlayer.transform.position + Vector3.up * 1.0f;

            if (_isFirstFrame)
            {
                _currentCenter = targetPos;
                _isFirstFrame = false;
            }

            if (enableDrag)
            {
                _currentCenter = Vector3.Lerp(_currentCenter, targetPos, Time.deltaTime * 5f);
            }
            else
            {
                _currentCenter = targetPos;
            }

            float time = Time.time;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                if (p == null) continue;

                p.GetComponent<Renderer>().material.color = AuraColor;
                Vector4 props = _particleProperties[i];

                float x = Mathf.Sin(time * 0.3f + props.x) * swarmRadius;
                float y = Mathf.Sin(time * 0.2f + props.y) * (swarmRadius * 0.6f);
                float z = Mathf.Cos(time * 0.25f + props.z) * swarmRadius;

                float jitterX = Mathf.Sin(time * 8f + i) * 0.03f;
                float jitterY = Mathf.Cos(time * 9f + i) * 0.03f;

                p.transform.position = _currentCenter + new Vector3(x + jitterX, y + jitterY, z);
                float rotateSpeed = props.w * Time.deltaTime;
                p.transform.Rotate(new Vector3(rotateSpeed, rotateSpeed * 0.8f, rotateSpeed * 0.4f));
            }
        }

        private static void ClearParticles()
        {
            if (_particles.Count > 0)
            {
                foreach (var p in _particles)
                {
                    if (p != null) UnityEngine.Object.Destroy(p);
                }
                _particles.Clear();
                _particleProperties.Clear();
            }
        }
    }

    public class RemoteVisualsBehavior : MonoBehaviour
    {
        public TrailRenderer trail;
        public GameObject auraContainer;
        private List<GameObject> auraParticles = new List<GameObject>();
        private List<Vector4> auraProps = new List<Vector4>();
        public bool trailActive = false;
        public bool auraActive = false;
        public Color trailColor = Color.cyan;
        public Color auraColor = Color.cyan;
        private Vector3 lastPos;

        public RemoteVisualsBehavior(IntPtr ptr) : base(ptr) { }
        void Start() { lastPos = transform.position; }

        void Update()
        {
            if (UnhollowerBaseLib.IL2CPP.Il2CppObjectBaseToPtr(this) == IntPtr.Zero ||
                UnhollowerBaseLib.IL2CPP.Il2CppObjectBaseToPtr(this.gameObject) == IntPtr.Zero)
            {
                Cleanup();
                return;
            }

            UpdateTrail();
            UpdateAura();
            lastPos = transform.position;
        }

        void OnDisable() { Cleanup(); }
        void OnDestroy() { Cleanup(); }

        private void Cleanup()
        {
            if (trail != null) { UnityEngine.Object.Destroy(trail); trail = null; }
            if (auraContainer != null) { UnityEngine.Object.Destroy(auraContainer); auraContainer = null; auraParticles.Clear(); auraProps.Clear(); }
        }

        private void UpdateTrail()
        {
            if (trailActive && trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.time = 0.5f;
                trail.startWidth = 0.4f;
                trail.endWidth = 0.0f;
                trail.autodestruct = false;
                trail.minVertexDistance = 0.1f;
                trail.startColor = trailColor;
                trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                trail.Clear();
            }

            if (!trailActive && trail != null)
            {
                trail.enabled = false;
                return;
            }

            if (trail != null && trailActive)
            {
                if (!trail.enabled) trail.enabled = true;
                if (trail.startColor != trailColor)
                {
                    trail.startColor = trailColor;
                    trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                }
                if (Vector3.Distance(transform.position, lastPos) > 10f) trail.Clear();
            }
        }

        private void UpdateAura()
        {
            if (!auraActive)
            {
                if (auraContainer != null) Cleanup();
                return;
            }

            if (auraContainer == null)
            {
                auraContainer = new GameObject("RemoteAura");
                auraContainer.transform.SetParent(transform, false);
                auraContainer.transform.localPosition = Vector3.zero;

                for (int i = 0; i < 35; i++)
                {
                    GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    UnityEngine.Object.Destroy(p.GetComponent<Collider>());
                    p.transform.SetParent(auraContainer.transform, false);

                    float size = UnityEngine.Random.Range(0.02f, 0.05f);
                    p.transform.localScale = new Vector3(size, size, size);

                    var r = p.GetComponent<Renderer>();
                    r.material = new Material(Shader.Find("Sprites/Default"));

                    auraParticles.Add(p);
                    auraProps.Add(new Vector4(
                        UnityEngine.Random.Range(0f, 100f),
                        UnityEngine.Random.Range(0f, 100f),
                        UnityEngine.Random.Range(0f, 100f),
                        UnityEngine.Random.Range(1f, 3f)
                    ));
                }
            }

            float time = Time.time;
            float radius = 2.0f;

            for (int i = 0; i < auraParticles.Count; i++)
            {
                var p = auraParticles[i];
                if (p == null) continue;

                p.GetComponent<Renderer>().material.color = auraColor;
                Vector4 props = auraProps[i];

                float x = Mathf.Sin(time * 0.5f + props.x) * radius;
                float z = Mathf.Cos(time * 0.4f + props.y) * radius;
                float y = Mathf.Sin(time * 1.5f + props.z) * 0.8f + 1.2f;

                p.transform.localPosition = new Vector3(x, y, z);
                p.transform.Rotate(Vector3.up, props.w * 100f * Time.deltaTime);
            }
        }
    }

    public static class NetworkVisualsManager
    {
        public static bool ShareVisuals = false;
        private static float syncTimer = 0f;
        public const string MSG_TRAIL = "BOT:Trail";
        public const string MSG_AURA = "BOT:Aura";
        public const string MSG_JUMP = "BOT:Jump";

        public static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<RemoteVisualsBehavior>();
            if (!BOTPlugin.HasModNet) return;
            ModNet.RegisterMessageHandler(MSG_TRAIL, HandleTrailPacket);
            ModNet.RegisterMessageHandler(MSG_AURA, HandleAuraPacket);
            ModNet.RegisterMessageHandler(MSG_JUMP, HandleJumpPacket);
        }

        public static void Update()
        {
            if (!BOTPlugin.HasModNet || GameManager.Instance == null || SteamManager.Instance == null) return;
            if (ShareVisuals)
            {
                syncTimer += Time.deltaTime;
                if (syncTimer > 3.0f)
                {
                    syncTimer = 0f;
                    SendState(true);
                }
            }
        }

        public static void SendState(bool active = true)
        {
            if (!BOTPlugin.HasModNet) return;

            bool sendTrail = active && VisualsManager.enableTrail && ShareVisuals;
            bool sendAura = active && AuraManager.enabled && ShareVisuals;

            Packet pTrail = new Packet();
            pTrail.Write(sendTrail);
            pTrail.Write(ColorUtility.ToHtmlStringRGB(VisualsManager.TrailColor));
            ModNet.SendMessage(MSG_TRAIL, pTrail, Utility.GetAllLobbyMembers());

            Packet pAura = new Packet();
            pAura.Write(sendAura);
            pAura.Write(ColorUtility.ToHtmlStringRGB(AuraManager.AuraColor));
            ModNet.SendMessage(MSG_AURA, pAura, Utility.GetAllLobbyMembers());
        }

        public static void SendJump(Vector3 pos)
        {
            if (!BOTPlugin.HasModNet || !ShareVisuals || !VisualsManager.enableJumpRing) return;
            Packet p = new Packet();
            p.Write(pos);
            p.Write(ColorUtility.ToHtmlStringRGB(VisualsManager.JumpRingColor));
            ModNet.SendMessage(MSG_JUMP, p, Utility.GetAllLobbyMembers());
        }

        private static RemoteVisualsBehavior GetRemoteBehavior(ulong senderId)
        {
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return null;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (senderId == myId) return null;
            if (!GameManager.Instance.activePlayers.ContainsKey(senderId)) return null;

            var player = GameManager.Instance.activePlayers[senderId];
            if (player == null || player.transform == null) return null;

            var behavior = player.gameObject.GetComponent<RemoteVisualsBehavior>();
            if (behavior == null) behavior = player.gameObject.AddComponent<RemoteVisualsBehavior>();
            return behavior;
        }

        private static void HandleTrailPacket(ulong senderId, Packet p)
        {
            var behavior = GetRemoteBehavior(senderId);
            if (behavior == null) return;
            bool active = p.ReadBool();
            string colorHtml = p.ReadString();
            behavior.trailActive = active;
            if (ColorUtility.TryParseHtmlString("#" + colorHtml, out Color c)) behavior.trailColor = c;
        }

        private static void HandleAuraPacket(ulong senderId, Packet p)
        {
            var behavior = GetRemoteBehavior(senderId);
            if (behavior == null) return;
            bool active = p.ReadBool();
            string colorHtml = p.ReadString();
            behavior.auraActive = active;
            if (ColorUtility.TryParseHtmlString("#" + colorHtml, out Color c)) behavior.auraColor = c;
        }

        private static void HandleJumpPacket(ulong senderId, Packet p)
        {
            Vector3 pos = p.ReadVector3();
            string colorHtml = p.ReadString();
            Color ringColor = Color.white;
            ColorUtility.TryParseHtmlString("#" + colorHtml, out ringColor);
            VisualsManager.SpawnRemoteJumpEffect(pos, ringColor);
        }
    }
}
