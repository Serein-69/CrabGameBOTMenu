using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class ForceKillManager
    {
        public static bool isActive = false;
        public static bool killAuraEnabled = false;
        public static float auraRange = 5.0f;
        private static float _timer = 0f;
        private static float _auraCooldown = 0f;
        private static Vector3 _returnPos;
        public static float killAuraDelay = 0.05f;

        public static void Execute()
        {
            if (GameManager.Instance == null || SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (myId == 0 || !GameManager.Instance.activePlayers.ContainsKey(myId)) return;

            var players = GameManager.Instance.activePlayers;
            List<ulong> targets = new List<ulong>();
            foreach (var kvp in players)
            {
                if (kvp.Key != myId && !kvp.Value.dead) targets.Add(kvp.Key);
            }
            if (targets.Count == 0) return;

            ulong targetId = targets[UnityEngine.Random.Range(0, targets.Count)];
            var me = players[myId];
            var targetObj = players[targetId];

            _returnPos = me.transform.position;
            me.transform.position = targetObj.transform.position;

            Vector3 dir = (targetObj.transform.position - _returnPos).normalized;
            if (dir == Vector3.zero) dir = Vector3.up;

            DoWeaponLogic(targetId, dir);
            isActive = true;
            _timer = 0.5f;
        }

        public static void Update()
        {
            if (isActive)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    isActive = false;
                    ReturnMe();
                }
            }

            if (killAuraEnabled && !isActive)
            {
                HandleKillAura();
            }
        }

        private static void HandleKillAura()
        {
            _auraCooldown -= Time.deltaTime;
            if (_auraCooldown > 0) return;

            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (Camera.main == null) return;

            Vector3 myPos = Camera.main.transform.position;
            bool isHatMode = LobbyManager.Instance != null && LobbyManager.Instance.gameMode != null && LobbyManager.Instance.gameMode.id == 2;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.gameObject == null || enemy.transform == null) continue;

                float dist = Vector3.Distance(myPos, enemy.transform.position);
                float maxAllowedRange = isHatMode ? 3.5f : auraRange;

                if (dist <= maxAllowedRange)
                {
                    Vector3 dir = (enemy.transform.position - myPos).normalized;
                    if (dir == Vector3.zero) dir = Vector3.forward;

                    int burstCount = isHatMode ? 3 : 1;
                    for (int i = 0; i < burstCount; i++)
                    {
                        DoWeaponLogic(kvp.Key, dir);
                    }

                    _auraCooldown = killAuraDelay;
                    break;
                }
            }
        }

        private static void DoWeaponLogic(ulong targetId, Vector3 dir)
        {
            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inv == null || inv.currentItem == null)
            {
                MonoBehaviourPublicInpabyInInInUnique.PunchPlayer(targetId, dir);
                return;
            }

            GameObject heldObj = inv.currentItem;
            int weaponId = -1;
            int instanceId = -1;

            var meleeComp = heldObj.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();
            if (meleeComp != null)
            {
                weaponId = meleeComp.itemData.itemID;
                instanceId = meleeComp.id;
            }

            if (weaponId != -1)
            {
                int hitCount = (weaponId == 7) ? 3 : 1;
                for (int i = 0; i < hitCount; i++)
                {
                    if (meleeComp != null) meleeComp.field_Protected_Boolean_0 = true;
                    MonoBehaviourPublicInpabyInInInUnique.UseItem(weaponId, dir);
                    MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(targetId, 0, dir, weaponId, instanceId);
                }
            }
            else
            {
                MonoBehaviourPublicInpabyInInInUnique.PunchPlayer(targetId, dir);
            }
        }

        private static void ReturnMe()
        {
            if (SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (GameManager.Instance.activePlayers.ContainsKey(myId))
            {
                var me = GameManager.Instance.activePlayers[myId];
                if (me != null) me.transform.position = _returnPos;
            }
        }
    }

    public static class SnowballManager
    {
        public static bool autoPickup = false;
        public static bool autoShoot = false;
        public static bool autoAim = false;
        public static bool noShootDelay = false;
        public static int loopBoost = 1;

        private static int _targetIndex = 0;
        private static float _scanTimer = 0f;
        private static GameObject _closestPile = null;

        public static void Update()
        {
            if (loopBoost > 2) HandleFXSuppression();

            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inv == null) return;

            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 0.5f)
            {
                _scanTimer = 0f;
                UpdateClosestPile();
            }

            for (int i = 0; i < loopBoost; i++)
            {
                HandleAutoPickUp();
                if (inv.currentItem != null && inv.currentItem.name.Contains("Snowball"))
                {
                    HandleNoShootDelay(inv);
                    HandleAutoAim(inv);
                    HandleAutoShoot(inv);
                }
            }
        }

        private static void HandleAutoPickUp()
        {
            if (!autoPickup || _closestPile == null) return;
            var interact = _closestPile.GetComponent<MonoBehaviour1PublicBoInSiUnique>();
            if (interact != null)
            {
                MonoBehaviourPublicInpabyInInInUnique.TryInteract(interact.id);
            }
        }

        private static void HandleNoShootDelay(MonoBehaviourPublicItDi2ObIninInTrweGaUnique inv)
        {
            if (!noShootDelay) return;
            var currentItem = inv.currentItem;
            var comp1 = currentItem.GetComponent<MonoBehaviour2PublicGathObauTrgumuGaSiBoUnique>();
            var comp2 = currentItem.GetComponent<MonoBehaviour1PublicAbstractItitBoGapiTrrileTrObUnique>();
            if (comp1 != null) comp1.field_Protected_Boolean_0 = true;
            if (comp2 != null) comp2.field_Private_Boolean_0 = true;
        }

        private static void HandleAutoShoot(MonoBehaviourPublicItDi2ObIninInTrweGaUnique inv)
        {
            if (autoShoot)
            {
                inv.UseItem();
                if (autoAim && GameManager.Instance != null && GameManager.Instance.activePlayers.Count > 0)
                {
                    _targetIndex = (_targetIndex + 1) % GameManager.Instance.activePlayers.Count;
                }
            }
            else if (noShootDelay && Input.GetMouseButton(0))
            {
                inv.UseItem();
            }
        }

        private static void HandleAutoAim(MonoBehaviourPublicItDi2ObIninInTrweGaUnique inv)
        {
            if (!autoAim || GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            int count = GameManager.Instance.activePlayers.Count;
            if (count == 0) return;

            var myPlayer = GameManager.Instance.activePlayers.ContainsKey(myId) ? GameManager.Instance.activePlayers[myId] : null;
            if (myPlayer == null) return;
            Vector3 myPos = myPlayer.transform.position;

            var playersList = new List<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
            foreach (var p in GameManager.Instance.activePlayers.Values) playersList.Add(p);

            if (_targetIndex >= playersList.Count) _targetIndex = 0;
            var target = playersList[_targetIndex];

            int attempts = 0;
            bool targetValid = false;

            while (attempts < count)
            {
                target = playersList[_targetIndex];
                bool baseValid = (target != null && target.steamProfile.m_SteamID != myId && !target.dead);
                if (baseValid && TeamRadarManager.IsEnemy(myPos, target.transform.position))
                {
                    targetValid = true;
                    break;
                }
                _targetIndex = (_targetIndex + 1) % count;
                attempts++;
            }

            if (targetValid && target != null)
            {
                Vector3 targetHead = target.transform.position + new Vector3(0, 1.5f, 0);
                inv.currentItem.transform.position = targetHead;
            }
        }

        private static void HandleFXSuppression()
        {
            var sfxObj = GameObject.Find("SnowballThrownPrefab(Clone)/SFX_&_Trail (2)");
            if (sfxObj != null) UnityEngine.Object.Destroy(sfxObj);

            var thrownObj = GameObject.Find("SnowballThrownPrefab(Clone)");
            if (thrownObj != null)
            {
                var comp = thrownObj.GetComponent<MonoBehaviour1PublicTrtrGahiRiCoBoItVeBoUnique>();
                if (comp != null && comp.hitFx != null)
                {
                    UnityEngine.Object.Destroy(comp.hitFx);
                    comp.hitFx = null;
                }
            }
        }

        private static void UpdateClosestPile()
        {
            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me == null) return;

            float minDist = float.MaxValue;
            _closestPile = null;

            var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var go in all)
            {
                if (go != null && go.name.Contains("SnowballPile"))
                {
                    float dist = Vector3.Distance(me.transform.position, go.transform.position);
                    if (Mathf.Abs(go.transform.position.y - me.transform.position.y) <= 3f && dist < minDist)
                    {
                        minDist = dist;
                        _closestPile = go;
                    }
                }
            }
        }
    }

    public static class NetworkFreezeManager
    {
        public static ulong TargetId = 0;
        public static bool IsEnabled = false;
        public static int FreezePower = 200;
        private static float _checkTimer = 0f;

        public static void Update()
        {
            if (!IsEnabled || TargetId == 0) return;
            if (GameManager.Instance == null || SteamManager.Instance == null) return;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (TargetId == myId) return;

            if (!GameManager.Instance.activePlayers.ContainsKey(TargetId) || GameManager.Instance.activePlayers[TargetId].dead)
            {
                _checkTimer += Time.deltaTime;
                if (_checkTimer > 2.0f)
                {
                    _checkTimer = 0f;
                    if (!IsPlayerInSteamLobby(TargetId))
                    {
                        IsEnabled = false;
                        TargetId = 0;
                        Utility.ForceMessage("<color=yellow>Target left the server</color>");
                    }
                }
                return;
            }

            for (int i = 0; i < FreezePower; i++)
            {
                try
                {
                    var jamPacket = new ObjectPublicIDisposableLi1ByInByBoUnique(20);
                    jamPacket.Write(-1);
                    MonoBehaviourPublicInStInpabyDiInpaby2Unique.SendPacket(new CSteamID(TargetId), jamPacket, 20, MonoBehaviourPublicInStInpabyDiInpaby2Unique.EnumNPublicSealedvaTo3vToUnique.ToClient);

                    var crouchPacket = new ObjectPublicIDisposableLi1ByInByBoUnique(19);
                    crouchPacket.Write(true);
                    MonoBehaviourPublicInStInpabyDiInpaby2Unique.SendPacket(new CSteamID(TargetId), crouchPacket, 19, MonoBehaviourPublicInStInpabyDiInpaby2Unique.EnumNPublicSealedvaTo3vToUnique.ToClient);
                }
                catch { }
            }
        }

        private static bool IsPlayerInSteamLobby(ulong id)
        {
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return false;
            int count = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);
            for (int i = 0; i < count; i++)
            {
                if (SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i).m_SteamID == id)
                    return true;
            }
            return false;
        }
    }

    public static class CrabKillerManager
    {
        public static void ExecuteKill()
        {
            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 18)
            {
                Utility.ForceMessage("<color=red>You are not in Crab Fight!</color>");
                return;
            }
            try
            {
                for (int i = 0; i < 500; i++) ClientSend.DamageCrab(1, 1);
            }
            catch { }
        }
    }

    public static class VisualBlockManager
    {
        public static bool enabled = false;
        private static Vector3 blockPosition = new Vector3(-0.35f, -0.05f, 0.15f);
        private static Vector3 blockRotation = new Vector3(10f, -60f, 20f);
        private static Vector3 originalPosition;
        private static Quaternion originalRotation;
        private static bool isBlocking = false;
        private static float animSpeed = 15f;

        public static void LateUpdate()
        {
            if (!enabled)
            {
                if (isBlocking)
                {
                    isBlocking = false;
                    var invOff = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
                    if (invOff != null && invOff.currentItem != null)
                    {
                        GameObject weaponOff = invOff.currentItem;
                        weaponOff.transform.localPosition = originalPosition;
                        weaponOff.transform.localRotation = originalRotation;
                    }
                }
                return;
            }

            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inv == null || inv.currentItem == null) return;

            GameObject weapon = inv.currentItem;
            if (Input.GetMouseButton(1))
            {
                if (!isBlocking)
                {
                    originalPosition = weapon.transform.localPosition;
                    originalRotation = weapon.transform.localRotation;
                    isBlocking = true;
                }
                weapon.transform.localPosition = Vector3.Lerp(weapon.transform.localPosition, blockPosition, Time.deltaTime * animSpeed);
                weapon.transform.localRotation = Quaternion.Slerp(weapon.transform.localRotation, Quaternion.Euler(blockRotation), Time.deltaTime * animSpeed);
            }
            else
            {
                if (isBlocking)
                {
                    weapon.transform.localPosition = Vector3.Lerp(weapon.transform.localPosition, originalPosition, Time.deltaTime * animSpeed);
                    weapon.transform.localRotation = Quaternion.Slerp(weapon.transform.localRotation, originalRotation, Time.deltaTime * animSpeed);
                    if (Vector3.Distance(weapon.transform.localPosition, originalPosition) < 0.01f) isBlocking = false;
                }
            }
        }
    }

    public static class WeaponInspectManager
    {
        public static KeyCode InspectKey = KeyCode.None;
        private static float _currentReloadTime = 0f;
        private static float _totalReloadTime = 0f;

        public static void Update()
        {
            if (InspectKey == KeyCode.None) return;
            var inventory = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            if (inventory == null || inventory.currentItem == null)
            {
                _totalReloadTime = 0f;
                _currentReloadTime = 0f;
                return;
            }

            if (Input.GetKeyDown(InspectKey))
            {
                _totalReloadTime += 1f;
                try { MonoBehaviourPublicInpabyInInInUnique.PlayerReload(_totalReloadTime); } catch { }
            }

            if (_totalReloadTime > 0f)
            {
                _currentReloadTime += Time.deltaTime;
                float progress = _currentReloadTime / _totalReloadTime;
                float angleX = -EaseInOutBack(progress) * (360f * Mathf.CeilToInt(_totalReloadTime));
                inventory.currentItem.transform.localRotation = Quaternion.Euler(angleX, 0f, 0f);

                if (progress >= 1f)
                {
                    _totalReloadTime = 0f;
                    _currentReloadTime = 0f;
                }
            }
        }

        private static float EaseInOutBack(float x)
        {
            float num = 2.5949094f;
            if (x < 0.5f) return Mathf.Pow(2f * x, 2f) * ((num + 1f) * 2f * x - num) / 2f;
            else return (Mathf.Pow(2f * x - 2f, 2f) * ((num + 1f) * (x * 2f - 2f) + num) + 2f) / 2f;
        }
    }
}
