using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BOTMenuMod
{
    public static class ChatHistoryManager
    {
        private struct ChatEntry
        {
            public ulong senderId;
            public string message;
            public string senderName;
        }

        private static List<ChatEntry> _chatHistory = new List<ChatEntry>();

        public static bool IsRestoring = false;

        public static int MaxHistory = 25;

        public static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(ChatHistoryManager));
            Utility.Log("Passive Chat History System Initialized (Anti-Duplication).");
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicRaovTMinTemeColoonCoUnique), "Awake")]
        [HarmonyPostfix]
        public static void Postfix_ChatAwake(MonoBehaviourPublicRaovTMinTemeColoonCoUnique __instance)
        {

            IsRestoring = true;
            try
            {
                foreach (var entry in _chatHistory)
                {
                    __instance.AppendMessage(entry.senderId, entry.message, entry.senderName);
                }
            }
            catch { }

            IsRestoring = false;
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicRaovTMinTemeColoonCoUnique), "AppendMessage")]
        [HarmonyPostfix]
        public static void Postfix_AppendMessage(ulong __0, string __1, string __2)
        {
            if (IsRestoring) return;

            _chatHistory.Add(new ChatEntry { senderId = __0, message = __1, senderName = __2 });
            if (_chatHistory.Count > MaxHistory) _chatHistory.RemoveAt(0);
        }
    }
}
