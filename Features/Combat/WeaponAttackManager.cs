using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BOTMenuMod
{
    public class WeaponAttackManager : MonoBehaviour
    {
        public static WeaponAttackManager Instance;
        public static float knockbackMult = 1.0f;
        public static bool enableKnockbackMult = false;
        public static bool enableInfiniteAmmo = false;
        public static bool enableNoCooldown = false;
        public static bool enableKnockbackDeflect = false;
        public static float knockbackDeflectAngle = 90f;
        public static float rapidFireDelay = 0.08f;

        private MonoBehaviourPublicItDi2ObIninInTrweGaUnique _myInventory;
        private float _attackTimer = 0f;
        private static Dictionary<int, GunComponent> _overriddenGuns = new Dictionary<int, GunComponent>();

        public WeaponAttackManager(IntPtr ptr) : base(ptr) { }
        void Awake() { Instance = this; }

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(WeaponAttackManager));
            Utility.Log("Passive Weapon Attack Manager Initialized.");
        }

        void Update()
        {
            if (_myInventory == null || Time.frameCount % 60 == 0)
                _myInventory = GetLocalInventory();

            if (_myInventory != null)
            {
                if (enableInfiniteAmmo) HandleInfiniteAmmo();
                if (enableNoCooldown || GunMasterManager.autoShoot)
                {
                    HandleUltimateAutoFire();
                }
            }

            if (!enableNoCooldown && !GunMasterManager.autoShoot && _overriddenGuns.Count > 0)
            {
                RestoreGunAutomatic();
            }
        }

        private static void RestoreGunAutomatic()
        {
            foreach (var kvp in _overriddenGuns)
            {
                var gc = kvp.Value;
                if (gc == null) continue;
                try { gc.automatic = false; } catch { }
            }
            _overriddenGuns.Clear();
        }

        private MonoBehaviourPublicItDi2ObIninInTrweGaUnique GetLocalInventory()
        {
            var invs = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            foreach (var i in invs)
                if (i.enabled && i.gameObject.activeInHierarchy) return i;
            return null;
        }

        private void HandleInfiniteAmmo()
        {
            if (_myInventory.currentItem == null) return;
            var gun = _myInventory.currentItem.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
            if (gun != null && gun.itemData != null) gun.itemData.currentAmmo = 999;
        }

        private void HandleUltimateAutoFire()
        {
            if (_myInventory.currentItem == null) return;

            var item = _myInventory.currentItem;
            var gun = item.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
            var melee = item.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();

            if (gun != null && gun.itemData != null && gun.itemData.gunComponent != null)
            {
                var gc = gun.itemData.gunComponent;
                if (!gc.automatic)
                {
                    int gcKey = gc.GetInstanceID();
                    if (!_overriddenGuns.ContainsKey(gcKey)) _overriddenGuns[gcKey] = gc;
                    gc.automatic = true;
                }
            }

            bool isHoldingLeftClick = enableNoCooldown && Input.GetMouseButton(0);
            bool isAutoShooting = GunMasterManager.autoShoot;

            if (isHoldingLeftClick || isAutoShooting)
            {
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= rapidFireDelay)
                {
                    _attackTimer = 0f;
                    if (gun != null) gun.field_Protected_Boolean_0 = true;
                    if (melee != null) melee.field_Protected_Boolean_0 = true;
                    try { _myInventory.UseItem(); } catch { }
                }
            }
            else
            {
                _attackTimer = rapidFireDelay;
            }
        }

        [HarmonyPatch(typeof(Input), "GetKeyDown", new Type[] { typeof(KeyCode) })]
        [HarmonyPrefix]
        public static bool PreInputGetKeyDown(ref bool __result, KeyCode key)
        {
            if (!enableNoCooldown) return true;

            if (key == KeyCode.Mouse0)
            {
                var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
                if (inv != null && inv.currentItem != null)
                {
                    __result = Input.GetKey(key);
                    return false;
                }
            }
            return true;
        }
    }
}
