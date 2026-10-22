using System;
using UnityEngine;
using UnityEngine.UI;

namespace BOTMenuMod
{
    public class BOTNotification : MonoBehaviour
    {
        public static BOTNotification Instance;
        private Text _textComponent;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Outline _outline;

        public static float PosX = 30f;
        public static float PosY = 40f;
        public static float AnimSpeed = 10f;
        public static float Duration = 3.5f;

        private float _timer = 0f;
        private bool _isActive = false;

        public BOTNotification(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.anchorMin = _rectTransform.anchorMax = _rectTransform.pivot = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            _outline = gameObject.AddComponent<Outline>();
            _outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("T");
            textObj.transform.SetParent(this.transform, false);
            _textComponent = textObj.AddComponent<Text>();
            _textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _textComponent.fontSize = 20;
            _textComponent.fontStyle = FontStyle.Bold;
            _textComponent.alignment = TextAnchor.MiddleLeft;
            _textComponent.color = Color.white;
            _textComponent.supportRichText = true;

            RectTransform tRect = _textComponent.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.anchoredPosition = new Vector2(20, 0);
            tRect.sizeDelta = new Vector2(-40, 0);
        }

        public void Show(string msg)
        {
            _textComponent.text = msg;
            _timer = 0f;
            _isActive = true;
            if (msg.Contains("ON")) _outline.effectColor = new Color(0f, 1f, 0.5f, 0.5f);
            else if (msg.Contains("OFF")) _outline.effectColor = new Color(1f, 0.2f, 0.2f, 0.5f);
            else _outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        }

        void Update()
        {
            if (!_isActive && _canvasGroup.alpha < 0.01f) return;
            _timer += Time.unscaledDeltaTime;

            Vector2 targetPos;
            float targetWidth;
            float targetAlpha;

            if (_timer < Duration)
            {
                targetPos = new Vector2(PosX, PosY);
                targetWidth = Mathf.Max(_textComponent.preferredWidth + 60f, 180f);
                targetAlpha = 1f;
            }
            else
            {
                targetPos = new Vector2(PosX, -100f);
                targetWidth = 40f;
                targetAlpha = 0f;
                _isActive = false;
            }

            float step = Time.unscaledDeltaTime * AnimSpeed;
            _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, targetPos, step);
            _rectTransform.sizeDelta = new Vector2(Mathf.Lerp(_rectTransform.sizeDelta.x, targetWidth, step), 50f);
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, step);

            if (_rectTransform.sizeDelta.x < targetWidth * 0.7f) _textComponent.color = Color.clear;
            else _textComponent.color = Color.Lerp(_textComponent.color, Color.white, step);
        }
    }
}
