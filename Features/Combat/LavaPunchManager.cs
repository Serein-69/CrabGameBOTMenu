using System;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class LavaPunchManager
    {
        public static bool enableLavaLegit = false;
        public static bool enableKillAura = false;
        public static bool antiHost = true;
        public static int attackMode = 0;
        public static float attackRange = 4.0f;
        public static int punchMultiplier = 1;
        public static int loopBoost = 5;
        public static float attackCooldown = 0f;
        public static bool lockOnTarget = false;

        private static float _lastAuraTime = 0f;
        public static float auraSendDelay = 0.1f;

        public static void Update()
        {
            if (!enableLavaLegit && !enableKillAura) return;
            if (Time.unscaledTime - _lastAuraTime < auraSendDelay) return;
            _lastAuraTime = Time.unscaledTime;

            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me == null || me.dead) return;

            bool inLava = UnityEngine.Object.FindObjectOfType<GameModeFloorIsLava>() != null;
            if (!inLava && !enableKillAura) return;

            int weaponId = -1;
            int itemId = -1;
            if (!GetCurrentWeaponIds(ref weaponId, ref itemId)) return;

            Vector3 myRealPos = me.transform.position;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            var allPlayers = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
            if (allPlayers.Length == 0) return;

            bool hasAttacked = false;

            if (attackMode == 2)
            {
                foreach (var p in allPlayers)
                {
                    if (IsTargetValid(p, myRealPos, myId))
                    {
                        FastExecuteKill(me, p, myRealPos, weaponId, itemId);
                        hasAttacked = true;
                    }
                }
            }
            else if (attackMode == 1)
            {
                ulong targetUid = JoinGameManager.SelectedPlayerUid;
                foreach (var p in allPlayers)
                {
                    if (GetPlayerSteamID(p) == targetUid && IsTargetValid(p, myRealPos, myId))
                    {
                        FastExecuteKill(me, p, myRealPos, weaponId, itemId);
                        hasAttacked = true;
                        break;
                    }
                }
            }
            else
            {
                MonoBehaviourPublicCSstReshTrheObplBojuUnique bestTarget = null;
                float minDist = attackRange;
                foreach (var p in allPlayers)
                {
                    if (IsTargetValid(p, myRealPos, myId))
                    {
                        float d = Vector3.Distance(myRealPos, p.transform.position);
                        if (d < minDist) { minDist = d; bestTarget = p; }
                    }
                }
                if (bestTarget != null)
                {
                    FastExecuteKill(me, bestTarget, myRealPos, weaponId, itemId);
                    hasAttacked = true;
                }
            }

            if (!hasAttacked && enableKillAura && attackMode == 2)
            {
                enableKillAura = false;
            }
        }

        private static void FastExecuteKill(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique me, MonoBehaviourPublicCSstReshTrheObplBojuUnique target, Vector3 originalPos, int weaponId, int itemId)
        {
            Vector3 targetPos = target.transform.position;
            bool needTeleport = Vector3.Distance(originalPos, targetPos) > 5f;

            if (needTeleport || lockOnTarget)
            {
                me.transform.position = targetPos - (Camera.main.transform.forward * 0.6f);
            }

            ulong targetId = GetPlayerSteamID(target);
            int actualHits = punchMultiplier * loopBoost;
            for (int i = 0; i < actualHits; i++)
            {
                MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(targetId, 0, Vector3.zero, weaponId, itemId);
            }

            if (!lockOnTarget && needTeleport)
            {
                me.transform.position = originalPos;
            }
        }

        private static bool IsTargetValid(MonoBehaviourPublicCSstReshTrheObplBojuUnique p, Vector3 myPos, ulong myId)
        {
            if (p == null || p.dead) return false;
            ulong pid = GetPlayerSteamID(p);
            if (pid == 0 || pid == myId) return false;

            if (antiHost && SteamManager.Instance != null)
            {
                ulong hostId = SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID;
                if (pid == hostId) return false;
            }

            if (attackMode == 2) return true;
            return Vector3.Distance(myPos, p.transform.position) <= attackRange;
        }

        private static ulong GetPlayerSteamID(MonoBehaviourPublicCSstReshTrheObplBojuUnique player)
        {
            try { return player.steamProfile.m_SteamID; } catch { return 0; }
        }

        private static bool GetCurrentWeaponIds(ref int weaponId, ref int itemId)
        {
            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inv == null || inv.currentItem == null) return false;

            GameObject currentItem = inv.currentItem;
            string name = currentItem.name.Replace("(Clone)", "").Trim();
            weaponId = GetIdFromName(name);

            if (weaponId == 1 || weaponId == 2)
            {
                var component = currentItem.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
                if (component == null) return false;
                itemId = component.id;
            }
            else if (weaponId == 4 || weaponId == 5 || weaponId == 6 || weaponId == 7 || weaponId == 8 || weaponId == 10)
            {
                var component2 = currentItem.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();
                if (component2 == null) return false;
                itemId = component2.id;
            }
            else if (weaponId == 9)
            {
                var component3 = currentItem.GetComponent<MonoBehaviour2PublicGathObauTrgumuGaSiBoUnique>();
                if (component3 == null) return false;
                itemId = component3.id;
            }
            else itemId = 0;
            return true;
        }

        private static int GetIdFromName(string name)
        {
            if (name.Contains("Pistol")) return 1;
            if (name.Contains("Revolver") || name.Contains("AK") || name.Contains("Shotgun")) return 2;
            if (name.Contains("Bat")) return 4;
            if (name.Contains("Bomb")) return 5;
            if (name.Contains("Pipe")) return 6;
            if (name.Contains("Knife")) return 7;
            if (name.Contains("Katana")) return 8;
            if (name.Contains("Snowball")) return 9;
            if (name.Contains("Stick")) return 10;
            return -1;
        }
    }
}
