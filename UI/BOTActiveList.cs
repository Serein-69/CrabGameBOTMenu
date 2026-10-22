using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BOTMenuMod
{
    public class BOTActiveList : MonoBehaviour
    {
        public static bool IsVisible = true;
        public static bool ShowBackground = true;
        public static int ColorMode = 2;
        public static BOTActiveList Instance;
        public static float CurrentHeight = 0f;

        private RectTransform _container;
        private CanvasGroup _canvasGroup;
        private List<ActiveItem> _uiItems = new List<ActiveItem>();
        private float _updateTimer;

        private class ActiveItem
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public Image RightLine;
            public Text Label;
        }

        public BOTActiveList(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;
            _container = gameObject.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(1f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(1f, 1f);
            _container.anchoredPosition = new Vector2(0f, -80f);
            _container.sizeDelta = new Vector2(300, 800);

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        void Update()
        {
            _canvasGroup.alpha = IsVisible ? 1f : 0f;
            if (!IsVisible) return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.1f)
            {
                _updateTimer = 0f;
                RefreshList();
            }
        }

        private void RefreshList()
        {
            List<ModuleEntry> activeModules = new List<ModuleEntry>();
            string colAggressive = "#FF4444", colMovement = "#55AAFF", colVisuals = "#55FF55", colMisc = "#FF55FF", colProtect = "#FFFF55";

            AddMod(activeModules, GodModeManager.enabled, "God Mode", colProtect);
            AddMod(activeModules, GodModeManager.autoRescue, "Void Rescue", colProtect);
            AddMod(activeModules, MiniGameTrollManager.rlglGodMode, "RLGL God", colProtect);
            AddMod(activeModules, HillGodManager.enabled, "Hill God", colProtect);
            AddMod(activeModules, BridgeHelperManager.noGlassBreak, "Glass God", colProtect);
            AddMod(activeModules, GunMasterManager.autoAim, "Gun Aimbot", colAggressive);
            AddMod(activeModules, GunMasterManager.autoShoot, "Gun Auto Fire", colAggressive);
            AddMod(activeModules, GunMasterManager.aimThroughWalls, "Aim Thru Walls", colAggressive);
            AddMod(activeModules, LavaPunchManager.enableKillAura, "Kill Aura", colAggressive);
            AddMod(activeModules, ForceKillManager.killAuraEnabled, "Silent Kill", colAggressive);
            AddMod(activeModules, ShadowModePatches.crashPlayerOnHit, "Crash Player", colAggressive);
            AddMod(activeModules, WeaponAttackManager.enableInfiniteAmmo, "Inf Ammo", colAggressive);
            AddMod(activeModules, WeaponAttackManager.enableNoCooldown, "No Cooldown", colAggressive);
            AddMod(activeModules, SnowballManager.autoShoot, "Snow Auto Fire", colAggressive);
            AddMod(activeModules, SnowballManager.autoAim, "Snow Aimbot", colAggressive);
            AddMod(activeModules, SnowballManager.noShootDelay, "Snow No Delay", colAggressive);
            AddMod(activeModules, LegitESPManager.enabled, "Legit ESP", colVisuals);
            AddMod(activeModules, ItemESPManager.enabled, "Item ESP", colVisuals);
            AddMod(activeModules, BridgeESPManager.enabled, "Bridge ESP", colVisuals);
            AddMod(activeModules, ButtonESPManager.enabled, "Button ESP", colVisuals);
            AddMod(activeModules, VisualsManager.enableTrail, "Trail", colVisuals);
            AddMod(activeModules, VisualsManager.enableJumpRing, "Jump Ring", colVisuals);
            AddMod(activeModules, AuraManager.enabled, "Aura", colVisuals);
            AddMod(activeModules, CrosshairManager.enableDot, "Crosshair", colVisuals);
            AddMod(activeModules, FullbrightManager.enabled, "Fullbright", colVisuals);
            AddMod(activeModules, ThirdPersonManager.enabled, "3rd Person", colVisuals);
            AddMod(activeModules, SpectatorPOVManager.enabled, "Spec POV", colVisuals);
            AddMod(activeModules, CameraManager.EnableZoom, "Zoom Active", colVisuals);
            AddMod(activeModules, CameraManager.OverrideFOV, "FOV Override", colVisuals);
            AddMod(activeModules, MirrorCamManager.enabled, "Mirror Cam", colVisuals);
            AddMod(activeModules, CinematicGraphicsManager.isEnabled, "Cinematic FX", colVisuals);
            AddMod(activeModules, DamageNotificationManager.enabled, "Damage Feed", colVisuals);
            AddMod(activeModules, HitEffectManager.enabled, "Hit Heart FX", colVisuals);
            AddMod(activeModules, ShinyIntensityManager.Enabled, "Max Shiny", colVisuals);
            AddMod(activeModules, FreecamManager.enabled, "Freecam", colMovement);
            AddMod(activeModules, TeleportManager.enabled, "Click TP", colMovement);
            AddMod(activeModules, BacktrackingManager.enabled, "Fake Lag", colMovement);
            AddMod(activeModules, PlayerNoClipManager.enabled, "Player NoClip", colMovement);
            AddMod(activeModules, PlayerNoClipManager.wallsEnabled, "Wall NoClip", colMovement);
            AddMod(activeModules, WaterJumpManager.enabled, "Water Jump", colMovement);
            AddMod(activeModules, WaterWalkManager.enabled, "Water Walk", colMovement);
            AddMod(activeModules, OrbitManager.enabled, "Orbit Walk", colMovement);
            AddMod(activeModules, PlayerLauncherManager.enabled, "Player Launcher", colMovement);
            AddMod(activeModules, AutoPlayDodgeball.enabled, "Auto Play BOT", colMisc);
            AddMod(activeModules, ShadowModePatches.preventKickOnBan, "Anti-Kick", colMisc);
            AddMod(activeModules, ShadowModePatches.hideFromPlayerList, "Shadow Mode", colMisc);
            AddMod(activeModules, ShadowModePatches.bypassFreeze, "Bypass Freeze", colMisc);
            AddMod(activeModules, ShadowModePatches.enableAutoReady, "Auto Ready", colMisc);
            AddMod(activeModules, ShadowModePatches.blockLoadMap, "Block Map", colMisc);
            AddMod(activeModules, ShadowModePatches.enableAntiSpectate, "Anti-Spectate", colMisc);
            AddMod(activeModules, ShadowModePatches.enableFakePing, "Fake Ping", colMisc);
            AddMod(activeModules, ShadowModePatches.enableSpinbot, "Spinbot", colMisc);
            AddMod(activeModules, BodyRotationManager.IsActive, "Demon Pose", colMisc);
            AddMod(activeModules, FunnyScaleModule._isLocalActive, "Funny Scale", colMisc);
            AddMod(activeModules, WeaponSpawnerManager.IsActive, "Item Spawner", colMisc);
            AddMod(activeModules, AutoAdvertiser.enabled, "Auto Ad", colMisc);
            AddMod(activeModules, ItemSkinChangerManager.itemSkinChanger, "Skin Changer", colMisc);
            AddMod(activeModules, SkinManager.AutoApply, "Auto Black Skin", colMisc);
            AddMod(activeModules, ShadowModePatches.enableUncensoredChat, "Uncensored Chat", colMisc);
            AddMod(activeModules, TASManager.isRecording, "TAS Recording", colMisc);
            AddMod(activeModules, TASManager.isPlaying, "TAS Playing", colMisc);

            var sorted = activeModules.OrderByDescending(m => m.Name.Length).ToList();
            while (_uiItems.Count < sorted.Count) CreateNewItemUI();

            float currentY = 0f;
            float itemHeight = 22f;

            for (int i = 0; i < _uiItems.Count; i++)
            {
                var item = _uiItems[i];
                if (i >= sorted.Count) { item.Root.SetActive(false); continue; }

                item.Root.SetActive(true);
                var mod = sorted[i];

                Color finalColor = Color.white;

                if (ColorMode == 0)
                {

                    if (!ColorUtility.TryParseHtmlString(MenuThemeManager.CurrentAccentHex, out Color themeColor))
                        themeColor = Color.white;
                    finalColor = BuildThemeWaveColor(themeColor, i);
                    item.Label.text = mod.Name;
                }
                else if (ColorMode == 1) ColorUtility.TryParseHtmlString(MenuThemeManager.CurrentAccentHex, out finalColor);
                else if (ColorMode == 2)
                {
                    float hue = (Time.time * 0.4f + i * 0.08f) % 1f;
                    finalColor = Color.HSVToRGB(hue, 0.7f, 1f);
                }
                else if (ColorMode == 3)
                {

                    finalColor = Color.white;
                }

                if (ColorMode != 0)
                    item.Label.text = ColorMode == 3 ? BuildRainbowText(mod.Name, i) : mod.Name;
                item.Label.color = finalColor;
                item.RightLine.color = finalColor;
                item.Background.color = ShowBackground ? new Color(0.1f, 0.1f, 0.1f, 0.75f) : Color.clear;

                float targetWidth = mod.Name.Length * 9.5f + 16f;
                item.Rect.sizeDelta = new Vector2(targetWidth, itemHeight);
                item.Rect.anchoredPosition = new Vector2(0, currentY);
                currentY -= itemHeight;
            }
            CurrentHeight = sorted.Count * itemHeight;
        }

        private Color BuildThemeWaveColor(Color baseColor, int rowIndex)
        {
            Color.RGBToHSV(baseColor, out float hue, out float saturation, out float value);
            float wave = (Mathf.Sin(Time.time * 5f - rowIndex * 0.65f) + 1f) * 0.5f;
            float animatedValue = Mathf.Lerp(Mathf.Max(0.35f, value * 0.65f), Mathf.Min(1f, value * 1.15f), wave);
            return Color.HSVToRGB(hue, saturation, animatedValue);
        }

        private string BuildUniformWaveText(string text, Color baseColor, int rowIndex)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string result = "";

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    result += " ";
                    continue;
                }

                float wave = (Mathf.Sin(Time.time * 4f - i * 0.8f - rowIndex * 0.25f) + 1f) * 0.5f;
                float brightness = Mathf.Lerp(0.65f, 1f, wave);
                Color animatedColor = new Color(
                    Mathf.Clamp01(baseColor.r * brightness),
                    Mathf.Clamp01(baseColor.g * brightness),
                    Mathf.Clamp01(baseColor.b * brightness),
                    Mathf.Lerp(0.75f, 1f, wave));

                result += "<color=#" + ColorUtility.ToHtmlStringRGBA(animatedColor) + ">" + text[i] + "</color>";
            }
            return result;
        }

        private string BuildRainbowText(string text, int rowIndex)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string result = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    result += " ";
                    continue;
                }

                float hue = (Time.time * 0.35f + rowIndex * 0.07f + i * 0.055f) % 1f;
                Color color = Color.HSVToRGB(hue, 0.65f, 1f);
                result += "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text[i] + "</color>";
            }
            return result;
        }

        private void CreateNewItemUI()
        {
            GameObject root = new GameObject("ActiveItem");
            root.transform.SetParent(_container, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1);

            Image bg = root.AddComponent<Image>();

            GameObject lineObj = new GameObject("RightLine");
            lineObj.transform.SetParent(root.transform, false);
            Image lineImg = lineObj.AddComponent<Image>();
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(1, 0); lineRect.anchorMax = new Vector2(1, 1); lineRect.pivot = new Vector2(1, 0.5f);
            lineRect.anchoredPosition = Vector2.zero; lineRect.sizeDelta = new Vector2(2.5f, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(root.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 15; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleRight;
            text.supportRichText = true;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0); textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(5, 0); textRect.offsetMax = new Vector2(-6, 0);

            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f); shadow.effectDistance = new Vector2(1f, -1f);

            _uiItems.Add(new ActiveItem { Root = root, Rect = rect, Background = bg, RightLine = lineImg, Label = text });
        }

        private void AddMod(List<ModuleEntry> list, bool condition, string name, string color)
        {
            if (condition) list.Add(new ModuleEntry { Name = name, Color = color });
        }
        private struct ModuleEntry { public string Name; public string Color; }
    }
}
