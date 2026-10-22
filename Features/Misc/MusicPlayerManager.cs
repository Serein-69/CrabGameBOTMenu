using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using TMPro;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BOTMenuMod
{
    public class MusicPlayerManager : MonoBehaviour
    {
        public static MusicPlayerManager Instance;
        public static bool menuVisible = false;
        public static bool lyricVisible = false;

        public static string currentPlayingName = "No Song";
        public static bool isPlaying = false;
        private static float currentVolume = 0.5f;
        private const string currentSource = "netease";

        private static string MusicApiBase => BOTPlugin.ConfigMusicApiBase != null ? BOTPlugin.ConfigMusicApiBase.Value : "";
        private static string MusicReferer => BOTPlugin.ConfigMusicReferer != null ? BOTPlugin.ConfigMusicReferer.Value : "";

        private Rect panelRect;
        private float panelWidth = 350f;
        private float panelHeight = 390f;
        private GUIStyle boxStyle, buttonStyle, labelStyle, inputStyle, listButtonStyle;
        private bool stylesReady = false;

        private string searchQuery = "";
        private static string statusMessage = "Idle";
        private static float statusTimer = 0f;

        private static int _requestId = 0;
        private static System.Threading.CancellationTokenSource _requestCts = null;

        private static int currentColorIndex = 0;
        private static readonly Color[] lyricColors = new Color[]
        {
            new Color(1f, 0.85f, 0.2f, 1f),
            Color.white,
            Color.green,
            Color.cyan,
            new Color(1f, 0.5f, 0.8f, 1f),
            Color.yellow
        };
        public static Color currentLyricColor = lyricColors[0];

        public struct LyricLine { public float time; public string text; }
        private static List<LyricLine> activeLyrics = new List<LyricLine>();
        private static string currentLyricText = "";
        private static float currentPlaybackTime = 0f;
        private static float totalDurationSeconds = 0f;

        private static Rect lyricRect = new Rect(200f, 600f, 500f, 60f);
        private GUIStyle lyricStyleSetup, lyricStyleActive, lyricStyleShadow, lyricWindowStyle;

        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private static readonly Queue<string> mainThreadLogQueue = new Queue<string>();
        private static readonly object logLock = new object();

        private static readonly HttpClient httpClient = InitializeHttpClient();

        private static string TempFilePath => Path.Combine(Paths.GameRootPath, "temp_bgm.mp3");

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string lpszCommand, StringBuilder lpszReturnString, int cchReturn, IntPtr hwndCallback);

        private static HttpClient InitializeHttpClient()
        {
            var client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                if (!string.IsNullOrEmpty(MusicReferer)) client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", MusicReferer);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[MusicPlayer] Failed to set spoofing headers: " + ex.Message);
            }
            return client;
        }

        public struct SongInfo { public string id; public string name; public string artist; public string source; }
        private static List<SongInfo> searchResults = new List<SongInfo>();

        public MusicPlayerManager(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLyricPosition();
            LogOnMainThread("Windows Native BGM Player initialized.");
        }

        private void Update()
        {
            if (statusTimer > 0) statusTimer -= Time.deltaTime;

            if (isPlaying)
            {
                try
                {
                    StringBuilder sb = new StringBuilder(64);
                    mciSendString("status SecretBGM position", sb, sb.Capacity, IntPtr.Zero);
                    if (int.TryParse(sb.ToString(), out int ms))
                    {
                        currentPlaybackTime = ms / 1000f;
                        UpdateActiveLyric(currentPlaybackTime);
                    }
                }
                catch { }
            }

            lock (logLock)
            {
                while (mainThreadLogQueue.Count > 0)
                {
                    string msg = mainThreadLogQueue.Dequeue();
                    if (msg.StartsWith("[ERR] "))
                        Debug.LogError(msg.Substring(6));
                    else
                        Debug.Log(msg.Substring(6));
                }
            }

            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                {
                    var action = mainThreadActions.Dequeue();
                    try { action?.Invoke(); }
                    catch (Exception ex) { LogOnMainThread("Main thread action error: " + ex.Message, true); }
                }
            }
        }

        private void OnDestroy()
        {
            mciSendString("close SecretBGM", null, 0, IntPtr.Zero);
            DeleteTempFile();
        }

        public static void CloseMenu()
        {
            menuVisible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void LogOnMainThread(string message, bool isError = false)
        {
            lock (logLock) { mainThreadLogQueue.Enqueue((isError ? "[ERR] " : "[INF] ") + message); }
        }

        private static void DeleteTempFile()
        {
            try
            {
                if (File.Exists(TempFilePath))
                    File.Delete(TempFilePath);
            }
            catch { }
        }

        private static string CustomUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            StringBuilder sb = new StringBuilder();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            foreach (byte b in bytes)
            {
                if ((b >= 48 && b <= 57) || (b >= 65 && b <= 90) || (b >= 97 && b <= 122) ||
                    b == '-' || b == '_' || b == '.' || b == '~')
                    sb.Append((char)b);
                else if (b == ' ') sb.Append("%20");
                else sb.Append('%' + b.ToString("X2"));
            }
            return sb.ToString();
        }

        public void StopMusic()
        {
            System.Threading.Interlocked.Increment(ref _requestId);
            System.Threading.CancellationTokenSource pendingCts = _requestCts;
            _requestCts = null;
            if (pendingCts != null)
            {
                try { pendingCts.Cancel(); pendingCts.Dispose(); } catch { }
            }

            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(() =>
                {
                    mciSendString("stop SecretBGM", null, 0, IntPtr.Zero);
                    mciSendString("close SecretBGM", null, 0, IntPtr.Zero);
                    DeleteTempFile();
                });
            }
            isPlaying = false;
            currentPlayingName = "No Song";
            currentLyricText = "";
            activeLyrics.Clear();
            totalDurationSeconds = 0f;
            statusMessage = "Stopped";
            statusTimer = 2f;
        }

        public void TogglePause()
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(() =>
                {
                    if (isPlaying)
                    {
                        mciSendString("pause SecretBGM", null, 0, IntPtr.Zero);
                        isPlaying = false;
                        statusMessage = "Paused";
                    }
                    else
                    {
                        mciSendString("resume SecretBGM", null, 0, IntPtr.Zero);
                        isPlaying = true;
                        statusMessage = "Playing";
                    }
                    statusTimer = 2f;
                });
            }
        }

        public static void SetMciVolume(float vol)
        {
            currentVolume = vol;
            if (currentVolume < 0f) currentVolume = 0f;
            if (currentVolume > 1f) currentVolume = 1f;

            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(() =>
                {
                    try
                    {
                        int mciVol = (int)(currentVolume * 1000f);
                        mciSendString($"setaudio SecretBGM volume to {mciVol}", null, 0, IntPtr.Zero);
                    }
                    catch (Exception ex) { LogOnMainThread("Setaudio failed: " + ex.Message, true); }
                });
            }
        }

        public void StartSearch(string songName)
        {
            if (string.IsNullOrWhiteSpace(songName)) return;
            Task.Run(() => SearchWithRetryAsync(songName));
        }

        private static async Task SearchWithRetryAsync(string songName)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                await SearchSongsAsync(songName);
                if (searchResults.Count > 0 || statusMessage != "No Results")
                    break;
                await Task.Delay(1000);
            }
        }

        private static async Task SearchSongsAsync(string songName)
        {
            if (string.IsNullOrEmpty(MusicApiBase))
            {
                statusMessage = "Music API not configured";
                statusTimer = 4f;
                return;
            }
            statusMessage = "Searching...";
            statusTimer = 5f;
            searchResults.Clear();

            string searchUrl = $"{MusicApiBase}?types=search&source={currentSource}&name={CustomUrlEncode(songName)}&count=3";
            LogOnMainThread($"Requesting Search API: {searchUrl}");

            try
            {
                string json = await httpClient.GetStringAsync(searchUrl);
                if (string.IsNullOrEmpty(json) || json.Trim() == "[]")
                {
                    statusMessage = "No Results";
                    return;
                }

                var matches = Regex.Matches(json, @"\{[^{}]*?\}");
                foreach (Match match in matches)
                {
                    string block = match.Value;
                    string id = Regex.Match(block, @"""id""\s*:\s*""?(\d+)""?").Groups[1].Value;
                    string name = Regex.Match(block, @"""name""\s*:\s*""([^""]+)""").Groups[1].Value;
                    string artist = Regex.Match(block, @"""artist""\s*:\s*\[\s*""([^""]+)""").Groups[1].Value;
                    string source = Regex.Match(block, @"""source""\s*:\s*""([^""]+)""").Groups[1].Value;

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    {
                        searchResults.Add(new SongInfo
                        {
                            id = id,
                            name = name,
                            artist = string.IsNullOrEmpty(artist) ? "Unknown" : artist,
                            source = string.IsNullOrEmpty(source) ? "netease" : source
                        });
                    }
                }

                if (searchResults.Count == 0)
                    statusMessage = "No Results";
                else
                    statusMessage = $"Found {searchResults.Count} songs";
            }
            catch (Exception ex)
            {
                LogOnMainThread("Search Task failed: " + ex.Message, true);
                statusMessage = "Search Failed";
            }
        }

        public void PlaySelectedSong(string trackId, string source, string displayName)
        {
            int reqId = System.Threading.Interlocked.Increment(ref _requestId);

            System.Threading.CancellationTokenSource oldCts = _requestCts;
            System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
            _requestCts = cts;
            if (oldCts != null)
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }

            System.Threading.CancellationToken token = cts.Token;
            Task.Run(() => PlayMusicAsync(trackId, source, displayName, reqId, token));
        }

        private static async Task PlayMusicAsync(string trackId, string source, string displayName, int reqId, System.Threading.CancellationToken token)
        {
            if (string.IsNullOrEmpty(MusicApiBase))
            {
                statusMessage = "Music API not configured";
                statusTimer = 4f;
                return;
            }
            if (reqId != _requestId) return;

            statusMessage = "Fetching Link...";
            statusTimer = 5f;
            currentPlayingName = displayName;
            activeLyrics.Clear();
            currentLyricText = "...";

            try
            {
                string urlApi = $"{MusicApiBase}?types=url&source={source}&id={trackId}&br=128";
                string urlJson = await httpClient.GetStringAsync(urlApi);

                if (reqId != _requestId || token.IsCancellationRequested) return;

                string musicUrl = Regex.Match(urlJson, @"""url""\s*:\s*""([^"" ]*)""").Groups[1].Value;
                musicUrl = musicUrl.Replace("\\/", "/");

                if (string.IsNullOrEmpty(musicUrl))
                {
                    if (reqId != _requestId) return;
                    LogOnMainThread($"[Blocked] API returned empty playable link for {displayName}. Try switching music source!");
                    statusMessage = "Copyright Blocked";
                    currentPlayingName = "Blocked";
                    return;
                }

                statusMessage = "Downloading...";
                LogOnMainThread("HttpClient streaming bytes from direct link...");

                _ = FetchLyricsAsync(trackId, source, reqId);

                byte[] audioBytes = await httpClient.GetByteArrayAsync(musicUrl);

                if (reqId != _requestId || token.IsCancellationRequested) return;

                lock (mainThreadActions)
                {
                    mainThreadActions.Enqueue(() =>
                    {
                        if (reqId != _requestId) return;

                        mciSendString("stop SecretBGM", null, 0, IntPtr.Zero);
                        mciSendString("close SecretBGM", null, 0, IntPtr.Zero);

                        DeleteTempFile();
                        File.WriteAllBytes(TempFilePath, audioBytes);
                        string cleanPath = TempFilePath.Replace("\\", "/");

                        LogOnMainThread("MCI Player Attempting to stream: " + cleanPath);
                        statusMessage = "Playing...";

                        int openResult = mciSendString($"open \"{cleanPath}\" type mpegvideo alias SecretBGM", null, 0, IntPtr.Zero);
                        if (openResult != 0)
                            openResult = mciSendString($"open \"{cleanPath}\" alias SecretBGM", null, 0, IntPtr.Zero);

                        if (openResult != 0)
                        {
                            LogOnMainThread("System player failed to open local file. Error code: " + openResult, true);
                            statusMessage = "Play Failed";
                            return;
                        }

                        mciSendString("set SecretBGM time format milliseconds", null, 0, IntPtr.Zero);

                        StringBuilder lenStr = new StringBuilder(64);
                        mciSendString("status SecretBGM length", lenStr, lenStr.Capacity, IntPtr.Zero);
                        if (int.TryParse(lenStr.ToString(), out int totalMs))
                            totalDurationSeconds = totalMs / 1000f;
                        else totalDurationSeconds = 0f;

                        mciSendString("play SecretBGM", null, 0, IntPtr.Zero);

                        int mciVol = (int)(currentVolume * 1000f);
                        mciSendString($"setaudio SecretBGM volume to {mciVol}", null, 0, IntPtr.Zero);

                        isPlaying = true;
                        statusMessage = "Playing";
                    });
                }
            }
            catch (Exception ex)
            {
                if (reqId != _requestId) return;
                LogOnMainThread("Play Task failed: " + ex.Message, true);
                statusMessage = "Playback Failed";
            }
        }

        private static async Task FetchLyricsAsync(string trackId, string source, int reqId)
        {
            try
            {
                string lyricUrl = $"{MusicApiBase}?types=lyric&source={source}&id={trackId}";
                string json = await httpClient.GetStringAsync(lyricUrl);

                if (reqId != _requestId) return;

                string lyricRaw = Regex.Match(json, @"""lyric""\s*:\s*""([^""]*)""").Groups[1].Value;
                lyricRaw = lyricRaw.Replace("\\n", "\n").Replace("\\r", "\r");

                if (string.IsNullOrEmpty(lyricRaw))
                {
                    if (reqId != _requestId) return;
                    currentLyricText = T("No Lyrics Found", "暂无歌词文本", "Текст не найден", "Şarkı Sözü Bulunamadı");
                    return;
                }

                var rows = lyricRaw.Split('\n');
                var parsedList = new List<LyricLine>();
                foreach (var row in rows)
                {
                    var match = Regex.Match(row.Trim(), @"\[(\d+):(\d+)(?:\.(\d+))?\](.*)");
                    if (!match.Success) continue;

                    int min = int.Parse(match.Groups[1].Value);
                    int sec = int.Parse(match.Groups[2].Value);
                    int ms = 0;
                    string msStr = match.Groups[3].Value;
                    if (!string.IsNullOrEmpty(msStr))
                        ms = msStr.Length == 2 ? int.Parse(msStr) * 10 : int.Parse(msStr);

                    string text = match.Groups[4].Value.Trim();
                    parsedList.Add(new LyricLine { time = min * 60f + sec + ms / 1000f, text = text });
                }

                if (reqId != _requestId) return;

                activeLyrics = parsedList.OrderBy(l => l.time).ToList();
                LogOnMainThread($"[Lyrics] Successfully parsed {activeLyrics.Count} lyric lines.");
            }
            catch (Exception ex)
            {
                if (reqId != _requestId) return;
                LogOnMainThread("Failed to fetch/parse lyrics: " + ex.Message, true);
                currentLyricText = T("Load Lyrics Failed", "歌词加载失败", "Ошибка загрузки текста", "Sözler Yüklenemedi");
            }
        }

        private static void UpdateActiveLyric(float time)
        {
            if (activeLyrics == null || activeLyrics.Count == 0) return;

            string activeText = "";
            for (int i = 0; i < activeLyrics.Count; i++)
            {
                if (time >= activeLyrics[i].time)
                    activeText = activeLyrics[i].text;
                else break;
            }

            if (!string.IsNullOrEmpty(activeText))
                currentLyricText = activeText;
        }

        private static void LoadLyricPosition()
        {
            try
            {
                string dir = Path.Combine(Paths.ConfigPath, "BOTMod");
                string path = Path.Combine(dir, "lyric_position.txt");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    if (lines.Length >= 2 && float.TryParse(lines[0], out float x) && float.TryParse(lines[1], out float y))
                        lyricRect = new Rect(x, y, 500f, 60f);
                }
            }
            catch { }
        }

        private static void SaveLyricPosition(float x, float y)
        {
            try
            {
                string dir = Path.Combine(Paths.ConfigPath, "BOTMod");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(Path.Combine(dir, "lyric_position.txt"), new string[] { x.ToString(), y.ToString() });
            }
            catch { }
        }

        public static void CycleLyricColor()
        {
            currentColorIndex = (currentColorIndex + 1) % lyricColors.Length;
            currentLyricColor = lyricColors[currentColorIndex];
        }

        private void OnGUI()
        {
            if (!stylesReady) { CreateStyles(); stylesReady = true; }

            if (menuVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (panelRect.width == 0)
                    panelRect = new Rect(40f, (Screen.height - panelHeight) / 2f, panelWidth, panelHeight);

                string windowTitle = T("BOT Music Player", "BOT 音乐播放器", "BOT Музыкальный плеер", "BOT Müzik Çalar");
                panelRect = GUI.Window(101, panelRect, (GUI.WindowFunction)DrawMusicWindow, windowTitle, boxStyle);
            }

            if (lyricVisible)
            {
                float oldX = lyricRect.x, oldY = lyricRect.y;
                lyricRect = GUI.Window(102, lyricRect, (GUI.WindowFunction)DrawLyricWindow, "", lyricWindowStyle);
                if (lyricRect.x != oldX || lyricRect.y != oldY)
                    SaveLyricPosition(lyricRect.x, lyricRect.y);
            }
        }

        private void DrawMusicWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, panelWidth, 25));

            string nowPlayingLabel = T("Now Playing:", "当前播放:", "Сейчас играет:", "Şimdi Oynatılıyor:");
            string noSongLabel = T("No Song", "未在播放", "Нет песни", "Şarkı Yok");
            string songDisplayName = currentPlayingName == "No Song" ? noSongLabel : currentPlayingName;

            GUI.Label(new Rect(15, 30, panelWidth - 30, 40), $"<b>{nowPlayingLabel}</b>\n<color=cyan>{songDisplayName}</color>", labelStyle);

            searchQuery = GUI.TextField(new Rect(15, 75, 140, 30), searchQuery, inputStyle);

            string sourceBtnLabel = currentSource.ToUpper();
            GUI.Button(new Rect(160, 75, 65, 30), sourceBtnLabel, buttonStyle);

            string searchBtnLabel = T("Search", "搜索", "Поиск", "Ara");
            if (GUI.Button(new Rect(230, 75, 75, 30), searchBtnLabel, buttonStyle))
            {
                StartSearch(searchQuery);
            }

            string selectSongLabel = T("Select Song to Play:", "选择歌曲进行播放:", "Выберите песню для воспроизведения:", "Oynatılacak Şarkıyı Seçin:");
            GUI.Label(new Rect(15, 115, panelWidth - 30, 20), $"<b>{selectSongLabel}</b>", labelStyle);

            float startY = 135f;
            for (int i = 0; i < 3; i++)
            {
                if (i < searchResults.Count)
                {
                    var song = searchResults[i];
                    string btnLabel = $"▶ {song.name} - {song.artist}";
                    if (GUI.Button(new Rect(15, startY + (i * 32), panelWidth - 30, 28), btnLabel, listButtonStyle))
                    {
                        PlaySelectedSong(song.id, song.source, $"{song.name} - {song.artist}");
                    }
                }
                else
                {
                    string placeholderText = T("Enter keywords and search...", "输入关键词并搜索...", "Введите ключевые слова...", "Anahtar kelimeler girin...");
                    GUI.Box(new Rect(15, startY + (i * 32), panelWidth - 30, 28), i == 0 && searchResults.Count == 0 ? placeholderText : "", listButtonStyle);
                }
            }

            string timeDisplay = FormatTime(currentPlaybackTime) + " / " + FormatTime(totalDurationSeconds);
            GUI.Label(new Rect(15, 235, panelWidth - 30, 20), timeDisplay, labelStyle);

            string volLabel = T("Vol:", "音量:", "Громк:", "Ses:");
            GUI.Label(new Rect(15, 255, 100, 20), $" {volLabel} {(int)(currentVolume * 100f)}%", labelStyle);
            float prevVolume = currentVolume;
            currentVolume = GUI.HorizontalSlider(new Rect(105, 260, 200, 15), currentVolume, 0f, 1f);
            if (prevVolume != currentVolume)
                SetMciVolume(currentVolume);

            string pauseText = T("PAUSE", "暂停", "ПАУЗА", "DURDUR");
            string playText = T("PLAY", "播放", "ИГРАТЬ", "OYNAT");
            string playPauseText = isPlaying ? $" {pauseText}" : $" {playText}";
            if (GUI.Button(new Rect(15, 285, 65, 35), playPauseText, buttonStyle)) TogglePause();

            string stopText = T("STOP", "停止", "СТОП", "DURDUR");
            if (GUI.Button(new Rect(85, 285, 65, 35), $" {stopText}", buttonStyle)) StopMusic();

            string lyricBtnText = T("Lrc", "歌词", "Текст", "Sözler");
            if (GUI.Button(new Rect(155, 285, 50, 35), lyricBtnText, buttonStyle))
            {
                lyricVisible = !lyricVisible;
            }

            string colorBtnText = T("Col", "颜色", "Цвет", "Renk");
            if (GUI.Button(new Rect(210, 285, 50, 35), colorBtnText, buttonStyle))
            {
                CycleLyricColor();
            }

            if (GUI.Button(new Rect(265, 285, 50, 35), "✕", buttonStyle)) CloseMenu();

            if (statusTimer > 0)
            {
                string localizedStatus = statusMessage;
                if (statusMessage == "Idle") localizedStatus = T("Idle", "空闲", "Ожидание", "Boşta");
                else if (statusMessage == "Searching...") localizedStatus = T("Searching...", "正在搜索...", "Поиск...", "Aranıyor...");
                else if (statusMessage.StartsWith("Found")) localizedStatus = statusMessage.Replace("Found", T("Found", "找到", "Найдено", "Bulundu")).Replace("songs", T("songs", "首歌曲", "песен", "şarkı"));
                else if (statusMessage == "Fetching Link...") localizedStatus = T("Fetching Link...", "正在获取链接...", "Получение ссылки...", "Bağlantı alınıyor...");
                else if (statusMessage == "Downloading...") localizedStatus = T("Downloading...", "正在下载...", "Загрузка...", "İndiriliyor...");
                else if (statusMessage == "Playing...") localizedStatus = T("Playing...", "正在播放...", "Играет...", "Oynatılıyor...");
                else if (statusMessage == "Stopped") localizedStatus = T("Stopped", "已停止", "Остановлено", "Durduruldu");
                else if (statusMessage == "Paused") localizedStatus = T("Paused", "已暂停", "Пауза", "Duraklatıldı");
                else if (statusMessage == "Play Failed") localizedStatus = T("Play Failed", "播放失败", "Ошибка播放", "Oynatma Başarısız");
                else if (statusMessage == "Copyright Blocked") localizedStatus = T("Copyright Blocked", "版权限制/解析失败", "Авторские права заблокированы", "Telif Hakkı Engellendi");

                GUI.Label(new Rect(15, 325, panelWidth - 30, 20), $"<color=orange>{localizedStatus}</color>", labelStyle);
            }
        }

        private string FormatTime(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0) return "00:00";
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        }

        private void DrawLyricWindow(int windowID)
        {
            if (lyricStyleActive != null)
                lyricStyleActive.normal.textColor = currentLyricColor;
            GUI.DragWindow(new Rect(0, 0, lyricRect.width, lyricRect.height));
            string text = isPlaying ? currentLyricText : T("[ BOT Lyric Player - Drag Me ]", "[ BOT 歌词播放器 - 可拖动 ]", "[ BOT Плеер - Перетащите ]", "[ BOT Şarkı Sözü - Sürükle ]");

            if (isPlaying)
            {
                GUI.Label(new Rect(1.5f, 1.5f, lyricRect.width, lyricRect.height), text, lyricStyleShadow);
                GUI.Label(new Rect(-1f, -1f, lyricRect.width, lyricRect.height), text, lyricStyleShadow);
                GUI.Label(new Rect(0, 0, lyricRect.width, lyricRect.height), text, lyricStyleActive);
            }
            else
            {
                GUI.Label(new Rect(0, 0, lyricRect.width, lyricRect.height), text, lyricStyleSetup);
            }
        }

        private static string T(string en, string zh, string ru = "", string tr = "")
        {
            int lang = MenuManager.currentLanguage;
            if (lang == 1) return zh;
            if (lang == 2 && !string.IsNullOrEmpty(ru)) return ru;
            if (lang == 3 && !string.IsNullOrEmpty(tr)) return tr;
            return en;
        }

        private void CreateStyles()
        {
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.08f, 0.95f));
            bgTex.Apply();

            Texture2D inputTex = new Texture2D(1, 1);
            inputTex.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1f));
            inputTex.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = bgTex;
            boxStyle.fontSize = 14;
            boxStyle.fontStyle = FontStyle.Bold;
            boxStyle.alignment = TextAnchor.UpperCenter;
            var boxPad = new RectOffset();
            boxPad.left = 10; boxPad.right = 10; boxPad.top = 5; boxPad.bottom = 10;
            boxStyle.padding = boxPad;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.richText = true;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.normal.textColor = Color.white;

            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.normal.background = inputTex;
            inputStyle.normal.textColor = Color.white;
            inputStyle.fontSize = 13;
            inputStyle.alignment = TextAnchor.MiddleLeft;
            var inputPad = new RectOffset();
            inputPad.left = 5; inputPad.right = 5; inputPad.top = 2; inputPad.bottom = 2;
            inputStyle.padding = inputPad;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 13;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.richText = true;

            listButtonStyle = new GUIStyle(GUI.skin.button);
            listButtonStyle.fontSize = 12;
            listButtonStyle.alignment = TextAnchor.MiddleLeft;
            listButtonStyle.richText = true;

            Texture2D testLrcTex = new Texture2D(1, 1);
            testLrcTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            testLrcTex.Apply();

            lyricStyleSetup = new GUIStyle(GUI.skin.label);
            lyricStyleSetup.normal.background = testLrcTex;
            lyricStyleSetup.normal.textColor = Color.yellow;
            lyricStyleSetup.fontSize = 15;
            lyricStyleSetup.fontStyle = FontStyle.Bold;
            lyricStyleSetup.alignment = TextAnchor.MiddleCenter;

            lyricStyleActive = new GUIStyle(GUI.skin.label);
            lyricStyleActive.normal.textColor = currentLyricColor;
            lyricStyleActive.fontSize = 18;
            lyricStyleActive.fontStyle = FontStyle.Bold;
            lyricStyleActive.richText = true;
            lyricStyleActive.alignment = TextAnchor.MiddleCenter;

            lyricStyleShadow = new GUIStyle(GUI.skin.label);
            lyricStyleShadow.normal.textColor = Color.black;
            lyricStyleShadow.fontSize = 18;
            lyricStyleShadow.fontStyle = FontStyle.Bold;
            lyricStyleShadow.richText = true;
            lyricStyleShadow.alignment = TextAnchor.MiddleCenter;

            lyricWindowStyle = new GUIStyle(GUI.skin.window);
            lyricWindowStyle.normal.background = null;
            lyricWindowStyle.onNormal.background = null;
            var border = new RectOffset();
            border.left = 0; border.right = 0; border.top = 0; border.bottom = 0;
            lyricWindowStyle.border = border;
            var pad = new RectOffset();
            pad.left = 0; pad.right = 0; pad.top = 0; pad.bottom = 0;
            lyricWindowStyle.padding = pad;
            lyricWindowStyle.contentOffset = Vector2.zero;
        }

        private void EnsureLyricColor()
        {
            if (lyricStyleActive != null)
                lyricStyleActive.normal.textColor = currentLyricColor;
        }
    }
}
