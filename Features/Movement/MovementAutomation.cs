using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BOTMenuMod
{
    public static class TASManager
    {
        public struct TASFrame { public Vector3 pos; public Quaternion rot; }
        public static List<TASFrame> recordedPath = new List<TASFrame>();
        public static List<string> savedFiles = new List<string>();
        public static int selectedFileIndex = 0;
        public static bool isRecording = false;
        public static bool isPlaying = false;
        private static int playIndex = 0;
        private static string TasFolder = Path.Combine(Variables.mainFolderPath, "TAS");

        public static void Update()
        {
            var me = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (me == null || me.dead) { isRecording = isPlaying = false; return; }
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            if (isRecording)
            {
                recordedPath.Add(new TASFrame { pos = me.transform.position, rot = me.transform.rotation });
            }
            else if (isPlaying && recordedPath.Count > 0)
            {
                if (playIndex < recordedPath.Count)
                {
                    var f = recordedPath[playIndex];
                    me.transform.position = f.pos;
                    me.transform.rotation = f.rot;
                    ClientSendClass.PlayerPosition(f.pos, myId);
                    playIndex++;
                }
                else { isPlaying = false; playIndex = 0; Utility.ForceMessage("TAS: Playback Finished"); }
            }
        }

        public static void RefreshFileList()
        {
            if (!Directory.Exists(TasFolder)) Directory.CreateDirectory(TasFolder);
            savedFiles = Directory.GetFiles(TasFolder, "*.tas").Select(Path.GetFileName).ToList();
            if (selectedFileIndex >= savedFiles.Count) selectedFileIndex = 0;
        }

        public static void SaveCurrentPath(string fileName)
        {
            if (recordedPath.Count == 0) return;
            List<string> lines = new List<string>();
            foreach (var f in recordedPath)
            {
                lines.Add($"{f.pos.x}|{f.pos.y}|{f.pos.z}|{f.rot.x}|{f.rot.y}|{f.rot.z}|{f.rot.w}");
            }
            File.WriteAllLines(Path.Combine(TasFolder, fileName + ".tas"), lines);
            RefreshFileList();
            Utility.ForceMessage($"TAS Saved: {fileName}");
        }

        public static void LoadSelectedPath()
        {
            if (savedFiles.Count == 0) return;
            string path = Path.Combine(TasFolder, savedFiles[selectedFileIndex]);
            if (!File.Exists(path)) return;

            recordedPath.Clear();
            string[] lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                string[] d = line.Split('|');
                if (d.Length == 7)
                {
                    recordedPath.Add(new TASFrame
                    {
                        pos = new Vector3(float.Parse(d[0]), float.Parse(d[1]), float.Parse(d[2])),
                        rot = new Quaternion(float.Parse(d[3]), float.Parse(d[4]), float.Parse(d[5]), float.Parse(d[6]))
                    });
                }
            }
            Utility.ForceMessage("TAS: Route Loaded");
        }

        public static void DeleteSelectedFile()
        {
            if (savedFiles.Count == 0) return;
            try
            {
                string fileName = savedFiles[selectedFileIndex];
                string path = Path.Combine(TasFolder, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Utility.ForceMessage("TAS: File Deleted");
                    recordedPath.Clear();
                    RefreshFileList();
                }
            }
            catch { }
        }
    }

    public static class WinManager
    {
        public static void InstantWin()
        {
            if (GameManager.Instance == null || SteamManager.Instance == null) return;
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
            if (!GameManager.Instance.activePlayers.ContainsKey(myId)) return;
            var me = GameManager.Instance.activePlayers[myId];

            string scene = SceneManager.GetActiveScene().name.ToLower();
            Utility.ForceMessage($"Scene: {scene}");

            Vector3? pos = null;

            if (scene.Contains("glass") && scene.Contains("small")) pos = new Vector3(-1f, 12.9f, 40f);
            else if (scene.Contains("glass")) pos = new Vector3(-4.7f, 12.9f, 112.8f);
            else if (scene.Contains("icy") && scene.Contains("small")) pos = new Vector3(1f, -8f, 85f);
            else if (scene.Contains("icy")) pos = new Vector3(1.5f, -8.1f, 139.2f);
            else if (scene.Contains("islands") && scene.Contains("small")) pos = new Vector3(2f, -8f, 94f);
            else if (scene.Contains("islands")) pos = new Vector3(2f, -8f, 134f);
            else if (scene.Contains("color") && scene.Contains("small")) pos = new Vector3(0f, 100f, 0f);
            else if (scene.Contains("color")) pos = new Vector3(0f, 200f, 0f);
            else if (scene.Contains("plains")) pos = new Vector3(-2f, 26f, 144f);
            else if (scene.Contains("slippery") || scene.Contains("slope")) pos = new Vector3(-135.0f, 47.1f, 540.5f);
            else if (scene.Contains("desert")) pos = new Vector3(-5f, 3f, 148f);
            else if (scene.Contains("mountain")) pos = new Vector3(-64f, 36f, 124f);
            else if (scene.Contains("lava") && scene.Contains("climb")) pos = new Vector3(33f, 39f, -13f);
            else if (scene.Contains("sandcastle")) pos = new Vector3(-74.4f, 31.9f, 135.7f);

            if (pos.HasValue)
            {
                var rb = me.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = Vector3.zero;
                me.transform.position = pos.Value;
                Utility.ForceMessage("Instant Win!");
            }
            else
            {
                GameObject finish = GameObject.Find("Finish") ?? GameObject.Find("WinZone");
                if (finish != null)
                {
                    me.transform.position = finish.transform.position + Vector3.up * 2f;
                    Utility.ForceMessage("Auto TP to Finish Object");
                }
                else
                {
                    Utility.ForceMessage("Map coordinates not found!");
                }
            }
        }
    }
}
