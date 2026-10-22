using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using UnhollowerBaseLib;

namespace BOTMenuMod
{
    [HarmonyPatch(typeof(ChatBox), "SendMessage", new Type[] { typeof(string) })]
    public static class ChatFontPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(ref string __0)
        {
            if (!string.IsNullOrEmpty(__0))
                __0 = Regex.Replace(__0, @">([a-zA-Z0-9#=/+ _,.:""]+)>", "<$1>");
            return true;
        }
    }

    public class FakeBoxManager : MonoBehaviour
    {
        public static FakeBoxManager Instance;
        public static bool fakeBox = false;
        public static bool isReplayingFakeDrop = false;
        public static float dropSendDelay = 0.25f;
        private float dropTimer;

        public FakeBoxManager(IntPtr ptr) : base(ptr) { }

        private void Start()
        {
            Instance = this;
            FakeBoxUtility.LoadDrops();
        }

        private void Update()
        {
            if (!fakeBox) return;
            dropTimer += Time.deltaTime;
            if (dropTimer >= dropSendDelay)
            {
                FakeBoxUtility.ReplayRandomDrop();
                dropTimer = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(MonoBehaviourPublicInpabyInInInUnique), "SendReceivedDrop")]
    public static class FakeBoxPatches
    {
        [HarmonyPrefix]
        public static void OnSendReceivedDrop(Il2CppStructArray<byte> __0, ref int __1)
        {
            if (__0 == null || __0.Length == 0 || FakeBoxManager.isReplayingFakeDrop) return;
            if (__1 == __0.Length) FakeBoxUtility.SaveDrop(__0);
        }
    }

    public static class FakeBoxUtility
    {
        public static Dictionary<Il2CppStructArray<byte>, int> LoadedItemDrops = new Dictionary<Il2CppStructArray<byte>, int>();
        private static readonly System.Random _rng = new System.Random();
        private static string FilePath => Path.Combine(Paths.ConfigPath, "BOTMod", "fakebox_drops.txt");

        public static void LoadDrops()
        {
            LoadedItemDrops.Clear();
            if (!File.Exists(FilePath)) return;
            try
            {
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] hex = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Il2CppStructArray<byte> data = new Il2CppStructArray<byte>((long)hex.Length);
                    for (int i = 0; i < hex.Length; i++) data[i] = Convert.ToByte(hex[i], 16);
                    LoadedItemDrops[data] = data.Length;
                }
            }
            catch { }
        }

        public static void SaveDrop(Il2CppStructArray<byte> bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            try
            {
                string hexLine = string.Join(" ", bytes.Select(b => b.ToString("X2")));
                using (StreamWriter sw = new StreamWriter(FilePath, true))
                {
                    sw.WriteLine(hexLine);
                    LoadedItemDrops[bytes] = bytes.Length;
                }
            }
            catch { }
        }

        public static void ReplayRandomDrop()
        {
            if (LoadedItemDrops.Count == 0) return;
            var pair = LoadedItemDrops.ElementAt(_rng.Next(LoadedItemDrops.Count));
            FakeBoxManager.isReplayingFakeDrop = true;
            try { MonoBehaviourPublicInpabyInInInUnique.SendReceivedDrop(pair.Key, pair.Value); } catch { }
            FakeBoxManager.isReplayingFakeDrop = false;
        }
    }
}
