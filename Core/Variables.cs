using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using BepInEx;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class Variables
    {
        public static string mainFolderPath = Path.Combine(Paths.ConfigPath, "BOTMod");
        public static string logFilePath = Path.Combine(mainFolderPath, "BOT_log.txt");
        public static string configFilePath = Path.Combine(mainFolderPath, "BOT_config.txt");
        public static KeyCode menuKey = KeyCode.Insert;
        public static bool menuTrigger;
    }

    public static class Utility
    {
        public static void CreateFolder(string path) { try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); } catch { } }
        public static void CreateFile(string path) { try { if (!File.Exists(path)) using (File.CreateText(path)) { } } catch { } }
        public static void Log(string line) { Console.WriteLine($"[BOT] {line}"); }
        public static void SetConfigFile(string configFilePath) { try { if (!File.Exists(configFilePath)) { using StreamWriter sw = File.CreateText(configFilePath); sw.WriteLine("menuKey=insert"); sw.WriteLine("# Legacy config."); } } catch (Exception ex) { Log("Old Config Error: " + ex.Message); } }
        public static void ForceMessage(string msg) { try { if (BOTNotification.Instance != null) BOTNotification.Instance.Show(msg); else Log(StripColorTags(msg)); } catch { Log(msg); } }
        public static string StripColorTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
        public static bool IsChatOpen() { try { var go = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject; return go != null && (go.GetComponent<UnityEngine.UI.InputField>() != null || go.GetComponent<TMPro.TMP_InputField>() != null); } catch { } return false; }
        public static bool IsHost()
        {
            if (SteamManager.Instance == null) return false;
            if (SteamManager.Instance.currentLobby.m_SteamID == 0) return true;
            return SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID == SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
        }
        public static ulong GetHostID()
        {
            if (SteamManager.Instance == null) return 0;
            if (SteamManager.Instance.currentLobby.m_SteamID == 0) return 0;
            return SteamMatchmaking.GetLobbyOwner(SteamManager.Instance.currentLobby).m_SteamID;
        }
        public static IEnumerable<CSteamID> GetAllLobbyMembers()
        {
            if (SteamManager.Instance == null) yield break;
            int count = SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby);
            for (int i = 0; i < count; i++) { yield return SteamMatchmaking.GetLobbyMemberByIndex(SteamManager.Instance.currentLobby, i); }
        }
    }
}
