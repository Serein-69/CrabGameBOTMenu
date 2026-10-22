using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class LegitESPManager
    {
        public static bool enabled = false;
        public static Color espColor = Color.cyan;

        private static GUIStyle _nameStyle;
        private static GUIStyle _distStyle;
        private static readonly Dictionary<ulong, string> _nameCache = new Dictionary<ulong, string>();
        private static readonly Dictionary<ulong, bool> _visibleCache = new Dictionary<ulong, bool>();

        public static void Render()
        {
            if (!enabled || GameManager.Instance == null) return;

            var mainCam = Camera.main;
            if (mainCam == null) return;

            ulong myId = SteamManager.Instance?.field_Private_CSteamID_0.m_SteamID ?? 0;
            bool doRaycast = Event.current == null || Event.current.type == EventType.Repaint;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                var player = kvp.Value;
                if (kvp.Key == myId || player == null || player.dead) continue;

                bool visible;
                if (doRaycast)
                {
                    visible = true;
                    Vector3 checkPos = player.transform.position + Vector3.up * 1.5f;
                    if (Physics.Linecast(mainCam.transform.position, checkPos, out RaycastHit hit))
                    {
                        if (hit.collider.transform.root != player.transform.root) visible = false;
                    }
                    _visibleCache[kvp.Key] = visible;
                }
                else if (!_visibleCache.TryGetValue(kvp.Key, out visible))
                {
                    visible = true;
                }

                if (!visible) continue;

                float distance = Vector3.Distance(mainCam.transform.position, player.transform.position);

                Vector3 worldHead = player.transform.position + Vector3.up * 2.45f;
                Vector3 worldFoot = player.transform.position - Vector3.up * 2.45f;

                Vector3 sHead = mainCam.WorldToScreenPoint(worldHead);
                Vector3 sFoot = mainCam.WorldToScreenPoint(worldFoot);

                if (sHead.z > 0)
                {
                    float height = Mathf.Abs(sHead.y - sFoot.y);
                    float width = height / 1.7f;
                    float x = sHead.x - width / 2f;
                    float y = Screen.height - sHead.y;

                    DrawGlowCornerBox(x, y, width, height, espColor);

                    string name;
                    if (!_nameCache.TryGetValue(kvp.Key, out name))
                    {
                        name = SteamFriends.GetFriendPersonaName(new CSteamID(kvp.Key));
                        _nameCache[kvp.Key] = name;
                    }
                    DrawNameTag(x, y, width, name);
                    DrawDistanceTag(x, y, width, height, (int)distance);
                }
            }
        }

        private static void DrawGlowCornerBox(float x, float y, float w, float h, Color color)
        {
            float lineW = w * 0.25f;
            float lineH = h * 0.15f;

            Color glowColor = new Color(color.r, color.g, color.b, 0.35f);
            RenderInternal.DrawCorners(x - 1, y - 1, w + 2, h + 2, lineW, lineH, 3f, glowColor);
            RenderInternal.DrawCorners(x, y, w, h, lineW, lineH, 1.5f, color);
        }

        private static void DrawNameTag(float x, float y, float w, string name)
        {
            if (_nameStyle == null) _nameStyle = new GUIStyle { alignment = TextAnchor.LowerCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            var style = _nameStyle;
            style.normal.textColor = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(x + 1, y - 19, w, 20), name, style);
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y - 20, w, 20), name, style);
        }

        private static void DrawDistanceTag(float x, float y, float w, float h, int distance)
        {
            string distStr = $"[{distance}m]";
            if (_distStyle == null) _distStyle = new GUIStyle { alignment = TextAnchor.UpperCenter, fontSize = 11, fontStyle = FontStyle.Bold };
            var style = _distStyle;
            style.normal.textColor = new Color(0, 0, 0, 0.7f);
            GUI.Label(new Rect(x + 1, y + h + 3, w, 20), distStr, style);
            style.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(x, y + h + 2, w, 20), distStr, style);
        }
    }

    public static class RenderInternal
    {
        private static Texture2D _whiteTexture;
        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        public static void DrawCorners(float x, float y, float w, float h, float Lw, float Lh, float t, Color col)
        {
            GUI.color = col;
            GUI.DrawTexture(new Rect(x, y, Lw, t), WhiteTexture);
            GUI.DrawTexture(new Rect(x, y, t, Lh), WhiteTexture);
            GUI.DrawTexture(new Rect(x + w - Lw, y, Lw, t), WhiteTexture);
            GUI.DrawTexture(new Rect(x + w - t, y, t, Lh), WhiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - t, Lw, t), WhiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - Lh, t, Lh), WhiteTexture);
            GUI.DrawTexture(new Rect(x + w - Lw, y + h - t, Lw, t), WhiteTexture);
            GUI.DrawTexture(new Rect(x + w - t, y + h - Lh, t, Lh), WhiteTexture);
            GUI.color = Color.white;
        }
    }
}
