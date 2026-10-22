using System;
using UnityEngine;
using UnityEngine.UI;

namespace BOTMenuMod
{
    public static class CrosshairManager
    {
        public static bool enableDot = false;
        public static int dotSize = 2;
        public static int gapSize = 6;
        public static int length = 10;
        public static int width = 4;
        public static string colorHtml = "#FFFFFF";
        private static CrosshairClass _currentCrosshair;
        private static float _retryTimer = 0f;

        public static void Update()
        {
            if (_currentCrosshair != null) return;
            _retryTimer += Time.deltaTime;
            if (_retryTimer < 1.0f) return;
            _retryTimer = 0f;

            var cross = UnityEngine.Object.FindObjectOfType<CrosshairClass>();
            if (cross != null)
            {
                _currentCrosshair = cross;
                ApplySettings();
                Utility.Log("Crosshair hooked via Update.");
            }
        }

        public static void ApplySettings()
        {
            if (_currentCrosshair == null) return;
            _currentCrosshair.enableDot = enableDot;
            _currentCrosshair.dotSize = dotSize;
            _currentCrosshair.gap = gapSize;
            _currentCrosshair.length = length;
            _currentCrosshair.width = width;

            Color color;
            if (ColorUtility.TryParseHtmlString(colorHtml, out color))
            {
                try
                {
                    var t = _currentCrosshair.transform;
                    if (t.childCount > 0)
                    {
                        var dot = t.GetChild(0).GetComponent<RawImage>();
                        if (dot != null) dot.color = color;
                    }

                    int childCount = Math.Min(5, t.childCount);
                    for (int i = 1; i < childCount; i++)
                    {
                        var child = t.GetChild(i);
                        if (child != null && child.childCount > 0)
                        {
                            var rawImage = child.GetChild(0).GetComponent<RawImage>();
                            if (rawImage != null) rawImage.color = color;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
