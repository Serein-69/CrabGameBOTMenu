using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTMenuMod
{
    public static class PlayerNoClipManager
    {
        public static bool enabled = false;
        public static bool wallsEnabled = false;
        private static bool _lastWallsState = false;
        private static float _timer = 0f;
        private static readonly List<Collider[]> _ignoredPairs = new List<Collider[]>();
        private static readonly HashSet<long> _ignoredPairKeys = new HashSet<long>();

        public static void Update()
        {
            if (!enabled && _ignoredPairs.Count > 0) ClearPlayerNoClip();

            var localPlayer = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (localPlayer == null || localPlayer.dead) return;

            if (wallsEnabled != _lastWallsState)
            {
                ToggleWorldNoClip(localPlayer, wallsEnabled);
                _lastWallsState = wallsEnabled;
            }

            if (wallsEnabled)
            {
                HandleNoClipMovement(localPlayer);
            }

            if (enabled)
            {
                _timer += Time.deltaTime;
                if (_timer >= 0.5f)
                {
                    _timer = 0f;
                    ApplyPlayerNoClip(localPlayer);
                }
            }
        }

        private static void ToggleWorldNoClip(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, bool state)
        {
            var rb = player.GetComponent<Rigidbody>();
            var colliders = player.GetComponentsInChildren<Collider>();

            if (rb != null)
            {
                rb.useGravity = !state;
                if (state) rb.velocity = Vector3.zero;
            }

            foreach (var col in colliders) col.isTrigger = state;
            Utility.ForceMessage(state ? "Wall NoClip: ON (Gravity Disabled)" : "Wall NoClip: OFF");
        }

        private static void HandleNoClipMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            if (Input.GetKey(KeyCode.Space)) player.transform.position += Vector3.up * 10f * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftControl)) player.transform.position += Vector3.down * 10f * Time.deltaTime;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 vel = rb.velocity;
                vel.y = 0;
                rb.velocity = vel;
            }
        }

        private static void ApplyPlayerNoClip(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique local)
        {
            var localCols = local.GetComponentsInChildren<Collider>();
            var remotes = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
            foreach (var r in remotes)
            {
                if (r == null) continue;
                var remoteCols = r.GetComponentsInChildren<Collider>();
                foreach (var lc in localCols)
                {
                    if (lc == null) continue;
                    foreach (var rc in remoteCols)
                    {
                        if (rc == null) continue;
                        long pairKey = ((long)lc.GetInstanceID() << 32) ^ (uint)rc.GetInstanceID();
                        if (!_ignoredPairKeys.Add(pairKey)) continue;
                        try
                        {
                            Physics.IgnoreCollision(lc, rc, true);
                            _ignoredPairs.Add(new Collider[] { lc, rc });
                        }
                        catch { }
                    }
                }
            }
        }

        private static void ClearPlayerNoClip()
        {
            foreach (var pair in _ignoredPairs)
            {
                if (pair == null || pair.Length < 2) continue;
                if (pair[0] == null || pair[1] == null) continue;
                try { Physics.IgnoreCollision(pair[0], pair[1], false); } catch { }
            }
            _ignoredPairs.Clear();
            _ignoredPairKeys.Clear();
        }
    }

    public static class WaterWalkManager
    {
        public static bool enabled = false;
        public static float floorOffset = 0.1f;
        private const int SCAN_INTERVAL_FRAMES = 15;
        private static int _frameCounter = 0;
        private static Dictionary<GameObject, GameObject> _liquidFloors = new Dictionary<GameObject, GameObject>();
        private static readonly List<Behaviour> _disabledHazards = new List<Behaviour>();
        private static readonly List<Collider> _disabledHazardColliders = new List<Collider>();

        public static void Update()
        {
            if (!enabled) return;

            _frameCounter++;
            if (_frameCounter >= SCAN_INTERVAL_FRAMES)
            {
                _frameCounter = 0;
                ScanAndBuildFloors();
            }

            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kv in _liquidFloors)
            {
                if (kv.Key == null || kv.Value == null) toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove) _liquidFloors.Remove(key);
        }

        private static void ScanAndBuildFloors()
        {
            var allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjs)
            {
                if (!obj.activeInHierarchy) continue;
                string n = obj.name;
                if (n.Contains("Lava") || n.Contains("WaterTop") || n.Contains("Acid"))
                {
                    if (_liquidFloors.ContainsKey(obj) && _liquidFloors[obj] != null) continue;

                    DisableLiquidHazards(obj);
                    GameObject floor = CreateSafetyFloor(obj);
                    if (floor != null) _liquidFloors[obj] = floor;
                }
            }
        }

        private static void DisableLiquidHazards(GameObject liquid)
        {
            var lavaScript = liquid.GetComponent<MonoBehaviourPublicGasfUnique>();
            if (lavaScript != null && lavaScript.enabled)
            {
                lavaScript.enabled = false;
                _disabledHazards.Add(lavaScript);
            }
            var killScript = liquid.GetComponent<MonoBehaviourPublicSikiUnique>();
            if (killScript != null && killScript.enabled)
            {
                killScript.enabled = false;
                _disabledHazards.Add(killScript);
            }
            var col = liquid.GetComponent<Collider>();
            if (col != null && col.enabled)
            {
                col.enabled = false;
                _disabledHazardColliders.Add(col);
            }
        }

        private static GameObject CreateSafetyFloor(GameObject liquid)
        {
            var rend = liquid.GetComponent<Renderer>();
            if (rend == null) return null;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "WaterWalk_Floor";
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = LayerMask.NameToLayer("Default");
            floor.layer = groundLayer;

            float width = rend.bounds.size.x * 5f;
            float length = rend.bounds.size.z * 5f;
            float height = 1f;
            floor.transform.localScale = new Vector3(width, height, length);

            float targetTopY = rend.bounds.max.y + floorOffset;
            float centerY = targetTopY - (height / 2f);
            floor.transform.position = new Vector3(rend.bounds.center.x, centerY, rend.bounds.center.z);

            var floorRend = floor.GetComponent<Renderer>();
            if (floorRend != null) floorRend.enabled = false;

            floor.transform.SetParent(liquid.transform, true);
            var box = floor.GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = false;

            return floor;
        }

        public static void Cleanup()
        {
            foreach (var kv in _liquidFloors)
            {
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            }
            _liquidFloors.Clear();

            foreach (var hazard in _disabledHazards)
            {
                if (hazard != null) hazard.enabled = true;
            }
            _disabledHazards.Clear();

            foreach (var hazardCol in _disabledHazardColliders)
            {
                if (hazardCol != null) hazardCol.enabled = true;
            }
            _disabledHazardColliders.Clear();
        }
    }

    public static class WaterJumpManager
    {
        public static bool enabled = false;
        public static float waterLevel = -3.5f;
        public static float bouncePower = 25f;
        private static float _lastBounceTime = 0f;
        private static float _cooldown = 0.1f;

        public static void Update()
        {
            if (!enabled) return;
            var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (player == null || player.dead) return;

            if (player.transform.position.y <= waterLevel)
            {
                if (Time.time - _lastBounceTime > _cooldown)
                {
                    _lastBounceTime = Time.time;
                    Bounce(player);
                }
            }
        }

        private static void Bounce(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 vel = rb.velocity;
                vel.y = bouncePower;
                rb.velocity = vel;
            }
        }

        public static void SetLevelToCurrent()
        {
            var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (player != null)
            {
                waterLevel = player.transform.position.y - 0.5f;
                Utility.ForceMessage($"Water Jump Level: {waterLevel:F1}");
            }
        }
    }
}
