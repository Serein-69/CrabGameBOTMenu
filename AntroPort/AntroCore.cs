using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using UnhollowerRuntimeLib;
using HarmonyLib;
using SteamworksNative;

namespace BOTMenuMod.AntroPort
{
    public class SkinConfig
    {
        public float GlobalHeightOffset = 0f;
        public Dictionary<string, Vector3> BonePosOffsets = new Dictionary<string, Vector3>();
        public Dictionary<string, Vector3> BoneRotOffsets = new Dictionary<string, Vector3>();
    }

    public static class AntroConfigManager
    {
        public static ConfigEntry<bool> RememberState;
        public static ConfigEntry<string> SavedMode;
        public static ConfigEntry<string> SavedSingleSkin;
        public static ConfigEntry<string> DisabledSkinsStr;
        public static ConfigEntry<string> SavedActiveSkinsStr;
        public static ConfigEntry<string> SavedSkinConfigsStr;
        private static ConfigFile _config;

        public static void Init(ConfigFile config)
        {
            _config = config;
            RememberState = config.Bind<bool>("AntroSkins", "RememberState", false, "Remember active skins between sessions");
            SavedMode = config.Bind<string>("AntroSkins", "SavedMode", "None", "Last active mode");
            SavedSingleSkin = config.Bind<string>("AntroSkins", "SavedSingleSkin", "", "Last selected single skin");
            DisabledSkinsStr = config.Bind<string>("AntroSkins", "DisabledSkins", "", "Skins excluded from Random");
            SavedActiveSkinsStr = config.Bind<string>("AntroSkins", "SavedActiveSkins", "", "Last item skin assignments");
            SavedSkinConfigsStr = config.Bind<string>("AntroSkins", "SavedSkinConfigs", "", "Last skin bone configs");
            LoadSettings();
        }

        public static void LoadSettings()
        {
            AntroSkinManager.DisabledSkins = new HashSet<string>(DisabledSkinsStr.Value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            if (RememberState.Value)
            {
                if (Enum.TryParse<AntroSkinManager.SkinMode>(SavedMode.Value, out var result))
                    AntroSkinManager.CurrentMode = result;
                AntroSkinManager.GlobalSelectedSkin = SavedSingleSkin.Value;
            }
            try
            {
                AntroStuffManager.ActiveSkins.Clear();
                foreach (var rec in SplitRecords(SavedActiveSkinsStr.Value))
                {
                    var f = SplitFields(rec);
                    if (f.Count != 3 || f[0] != "K") continue;
                    string item = Unescape(f[1]);
                    if (string.IsNullOrEmpty(item)) continue;
                    AntroStuffManager.ActiveSkins[item] = Unescape(f[2]);
                }
            }
            catch { }

            try
            {
                SkinsMenu.SkinConfigs.Clear();
                var loaded = new Dictionary<string, SkinConfig>();
                foreach (var rec in SplitRecords(SavedSkinConfigsStr.Value))
                {
                    var f = SplitFields(rec);
                    if (f.Count < 3) continue;
                    if (f[0] == "S")
                    {
                        var cfg = GetOrCreateConfig(loaded, Unescape(f[1]));
                        float h;
                        if (float.TryParse(f[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h))
                            cfg.GlobalHeightOffset = h;
                    }
                    else if (f.Count >= 6 && (f[0] == "P" || f[0] == "R"))
                    {
                        float x, y, z;
                        if (!float.TryParse(f[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) continue;
                        if (!float.TryParse(f[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) continue;
                        if (!float.TryParse(f[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) continue;
                        var cfg = GetOrCreateConfig(loaded, Unescape(f[1]));
                        string bone = Unescape(f[2]);
                        if (string.IsNullOrEmpty(bone)) continue;
                        if (f[0] == "P") cfg.BonePosOffsets[bone] = new Vector3(x, y, z);
                        else cfg.BoneRotOffsets[bone] = new Vector3(x, y, z);
                    }
                }
                foreach (var kvp in loaded) SkinsMenu.SkinConfigs[kvp.Key] = kvp.Value;
            }
            catch { }
        }

        private static SkinConfig GetOrCreateConfig(Dictionary<string, SkinConfig> map, string name)
        {
            SkinConfig cfg;
            if (!map.TryGetValue(name, out cfg))
            {
                cfg = new SkinConfig();
                map[name] = cfg;
            }
            return cfg;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '\\' || c == ';' || c == '|') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i++; }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static List<string> SplitRecords(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { sb.Append(c); sb.Append(s[i + 1]); i++; continue; }
                if (c == ';') { if (sb.Length > 0) list.Add(sb.ToString()); sb.Length = 0; continue; }
                sb.Append(c);
            }
            if (sb.Length > 0) list.Add(sb.ToString());
            return list;
        }

        private static List<string> SplitFields(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length) { sb.Append(c); sb.Append(s[i + 1]); i++; continue; }
                if (c == '|') { list.Add(sb.ToString()); sb.Length = 0; continue; }
                sb.Append(c);
            }
            list.Add(sb.ToString());
            return list;
        }

        private static void AppendRecord(System.Text.StringBuilder sb, params string[] fields)
        {
            if (sb.Length > 0) sb.Append(';');
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(fields[i]);
            }
        }

        private static string Num(float v)
        {
            return v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void SaveSettings()
        {
            SavedMode.Value = AntroSkinManager.CurrentMode.ToString();
            SavedSingleSkin.Value = AntroSkinManager.GlobalSelectedSkin;
            DisabledSkinsStr.Value = string.Join("|", AntroSkinManager.DisabledSkins);
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kvp in AntroStuffManager.ActiveSkins)
                {
                    AppendRecord(sb, "K", Escape(kvp.Key), Escape(kvp.Value));
                }
                SavedActiveSkinsStr.Value = sb.ToString();
            }
            catch { }
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kvp in SkinsMenu.SkinConfigs)
                {
                    if (kvp.Value == null) continue;
                    string name = Escape(kvp.Key);
                    AppendRecord(sb, "S", name, Num(kvp.Value.GlobalHeightOffset));
                    foreach (var b in kvp.Value.BonePosOffsets)
                        AppendRecord(sb, "P", name, Escape(b.Key), Num(b.Value.x), Num(b.Value.y), Num(b.Value.z));
                    foreach (var b in kvp.Value.BoneRotOffsets)
                        AppendRecord(sb, "R", name, Escape(b.Key), Num(b.Value.x), Num(b.Value.y), Num(b.Value.z));
                }
                SavedSkinConfigsStr.Value = sb.ToString();
            }
            catch { }
            _config.Save();
        }
    }

    public class SyncData
    {
        public string SkinName;
        public ulong OwnerID;
        public Transform RootTransform, OriginalHips, CustomHips, OriginalRightHand, CustomRightHand;
        public List<Transform> CustomBones = new List<Transform>(), OriginalBones = new List<Transform>();
        public Dictionary<string, Transform> AllCustomTransforms = new Dictionary<string, Transform>();
        public Dictionary<string, Vector3> InitialLocalPositions = new Dictionary<string, Vector3>();
        public Dictionary<string, Quaternion> InitialLocalRotations = new Dictionary<string, Quaternion>();

        private bool _isCached = false;
        private Vector3 _cachedHipPosOffsetLocal;
        private Quaternion _cachedHipRotOffset;
        private Transform[] _staticTransforms;
        private Vector3[] _staticPositions;
        private Quaternion[] _staticRotations;

        public void InvalidateCache() { _isCached = false; }

        private void BuildCache()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            SkinConfig cfg = (OwnerID == myId || OwnerID == 0)
                ? SkinsMenu.GetSkinConfig(SkinName)
                : (AntroSkinManager.RemoteSkinConfigs.ContainsKey(OwnerID) ? AntroSkinManager.RemoteSkinConfigs[OwnerID] : new SkinConfig());

            _cachedHipPosOffsetLocal = cfg.BonePosOffsets.ContainsKey("Hips") ? cfg.BonePosOffsets["Hips"] : Vector3.zero;
            Vector3 hipRotOffset = cfg.BoneRotOffsets.ContainsKey("Hips") ? cfg.BoneRotOffsets["Hips"] : Vector3.zero;
            _cachedHipRotOffset = Quaternion.Euler(hipRotOffset);

            List<Transform> tList = new List<Transform>();
            List<Vector3> pList = new List<Vector3>();
            List<Quaternion> rList = new List<Quaternion>();

            foreach (var kvp in AllCustomTransforms)
            {
                string boneName = kvp.Key; Transform customT = kvp.Value;
                if (customT == null || customT == CustomHips) continue;

                Vector3 rotOffset = cfg.BoneRotOffsets.ContainsKey(boneName) ? cfg.BoneRotOffsets[boneName] : Vector3.zero;
                Vector3 posOffset = cfg.BonePosOffsets.ContainsKey(boneName) ? cfg.BonePosOffsets[boneName] : Vector3.zero;

                Quaternion finalRot = Quaternion.Euler(rotOffset);
                Vector3 finalPos = InitialLocalPositions[boneName] + posOffset;

                if (!CustomBones.Contains(customT)) finalRot = InitialLocalRotations[boneName] * finalRot;

                tList.Add(customT); pList.Add(finalPos); rList.Add(finalRot);
            }
            _staticTransforms = tList.ToArray(); _staticPositions = pList.ToArray(); _staticRotations = rList.ToArray();
            _isCached = true;
        }

        public void Sync()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            float liveHeight = (OwnerID == myId || OwnerID == 0) ? SkinsMenu.GetSkinConfig(SkinName).GlobalHeightOffset :
                (AntroSkinManager.RemoteSkinConfigs.ContainsKey(OwnerID) ? AntroSkinManager.RemoteSkinConfigs[OwnerID].GlobalHeightOffset : 0f);

            if (OriginalHips == null || CustomHips == null)
            {
                if (RootTransform != null) RootTransform.localPosition = new Vector3(0, liveHeight, 0);
                return;
            }

            if (!_isCached) BuildCache();

            if (RootTransform != null) RootTransform.localPosition = Vector3.zero;
            Vector3 targetHipsPos = OriginalHips.position;
            if (RootTransform != null && RootTransform.parent != null)
            {
                targetHipsPos += RootTransform.parent.up * liveHeight;
                targetHipsPos += RootTransform.parent.TransformDirection(_cachedHipPosOffsetLocal);
            }
            else targetHipsPos += new Vector3(0, liveHeight, 0) + _cachedHipPosOffsetLocal;

            CustomHips.position = targetHipsPos;
            CustomHips.rotation = OriginalHips.rotation * _cachedHipRotOffset;

            for (int i = 0; i < CustomBones.Count; i++)
            {
                if (CustomBones[i] == CustomHips) continue;
                if (CustomBones[i] != null && OriginalBones[i] != null) CustomBones[i].localRotation = OriginalBones[i].localRotation;
            }
            for (int i = 0; i < _staticTransforms.Length; i++)
            {
                if (_staticTransforms[i] == null) continue;
                if (!CustomBones.Contains(_staticTransforms[i])) _staticTransforms[i].localRotation = _staticRotations[i];
                else _staticTransforms[i].localRotation *= _staticRotations[i];
                _staticTransforms[i].localPosition = _staticPositions[i];
            }

            if (OriginalRightHand != null && CustomRightHand != null)
            {
                for (int i = 0; i < OriginalRightHand.childCount; i++)
                {
                    Transform child = OriginalRightHand.GetChild(i);
                    string nameLow = child.name.ToLower();
                    if (!nameLow.Contains("finger") && !nameLow.Contains("mixamo") && !nameLow.StartsWith("antroskin"))
                    {
                        child.position = CustomRightHand.position;
                        child.rotation = CustomRightHand.rotation;
                    }
                }
            }
        }
    }

    public class BoneSynchronizer : MonoBehaviour
    {
        public BoneSynchronizer(IntPtr ptr) : base(ptr) { }
        public static Dictionary<IntPtr, SyncData> DataMap = new Dictionary<IntPtr, SyncData>();

        public static void ForceRecalculateAll()
        {
            foreach (var data in DataMap.Values) data.InvalidateCache();
        }

        void LateUpdate()
        {
            if (DataMap.TryGetValue(this.Pointer, out SyncData data))
            {
                data.Sync();
            }
        }

        void OnDestroy()
        {
            DataMap.Remove(this.Pointer);
        }
    }

    public static class AntroStuffManager
    {
        public static Dictionary<string, string> ActiveSkins = new Dictionary<string, string>();
        public static readonly string[] StuffTypes = { "ak", "pistol", "revolver", "dual", "bat", "bomb", "katana", "knife", "pipe", "snowball", "stick" };
        public static string StuffPath => Path.Combine(Paths.PluginPath, "AntroSkins", "Stuff");

        private static Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> _loadedPrefabs = new Dictionary<string, GameObject>();

        public static void InitFolder()
        {
            if (!Directory.Exists(StuffPath)) Directory.CreateDirectory(StuffPath);
            foreach (string itemType in StuffTypes)
            {
                string folderPath = Path.Combine(StuffPath, itemType.ToUpper());
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            }
        }

        public static List<string> GetAvailableSkins(string itemType)
        {
            List<string> skins = new List<string>();
            string folderPath = Path.Combine(StuffPath, itemType.ToUpper());
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath, "*.bundle")) skins.Add(Path.GetFileName(file));
            }
            return skins;
        }

        public static GameObject GetOrLoadPrefab(string itemType, string skinName)
        {
            string dictKey = $"{itemType}_{skinName}";
            if (_loadedPrefabs.ContainsKey(dictKey))
            {
                if (_loadedPrefabs[dictKey] != null) return _loadedPrefabs[dictKey];
                else _loadedPrefabs.Remove(dictKey);
            }

            string path = Path.Combine(StuffPath, itemType.ToUpper(), skinName);
            if (!File.Exists(path)) return null;

            if (!_loadedBundles.ContainsKey(dictKey))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    AssetBundle bundle = AssetBundle.LoadFromMemory(bytes);
                    if (bundle == null) return null;
                    _loadedBundles[dictKey] = bundle;
                }
                catch { return null; }
            }

            var assets = _loadedBundles[dictKey].LoadAllAssets(Il2CppType.Of<GameObject>());
            foreach (var asset in assets)
            {
                var go = asset.Cast<GameObject>();
                if (go != null && go.transform.parent == null)
                {
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.SetActive(false);
                    _loadedPrefabs[dictKey] = go;
                    return go;
                }
            }
            return null;
        }

        private static string IdentifyItem(string objName)
        {
            string n = objName.ToLower();
            if (n.Contains("ak")) return "ak";
            if (n.Contains("pistol") || n.Contains("glock")) return "pistol";
            if (n.Contains("revolver")) return "revolver";
            if (n.Contains("mac10") || n.Contains("dual") || n.Contains("uzi")) return "dual";
            if (n.Contains("bat")) return "bat";
            if (n.Contains("bomb") || n.Contains("tnt")) return "bomb";
            if (n.Contains("katana") || n.Contains("sword")) return "katana";
            if (n.Contains("knife") || n.Contains("dagger")) return "knife";
            if (n.Contains("pipe")) return "pipe";
            if (n.Contains("snowball")) return "snowball";
            if (n.Contains("stick") || n.Contains("branch")) return "stick";
            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains(name.ToLower())) return child;
            }
            return null;
        }

        public static void ApplyModels()
        {
            if (ActiveSkins.Count == 0) return;
            var players = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique>();
            foreach (var p in players)
            {
                Transform rightHand = FindDeepChild(p.transform, "RightHand") ?? FindDeepChild(p.transform, "handr");
                if (rightHand != null)
                {
                    for (int i = 0; i < rightHand.childCount; i++)
                    {
                        Transform child = rightHand.GetChild(i);
                        if (!child.name.StartsWith("AntroSkin_") && !child.name.ToLower().Contains("mixamo"))
                            ProcessItem(child.gameObject);
                    }
                }
            }
        }

        private static void ProcessItem(GameObject originalItem)
        {
            string detectedType = IdentifyItem(originalItem.name);
            if (detectedType == null) return;

            ActiveSkins.TryGetValue(detectedType, out string desiredBundle);
            Transform existingCustom = null;
            string currentApplied = "";

            foreach (Transform child in originalItem.transform)
            {
                if (child.name.StartsWith("AntroSkin_Stuff_"))
                {
                    existingCustom = child;
                    currentApplied = child.name.Replace("AntroSkin_Stuff_", "");
                    break;
                }
            }

            if (string.IsNullOrEmpty(desiredBundle))
            {
                if (existingCustom != null) UnityEngine.Object.Destroy(existingCustom.gameObject);
                foreach (var r in originalItem.GetComponentsInChildren<Renderer>()) r.enabled = true;
                return;
            }

            if (existingCustom != null && currentApplied == desiredBundle)
            {
                foreach (var r in originalItem.GetComponentsInChildren<Renderer>())
                    if (r.transform != existingCustom && !r.transform.IsChildOf(existingCustom))
                        if (r.GetComponent<MeshRenderer>() != null || r.GetComponent<SkinnedMeshRenderer>() != null) r.enabled = false;
                return;
            }

            if (existingCustom != null) UnityEngine.Object.Destroy(existingCustom.gameObject);

            GameObject prefab = GetOrLoadPrefab(detectedType, desiredBundle);
            if (prefab != null)
            {
                GameObject spawned = UnityEngine.Object.Instantiate(prefab, originalItem.transform);
                spawned.name = $"AntroSkin_Stuff_{desiredBundle}";
                spawned.SetActive(true);

                foreach (var cam in spawned.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.Destroy(cam);
                foreach (var audio in spawned.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.Destroy(audio);
                foreach (var col in spawned.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(col);
                foreach (var rb in spawned.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.Destroy(rb);

                foreach (var r in originalItem.GetComponentsInChildren<Renderer>())
                    if (r.transform != spawned.transform && !r.transform.IsChildOf(spawned.transform))
                        if (r.GetComponent<MeshRenderer>() != null || r.GetComponent<SkinnedMeshRenderer>() != null) r.enabled = false;

                BoneSynchronizer sync = spawned.AddComponent<BoneSynchronizer>();
                SyncData syncData = new SyncData();
                syncData.SkinName = desiredBundle;
                syncData.RootTransform = spawned.transform;
                syncData.OriginalHips = originalItem.transform;
                syncData.CustomHips = spawned.transform;

                BoneSynchronizer.DataMap[sync.Pointer] = syncData;
            }
        }
    }

    public static class AntroSkinManager
    {
        public enum SkinMode { None, Single }
        public static SkinMode CurrentMode = SkinMode.None;
        public static string GlobalSelectedSkin = "";
        private static float _validationTimer = 0f;
        private const string SYNC_CHANNEL = "BOT:SkinV5";
        public static Dictionary<ulong, string> RemoteSkins = new Dictionary<ulong, string>();
        private static float _heartbeatTimer = 0f;

        public static bool ForceReshuffle = false;
        public static bool FastUpdateNextFrame = false;
        public static HashSet<string> DisabledSkins = new HashSet<string>();
        public static Dictionary<ulong, SkinConfig> RemoteSkinConfigs = new Dictionary<ulong, SkinConfig>();
        public static string BasePath => Path.Combine(Paths.PluginPath, "AntroSkins");
        private static string PlayersPath => Path.Combine(BasePath, "Players");

        private static Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> _loadedPrefabs = new Dictionary<string, GameObject>();

        private static Dictionary<string, string> ExactMixamoToCrab = new Dictionary<string, string>()
        {
            {"Hips", "hips"}, {"Spine", "spine"}, {"Spine1", "chest"}, {"Spine2", "upperchest"}, {"Neck", "neck"}, {"Head", "head"},
            {"LeftShoulder", "shoulderl"}, {"LeftArm", "upperarml"}, {"LeftForeArm", "lowerarml"}, {"LeftHand", "handl"},
            {"RightShoulder", "shoulderr"}, {"RightArm", "upperarmr"}, {"RightForeArm", "lowerarmr"}, {"RightHand", "handr"},
            {"LeftUpLeg", "legl"}, {"LeftLeg", "footl"}, {"LeftFoot", "toel"},
            {"RightUpLeg", "legr"}, {"RightLeg", "footr"}, {"RightFoot", "toer"}
        };

        private static string NormalizeName(string name) => name.ToLower().Replace(" ", "").Replace(".", "").Replace("_", "");
        public static string CleanBoneName(string name) => name.Replace("mixamorig:", "").Replace(":", "_").Replace("/", "_").Replace("|", "_");

        public static void InitNetwork()
        {
            if (BOTPlugin.HasModNet)
            {
                ModNet.RegisterMessageHandler(SYNC_CHANNEL, OnReceiveSkinSync);
            }
        }

        public static void Update()
        {
            SendHeartbeat();
            _validationTimer += Time.deltaTime;
            if (_validationTimer >= 1.5f)
            {
                _validationTimer = 0f;
                var onlinePlayers = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique>();
                ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                bool needUpdate = false;

                foreach (var p in onlinePlayers)
                {
                    if (p == null) continue;
                    ulong playerId = 0;
                    var pm = p.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                    if (pm != null && pm.steamProfile != null) playerId = pm.steamProfile.m_SteamID;
                    else playerId = (ulong)p.gameObject.GetInstanceID();

                    string expectedSkin = (playerId == myId || playerId == 0)
                        ? (CurrentMode == SkinMode.Single ? GlobalSelectedSkin : "")
                        : (RemoteSkins.ContainsKey(playerId) ? RemoteSkins[playerId] : "");

                    if (expectedSkin == "None") expectedSkin = "";

                    if (!string.IsNullOrEmpty(expectedSkin))
                    {
                        bool actuallyHasSkin = false;
                        foreach (Transform child in p.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name.StartsWith("AntroSkin_Player_")) { actuallyHasSkin = true; break; }
                        }
                        if (!actuallyHasSkin) { needUpdate = true; break; }
                    }
                }
                if (needUpdate) FastUpdateNextFrame = true;
            }
        }

        private static void SendHeartbeat()
        {
            if (!BOTPlugin.HasModNet) return;
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= 5.0f)
            {
                _heartbeatTimer = 0f;
                try
                {
                    Packet p = new Packet();
                    string mySkin = (CurrentMode == SkinMode.Single && !string.IsNullOrEmpty(GlobalSelectedSkin)) ? GlobalSelectedSkin : "None";
                    p.Write(mySkin);

                    if (mySkin != "None")
                    {
                        SkinConfig cfg = SkinsMenu.GetSkinConfig(mySkin);
                        p.Write(cfg.GlobalHeightOffset);
                        p.Write(cfg.BonePosOffsets.Count);
                        foreach (var kv in cfg.BonePosOffsets) { p.Write(kv.Key); p.Write(kv.Value); }
                        p.Write(cfg.BoneRotOffsets.Count);
                        foreach (var kv in cfg.BoneRotOffsets) { p.Write(kv.Key); p.Write(kv.Value); }
                    }
                    ModNet.SendMessage(SYNC_CHANNEL, p, Utility.GetAllLobbyMembers());
                }
                catch { }
            }
        }

        private static void OnReceiveSkinSync(ulong senderId, Packet p)
        {
            if (GameManager.Instance == null) return;
            try
            {
                string remoteSkin = p.ReadString();
                if (remoteSkin != "None")
                {
                    SkinConfig cfg = new SkinConfig();
                    cfg.GlobalHeightOffset = p.ReadFloat();
                    int posCount = p.ReadInt();
                    for (int i = 0; i < posCount; i++) cfg.BonePosOffsets[p.ReadString()] = p.ReadVector3();
                    int rotCount = p.ReadInt();
                    for (int i = 0; i < rotCount; i++) cfg.BoneRotOffsets[p.ReadString()] = p.ReadVector3();
                    RemoteSkinConfigs[senderId] = cfg;
                }
                RemoteSkins[senderId] = remoteSkin;
                FastUpdateNextFrame = true;
                BoneSynchronizer.ForceRecalculateAll();
            }
            catch { }
        }

        public static void InitFolders()
        {
            if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);
            if (!Directory.Exists(PlayersPath)) Directory.CreateDirectory(PlayersPath);
            AntroStuffManager.InitFolder();
        }

        public static List<string> GetAvailableSkins()
        {
            List<string> skins = new List<string>();
            if (Directory.Exists(PlayersPath))
                foreach (string file in Directory.GetFiles(PlayersPath, "*.bundle")) skins.Add(Path.GetFileName(file));
            return skins;
        }

        public static GameObject GetOrLoadPrefab(string skinName)
        {
            if (_loadedPrefabs.ContainsKey(skinName))
            {
                if (_loadedPrefabs[skinName] != null) return _loadedPrefabs[skinName];
                else _loadedPrefabs.Remove(skinName);
            }
            string path = Path.Combine(PlayersPath, skinName);
            if (!File.Exists(path)) return null;

            if (!_loadedBundles.ContainsKey(skinName))
            {
                try
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(path);
                    if (bundle == null) return null;
                    _loadedBundles[skinName] = bundle;
                }
                catch { return null; }
            }

            var assets = _loadedBundles[skinName].LoadAllAssets(Il2CppType.Of<GameObject>());
            foreach (var asset in assets)
            {
                var go = asset.Cast<GameObject>();
                if (go != null && go.transform.parent == null)
                {
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.SetActive(false);
                    _loadedPrefabs[skinName] = go;
                    return go;
                }
            }
            return null;
        }

        public static void DisableAll() { CurrentMode = SkinMode.None; GlobalSelectedSkin = ""; ApplyModels(); }
        public static void ApplySingleSkin(string skinName) { GlobalSelectedSkin = skinName; CurrentMode = SkinMode.Single; ApplyModels(); }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            for (int i = 0; i < obj.transform.childCount; i++) SetLayerRecursively(obj.transform.GetChild(i).gameObject, newLayer);
        }

        private static Bounds GetModelBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        public static void ApplyModels()
        {
            var onlinePlayers = UnityEngine.Object.FindObjectsOfType<MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique>();
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;

            foreach (var p in onlinePlayers)
            {
                if (p == null) continue;
                Transform pTransform = p.transform;
                Transform originalVisuals = pTransform.Find("PlayerModel");
                SkinnedMeshRenderer origSMR = null;

                if (originalVisuals == null)
                {
                    origSMR = pTransform.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (origSMR != null) originalVisuals = origSMR.transform;
                }
                else origSMR = originalVisuals.GetComponentInChildren<SkinnedMeshRenderer>();

                if (originalVisuals == null || origSMR == null) continue;

                Transform existingCustom = null;
                string currentAppliedSkin = "";

                foreach (Transform child in pTransform.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.StartsWith("AntroSkin_Player_"))
                    {
                        existingCustom = child;
                        currentAppliedSkin = child.name.Replace("AntroSkin_Player_", "");
                        break;
                    }
                }

                ulong playerId = 0;
                if (GameManager.Instance?.activePlayers != null)
                {
                    foreach (var kvp in GameManager.Instance.activePlayers)
                    {
                        if (kvp.Value != null && kvp.Value.gameObject == p.gameObject) { playerId = kvp.Key; break; }
                    }
                }
                if (playerId == 0)
                {
                    var pm = p.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                    if (pm != null && pm.steamProfile != null) playerId = pm.steamProfile.m_SteamID;
                }

                string desiredSkin = (playerId == myId || playerId == 0)
                    ? (CurrentMode == SkinMode.Single ? GlobalSelectedSkin : "")
                    : (RemoteSkins.ContainsKey(playerId) ? (RemoteSkins[playerId] == "None" ? "" : RemoteSkins[playerId]) : "");

                if (string.IsNullOrEmpty(desiredSkin))
                {
                    if (existingCustom != null) UnityEngine.Object.Destroy(existingCustom.gameObject);
                    foreach (var r in originalVisuals.GetComponentsInChildren<Renderer>()) r.enabled = true;
                    continue;
                }

                if (existingCustom != null && currentAppliedSkin == desiredSkin)
                {
                    foreach (var r in originalVisuals.GetComponentsInChildren<Renderer>()) r.enabled = false;
                    continue;
                }

                if (existingCustom != null && currentAppliedSkin != desiredSkin)
                {
                    UnityEngine.Object.Destroy(existingCustom.gameObject);
                    existingCustom = null;
                }

                Shader safeShader = Shader.Find("Standard");
                Renderer origRenderer = originalVisuals.GetComponentInChildren<Renderer>();
                if (origRenderer != null && origRenderer.sharedMaterial != null) safeShader = origRenderer.sharedMaterial.shader;

                origSMR.updateWhenOffscreen = true;
                Animator origAnim = originalVisuals.GetComponent<Animator>() ?? pTransform.GetComponentInChildren<Animator>();
                if (origAnim != null) origAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                if (existingCustom == null)
                {
                    GameObject prefab = GetOrLoadPrefab(desiredSkin);
                    if (prefab == null)
                    {
                        foreach (var r in originalVisuals.GetComponentsInChildren<Renderer>()) r.enabled = true;
                        continue;
                    }

                    foreach (var r in originalVisuals.GetComponentsInChildren<Renderer>()) r.enabled = false;

                    if (prefab != null)
                    {
                        GameObject spawned = UnityEngine.Object.Instantiate(prefab, pTransform);
                        spawned.name = $"AntroSkin_Player_{desiredSkin}";
                        spawned.SetActive(true);

                        foreach (var anim in spawned.GetComponentsInChildren<Animator>(true)) anim.enabled = false;
                        foreach (var cam in spawned.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.Destroy(cam);
                        foreach (var audio in spawned.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.Destroy(audio);
                        foreach (var col in spawned.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(col);
                        foreach (var rb in spawned.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.Destroy(rb);

                        SkinnedMeshRenderer customSMR = spawned.GetComponentInChildren<SkinnedMeshRenderer>();
                        if (customSMR != null) { customSMR.updateWhenOffscreen = true; customSMR.localBounds = new Bounds(Vector3.zero, new Vector3(50f, 50f, 50f)); }

                        foreach (var r in spawned.GetComponentsInChildren<Renderer>())
                        {
                            foreach (var mat in r.materials) if (mat != null) mat.shader = safeShader;
                        }

                        spawned.transform.localPosition = Vector3.zero;
                        spawned.transform.localRotation = prefab.transform.localRotation;

                        Bounds customBounds = GetModelBounds(spawned);
                        float targetHeight = 2.0f;
                        Bounds origBounds = GetModelBounds(originalVisuals.gameObject);
                        if (origBounds.size.y > 0.1f) targetHeight = origBounds.size.y;
                        if (customBounds.size.y > 0.01f)
                        {
                            float ratio = targetHeight / customBounds.size.y;
                            float uniformScale = spawned.transform.localScale.y * ratio;
                            spawned.transform.localScale = new Vector3(uniformScale, uniformScale, uniformScale);
                        }

                        BoneSynchronizer sync = spawned.AddComponent<BoneSynchronizer>();
                        if (customSMR != null) { UnityEngine.Object.Destroy(sync); sync = customSMR.gameObject.AddComponent<BoneSynchronizer>(); }

                        SyncData syncData = new SyncData();
                        syncData.SkinName = desiredSkin;
                        syncData.OwnerID = playerId;
                        syncData.RootTransform = spawned.transform;

                        Dictionary<string, Transform> normOrigBones = new Dictionary<string, Transform>();
                        foreach (Transform t in pTransform.GetComponentsInChildren<Transform>(true))
                        {
                            string normName = NormalizeName(t.name);
                            if (!normOrigBones.ContainsKey(normName)) normOrigBones[normName] = t;
                        }

                        foreach (Transform customT in spawned.GetComponentsInChildren<Transform>(true))
                        {
                            string cleanCustomName = CleanBoneName(customT.name);
                            if (!syncData.AllCustomTransforms.ContainsKey(cleanCustomName))
                            {
                                syncData.AllCustomTransforms.Add(cleanCustomName, customT);
                                syncData.InitialLocalPositions.Add(cleanCustomName, customT.localPosition);
                                syncData.InitialLocalRotations.Add(cleanCustomName, customT.localRotation);
                            }

                            if (ExactMixamoToCrab.TryGetValue(cleanCustomName, out string targetNormName))
                            {
                                if (normOrigBones.TryGetValue(targetNormName, out Transform origT))
                                {
                                    syncData.CustomBones.Add(customT);
                                    syncData.OriginalBones.Add(origT);
                                    if (cleanCustomName == "Hips") { syncData.CustomHips = customT; syncData.OriginalHips = origT; }
                                    if (cleanCustomName == "RightHand") { syncData.CustomRightHand = customT; syncData.OriginalRightHand = origT; }
                                }
                            }
                        }
                        BoneSynchronizer.DataMap[sync.Pointer] = syncData;

                        int correctLayer = (playerId == myId || playerId == 0) ? 8 : 0;
                        SetLayerRecursively(spawned, correctLayer);
                    }
                }
            }
            if (ForceReshuffle) ForceReshuffle = false;
            AntroStuffManager.ApplyModels();
        }
    }

    public class SkinsMenu : MonoBehaviour
    {
        public SkinsMenu(IntPtr ptr) : base(ptr) { }
        public static bool IsWindowOpen = false;
        private Rect _windowRect = new Rect(60, 60, 520, 680);
        private Vector2 _scrollPos = Vector2.zero;
        private Vector2 _bonesScrollPos = Vector2.zero;

        private List<string> _availablePlayerSkins = new List<string>();
        private int _currentTab = 0;
        private string[] _tabs = { "皮肤列表 (Skins List)", "骨骼微调 (Bone Tweaker)" };
        private int _categoryTab = 0;
        private string[] _categories = { "玩家 (Players)", "物品 (Weapons)" };
        public string _selectedSkinInList = "";

        public static Dictionary<string, SkinConfig> SkinConfigs = new Dictionary<string, SkinConfig>();

        public static SkinConfig GetSkinConfig(string skinName)
        {
            if (string.IsNullOrEmpty(skinName)) return new SkinConfig();
            if (!SkinConfigs.ContainsKey(skinName)) SkinConfigs[skinName] = new SkinConfig();
            return SkinConfigs[skinName];
        }

        private string _globalHeightStr;
        private string[] _currentSkinBones = new string[0];
        private string _lastLoadedBonesSkin = "";
        private int _selectedBoneIndex = 0;
        private string _currentBoneEditing = "";
        private string _posXStr, _posYStr, _posZStr, _rotXStr, _rotYStr, _rotZStr;
        private bool _editPositionMode = false;

        public void Start() { RefreshLists(); }

        public void Update()
        {
            if (AntroSkinManager.FastUpdateNextFrame)
            {
                AntroSkinManager.FastUpdateNextFrame = false;
                AntroSkinManager.ApplyModels();
            }
        }

        public void OnGUI()
        {
            if (IsWindowOpen)
            {
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                _windowRect = GUI.Window(8889, _windowRect, (Action<int>)DrawWindow, "自定义模型系统 (Custom Avatars) - BOT Port");
                GUI.backgroundColor = Color.white;
            }
        }

        private void RefreshLists()
        {
            _availablePlayerSkins = AntroSkinManager.GetAvailableSkins();
            if (!_availablePlayerSkins.Contains(_selectedSkinInList))
            {
                bool isStuffSkin = false;
                foreach (string itemType in AntroStuffManager.StuffTypes)
                {
                    if (AntroStuffManager.GetAvailableSkins(itemType).Contains(_selectedSkinInList))
                    {
                        isStuffSkin = true; break;
                    }
                }
                if (!isStuffSkin) _selectedSkinInList = "";
            }
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Space(5);
            _currentTab = GUILayout.Toolbar(_currentTab, _tabs, GUILayout.Height(30));
            GUILayout.Space(10);

            if (_currentTab == 0) DrawSkinsTab();
            else if (_currentTab == 1) DrawTweakerTab();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawSkinsTab()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新本地文件 (Refresh Files)", GUILayout.Height(30))) RefreshLists();

            GUI.color = Color.yellow;
            if (GUILayout.Button("打开模型文件夹 (Open Folder)", GUILayout.Height(30)))
            {
                try { System.Diagnostics.Process.Start(AntroSkinManager.BasePath); } catch { }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            bool remState = GUILayout.Toggle(AntroConfigManager.RememberState.Value, " 记住当前配置并在下次启动时加载 (Remember Settings)");
            if (remState != AntroConfigManager.RememberState.Value)
            {
                AntroConfigManager.RememberState.Value = remState;
                AntroConfigManager.SaveSettings();
            }

            GUILayout.Space(10);
            _categoryTab = GUILayout.Toolbar(_categoryTab, _categories, GUILayout.Height(25));
            GUILayout.Space(10);

            if (_categoryTab == 0) DrawPlayersCategory();
            else
            {
                List<string> specificSkins = AntroStuffManager.GetAvailableSkins(StuffMenuUI.SelectedItemType);
                StuffMenuUI.Draw(this, specificSkins);
            }
        }

        private void DrawPlayersCategory()
        {
            GUILayout.Label("--- 操作菜单 (FUNCTIONS) ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            GUI.color = AntroSkinManager.CurrentMode == AntroSkinManager.SkinMode.Single ? Color.green : Color.white;
            GUI.enabled = _availablePlayerSkins.Contains(_selectedSkinInList);
            if (GUILayout.Button($"应用到自己 (APPLY TO SELF): {(_availablePlayerSkins.Contains(_selectedSkinInList) ? _selectedSkinInList : "请在下方列表中选择...")}", GUILayout.Height(35)))
            {
                AntroSkinManager.ApplySingleSkin(_selectedSkinInList);
                AntroConfigManager.SaveSettings();
            }
            GUI.enabled = true;

            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("脱下皮肤 / 恢复原版 (DISABLE SKIN)", GUILayout.Height(35)))
            {
                AntroSkinManager.DisableAll();
                AntroConfigManager.SaveSettings();
            }
            GUI.color = Color.white;

            GUILayout.Space(15);
            GUILayout.Label($"--- 本地模型文件 (LOCAL BUNDLES): {_availablePlayerSkins.Count} ---");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.box);
            if (_availablePlayerSkins.Count == 0) GUILayout.Label("未在 BepInEx/plugins/AntroSkins/Players/ 找到 .bundle 文件");
            else
            {
                foreach (string pack in _availablePlayerSkins)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = _selectedSkinInList == pack ? Color.cyan : Color.white;
                    if (GUILayout.Button(pack, GUILayout.Height(30)))
                    {
                        _selectedSkinInList = pack;
                        ResetInputStrings();
                    }
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        private void LoadSkinBones(string skinName)
        {
            _lastLoadedBonesSkin = skinName;
            _selectedBoneIndex = 0;
            ResetInputStrings();

            bool isStuff = false;
            GameObject prefab = AntroSkinManager.GetOrLoadPrefab(skinName);

            if (prefab == null)
            {
                foreach (string itemType in AntroStuffManager.StuffTypes)
                {
                    prefab = AntroStuffManager.GetOrLoadPrefab(itemType, skinName);
                    if (prefab != null) { isStuff = true; break; }
                }
            }

            if (prefab == null) { _currentSkinBones = new string[0]; return; }

            List<string> boneNames = new List<string>();
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t == prefab.transform) continue;
                string cleanName = AntroSkinManager.CleanBoneName(t.name);
                if (!boneNames.Contains(cleanName)) boneNames.Add(cleanName);
            }
            if (isStuff && !boneNames.Contains("Hips")) boneNames.Insert(0, "Hips");
            _currentSkinBones = boneNames.ToArray();
        }

        private void DrawTweakerTab()
        {
            if (string.IsNullOrEmpty(_selectedSkinInList))
            {
                GUILayout.Label("请先在左侧 '皮肤列表(Skins List)' 中选择一个模型!", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red }, fontStyle = FontStyle.Bold });
                return;
            }
            if (_selectedSkinInList != _lastLoadedBonesSkin) LoadSkinBones(_selectedSkinInList);
            SkinConfig currentCfg = GetSkinConfig(_selectedSkinInList);

            GUILayout.Label($"当前微调模型 (Tweaking): {_selectedSkinInList}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } });
            GUILayout.Space(5);

            GUILayout.Label("--- 全局高度 (GLOBAL HEIGHT) ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            if (_globalHeightStr == null) _globalHeightStr = currentCfg.GlobalHeightOffset.ToString("F3");
            float newHeight = DrawAxisRow("高度 (Y轴):", ref _globalHeightStr, currentCfg.GlobalHeightOffset, -2.5f, 2.5f, "F3");

            if (Mathf.Abs(newHeight - currentCfg.GlobalHeightOffset) > 0.0001f)
            {
                currentCfg.GlobalHeightOffset = newHeight;
                AntroConfigManager.SaveSettings();
                BoneSynchronizer.ForceRecalculateAll();
                AntroSkinManager.FastUpdateNextFrame = true;
            }

            GUILayout.Space(10);
            GUILayout.Label("--- 骨骼编辑器 (BONE EDITOR) ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            if (_currentSkinBones.Length == 0)
            {
                GUILayout.Label("未找到骨骼数据", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.gray } });
                return;
            }

            _bonesScrollPos = GUILayout.BeginScrollView(_bonesScrollPos, GUI.skin.box, GUILayout.Height(150));
            int newBoneIndex = GUILayout.SelectionGrid(_selectedBoneIndex, _currentSkinBones, 2);
            GUILayout.EndScrollView();

            if (newBoneIndex != _selectedBoneIndex) { _selectedBoneIndex = newBoneIndex; ResetInputStrings(); }
            if (_selectedBoneIndex >= _currentSkinBones.Length) _selectedBoneIndex = 0;
            string currentBone = _currentSkinBones[_selectedBoneIndex];

            if (_currentBoneEditing != currentBone) { _currentBoneEditing = currentBone; ResetInputStrings(); }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.color = !_editPositionMode ? Color.yellow : Color.white;
            if (GUILayout.Button("编辑旋转 (ROTATION)", GUILayout.Height(30))) { if (_editPositionMode) ResetInputStrings(); _editPositionMode = false; }
            GUI.color = _editPositionMode ? Color.yellow : Color.white;
            if (GUILayout.Button("编辑位置 (POSITION)", GUILayout.Height(30))) { if (!_editPositionMode) ResetInputStrings(); _editPositionMode = true; }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"正在调整 {(_editPositionMode ? "位置(POSITION)" : "旋转(ROTATION)")}: {currentBone}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            bool changed = false;

            if (_editPositionMode)
            {
                if (!currentCfg.BonePosOffsets.ContainsKey(currentBone)) currentCfg.BonePosOffsets[currentBone] = Vector3.zero;
                Vector3 pos = currentCfg.BonePosOffsets[currentBone];
                if (_posXStr == null) _posXStr = pos.x.ToString("F4"); if (_posYStr == null) _posYStr = pos.y.ToString("F4"); if (_posZStr == null) _posZStr = pos.z.ToString("F4");

                float nx = DrawAxisRow("X 轴:", ref _posXStr, pos.x, -2.5f, 2.5f, "F4");
                float ny = DrawAxisRow("Y 轴:", ref _posYStr, pos.y, -2.5f, 2.5f, "F4");
                float nz = DrawAxisRow("Z 轴:", ref _posZStr, pos.z, -2.5f, 2.5f, "F4");

                if (nx != pos.x || ny != pos.y || nz != pos.z) changed = true;
                currentCfg.BonePosOffsets[currentBone] = new Vector3(nx, ny, nz);
            }
            else
            {
                if (!currentCfg.BoneRotOffsets.ContainsKey(currentBone)) currentCfg.BoneRotOffsets[currentBone] = Vector3.zero;
                Vector3 rot = currentCfg.BoneRotOffsets[currentBone];
                if (_rotXStr == null) _rotXStr = rot.x.ToString("F2"); if (_rotYStr == null) _rotYStr = rot.y.ToString("F2"); if (_rotZStr == null) _rotZStr = rot.z.ToString("F2");

                float rx = DrawAxisRow("X 轴:", ref _rotXStr, rot.x, -180f, 180f, "F2");
                float ry = DrawAxisRow("Y 轴:", ref _rotYStr, rot.y, -180f, 180f, "F2");
                float rz = DrawAxisRow("Z 轴:", ref _rotZStr, rot.z, -180f, 180f, "F2");

                if (rx != rot.x || ry != rot.y || rz != rot.z) changed = true;
                currentCfg.BoneRotOffsets[currentBone] = new Vector3(rx, ry, rz);
            }

            if (changed)
            {
                AntroConfigManager.SaveSettings();
                BoneSynchronizer.ForceRecalculateAll();
                AntroSkinManager.FastUpdateNextFrame = true;
            }

            GUILayout.Space(10);
            if (GUILayout.Button("重置该骨骼数据 (Reset to 0,0,0)", GUILayout.Height(30)))
            {
                if (_editPositionMode) currentCfg.BonePosOffsets[currentBone] = Vector3.zero;
                else currentCfg.BoneRotOffsets[currentBone] = Vector3.zero;
                ResetInputStrings();
                AntroConfigManager.SaveSettings();
                BoneSynchronizer.ForceRecalculateAll();
                AntroSkinManager.FastUpdateNextFrame = true;
            }
        }

        public void ResetInputStrings() { _posXStr = null; _posYStr = null; _posZStr = null; _rotXStr = null; _rotYStr = null; _rotZStr = null; _globalHeightStr = null; }

        private float DrawAxisRow(string label, ref string inputStr, float currentValue, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(65));
            inputStr = GUILayout.TextField(inputStr, GUILayout.Width(60));
            string parsedStr = inputStr.Replace(',', '.');
            if (float.TryParse(parsedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsedVal)) currentValue = parsedVal;
            float clampedCurrent = Mathf.Clamp(currentValue, min, max);
            float newValue = GUILayout.HorizontalSlider(currentValue, min, max);
            if (Mathf.Abs(newValue - clampedCurrent) > 0.0001f) { currentValue = newValue; inputStr = currentValue.ToString(format); }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            return currentValue;
        }
    }

    public static class StuffMenuUI
    {
        public static string SelectedItemType = "ak";
        private static Vector2 _itemsScrollPos = Vector2.zero;
        private static Vector2 _bundlesScrollPos = Vector2.zero;

        public static void Draw(SkinsMenu menuInstance, List<string> availableStuffSkins)
        {
            GUILayout.Label("--- 武器与物品模型 (WEAPONS & STUFF) ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label("先在左侧选择武器类型，再在右侧选择要套用的模型。");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(140));
            _itemsScrollPos = GUILayout.BeginScrollView(_itemsScrollPos, GUI.skin.box);
            foreach (string item in AntroStuffManager.StuffTypes)
            {
                AntroStuffManager.ActiveSkins.TryGetValue(item, out string assignedSkin);
                bool hasSkin = !string.IsNullOrEmpty(assignedSkin);
                GUI.color = SelectedItemType == item ? Color.yellow : (hasSkin ? Color.green : Color.white);
                if (GUILayout.Button(item.ToUpper() + (hasSkin ? " [ON]" : ""), GUILayout.Height(30))) SelectedItemType = item;
            }
            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button($"移除该类型的皮肤: {SelectedItemType.ToUpper()}", GUILayout.Height(30)))
            {
                AntroStuffManager.ActiveSkins.Remove(SelectedItemType);
                AntroConfigManager.SaveSettings();
                AntroSkinManager.FastUpdateNextFrame = true;
            }

            GUILayout.Space(5);
            GUILayout.Label($"AntroSkins/Stuff/ 内的模型文件:");

            _bundlesScrollPos = GUILayout.BeginScrollView(_bundlesScrollPos, GUI.skin.box);
            if (availableStuffSkins.Count == 0) GUILayout.Label("该类型下未找到 .bundle 文件");
            else
            {
                foreach (string pack in availableStuffSkins)
                {
                    AntroStuffManager.ActiveSkins.TryGetValue(SelectedItemType, out string currentSkinForItem);
                    bool isAppliedHere = currentSkinForItem == pack;

                    GUI.color = isAppliedHere ? Color.green : (menuInstance._selectedSkinInList == pack ? Color.cyan : Color.white);
                    if (GUILayout.Button(pack, GUILayout.Height(30)))
                    {
                        menuInstance._selectedSkinInList = pack;
                        AntroStuffManager.ActiveSkins[SelectedItemType] = pack;
                        AntroConfigManager.SaveSettings();
                        AntroSkinManager.FastUpdateNextFrame = true;
                        menuInstance.ResetInputStrings();
                    }
                }
                GUI.color = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }

    [HarmonyPatch(typeof(MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique), "Start")]
    public static class AntroPlayerSpawnPatch
    {
        [HarmonyPostfix]
        public static void Postfix() { AntroSkinManager.FastUpdateNextFrame = true; }
    }
}
