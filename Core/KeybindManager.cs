using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace BOTMenuMod
{
    public static class KeybindManager
    {
        private static string BindFilePath = Path.Combine(Variables.mainFolderPath, "keybinds.txt");
        public static Dictionary<string, KeyCode> SavedBinds = new Dictionary<string, KeyCode>();

        public static string GetButtonID(string label)
        {
            if (string.IsNullOrEmpty(label)) return "";
            return Utility.StripColorTags(label).Trim();
        }

        public static void LoadBinds()
        {
            SavedBinds.Clear();
            if (!File.Exists(BindFilePath)) return;
            try
            {
                var lines = File.ReadAllLines(BindFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string labelID = parts[0].Trim();
                        if (Enum.TryParse<KeyCode>(parts[1].Trim(), out var key))
                        {
                            if (!SavedBinds.ContainsKey(labelID)) SavedBinds.Add(labelID, key);
                        }
                    }
                }
                Utility.Log("Keybinds Loaded Successfully.");
            }
            catch { }
        }

        public static void SaveAllBinds()
        {
            List<string> lines = new List<string>();
            try
            {
                if (MenuManager.Categories != null)
                {
                    foreach (var cat in MenuManager.Categories)
                    {
                        if (cat == null || cat.Buttons == null) continue;
                        foreach (var btn in cat.Buttons)
                        {
                            CollectBind(btn, lines);
                        }
                    }
                }
                if (!Directory.Exists(Variables.mainFolderPath)) Directory.CreateDirectory(Variables.mainFolderPath);
                File.WriteAllLines(BindFilePath, lines);
                MenuManager.ApplyAllSpecialBinds();
                Utility.Log("Keybinds Saved.");
            }
            catch (Exception e) { Utility.Log("Save Failed: " + e.Message); }
        }

        private static void CollectBind(MenuButton btn, List<string> lines)
        {
            if (btn == null || lines == null) return;
            try
            {
                if (btn.BoundKey != KeyCode.None) lines.Add($"{btn.ID}={btn.BoundKey}");
            }
            catch { }
            if (btn.Children == null) return;
            foreach (var child in btn.Children)
            {
                CollectBind(child, lines);
            }
        }
    }
}
