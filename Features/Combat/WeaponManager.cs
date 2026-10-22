using HarmonyLib;
using System;
using UnityEngine;

namespace BOTMenuMod
{
    public static class WeaponManager
    {
        public static bool Enabled = false;
        public static int PacketMode = 0;
        private static float _lastPacketTime = 0f;
        public static float PacketSendDelay = 0.2f;

        public static void Update()
        {
            if (!Enabled) return;

            if (Time.unscaledTime - _lastPacketTime < PacketSendDelay) return;
            _lastPacketTime = Time.unscaledTime;

            int weaponId = -1;
            int itemId = -1;
            if (!GetCurrentWeaponIds(ref weaponId, ref itemId)) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            bool didAttack = false;

            if (GameManager.Instance != null && GameManager.Instance.activePlayers != null)
            {
                if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;
                var localPlayer = GameManager.Instance.activePlayers[myId];
                if (localPlayer == null) return;
                Vector3 myPosition = localPlayer.transform.position;

                foreach (var kvp in GameManager.Instance.activePlayers)
                {
                    var targetPlayer = kvp.Value;
                    if (targetPlayer != null && !targetPlayer.dead)
                    {
                        ulong steamID = targetPlayer.steamProfile.m_SteamID;

                        if (steamID != myId && TeamRadarManager.IsEnemy(myPosition, targetPlayer.transform.position))
                        {
                            SendWeaponPacket(steamID, weaponId, itemId);
                            didAttack = true;
                        }
                    }
                }
            }

            if (!didAttack)
            {
                Enabled = false;
            }
        }

        private static void SendWeaponPacket(ulong steamId, int weaponId, int itemId)
        {
            switch (PacketMode)
            {
                case 0:
                    MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(steamId, 0, new Vector3(0f, -1000f, 0f), weaponId, itemId);
                    break;
                case 1:
                    MonoBehaviourPublicInpabyInInInUnique.DamagePlayer(steamId, 0, new Vector3(0f, 1000f, 0f), weaponId, itemId);
                    break;
            }
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

    public static class SuperKnockbackManager
    {
        private static bool _isCustomSending = false;
        private static bool _snowballErrorLogged = false;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(SuperKnockbackManager));
            Utility.Log("[Knockback] 动态脉冲突破上限引擎已挂载！");
        }

        [HarmonyPatch(typeof(ClientSend), nameof(ClientSend.PunchPlayer))]
        [HarmonyPrefix]
        public static bool Prefix_ClientSendPunch(ulong __0, Vector3 __1)
        {

            if (_isCustomSending) return true;

            if (!WeaponAttackManager.enableKnockbackMult) return true;

            float mult = WeaponAttackManager.knockbackMult;
            if (Mathf.Abs(mult - 1.0f) < 0.05f) return true;

            try
            {
                _isCustomSending = true;

                float sign = Mathf.Sign(mult);
                float absMult = Mathf.Abs(mult);

                Vector3 dir = __1.normalized;
                if (dir == Vector3.zero) dir = Vector3.forward;

                if (sign > 0)
                {
                    dir = (dir + Vector3.up * 0.22f).normalized;
                }

                float singleClampLimit = 25f;
                float targetTotalForce = absMult * 15f;

                int burstCount = Mathf.Clamp(Mathf.CeilToInt(targetTotalForce / singleClampLimit), 1, 20);
                float forcePerBurst = (targetTotalForce / burstCount) * sign;

                Vector3 impulseVector = dir * forcePerBurst;

                for (int i = 0; i < burstCount; i++)
                {
                    ClientSend.PunchPlayer(__0, impulseVector);
                }

                _isCustomSending = false;
                return false;
            }
            catch
            {
                _isCustomSending = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(ThrownSnowball), "OnCollisionEnter")]
        [HarmonyPrefix]
        public static void Prefix_SnowballHit(ThrownSnowball __instance, Collision __0)
        {
            if (!WeaponAttackManager.enableKnockbackMult) return;

            float mult = WeaponAttackManager.knockbackMult;
            if (Mathf.Abs(mult - 1.0f) < 0.05f) return;
            if (__instance == null || __0 == null || __0.gameObject == null) return;

            try
            {
                var targetPm = __0.transform.root.GetComponent<PlayerManager>();
                var targetRb = __0.transform.root.GetComponent<Rigidbody>();

                if (targetPm != null && targetPm.steamProfile != null)
                {
                    ulong targetId = targetPm.steamProfile.m_SteamID;
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    if (targetId == 0 || targetId == myId) return;

                    Vector3 flyDir = __instance.transform.forward;
                    var sbRb = __instance.GetComponent<Rigidbody>();
                    if (sbRb != null && sbRb.velocity.magnitude > 0.2f)
                    {
                        flyDir = sbRb.velocity.normalized;
                    }

                    float sign = Mathf.Sign(mult);
                    float absMult = Mathf.Abs(mult);

                    Vector3 dir = (sign > 0) ? (flyDir + Vector3.up * 0.22f).normalized : -flyDir;

                    float singleClampLimit = 25f;
                    float targetTotalForce = absMult * 15f;
                    int burstCount = Mathf.Clamp(Mathf.CeilToInt(targetTotalForce / singleClampLimit), 1, 20);
                    float forcePerBurst = (targetTotalForce / burstCount) * sign;

                    Vector3 impulseVector = dir * forcePerBurst;

                    if (targetRb != null)
                    {
                        targetRb.AddForce(impulseVector * burstCount * 0.5f, ForceMode.Impulse);
                    }

                    _isCustomSending = true;
                    for (int i = 0; i < burstCount; i++)
                    {
                        ClientSend.PunchPlayer(targetId, impulseVector);
                    }
                    _isCustomSending = false;
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(ThrownSnowball), "OnCollisionEnter")]
        [HarmonyFinalizer]
        public static System.Exception Finalizer_SnowballHit(System.Exception __exception)
        {
            if (__exception == null) return null;

            if (!_snowballErrorLogged)
            {
                _snowballErrorLogged = true;
                Utility.Log("[Snowball] suppressed game collision exception: " + __exception.GetType().Name + ": " + __exception.Message);
            }

            return null;
        }
    }
}
