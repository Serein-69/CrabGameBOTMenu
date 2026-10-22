using System;
using UnityEngine;

namespace BOTMenuMod
{
    public static class TeleportManager
    {
        public static bool enabled = true;
        public static bool ignoreWalls = false;
        public static KeyCode triggerKey = KeyCode.Mouse2;

        public static void Update()
        {
            if (!enabled) return;
            if (Input.GetKeyDown(triggerKey)) DoTeleport();
        }

        private static void DoTeleport()
        {
            var cam = Camera.main;
            var player = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (cam == null || player == null || player.dead) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPos = Vector3.zero;
            bool hitFound = false;

            if (ignoreWalls)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
                float maxDist = -1f;
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.root == player.transform.root) continue;
                    if (hit.distance > maxDist)
                    {
                        maxDist = hit.distance;
                        targetPos = hit.point;
                        hitFound = true;
                    }
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    if (hit.collider.transform.root != player.transform.root)
                    {
                        targetPos = hit.point;
                        hitFound = true;
                    }
                }
            }

            if (hitFound)
            {
                Vector3 finalPos = targetPos + Vector3.up * 1.5f;
                Ray downRay = new Ray(targetPos + Vector3.up * 3f, Vector3.down);
                if (Physics.Raycast(downRay, out RaycastHit groundHit, 50f) && groundHit.collider != null &&
                    groundHit.collider.transform.root != player.transform.root)
                {
                    finalPos = groundHit.point + Vector3.up * 0.5f;
                }

                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = Vector3.zero;
                player.transform.position = finalPos;
                Utility.ForceMessage(ignoreWalls ? "TP: Through Wall" : "TP: Normal");
            }
        }
    }

    public static class FreecamManager
    {
        public static bool enabled = false;
        public static Vector3 frozenPosition;
        private static GameObject _dummyObject;
        public static Vector3 frozenRotation;

        public static void Toggle()
        {
            enabled = !enabled;
            var player = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            if (player == null) return;

            if (enabled)
            {
                Utility.ForceMessage("Freecam: ON (Dummy Spawned)");
                SpawnDummy(player.transform.position, player.transform.rotation);
            }
            else
            {
                DestroyDummy();
                Utility.ForceMessage("Freecam: OFF (Teleported)");
            }
        }

        private static void SpawnDummy(Vector3 pos, Quaternion rot)
        {
            if (_dummyObject != null) DestroyDummy();

            try
            {
                _dummyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _dummyObject.name = "Freecam_Dummy";
                _dummyObject.transform.position = pos;
                _dummyObject.transform.rotation = rot;
                _dummyObject.transform.localScale = new Vector3(1f, 2f, 1f);

                GameObject.Destroy(_dummyObject.GetComponent<Collider>());

                var renderer = _dummyObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = new Color(0f, 1f, 1f, 0.5f);
                    renderer.material.SetFloat("_Mode", 3);
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    renderer.material.SetInt("_ZWrite", 0);
                    renderer.material.DisableKeyword("_ALPHATEST_ON");
                    renderer.material.EnableKeyword("_ALPHABLEND_ON");
                    renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    renderer.material.renderQueue = 3000;
                }
            }
            catch { }
        }

        public static void DestroyDummy()
        {
            if (_dummyObject != null)
            {
                GameObject.Destroy(_dummyObject);
                _dummyObject = null;
            }
        }
    }

    public static class BacktrackingManager
    {
        public static bool enabled = false;
        public static float interval = 0.4f;
        public static float lastSendTime = 0f;
        public static Vector3 lastSentPos = Vector3.zero;
        private static GameObject _ghostObj;

        public static void Toggle()
        {
            enabled = !enabled;
            if (enabled)
            {
                lastSendTime = Time.time;
                var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                if (me != null) lastSentPos = me.transform.position;
                Utility.ForceMessage($"Backtrack: <color=green>ON</color> (Delay: {interval}s)");
            }
            else
            {
                if (_ghostObj != null) UnityEngine.Object.Destroy(_ghostObj);
                Utility.ForceMessage("Backtrack: <color=red>OFF</color>");
            }
        }

        public static void Update()
        {
            if (!enabled) return;

            if (_ghostObj == null)
            {
                _ghostObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _ghostObj.name = "FakeLag_Ghost";
                UnityEngine.Object.Destroy(_ghostObj.GetComponent<Collider>());
                _ghostObj.transform.localScale = new Vector3(1f, 1.8f, 1f);

                var rend = _ghostObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Standard"));
                    rend.material.color = new Color(0.6f, 0f, 1f, 0.4f);
                    rend.material.SetFloat("_Mode", 3);
                    rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    rend.material.SetInt("_ZWrite", 0);
                    rend.material.EnableKeyword("_ALPHABLEND_ON");
                    rend.material.renderQueue = 3000;
                }
            }

            if (_ghostObj != null)
            {
                _ghostObj.transform.position = lastSentPos;
            }
        }
    }
}
