using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOTMenuMod
{
    public static class ItemESPManager
    {
        public static bool enabled = false;
        private static List<GameObject> _cachedItems = new List<GameObject>();
        private static float _scanTimer = 0f;
        private static Camera _mainCam;
        private static GUIStyle _itemStyle;

        private static GUIStyle ItemStyle
        {
            get
            {
                if (_itemStyle == null)
                    _itemStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
                return _itemStyle;
            }
        }

        public static void Update()
        {
            if (!enabled) return;
            if (_mainCam == null) _mainCam = Camera.main;

            _scanTimer += Time.deltaTime;
            if (_scanTimer > 2.0f)
            {
                _scanTimer = 0f;
                _cachedItems.Clear();

                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;
                    string name = obj.name;

                    if (name.Contains("Fx") || name.Contains("FX") || name.Contains("Sound") ||
                        name.Contains("UI") || name.Contains("Player") || name.Contains("Trail"))
                        continue;

                    if (name.Contains("SnowballPile"))
                    {
                        _cachedItems.Add(obj);
                        continue;
                    }

                    var isRealWeapon = obj.GetComponent<MonoBehaviour1PublicAbstractItitBoGapiTrrileTrObUnique>() != null ||
                                       obj.GetComponent<MonoBehaviour2PublicGathObauTrgumuGaSiBoUnique>() != null ||
                                       obj.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>() != null ||
                                       obj.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>() != null;

                    if (isRealWeapon)
                    {
                        float distToCam = Vector3.Distance(_mainCam.transform.position, obj.transform.position);
                        if (distToCam > 1.5f) _cachedItems.Add(obj);
                    }
                }
            }
        }

        public static void Render()
        {
            if (!enabled || _mainCam == null || _cachedItems.Count == 0) return;

            Vector3 myPos = _mainCam.transform.position;

            foreach (var obj in _cachedItems)
            {
                if (obj == null || !obj.activeInHierarchy) continue;

                float dist = Vector3.Distance(myPos, obj.transform.position);
                if (dist > 150f || dist < 1.5f) continue;

                Vector3 screenPos = _mainCam.WorldToScreenPoint(obj.transform.position);
                if (screenPos.z > 0)
                {
                    string cleanName = obj.name.Replace("Pickup", "").Replace("(Clone)", "").Trim();
                    if (cleanName.Contains("SnowballPile")) cleanName = "❄️ SnowballPile";
                    else if (cleanName == "Ak") cleanName = "AK-47";
                    else if (cleanName == "Pistol") cleanName = "手枪";
                    else if (cleanName == "Dual") cleanName = "双管喷";
                    else if (cleanName == "Revolver") cleanName = "左轮";

                    if (obj.transform.parent != null) cleanName = $"[{cleanName}]";

                    string text = $"{cleanName}\n<size=10>[{(int)dist}m]</size>";
                    var style = ItemStyle;

                    style.normal.textColor = Color.black;
                    GUI.Label(new Rect(screenPos.x + 1, Screen.height - screenPos.y + 1, 100, 40), text, style);

                    style.normal.textColor = obj.transform.parent == null ? new Color(1f, 0.6f, 0f) : new Color(1f, 0.2f, 0.2f);
                    GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 100, 40), text, style);
                }
            }
        }
    }
}
