using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTMenuMod
{
    public abstract class BOTSetting
    {
        public string Name;
        public string Description;
        public abstract void Draw();
    }

    public class BOTSlider : BOTSetting
    {
        public float Value;
        public float Min;
        public float Max;
        public string Format;
        public Action<float> OnChanged;

        public BOTSlider(string name, string desc, float val, float min, float max, string format, Action<float> onChanged)
        {
            Name = name; Description = desc; Value = val; Min = min; Max = max; Format = format; OnChanged = onChanged;
        }

        public override void Draw()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{Name}</b>", BOTUIStyles.SettingTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format(Format, Value), BOTUIStyles.SettingValueStyle);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(Description))
                GUILayout.Label(Description, BOTUIStyles.SettingDescStyle);

            float newVal = GUILayout.HorizontalSlider(Value, Min, Max, BOTUIStyles.SliderStyle, BOTUIStyles.SliderThumbStyle);
            if (Mathf.Abs(newVal - Value) > 0.0001f)
            {
                Value = newVal;
                OnChanged?.Invoke(Value);
                if (Name.Contains("透明度") || Name.Contains("Opacity") || Name.Contains("Alpha"))
                    BOTUIStyles.RebuildTextures();
            }
            GUILayout.Space(6);
            GUILayout.EndVertical();
        }
    }

    public class BOTToggle : BOTSetting
    {
        public bool State;
        public Action<bool> OnChanged;

        public BOTToggle(string name, string desc, bool state, Action<bool> onChanged)
        {
            Name = name; Description = desc; State = state; OnChanged = onChanged;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{Name}</b>", BOTUIStyles.SettingTitleStyle);
            if (!string.IsNullOrEmpty(Description))
                GUILayout.Label(Description, BOTUIStyles.SettingDescStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.color = State ? BOTUIStyles.CurrentAccentColor : new Color(0.2f, 0.2f, 0.25f);
            if (GUILayout.Button(State ? "ON" : "OFF", BOTUIStyles.SmallBtnStyle, GUILayout.Width(50), GUILayout.Height(24)))
            {
                State = !State;
                OnChanged?.Invoke(State);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }
    }

    public class BOTDropdown : BOTSetting
    {
        public string[] Options;
        public int SelectedIndex;
        public Action<int> OnChanged;

        public BOTDropdown(string name, string desc, string[] options, int selectedIndex, Action<int> onChanged)
        {
            Name = name; Description = desc; Options = options; SelectedIndex = selectedIndex; OnChanged = onChanged;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{Name}</b>", BOTUIStyles.SettingTitleStyle);
            if (!string.IsNullOrEmpty(Description))
                GUILayout.Label(Description, BOTUIStyles.SettingDescStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            string currentText = (SelectedIndex >= 0 && SelectedIndex < Options.Length) ? Options[SelectedIndex] : "Select";
            if (GUILayout.Button($"{currentText}  ↕", BOTUIStyles.DropdownBtnStyle, GUILayout.Width(140), GUILayout.Height(26)))
            {
                SelectedIndex = (SelectedIndex + 1) % Options.Length;
                OnChanged?.Invoke(SelectedIndex);
                if (Name.Contains("强调色") || Name.Contains("Theme") || Name.Contains("Style"))
                    BOTUIStyles.RebuildTextures();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }
    }

    public class BOTActionBtn : BOTSetting
    {
        public string BtnText;
        public Action OnClick;

        public BOTActionBtn(string name, string desc, string btnText, Action onClick)
        {
            Name = name; Description = desc; BtnText = btnText; OnClick = onClick;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{Name}</b>", BOTUIStyles.SettingTitleStyle);
            if (!string.IsNullOrEmpty(Description))
                GUILayout.Label(Description, BOTUIStyles.SettingDescStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(BtnText, BOTUIStyles.ActionBtnStyle, GUILayout.Width(140), GUILayout.Height(26)))
            {
                OnClick?.Invoke();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }
    }

    public class BOTModule
    {
        public string ID;
        public string Title;
        public string Description;
        public string Category;
        public Func<bool> GetState;
        public Action<bool> SetState;
        public Action DirectAction;
        public KeyCode BoundKey = KeyCode.None;
        public bool IsBinding = false;
        public bool IsExpanded = false;
        public List<BOTSetting> Settings = new List<BOTSetting>();

        public BOTModule(string id, string title, string desc, string category, Func<bool> getState, Action<bool> setState)
        {
            ID = id; Title = title; Description = desc; Category = category; GetState = getState; SetState = setState;
        }

        public BOTModule(string id, string title, string desc, string category, Action directAction)
        {
            ID = id; Title = title; Description = desc; Category = category; DirectAction = directAction;
        }

        public BOTModule AddSetting(BOTSetting setting)
        {
            Settings.Add(setting);
            return this;
        }
    }

    public class MenuConstants
    {
        public static string HEADER => "";
        public static string SELECTED_PREFIX => "";
        public static string EDITING_PREFIX => "";
        public const string SELECTED_SUFFIX = "";
        public const string UNSELECTED_PREFIX = "";
        public const string UNSELECTED_SUFFIX = "";
        public const string SEPARATOR = "<color=#333333>──────────────────────</color>";
    }

    public class MenuButton
    {
        public string ID; public string Label; public Action Action; public Func<string> Status;
        public KeyCode BoundKey = KeyCode.None; public bool IsBinding = false; public bool IsMenuKeyBind = false;
        public bool IsScrollable; public int ScrollStep = 1;
        public Action<int> Setter; public Func<int> Getter; public Func<int> ScrollMin; public Func<int> ScrollMax;
        public List<MenuButton> Children = new List<MenuButton>();
        public bool IsExpanded = false; public bool IsChild = false;

        public MenuButton(string id, string label, Action action = null, Func<string> status = null, bool isMenuKeyBind = false)
        {
            ID = id; Label = label; Action = action; Status = status; IsMenuKeyBind = isMenuKeyBind;
        }
        public MenuButton(string id, string label, bool isScrollable, Action<int> setter, Func<int> getter, Func<int> scrollMin, Func<int> scrollMax, int step = 1)
        {
            ID = id; Label = label; IsScrollable = isScrollable; Setter = setter; Getter = getter; ScrollMin = scrollMin; ScrollMax = scrollMax; ScrollStep = step;
        }
        public void AdjustScrollValue(int direction)
        {
            if (IsScrollable && Getter != null && Setter != null)
                Setter(Mathf.Clamp(Getter() + (direction * ScrollStep), ScrollMin(), ScrollMax()));
        }
        public string GetFormattedLabel(bool isSelected) => Label;
    }

    public class MenuCategory
    {
        public string Name;
        public List<MenuButton> Buttons = new List<MenuButton>();
    }

    public class Draggable : MonoBehaviour
    {
        public Draggable(IntPtr ptr) : base(ptr) { }
    }

    public static class BOTUIStyles
    {
        public static Color CurrentAccentColor = new Color(0.16f, 0.47f, 1.0f);

        public static Texture2D MainBgRoundedTex;
        public static Texture2D SidebarRoundedTex;
        public static Texture2D UserCardTex;
        public static Texture2D CardNormalBgTex;
        public static Texture2D CardActiveBgTex;
        public static Texture2D TrayBgTex;
        public static Texture2D ActivePillTex;
        public static Texture2D InactivePillTex;
        public static Texture2D SearchBoxTex;
        public static Texture2D CheckedCircleTex;
        public static Texture2D UncheckedCircleTex;
        public static Texture2D ActiveNavPillTex;

        public static GUIStyle MainBgStyle;
        public static GUIStyle SidebarStyle;
        public static GUIStyle UserCardStyle;
        public static GUIStyle CardNormalStyle;
        public static GUIStyle CardActiveStyle;
        public static GUIStyle TrayStyle;
        public static GUIStyle ActiveNavPillStyle;
        public static GUIStyle SearchBoxStyle;
        public static GUIStyle ActivePillBoxStyle;
        public static GUIStyle InactivePillBoxStyle;
        public static GUIStyle ChatMsgStyle;

        public static GUIStyle LabelStyle;
        public static GUIStyle LogoTitleStyle;
        public static GUIStyle NavTextStyle;
        public static GUIStyle ActiveNavTextStyle;
        public static GUIStyle ActivePillTextStyle;
        public static GUIStyle InactivePillTextStyle;
        public static GUIStyle SearchTextStyle;
        public static GUIStyle SearchPlaceholderStyle;
        public static GUIStyle CardTitleStyle;
        public static GUIStyle CardDescStyle;
        public static GUIStyle DotsBtnStyle;
        public static GUIStyle IconBtnStyle;
        public static GUIStyle KeybindBtnStyle;
        public static GUIStyle SmallSubBtnStyle;
        public static GUIStyle ValueRightStyle;
        public static GUIStyle SettingTitleStyle;
        public static GUIStyle SettingDescStyle;
        public static GUIStyle SettingValueStyle;
        public static GUIStyle SliderStyle;
        public static GUIStyle SliderThumbStyle;
        public static GUIStyle SmallBtnStyle;
        public static GUIStyle DropdownBtnStyle;
        public static GUIStyle ActionBtnStyle;

        public static bool _initialized = false;
        public static Texture2D ChatBoxBgTex;

        public static void EnsureTextures()
        {
            if (MainBgRoundedTex == null || !_initialized)
            {
                Init();
            }
        }

        public static void Init()
        {
            _initialized = true;
            RebuildTextures();

            LabelStyle = new GUIStyle { fontSize = 12, normal = { textColor = Color.white } };
            LogoTitleStyle = new GUIStyle { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

            NavTextStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 12, padding = new RectOffset { left = 14 }, normal = { background = null, textColor = new Color(0.6f, 0.6f, 0.68f) } };
            ActiveNavTextStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 12, fontStyle = FontStyle.Bold, padding = new RectOffset { left = 14 }, normal = { background = null, textColor = Color.white } };

            ActivePillTextStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            InactivePillTextStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = new Color(0.65f, 0.65f, 0.72f) } };

            SearchTextStyle = new GUIStyle(GUI.skin.textField) { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { background = null, textColor = Color.white } };
            SearchPlaceholderStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.45f, 0.45f, 0.52f, 0.85f) } };

            CardTitleStyle = new GUIStyle { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            CardDescStyle = new GUIStyle { fontSize = 10, normal = { textColor = new Color(0.5f, 0.5f, 0.58f) } };
            DotsBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { background = null, textColor = new Color(0.6f, 0.6f, 0.7f) } };
            IconBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, normal = { background = null, textColor = new Color(0.6f, 0.6f, 0.7f) } };

            KeybindBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            SmallSubBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.3f, 0.8f, 1f) } };
            ValueRightStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = CurrentAccentColor } };

            SettingTitleStyle = new GUIStyle { fontSize = 12, normal = { textColor = Color.white } };
            SettingDescStyle = new GUIStyle { fontSize = 10, normal = { textColor = new Color(0.5f, 0.5f, 0.55f) } };
            SettingValueStyle = new GUIStyle { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };

            SliderStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 4 };
            SliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 14, fixedHeight = 14 };

            SmallBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            DropdownBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, normal = { textColor = Color.white } };
            ActionBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.3f, 0.8f, 1f) } };
        }

        private static void ReplaceTex(ref Texture2D target, Texture2D value)
        {
            if (target != null && !ReferenceEquals(target, value)) UnityEngine.Object.Destroy(target);
            target = value;
        }

        public static void RebuildTextures()
        {
            if (ColorUtility.TryParseHtmlString(MenuThemeManager.CurrentAccentHex, out Color parsedAccent))
                CurrentAccentColor = parsedAccent;
            else
                CurrentAccentColor = new Color(0.16f, 0.47f, 1.0f);

            float alpha = MenuThemeManager.BgAlpha;
            Color baseTint = MenuThemeManager.BackgroundTints[Mathf.Clamp(MenuThemeManager.ThemeIndex, 0, MenuThemeManager.BackgroundTints.Length - 1)];
            Color mainBgCol = new Color(baseTint.r * 0.35f + 0.04f, baseTint.g * 0.35f + 0.04f, baseTint.b * 0.35f + 0.05f, alpha);
            Color sidebarCol = new Color(baseTint.r * 0.25f + 0.02f, baseTint.g * 0.25f + 0.02f, baseTint.b * 0.25f + 0.03f, alpha);

            ReplaceTex(ref MainBgRoundedTex, Create9SliceRoundedTex(48, 12, mainBgCol, new Color(CurrentAccentColor.r, CurrentAccentColor.g, CurrentAccentColor.b, 0.35f), 1));
            ReplaceTex(ref SidebarRoundedTex, Create9SliceRoundedTex(48, 12, sidebarCol, Color.clear, 0));
            ReplaceTex(ref UserCardTex, Create9SliceRoundedTex(48, 8, new Color(0.08f, 0.08f, 0.10f, 0.95f), new Color(CurrentAccentColor.r, CurrentAccentColor.g, CurrentAccentColor.b, 0.4f), 1));

            ReplaceTex(ref CardNormalBgTex, Create9SliceRoundedTex(48, 8, new Color(0.09f, 0.09f, 0.11f, 0.95f), new Color(0.18f, 0.18f, 0.22f, 0.6f), 1));
            ReplaceTex(ref CardActiveBgTex, Create9SliceRoundedTex(48, 8, new Color(CurrentAccentColor.r * 0.15f + 0.08f, CurrentAccentColor.g * 0.15f + 0.08f, CurrentAccentColor.b * 0.15f + 0.12f, 0.98f), CurrentAccentColor, 1));
            ReplaceTex(ref TrayBgTex, Create9SliceRoundedTex(48, 6, new Color(0.05f, 0.05f, 0.06f, 0.98f), new Color(CurrentAccentColor.r, CurrentAccentColor.g, CurrentAccentColor.b, 0.3f), 1));

            ReplaceTex(ref ActivePillTex, Create9SliceRoundedTex(32, 6, CurrentAccentColor, Color.clear, 0));
            ReplaceTex(ref InactivePillTex, Create9SliceRoundedTex(32, 6, new Color(0.12f, 0.12f, 0.16f, 0.85f), Color.clear, 0));
            ReplaceTex(ref ActiveNavPillTex, Create9SliceRoundedTex(48, 8, new Color(CurrentAccentColor.r, CurrentAccentColor.g, CurrentAccentColor.b, 0.25f), CurrentAccentColor, 1));
            ReplaceTex(ref SearchBoxTex, Create9SliceRoundedTex(48, 8, new Color(0.11f, 0.11f, 0.14f, 0.9f), new Color(0.25f, 0.25f, 0.30f, 0.6f), 1));

            ReplaceTex(ref CheckedCircleTex, CreateCircleCheckTex(CurrentAccentColor, true));
            ReplaceTex(ref UncheckedCircleTex, CreateCircleCheckTex(new Color(0.3f, 0.3f, 0.35f), false));

            ReplaceTex(ref ChatBoxBgTex, CreateExactRoundedTex(460, 450, 8, new Color(0.08f, 0.08f, 0.10f, 0.95f), new Color(0.18f, 0.18f, 0.22f, 0.6f), 1, false));

            MainBgStyle = new GUIStyle { normal = { background = MainBgRoundedTex }, border = new RectOffset { left = 14, right = 14, top = 14, bottom = 14 } };
            SidebarStyle = new GUIStyle { normal = { background = SidebarRoundedTex }, border = new RectOffset { left = 14, right = 14, top = 14, bottom = 14 } };
            UserCardStyle = new GUIStyle { normal = { background = UserCardTex }, border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 } };
            CardNormalStyle = new GUIStyle { normal = { background = CardNormalBgTex }, border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 } };
            CardActiveStyle = new GUIStyle { normal = { background = CardActiveBgTex }, border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 } };
            TrayStyle = new GUIStyle { normal = { background = TrayBgTex }, border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 } };
            ActiveNavPillStyle = new GUIStyle { normal = { background = ActiveNavPillTex }, border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 } };
            SearchBoxStyle = new GUIStyle { normal = { background = SearchBoxTex }, border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 } };
            ActivePillBoxStyle = new GUIStyle { normal = { background = ActivePillTex }, border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 } };
            InactivePillBoxStyle = new GUIStyle { normal = { background = InactivePillTex }, border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 } };

            if (KeybindBtnStyle != null) KeybindBtnStyle.normal.background = InactivePillTex;
            if (SmallSubBtnStyle != null) SmallSubBtnStyle.normal.background = InactivePillTex;
            if (ValueRightStyle != null) ValueRightStyle.normal.textColor = CurrentAccentColor;
        }

        public static Texture2D CreateExactRoundedTex(int w, int h, int r, Color bg, Color border, int borderWidth = 1, bool roundLeftOnly = false)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = 0f;
                    float cy = 0f;

                    if (roundLeftOnly)
                    {
                        if (x < r) cx = r - x;
                        if (y < r && x < r) cy = r - y;
                        else if (y >= h - r && x < r) cy = y - (h - 1 - r);
                    }
                    else
                    {
                        if (x < r) cx = r - x;
                        else if (x >= w - r) cx = x - (w - 1 - r);

                        if (y < r) cy = r - y;
                        else if (y >= h - r) cy = y - (h - 1 - r);
                    }

                    float dist = Mathf.Sqrt(cx * cx + cy * cy);

                    if (dist > r + 0.5f)
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                    else if (dist > r - 0.5f)
                    {
                        float alpha = Mathf.Clamp01(r + 0.5f - dist);
                        Color edgeCol = (borderWidth > 0) ? border : bg;
                        pixels[y * w + x] = new Color(edgeCol.r, edgeCol.g, edgeCol.b, edgeCol.a * alpha);
                    }
                    else if (borderWidth > 0 && (dist > r - borderWidth || x < borderWidth || y < borderWidth || (!roundLeftOnly && x >= w - borderWidth) || y >= h - borderWidth))
                    {
                        pixels[y * w + x] = border;
                    }
                    else
                    {
                        pixels[y * w + x] = bg;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Create9SliceRoundedTex(int size, int radius, Color fillColor, Color borderColor, int borderWidth = 1)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = (x < radius) ? radius - x : (x >= size - radius ? x - (size - 1 - radius) : 0);
                    float cy = (y < radius) ? radius - y : (y >= size - radius ? y - (size - 1 - radius) : 0);
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);

                    if (dist > radius + 0.5f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else if (dist > radius - 0.5f)
                    {
                        float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                        Color edgeColor = (borderWidth > 0) ? borderColor : fillColor;
                        pixels[y * size + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, edgeColor.a * alpha);
                    }
                    else if (borderWidth > 0 && (dist > radius - borderWidth || x < borderWidth || y < borderWidth || x >= size - borderWidth || y >= size - borderWidth))
                    {
                        pixels[y * size + x] = borderColor;
                    }
                    else
                    {
                        pixels[y * size + x] = fillColor;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateCircleCheckTex(Color col, bool isChecked)
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < size / 2f)
                    {
                        cols[y * size + x] = isChecked ? col : ((dist < 3f) ? col : new Color(0.15f, 0.15f, 0.18f));
                    }
                    else
                    {
                        cols[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        public static void DrawBox(Rect rect, GUIStyle style)
        {
            if (Event.current != null && Event.current.type == EventType.Repaint && style != null)
            {
                style.Draw(rect, false, false, false, false);
            }
        }
    }
}
