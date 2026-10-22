using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;
using UnhollowerBaseLib;

namespace BOTMenuMod
{
    public enum GlowMode
    {
        Normal, Rainbow, Pulse, Police, Golden, Toxic, Disco, Fire
    }

    public static class GlowESPManager
    {
        public static bool enabled = false;
        public static GlowMode currentMode = GlowMode.Rainbow;
        private static bool _wasEnabled = false;
        private static float _hue = 0f;
        private static readonly Dictionary<int, float> _ogOutlineWidth = new Dictionary<int, float>();

        public static void Init() { }

        public static void Update()
        {
            if (GameManager.Instance?.activePlayers == null) return;
            if (!enabled)
            {
                if (_wasEnabled) { DisableAllOutlines(); _wasEnabled = false; }
                return;
            }
            _wasEnabled = true;

            Color currentColor = Color.red;
            float time = Time.time;

            switch (currentMode)
            {
                case GlowMode.Normal: currentColor = Color.red; break;
                case GlowMode.Rainbow: _hue = (_hue + Time.deltaTime * 0.5f) % 1f; currentColor = Color.HSVToRGB(_hue, 1f, 1f); break;
                case GlowMode.Pulse: currentColor = new Color(0.6f + Mathf.PingPong(time * 2f, 1f) * 0.4f, 0f, 0f); break;
                case GlowMode.Police: currentColor = ((int)(time * 8f) % 2 != 0) ? Color.red : Color.blue; break;
                case GlowMode.Golden: currentColor = Color.Lerp(new Color(0.7f, 0.5f, 0f), new Color(1f, 0.9f, 0.5f), Mathf.PingPong(time * 1.5f, 1f)); break;
                case GlowMode.Toxic: currentColor = Color.Lerp(Color.green, new Color(0.5f, 1f, 0f), Mathf.PingPong(time * 3f, 1f)); break;
                case GlowMode.Disco: currentColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value); break;
                case GlowMode.Fire: currentColor = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(time * 4f, 1f)); break;
            }

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId || kvp.Value == null || kvp.Value.dead) continue;
                try
                {
                    if (kvp.Value.shirt != null)
                    {
                        var outlineScript = kvp.Value.shirt.GetComponent<MonoBehaviourPublicHa1MeObouCoSiouBoprUnique>();
                        if (outlineScript != null) ApplyOutline(outlineScript, currentColor);
                    }
                    if (kvp.Value.pants != null)
                    {
                        var outlineScript = kvp.Value.pants.GetComponent<MonoBehaviourPublicHa1MeObouCoSiouBoprUnique>();
                        if (outlineScript != null) ApplyOutline(outlineScript, currentColor);
                    }
                }
                catch { }
            }
        }

        private static void ApplyOutline(MonoBehaviourPublicHa1MeObouCoSiouBoprUnique script, Color color)
        {
            int id = script.GetInstanceID();
            if (!_ogOutlineWidth.ContainsKey(id)) _ogOutlineWidth[id] = script.outlineWidth;
            script.outlineWidth = 15f;
            script.Method_Public_Void_Color_0(color);
        }

        private static void DisableAllOutlines()
        {
            if (GameManager.Instance?.activePlayers == null) return;
            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Value == null) continue;
                try
                {
                    if (kvp.Value.shirt != null) RestoreOutline(kvp.Value.shirt.GetComponent<MonoBehaviourPublicHa1MeObouCoSiouBoprUnique>());
                    if (kvp.Value.pants != null) RestoreOutline(kvp.Value.pants.GetComponent<MonoBehaviourPublicHa1MeObouCoSiouBoprUnique>());
                }
                catch { }
            }
            _ogOutlineWidth.Clear();
        }

        private static void RestoreOutline(MonoBehaviourPublicHa1MeObouCoSiouBoprUnique o)
        {
            if (o == null) return;
            float width;
            if (_ogOutlineWidth.TryGetValue(o.GetInstanceID(), out width)) o.outlineWidth = width;
            else o.outlineWidth = 0f;
        }
    }

    public static class ShinyIntensityManager
    {
        public static bool Enabled = false;
        public static float ShinyValue = 64f;
        private static bool _wasEnabled = false;
        private static bool _hasOgShine = false;
        private static float _ogMaxShine = 1f;

        public static void Update()
        {
            if (Enabled)
            {
                if (!_wasEnabled)
                {
                    try { _ogMaxShine = MonoBehaviourPublicSkhafaSkshtobaSkMeMaUnique.maxShineVal; _hasOgShine = true; } catch { }
                }
                _wasEnabled = true;
                try { MonoBehaviourPublicSkhafaSkshtobaSkMeMaUnique.maxShineVal = ShinyValue; } catch { }
            }
            else if (_wasEnabled)
            {
                _wasEnabled = false;
                try { MonoBehaviourPublicSkhafaSkshtobaSkMeMaUnique.maxShineVal = _hasOgShine ? _ogMaxShine : 1f; } catch { }
            }
        }
    }
}
