using System;
using UnityEngine;
using UnityEngine.UI;

namespace BOTMenuMod
{
    public static class MenuThemeManager
    {
        public static int ThemeIndex = 0;
        public static float BgAlpha = 0.90f;

        public static readonly string[] ThemeNames = {
            "Breeze Pink (粉)", "Breeze Purple (紫)", "Hacker Green (绿)", "Cyber Blue (蓝)",
            "Ice White (白)", "Blaze Orange (橙)", "Mint Green (薄荷)", "Gold (金)",
            "Pitch Black (纯黑)"
        };

        public static readonly string[] AccentHexes = {
            "#FF4499", "#9933FF", "#00FF44", "#00DDFF",
            "#E0FFFF", "#FF8800", "#00FF99", "#FFD700",
            "#FFFFFF"
        };

        public static readonly Color[] BackgroundTints = {
            new Color(0.20f, 0.06f, 0.12f),
            new Color(0.12f, 0.06f, 0.22f),
            new Color(0.05f, 0.18f, 0.08f),
            new Color(0.05f, 0.14f, 0.22f),
            new Color(0.14f, 0.16f, 0.18f),
            new Color(0.22f, 0.10f, 0.02f),
            new Color(0.05f, 0.18f, 0.14f),
            new Color(0.20f, 0.16f, 0.02f),
            new Color(0.02f, 0.02f, 0.03f)
        };

        public static string CurrentAccentHex => AccentHexes[Mathf.Clamp(ThemeIndex, 0, AccentHexes.Length - 1)];

        public static void ApplyTheme(Image mainBg, Image leftBg)
        {
            if (mainBg == null) return;
            mainBg.sprite = null;
            mainBg.type = Image.Type.Simple;

            Color baseColor = BackgroundTints[Mathf.Clamp(ThemeIndex, 0, BackgroundTints.Length - 1)];
            Color darkMain = new Color(baseColor.r, baseColor.g, baseColor.b, BgAlpha);
            Color darkLeft = new Color(baseColor.r * 0.6f, baseColor.g * 0.7f, baseColor.b * 0.7f, BgAlpha);

            mainBg.color = darkMain;
            if (leftBg != null) leftBg.color = darkLeft;
        }
    }
}
