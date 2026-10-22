using System;
using UnityEngine;

namespace BOTMenuMod
{
    public static class WeaponSpawnerManager
    {
        public static bool IsActive = false;
        public static bool ShouldPickUp = false;
        public static bool IsInfinite = false;
        public static int SelectedWeaponIdx = 0;
        public static int Multiplier = 1;

        private static int[] WEAPON_IDS = { 4, 5, 7, 8, 10 };
        private static int[] WEAPON_SLOTS = { 2, 0, 3, 2, 0 };
        private static string[] WEAPON_NAMES = { "Bat", "Bomb", "Knife", "Pipe", "Stick" };

        private static float _timer = 0f;

        public static void Update()
        {
            if (!IsActive) return;

            _timer += Time.deltaTime;
            if (_timer >= 0.15f)
            {
                _timer = 0f;
                ExecutePureSpawnerLogic();
            }
        }

        private static void ExecutePureSpawnerLogic()
        {
            try
            {
                if (SteamManager.Instance == null) return;
                ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

                int weaponId = WEAPON_IDS[SelectedWeaponIdx];
                int slot = WEAPON_SLOTS[SelectedWeaponIdx];

                for (int k = 0; k < Multiplier; k++)
                {
                    ClientSendClass.DamagePlayer(myId, 0, Vector3.zero, 9, slot);
                    ClientSendClass.TryBuyItem(weaponId);
                    ClientSendClass.SendActiveItem(weaponId);
                    ClientSendClass.TryInteract(slot);
                    ClientSendClass.TryDropItem(weaponId, slot, 0);

                    if (IsInfinite)
                    {
                        ClientSendClass.TryInteract(slot);
                        ClientSendClass.TryDropItem(weaponId, slot, 0);
                    }

                    if (ShouldPickUp)
                    {
                        ClientSendClass.TryInteract(slot);
                        if (MonoBehaviourPublicItDi2ObIninInTrweGaUnique.Instance != null)
                        {
                            var itemData = MonoBehaviourPublicDi2InItidGamoObInUnique.idToItem[weaponId];
                            if (itemData != null)
                            {
                                itemData.objectID = slot;
                                MonoBehaviourPublicItDi2ObIninInTrweGaUnique.Instance.ForceGiveItem(itemData);
                            }
                        }
                    }
                    else
                    {
                        ClientSendClass.SendActiveItem(-1);
                    }
                }
            }
            catch { }
        }

        public static string GetWeaponName() => WEAPON_NAMES[SelectedWeaponIdx];
    }
}
