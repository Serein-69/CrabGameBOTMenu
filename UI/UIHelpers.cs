using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamworksNative;
using UnhollowerRuntimeLib;

namespace BOTMenuMod
{
    public static class MenuSoundManager
    {
        private static AudioSource _source;
        private static AudioClip _clickClip;

        public static void Init(Transform parent)
        {
            if (_source != null) return;
            GameObject audioObj = new GameObject("BOT_AudioSource");
            audioObj.transform.SetParent(parent);
            _source = audioObj.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.volume = 0.6f;

            var allObjects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
            foreach (var obj in allObjects)
            {
                var clip = obj.TryCast<AudioClip>();
                if (clip == null) continue;
                string name = clip.name.ToLower();
                if (name.Contains("click") || name.Contains("ui_hover") || name.Contains("button"))
                {
                    _clickClip = clip;
                    break;
                }
            }
        }

        public static void PlayClick()
        {
            if (_source != null && _clickClip != null)
            {
                _source.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                _source.PlayOneShot(_clickClip);
            }
        }
    }

    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance;
        public static bool EnableZoom = false;
        public static KeyCode ZoomKey = KeyCode.C;
        public static float ZoomFOV = 20f;
        public static float ZoomSpeed = 15f;
        public static bool OverrideFOV = false;
        public static float BaseFOV = 100f;
        private float _currentVisualFov = 90f;
        private bool _isModifying = false;
        private Camera _mainCam;

        public CameraManager(IntPtr ptr) : base(ptr) { }
        void Awake() { Instance = this; }

        void LateUpdate()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu")
            {
                if (_mainCam.fieldOfView != 60f) _mainCam.fieldOfView = 60f;
                return;
            }

            if (Utility.IsChatOpen() && !_isModifying) return;

            float targetFov = 90f;
            bool active = false;

            if (EnableZoom && ZoomKey != KeyCode.None && Input.GetKey(ZoomKey))
            {
                targetFov = ZoomFOV;
                active = true;
            }
            else if (OverrideFOV)
            {
                targetFov = BaseFOV;
                active = true;
            }

            if (active)
            {
                _isModifying = true;
                _currentVisualFov = Mathf.Lerp(_currentVisualFov, targetFov, Time.unscaledDeltaTime * ZoomSpeed);
                _mainCam.fieldOfView = _currentVisualFov;
            }
            else if (_isModifying)
            {
                _currentVisualFov = Mathf.Lerp(_currentVisualFov, BaseFOV, Time.unscaledDeltaTime * ZoomSpeed);
                _mainCam.fieldOfView = _currentVisualFov;
                if (Mathf.Abs(_currentVisualFov - BaseFOV) < 0.5f) _isModifying = false;
            }
        }
    }

    public static class KeyDisplayManager
    {
        public static bool enabled = false;
        public static float posX = 50f;
        public static float posY = 150f;
        private static GameObject _container;
        private static RectTransform _containerRect;
        private static Dictionary<KeyCode, Image> _keyImages = new Dictionary<KeyCode, Image>();
        private static Dictionary<KeyCode, Text> _keyTexts = new Dictionary<KeyCode, Text>();

        public static void Update()
        {
            if (!enabled)
            {
                if (_container != null && _container.activeSelf) _container.SetActive(false);
                return;
            }
            if (_container == null)
            {
                if (!CreateUI()) return;
            }
            if (_container == null) return;
            if (!_container.activeSelf) _container.SetActive(true);
            if (_containerRect != null) _containerRect.anchoredPosition = new Vector2(posX, posY);

            UpdateKeyState(KeyCode.W); UpdateKeyState(KeyCode.A); UpdateKeyState(KeyCode.S);
            UpdateKeyState(KeyCode.D); UpdateKeyState(KeyCode.Space);
            UpdateKeyState(KeyCode.Mouse0); UpdateKeyState(KeyCode.Mouse1);
        }

        private static void UpdateKeyState(KeyCode key)
        {
            if (!_keyImages.ContainsKey(key)) return;
            bool isDown = Input.GetKey(key);
            _keyImages[key].color = isDown ? new Color(0, 1, 1, 0.9f) : new Color(0.05f, 0.05f, 0.05f, 0.8f);
            _keyTexts[key].color = isDown ? Color.black : Color.white;
        }

        private static bool CreateUI()
        {
            var canvas = GameObject.Find("BOTMenu_UI")?.transform.parent;
            if (canvas == null) return false;
            _container = new GameObject("BOT_KeyDisplay");
            _container.transform.SetParent(canvas, false);
            _containerRect = _container.AddComponent<RectTransform>();
            _containerRect.anchorMin = _containerRect.anchorMax = _containerRect.pivot = Vector2.zero;
            _containerRect.anchoredPosition = new Vector2(posX, posY);
            _containerRect.sizeDelta = new Vector2(160, 180);

            float size = 50f; float gap = 5f; float totalWidth = (size * 3) + (gap * 2);
            CreateKey(KeyCode.W, "W", new Vector2(size + gap, (size + gap) * 2), new Vector2(size, size));
            CreateKey(KeyCode.A, "A", new Vector2(0, size + gap), new Vector2(size, size));
            CreateKey(KeyCode.S, "S", new Vector2(size + gap, size + gap), new Vector2(size, size));
            CreateKey(KeyCode.D, "D", new Vector2((size + gap) * 2, size + gap), new Vector2(size, size));
            CreateKey(KeyCode.Space, "SPACE", new Vector2(55, 0), new Vector2(totalWidth, 30f));
            CreateKey(KeyCode.Mouse0, "LMB", new Vector2(55f, -35f), new Vector2((totalWidth - gap) / 2f, 30f));
            CreateKey(KeyCode.Mouse1, "RMB", new Vector2(55f + (totalWidth - gap) / 2f + gap, -35f), new Vector2((totalWidth - gap) / 2f, 30f));
            return _container != null;
        }

        private static void CreateKey(KeyCode key, string label, Vector2 pos, Vector2 boxSize)
        {
            GameObject kObj = new GameObject(key.ToString());
            kObj.transform.SetParent(_container.transform, false);
            RectTransform rt = kObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;
            Image img = kObj.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            GameObject txtObj = new GameObject("T");
            txtObj.transform.SetParent(kObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = label; t.fontSize = (key == KeyCode.Space) ? 12 : 20;
            t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            _keyImages[key] = img; _keyTexts[key] = t;
        }
    }

    public static class DamageNotificationManager
    {
        public static bool enabled = false;
        private static GameObject _uiContainer;
        private static RectTransform _containerRect;
        private class DamageNote
        {
            public GameObject obj; public RectTransform rect; public CanvasGroup canvasGroup;
            public Text damageTextComponent; public string targetName; public float totalDamage; public float spawnTime;
        }
        private static List<DamageNote> _activeNotes = new List<DamageNote>();

        public static void Update()
        {
            if (!enabled)
            {
                if (_uiContainer != null) UnityEngine.Object.Destroy(_uiContainer);
                _activeNotes.Clear();
                return;
            }
            if (_uiContainer == null) CreateContainer();
            if (_containerRect != null)
            {
                float dynamicOffsetY = BOTActiveList.IsVisible ? BOTActiveList.CurrentHeight + 80f : 80f;
                _containerRect.anchoredPosition = Vector2.Lerp(_containerRect.anchoredPosition, new Vector2(-15f, -dynamicOffsetY), Time.deltaTime * 5f);
            }

            float currentTime = Time.time;
            List<DamageNote> toRemove = new List<DamageNote>();
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (note.obj == null) continue;
                float elapsed = currentTime - note.spawnTime;
                float targetY = -i * 45f;
                Vector2 targetPos = new Vector2(0, targetY);

                if (elapsed < 0.2f)
                {
                    float t = elapsed / 0.2f;
                    note.rect.anchoredPosition = Vector2.Lerp(new Vector2(250f, targetY), targetPos, 1f - Mathf.Pow(1f - t, 3f));
                }
                else
                {
                    note.rect.anchoredPosition = Vector2.Lerp(note.rect.anchoredPosition, targetPos, Time.deltaTime * 15f);
                }

                if (elapsed > 2.5f)
                {
                    float fadeProgress = (elapsed - 2.5f) / 0.5f;
                    if (fadeProgress >= 1f) toRemove.Add(note);
                    else note.canvasGroup.alpha = 1f - fadeProgress;
                }
            }
            foreach (var note in toRemove) { if (note.obj != null) UnityEngine.Object.Destroy(note.obj); _activeNotes.Remove(note); }
        }

        public static void AddDamageLog(string targetName, float damage)
        {
            if (!enabled || damage == 0f) return;
            if (_uiContainer == null)
            {
                CreateContainer();
                if (_uiContainer == null) return;
            }

            foreach (var existingNote in _activeNotes)
            {
                if (existingNote.targetName == targetName && (Time.time - existingNote.spawnTime < 2.5f))
                {
                    existingNote.totalDamage += damage;
                    existingNote.spawnTime = Time.time;
                    existingNote.damageTextComponent.text = $"<color=#FFFFFF>-{Mathf.Abs(existingNote.totalDamage):F0}</color>";
                    existingNote.rect.localScale = new Vector3(1.1f, 1.1f, 1f);
                    return;
                }
            }

            if (_activeNotes.Count >= 4)
            {
                if (_activeNotes[_activeNotes.Count - 1].obj != null) UnityEngine.Object.Destroy(_activeNotes[_activeNotes.Count - 1].obj);
                _activeNotes.RemoveAt(_activeNotes.Count - 1);
            }

            GameObject noteObj = new GameObject("DamageNote");
            noteObj.transform.SetParent(_uiContainer.transform, false);
            RectTransform rect = noteObj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(220, 40);
            rect.anchoredPosition = new Vector2(250, 0);

            CanvasGroup cg = noteObj.AddComponent<CanvasGroup>();
            Image bg = noteObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

            GameObject leftObj = new GameObject("NameText");
            leftObj.transform.SetParent(noteObj.transform, false);
            RectTransform leftRect = leftObj.AddComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero; leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = new Vector2(15, 0); leftRect.offsetMax = new Vector2(-70, 0);
            var leftTxt = leftObj.AddComponent<Text>();
            leftTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            leftTxt.text = $"<color=#BBBBBB>{targetName}</color>";
            leftTxt.fontSize = 16; leftTxt.fontStyle = FontStyle.Bold; leftTxt.alignment = TextAnchor.MiddleLeft;

            GameObject rightObj = new GameObject("DamageText");
            rightObj.transform.SetParent(noteObj.transform, false);
            RectTransform rightRect = rightObj.AddComponent<RectTransform>();
            rightRect.anchorMin = Vector2.zero; rightRect.anchorMax = Vector2.one;
            rightRect.offsetMin = new Vector2(150, 0); rightRect.offsetMax = new Vector2(-15, 0);
            var rightTxt = rightObj.AddComponent<Text>();
            rightTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            rightTxt.text = $"<color=#FFFFFF>-{Mathf.Abs(damage):F0}</color>";
            rightTxt.fontSize = 18; rightTxt.fontStyle = FontStyle.Bold; rightTxt.alignment = TextAnchor.MiddleRight;

            _activeNotes.Insert(0, new DamageNote { obj = noteObj, rect = rect, canvasGroup = cg, damageTextComponent = rightTxt, targetName = targetName, totalDamage = damage, spawnTime = Time.time });
        }

        private static void CreateContainer()
        {
            var canvas = GameObject.Find("BOTMenu_UI")?.transform.parent?.gameObject ?? GameObject.Find("GameUI") ?? GameObject.Find("Canvas");
            if (canvas == null) return;
            _uiContainer = new GameObject("BOT_DamageLogContainer");
            _uiContainer.transform.SetParent(canvas.transform, false);
            _containerRect = _uiContainer.AddComponent<RectTransform>();
            _containerRect.anchorMin = _containerRect.anchorMax = _containerRect.pivot = new Vector2(1f, 1f);
            _containerRect.anchoredPosition = new Vector2(-15f, -80f);
            _containerRect.sizeDelta = new Vector2(300, 300);
        }
    }

    public static class HitEffectManager
    {
        public static bool enabled = false;
        private static GameObject _heartPrefab;
        private class ActiveHeart { public GameObject obj; public float timer; }
        private static List<ActiveHeart> _hearts = new List<ActiveHeart>();

        public static void Update()
        {
            if (!enabled)
            {
                if (_hearts.Count > 0) { foreach (var h in _hearts) if (h.obj != null) UnityEngine.Object.Destroy(h.obj); _hearts.Clear(); }
                return;
            }
            float dt = Time.deltaTime;
            for (int i = _hearts.Count - 1; i >= 0; i--)
            {
                var h = _hearts[i];
                if (h.obj == null) { _hearts.RemoveAt(i); continue; }
                h.timer -= dt;
                h.obj.transform.position += Vector3.up * 1.5f * dt;
                if (Camera.main != null) { h.obj.transform.LookAt(Camera.main.transform); h.obj.transform.Rotate(0, 180, 0); }
                float scale = h.timer > 0.8f ? Mathf.Lerp(0.8f, 0f, (h.timer - 0.8f) / 0.2f) : Mathf.Lerp(0f, 0.8f, h.timer / 0.8f);
                h.obj.transform.localScale = new Vector3(scale, scale, scale);
                if (h.timer <= 0) { UnityEngine.Object.Destroy(h.obj); _hearts.RemoveAt(i); }
            }
        }

        public static void SpawnHeart(ulong targetId)
        {
            if (!enabled || GameManager.Instance == null || !GameManager.Instance.activePlayers.ContainsKey(targetId)) return;
            var target = GameManager.Instance.activePlayers[targetId];
            if (target == null) return;
            if (_heartPrefab == null) CreateHeartPrefab();

            Vector3 spawnPos = target.transform.position + Vector3.up * 1.4f + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0, 0.5f), UnityEngine.Random.Range(-0.4f, 0.4f));
            GameObject inst = UnityEngine.Object.Instantiate(_heartPrefab, spawnPos, Quaternion.identity);
            inst.SetActive(true);
            _hearts.Add(new ActiveHeart { obj = inst, timer = 1.0f });
        }

        private static void CreateHeartPrefab()
        {
            _heartPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _heartPrefab.name = "BOT_HitHeart";
            UnityEngine.Object.Destroy(_heartPrefab.GetComponent<Collider>());
            _heartPrefab.SetActive(false);
            var rend = _heartPrefab.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Sprites/Default"));

            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = (x / (float)size) * 2f - 1f;
                    float fy = (y / (float)size) * 2f - 1f;
                    pixels[y * size + x] = (Mathf.Pow(fx * fx + fy * fy - 1f, 3f) - (fx * fx * fy * fy * fy) <= 0f) ? new Color(1f, 0.2f, 0.6f, 1f) : Color.clear;
                }
            tex.SetPixels(pixels); tex.Apply(); rend.material.mainTexture = tex;
        }
    }

    public class SpectatorWarningManager : MonoBehaviour
    {
        private List<string> _spectators = new List<string>();
        private float _timer = 0f;
        private GUIStyle _warnStyle;
        private GUIStyle _warnShadowStyle;
        private string _warnText = string.Empty;
        private string _warnShadowText = string.Empty;
        public SpectatorWarningManager(IntPtr ptr) : base(ptr) { }

        void Update()
        {

            _timer += Time.deltaTime;
            if (_timer < 0.5f) return;
            _timer = 0f;

            _spectators.Clear();
            _warnText = string.Empty;
            _warnShadowText = string.Empty;

            if (GameManager.Instance == null || GameManager.Instance.spectators == null) return;
            if (SteamManager.Instance == null) return;

            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;

            var me = GameManager.Instance.activePlayers[myId];
            if (me == null || me.dead) return;

            foreach (var kvp in GameManager.Instance.spectators)
            {
                var spec = kvp.Value;
                if (spec == null) continue;

                float dist = Vector3.Distance(spec.transform.position, me.transform.position);
                if (dist < 8.0f)
                {
                    string name = SteamFriends.GetFriendPersonaName(new CSteamID(kvp.Key));
                    _spectators.Add(Utility.StripColorTags(name));
                }
            }

            if (_spectators.Count > 0)
            {
                _warnText = $"<color=#FF3333>⚠️ [WARNING] {_spectators.Count} PLAYER(S) SPECTATING YOU!</color>\n";
                foreach (var s in _spectators) _warnText += $"<color=#FFCC00>👀 {s}</color>\n";
                _warnShadowText = _warnText.Replace("#FF3333", "#000000").Replace("#FFCC00", "#000000");
            }
        }

        void OnGUI()
        {

            if (_spectators.Count > 0)
            {
                if (_warnStyle == null)
                {
                    _warnStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 24,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true,
                        richText = true,
                        alignment = TextAnchor.UpperCenter
                    };
                    _warnShadowStyle = new GUIStyle(_warnStyle);
                }
                float width = 600f;
                float height = 200f;
                float x = (Screen.width - width) / 2f;
                float y = 90f;

                GUI.Label(new Rect(x + 2, y + 2, width, height), _warnShadowText, _warnShadowStyle);

                GUI.Label(new Rect(x, y, width, height), _warnText, _warnStyle);
            }
        }
    }

    public class ClickRippleManager : MonoBehaviour
    {
        public static bool IsActive = true;
        public ClickRippleManager(IntPtr ptr) : base(ptr) { }
        private class Ripple { public Vector2 Position; public float Radius; public float Alpha; }
        private List<Ripple> _ripples = new List<Ripple>();

        void Update()
        {
            if (!IsActive) return;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                _ripples.Add(new Ripple { Position = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), Radius = 5f, Alpha = 0.6f });
        }

        void OnGUI()
        {
            if (!IsActive || _ripples.Count == 0) return;
            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                var r = _ripples[i];
                r.Radius += Time.deltaTime * 150f; r.Alpha -= Time.deltaTime * 1.5f;
                if (r.Alpha <= 0) { _ripples.RemoveAt(i); continue; }
                FOVCircleDrawer.DrawCircle(r.Position, r.Radius, new Color(1f, 1f, 1f, r.Alpha), 2f);
            }
        }
    }

    public class GalaxyBackground : MonoBehaviour
    {
        public static GalaxyBackground Instance;
        public static new bool enabled = true;

        private const int STAR_COUNT = 100;
        private const float ROTATION_SPEED = 5f;
        private const float REPULSION_FORCE = 50f;

        private List<StarParticle> _stars = new List<StarParticle>();
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Image _bgImage;

        private class StarParticle
        {
            public GameObject obj;
            public RectTransform rect;
            public Image img;
            public Vector2 basePos;
            public Vector2 currentPos;
            public float speed;
            public float twinkleOffset;
            public Color color;
        }

        public GalaxyBackground(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.sizeDelta = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;

            _bgImage = gameObject.AddComponent<Image>();
            _bgImage.color = Color.clear;

            GenerateGalaxy();
        }

        private void GenerateGalaxy()
        {
            float width = 900f;

            for (int i = 0; i < STAR_COUNT; i++)
            {
                GameObject starObj = new GameObject($"Star_{i}");
                starObj.transform.SetParent(transform, false);

                RectTransform rect = starObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);

                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = UnityEngine.Random.Range(20f, width * 0.55f);
                Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                rect.anchoredPosition = pos;

                float size = UnityEngine.Random.Range(2f, 5f);
                rect.sizeDelta = new Vector2(size, size);

                Image img = starObj.AddComponent<Image>();

                Color starColor;
                float colorSeed = UnityEngine.Random.value;
                if (colorSeed > 0.7f) starColor = new Color(0.6f, 0.8f, 1f, 0.8f);
                else if (colorSeed > 0.4f) starColor = new Color(1f, 0.9f, 0.6f, 0.8f);
                else starColor = new Color(1f, 1f, 1f, 0.9f);

                img.color = starColor;

                _stars.Add(new StarParticle
                {
                    obj = starObj,
                    rect = rect,
                    img = img,
                    basePos = pos,
                    currentPos = pos,
                    speed = UnityEngine.Random.Range(0.5f, 2.0f),
                    twinkleOffset = UnityEngine.Random.Range(0f, 100f),
                    color = starColor
                });
            }
        }

        void Update()
        {
            if (!Variables.menuTrigger) return;

            if (!enabled)
            {
                if (_bgImage != null && _bgImage.enabled) _bgImage.enabled = false;
                if (_stars.Count > 0 && _stars[0].obj.activeSelf)
                {
                    foreach (var s in _stars) if (s.obj != null) s.obj.SetActive(false);
                }
                return;
            }

            if (_bgImage != null && !_bgImage.enabled) _bgImage.enabled = true;
            if (_stars.Count > 0 && !_stars[0].obj.activeSelf)
            {
                foreach (var s in _stars) if (s.obj != null) s.obj.SetActive(true);
            }

            float time = Time.unscaledTime;
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, Input.mousePosition, null, out mousePos);

            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                if (star == null || star.obj == null) continue;

                float rotAngle = time * ROTATION_SPEED * 0.02f * star.speed;
                float cos = Mathf.Cos(rotAngle);
                float sin = Mathf.Sin(rotAngle);
                Vector2 rotatedPos = new Vector2(
                    star.basePos.x * cos - star.basePos.y * sin,
                    star.basePos.x * sin + star.basePos.y * cos
                );

                Vector2 dirToMouse = rotatedPos - mousePos;
                float distToMouse = dirToMouse.magnitude;
                Vector2 offset = Vector2.zero;

                if (distToMouse < 100f && distToMouse > 0.1f)
                {
                    float factor = (1f - (distToMouse / 100f));
                    offset = dirToMouse.normalized * (factor * REPULSION_FORCE);
                }

                star.currentPos = Vector2.Lerp(star.currentPos, rotatedPos + offset, Time.unscaledDeltaTime * 6f);
                star.rect.anchoredPosition = star.currentPos;

                float alpha = Mathf.PingPong(time * star.speed + star.twinkleOffset, 1f);
                alpha = Mathf.Clamp(alpha, 0.2f, 1f);
                star.img.color = new Color(star.color.r, star.color.g, star.color.b, alpha);
            }
        }
    }
}
