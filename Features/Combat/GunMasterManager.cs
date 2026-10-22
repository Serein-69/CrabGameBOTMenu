using System;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class GunMasterManager
    {
        public static bool autoAim = false;
        public static bool autoShoot = false;
        public static bool aimThroughWalls = false;
        public static bool magicBullet = false;
        public static bool globalMagicBullet = false;
        public static bool showFovCircle = true;
        public static float fovRadius = 150f;

        private static float _shootTimer = 0f;

        public static void Update()
        {
            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inv == null || inv.currentItem == null) return;

            var gun = inv.currentItem.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
            if (gun == null) return;

            bool isTryingToShoot = autoShoot || Input.GetMouseButton(0);

            if (globalMagicBullet && isTryingToShoot)
            {
                _shootTimer += Time.deltaTime;
                if (_shootTimer >= 0.15f)
                {
                    _shootTimer = 0f;
                    gun.field_Protected_Boolean_0 = true;

                    Vector3 fakeDir = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                    try { MonoBehaviourPublicInpabyInInInUnique.UseItem(gun.itemData.itemID, fakeDir); } catch { }

                    if (GameManager.Instance != null && GameManager.Instance.activePlayers != null)
                    {
                        ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                        Vector3 myPos = inv.transform.position;

                        foreach (var kvp in GameManager.Instance.activePlayers)
                        {
                            if (kvp.Key == myId) continue;
                            var enemy = kvp.Value;
                            if (enemy == null || enemy.dead) continue;
                            if (!TeamRadarManager.IsEnemy(myPos, enemy.transform.position)) continue;

                            ulong targetId = enemy.steamProfile.m_SteamID;
                            Vector3 dirToEnemy = (enemy.transform.position + Vector3.up * 1.5f - myPos).normalized;

                            try { MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(targetId, 0, dirToEnemy, gun.itemData.itemID, gun.id); } catch { }

                            if (HitEffectManager.enabled) HitEffectManager.SpawnHeart(targetId);
                            if (DamageNotificationManager.enabled)
                            {
                                string targetName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(targetId)));
                                DamageNotificationManager.AddDamageLog(targetName, 15f);
                            }
                        }
                    }
                }
                return;
            }

            var target = GetBestTarget();
            if (target == null) return;

            Vector3 myEyePos = Camera.main != null ? Camera.main.transform.position : inv.transform.position;
            Vector3 targetHead = target.transform.position + Vector3.up * 1.5f;
            Vector3 dirToTarget = (targetHead - myEyePos).normalized;

            if (autoAim) LookAt(targetHead);

            if (isTryingToShoot)
            {
                if (magicBullet)
                {
                    _shootTimer += Time.deltaTime;
                    if (_shootTimer >= 0.15f)
                    {
                        _shootTimer = 0f;
                        gun.field_Protected_Boolean_0 = true;
                        try { MonoBehaviourPublicInpabyInInInUnique.UseItem(gun.itemData.itemID, dirToTarget); } catch { }

                        ulong targetId = target.steamProfile.m_SteamID;
                        try { MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(targetId, 0, dirToTarget, gun.itemData.itemID, gun.id); } catch { }

                        if (HitEffectManager.enabled) HitEffectManager.SpawnHeart(targetId);
                        if (DamageNotificationManager.enabled)
                        {
                            string targetName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(new CSteamID(targetId)));
                            DamageNotificationManager.AddDamageLog(targetName, 15f);
                        }
                    }
                }
            }
        }

        public static void RenderFOV()
        {
            if (showFovCircle && (autoAim || magicBullet) && !globalMagicBullet)
            {
                Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
                FOVCircleDrawer.DrawCircle(center, fovRadius, new Color(0.2f, 0.8f, 1f, 0.5f), 1.5f);
            }
        }

        private static void LookAt(Vector3 targetPos)
        {
            if (Camera.main == null || PlayerInput.Instance == null) return;
            Vector3 dir = targetPos - Camera.main.transform.position;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            Vector3 angles = lookRot.eulerAngles;
            float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
            PlayerInput.Instance.cameraRot = new Vector2(pitch, angles.y);
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetBestTarget()
        {
            if (GameManager.Instance == null || SteamManager.Instance == null) return null;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            var myPlayer = GameManager.Instance.activePlayers.ContainsKey(myId) ? GameManager.Instance.activePlayers[myId] : null;
            if (myPlayer == null) return null;

            Vector3 myPos = myPlayer.transform.position;
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closest = null;
            float minFovDist = fovRadius;
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var p = kvp.Value;
                if (p == null || p.dead) continue;
                if (!TeamRadarManager.IsEnemy(myPos, p.transform.position)) continue;
                if (!magicBullet && !aimThroughWalls && !IsVisible(p)) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(p.transform.position + Vector3.up * 1f);
                if (screenPos.z < 0) continue;

                float distToCenter = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));
                if (distToCenter < minFovDist)
                {
                    minFovDist = distToCenter;
                    closest = p;
                }
            }
            return closest;
        }

        private static bool IsVisible(MonoBehaviourPublicCSstReshTrheObplBojuUnique target)
        {
            Vector3 start = Camera.main.transform.position;
            Vector3 end = target.transform.position + Vector3.up * 1.5f;
            if (Physics.Linecast(start, end, out RaycastHit hit))
                return hit.collider.transform.root == target.transform.root;
            return true;
        }
    }

    public static class FOVCircleDrawer
    {
        public static void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1.5f)
        {
            Texture2D tex = RenderInternal.WhiteTexture;
            GUI.color = color;
            for (float i = 0; i < 360; i += 10)
            {
                float angle1 = i * Mathf.Deg2Rad;
                float angle2 = (i + 10) * Mathf.Deg2Rad;
                Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
                DrawLine(p1, p2, thickness, tex);
            }
            GUI.color = Color.white;
        }

        private static void DrawLine(Vector2 pointA, Vector2 pointB, float width, Texture2D tex)
        {
            Matrix4x4 matrix = GUI.matrix;
            float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, pointA);
            float length = Vector2.Distance(pointA, pointB);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2, length, width), tex);
            GUI.matrix = matrix;
        }
    }
}
