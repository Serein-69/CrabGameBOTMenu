using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SteamworksNative;
using UnhollowerBaseLib;

namespace BOTMenuMod
{
    public static class VoiceIndicatorManager
    {

        public static Dictionary<ulong, RawImage> playerMicImages = new Dictionary<ulong, RawImage>();
        private static float _updateTimer = 0f;

        public static KeyCode alternativeJump = KeyCode.Space;
        public static KeyCode alternativeCrouch = KeyCode.C;
        public static bool holdJump = false;
        public static bool holdSprint = false;

        private const string SAVE_KEY_JUMP = "BOT_AltJump";
        private const string SAVE_KEY_CROUCH = "BOT_AltCrouch";
        private const string SAVE_KEY_HOLD_JUMP = "BOT_HoldJump";
        private const string SAVE_KEY_HOLD_SPRINT = "BOT_HoldSprint";

        private static bool sprintToggled = false;

        internal static ControlSetting uiAltJump;
        internal static ControlSetting uiAltCrouch;
        internal static MyBoolSetting uiHoldJump;
        internal static MyBoolSetting uiHoldSprint;

        public static void Init()
        {
            LoadBinds();
            Harmony.CreateAndPatchAll(typeof(VoiceIndicatorManager));
            Debug.Log("[VoiceIndicator & Custom Settings] Initialized perfectly.");
        }

        public static void Update()
        {
            if (DissonanceManager.Instance != null && DissonanceManager.Instance.comms != null)
            {
                _updateTimer += Time.deltaTime;
                if (_updateTimer >= 0.1f)
                {
                    _updateTimer = 0f;
                    foreach (var kvp in playerMicImages)
                        if (kvp.Value != null)
                            kvp.Value.enabled = IsPlayerTalking(kvp.Key);
                }
            }
        }

        private static bool IsPlayerTalking(ulong steamId)
        {
            try
            {
                var state = DissonanceManager.Instance.comms.FindPlayer(steamId.ToString());
                return state != null && state.prop_Boolean_1;
            }
            catch { return false; }
        }

        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.SetInput))]
        [HarmonyPrefix]
        private static void PrePlayerMovementSetInput(ref bool param_2, ref bool param_3, ref bool param_4)
        {
            if (Input.GetKey(alternativeJump)) param_3 = true;
            if (Input.GetKey(alternativeCrouch)) param_2 = true;

            if (holdJump)
                param_3 = param_3 || Input.GetKey((KeyCode)InputManager.jump);

            if (holdSprint)
            {

                if (Input.GetKeyDown((KeyCode)InputManager.sprint))
                    sprintToggled = !sprintToggled;
                param_4 = sprintToggled;
            }
            else
            {

                param_4 = param_4 || Input.GetKey((KeyCode)InputManager.sprint);
            }
        }

        [HarmonyPatch(typeof(PlayerListingPrefab), "SetPlayer")]
        [HarmonyPostfix]
        public static void Postfix_SetPlayer(PlayerListingPrefab __instance, PlayerManager __0)
        {
            if (__0?.steamProfile != null) InjectMicIcon(__instance, __0.steamProfile.m_SteamID);
        }

        [HarmonyPatch(typeof(PlayerListingPrefab), "SetSpectator")]
        [HarmonyPostfix]
        public static void Postfix_SetSpectator(PlayerListingPrefab __instance, ulong __0)
        {
            InjectMicIcon(__instance, __0);
        }

        private static void InjectMicIcon(PlayerListingPrefab prefab, ulong steamId)
        {
            if (prefab == null || (playerMicImages.TryGetValue(steamId, out var img) && img != null)) return;
            try
            {

                GameObject micObj = UnityEngine.Object.Instantiate(prefab.icon.gameObject, prefab.transform);
                micObj.name = "BOT_TalkingIcon";
                micObj.transform.SetSiblingIndex(4);
                RawImage micImage = micObj.GetComponent<RawImage>();
                micImage.texture = Texture2D.whiteTexture;
                micImage.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                micImage.rectTransform.localScale = new Vector3(0.5f, 0.5f, 1f);
                micImage.enabled = false;
                playerMicImages[steamId] = micImage;
            }
            catch { }
        }

        [HarmonyPatch(typeof(PlayerList), "Awake")]
        [HarmonyPostfix]
        public static void PostPlayerListAwake() => playerMicImages.Clear();

        [HarmonyPatch(typeof(Settings), nameof(Settings.Start))]
        [HarmonyPostfix]
        private static void PostSettingsStart(Settings __instance)
        {

            ControlSetting jumpTemplate = null, crouchTemplate = null;
            MyBoolSetting holdCrouchTemplate = null;

            foreach (var field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(ControlSetting))
                {
                    var ctrl = field.GetValue(__instance) as ControlSetting;
                    if (ctrl == null) continue;
                    string title = GetSettingTitle(ctrl.transform);
                    if (title != null)
                    {
                        if (title.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) >= 0) jumpTemplate = ctrl;
                        else if (title.IndexOf("Crouch", StringComparison.OrdinalIgnoreCase) >= 0) crouchTemplate = ctrl;
                    }
                }
                else if (field.FieldType == typeof(MyBoolSetting))
                {
                    var boolSet = field.GetValue(__instance) as MyBoolSetting;
                    if (boolSet == null) continue;
                    string title = GetSettingTitle(boolSet.transform);
                    if (title != null && title.IndexOf("Hold", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        title.IndexOf("crouch", StringComparison.OrdinalIgnoreCase) >= 0)
                        holdCrouchTemplate = boolSet;
                }
            }

            if (jumpTemplate == null) jumpTemplate = GetFirst<ControlSetting>(__instance);
            if (crouchTemplate == null) crouchTemplate = jumpTemplate;
            if (holdCrouchTemplate == null) holdCrouchTemplate = GetFirst<MyBoolSetting>(__instance);

            if (jumpTemplate != null)
            {
                uiAltJump = CreateControlSetting(jumpTemplate, jumpTemplate.transform.GetSiblingIndex() + 0, "Jump (Alternative)");

                uiAltJump.SetSetting((int)alternativeJump, "Jump (Alternative)");
            }

            if (crouchTemplate != null)
            {
                uiAltCrouch = CreateControlSetting(crouchTemplate, crouchTemplate.transform.GetSiblingIndex() + 0, "Crouch (Alternative)");
                uiAltCrouch.SetSetting((int)alternativeCrouch, "Crouch (Alternative)");
            }

            if (holdCrouchTemplate != null)
            {
                int idx = holdCrouchTemplate.transform.GetSiblingIndex();
                uiHoldJump = CreateBoolSetting(holdCrouchTemplate, idx + 1, "Hold to jump");
                uiHoldJump.SetSetting(holdJump);

                uiHoldSprint = CreateBoolSetting(holdCrouchTemplate, idx + 2, "Hold to sprint");
                uiHoldSprint.SetSetting(holdSprint);
            }
        }

        private static ControlSetting CreateControlSetting(ControlSetting template, int siblingIndex, string title)
        {
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
            go.transform.SetSiblingIndex(siblingIndex);

            var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps != null && tmps.Length > 0) tmps[0].text = title;

            ControlSetting newSetting = go.GetComponent<ControlSetting>();

            if (newSetting.m_OnClick != null)
            {
                newSetting.m_OnClick.RemoveAllListeners();
            }

            return newSetting;
        }

        private static MyBoolSetting CreateBoolSetting(MyBoolSetting template, int siblingIndex, string title)
        {
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
            go.transform.SetSiblingIndex(siblingIndex);

            var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps != null && tmps.Length > 0) tmps[0].text = title;

            MyBoolSetting newSetting = go.GetComponent<MyBoolSetting>();

            if (newSetting.m_OnClick != null)
            {
                newSetting.m_OnClick.RemoveAllListeners();
            }

            return newSetting;
        }

        [HarmonyPatch(typeof(ControlSetting), nameof(ControlSetting.SetKey))]
        [HarmonyPostfix]
        private static void PostControlSettingSetKey(ControlSetting __instance)
        {
            if (__instance == uiAltJump)
            {
                alternativeJump = (KeyCode)__instance.currentKey;
                SaveBinds();
            }
            else if (__instance == uiAltCrouch)
            {
                alternativeCrouch = (KeyCode)__instance.currentKey;
                SaveBinds();
            }
        }

        [HarmonyPatch(typeof(MyBoolSetting), nameof(MyBoolSetting.ToggleSetting))]
        [HarmonyPostfix]
        private static void PostMyBoolSettingToggleSetting(MyBoolSetting __instance)
        {
            if (__instance == uiHoldJump)
            {
                holdJump = __instance.currentSetting == 1;
                SaveBinds();
            }
            else if (__instance == uiHoldSprint)
            {
                holdSprint = __instance.currentSetting == 1;
                SaveBinds();
            }
        }

        private static string GetSettingTitle(Transform root)
        {
            try
            {
                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmps != null && tmps.Length > 0) return tmps[0].text;
            }
            catch { }
            return null;
        }

        private static T GetFirst<T>(Settings settings) where T : MonoBehaviour
        {
            var all = settings.GetComponentsInChildren<T>(true);
            return (all != null && all.Length > 0) ? all[0] : null;
        }

        private static void LoadBinds()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY_JUMP)) alternativeJump = (KeyCode)PlayerPrefs.GetInt(SAVE_KEY_JUMP);
            if (PlayerPrefs.HasKey(SAVE_KEY_CROUCH)) alternativeCrouch = (KeyCode)PlayerPrefs.GetInt(SAVE_KEY_CROUCH);
            if (PlayerPrefs.HasKey(SAVE_KEY_HOLD_JUMP)) holdJump = PlayerPrefs.GetInt(SAVE_KEY_HOLD_JUMP) == 1;
            if (PlayerPrefs.HasKey(SAVE_KEY_HOLD_SPRINT)) holdSprint = PlayerPrefs.GetInt(SAVE_KEY_HOLD_SPRINT) == 1;
        }

        private static void SaveBinds()
        {
            PlayerPrefs.SetInt(SAVE_KEY_JUMP, (int)alternativeJump);
            PlayerPrefs.SetInt(SAVE_KEY_CROUCH, (int)alternativeCrouch);
            PlayerPrefs.SetInt(SAVE_KEY_HOLD_JUMP, holdJump ? 1 : 0);
            PlayerPrefs.SetInt(SAVE_KEY_HOLD_SPRINT, holdSprint ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
