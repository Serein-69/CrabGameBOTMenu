using System;
using UnityEngine;
using UnityEngine.UI;
using SteamworksNative;

namespace BOTMenuMod
{
    public class BOTStatusIsland : MonoBehaviour
    {
        public static bool IsVisible = true;
        public static BOTStatusIsland Instance;
        private Text _infoText;
        private RectTransform _rect;
        private float _updateTimer;
        private float _fps;
        private bool _last12HourSetting;

        public BOTStatusIsland(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;

            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.anchoredPosition = new Vector2(0, -5f);
            _rect.sizeDelta = new Vector2(300, 32);

            var bg = gameObject.AddComponent<Image>();
            bg.color = Color.clear;

            GameObject textObj = new GameObject("IslandText");
            textObj.transform.SetParent(this.transform, false);
            _infoText = textObj.AddComponent<Text>();
            _infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _infoText.fontSize = 16;
            _infoText.fontStyle = FontStyle.Bold;
            _infoText.alignment = TextAnchor.MiddleCenter;
            _infoText.color = Color.white;
            _infoText.supportRichText = true;

            var textShadow = textObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            textShadow.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform tRect = _infoText.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
        }

        void Update()
        {
            if (this.GetComponent<CanvasGroup>() == null) gameObject.AddComponent<CanvasGroup>();
            this.GetComponent<CanvasGroup>().alpha = IsVisible ? 1f : 0f;

            if (!IsVisible) return;

            _fps = 1.0f / Time.unscaledDeltaTime;

            if (_last12HourSetting != BOTPlugin.Config12HourTime.Value)
            {
                _last12HourSetting = BOTPlugin.Config12HourTime.Value;
                UpdateContent();
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.5f)
            {
                _updateTimer = 0f;
                UpdateContent();
            }
        }

        public void UpdateContent()
        {
            string menuTag = "<color=#A020F0>BOT MENU</color>";
            string userName = "User";
            string timeFormat = BOTPlugin.Config12HourTime.Value ? "hh:mm:ss tt" : "HH:mm:ss";
            string currentTime = DateTime.Now.ToString(timeFormat);

            try
            {
                if (SteamManager.Instance != null && SteamManager.Instance.field_Private_CSteamID_0 != null)
                {
                    string rawName = SteamFriends.GetFriendPersonaName(SteamManager.Instance.field_Private_CSteamID_0);
                    string cleanName = Utility.StripColorTags(rawName);
                    if (cleanName.Length > 12) userName = cleanName.Substring(0, 10) + "..";
                    else userName = cleanName;
                }
            }
            catch { }

            _infoText.text = $"{menuTag}  |  {userName}  |  {(int)_fps} fps  |  {currentTime}";
            float preferredWidth = _infoText.preferredWidth + 20f;
            _rect.sizeDelta = new Vector2(Mathf.Max(preferredWidth, 200f), 32);
        }
    }
}
