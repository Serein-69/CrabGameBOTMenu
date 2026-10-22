using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public class CloudChatMessage
    {
        public int Id;
        public string UserName;
        public string SteamId;
        public string Message;
        public string Time;
        public bool IsPlus;
        public bool IsOnline;
    }

    public static class CloudEndpoints
    {
        public static string ChatBase = "";
        public static string ConfigBase = "";
    }

    public static class CloudChatManager
    {
        private static string BASE_URL => CloudEndpoints.ChatBase;
        public static List<CloudChatMessage> Messages = new List<CloudChatMessage>();
        public static bool EnableNotification = true;
        private static bool _isFirstFetch = true;
        private static int _maxKnownId = 0;
        private static float _pollTimer = 0f;
        public static bool IsSending = false;

        public static void Update()
        {
            if (string.IsNullOrEmpty(BASE_URL)) return;
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= 2.5f)
            {
                _pollTimer = 0f;
                _ = Task.Run(() => FetchMessagesAsync());
            }
        }

        public static void FetchMessagesAsync()
        {
            if (string.IsNullOrEmpty(BASE_URL)) return;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = false;

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                    client.Encoding = Encoding.UTF8;

                    string json = client.DownloadString($"{BASE_URL}/messages");
                    if (string.IsNullOrEmpty(json)) return;

                    var matches = Regex.Matches(json, @"\{""id"":(\d+),""steam_id"":""([^""]+)"",""user_name"":""([^""]+)"",""message"":""([^""]+)"",""is_plus"":(\d+),""time_str"":""([^""]+)""(?:,""is_online"":(\d+))?\}");

                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    string myIdStr = myId.ToString();

                    List<CloudChatMessage> newList = new List<CloudChatMessage>();
                    int highestIdInBatch = _maxKnownId;

                    foreach (Match m in matches)
                    {
                        int msgId = int.Parse(m.Groups[1].Value);
                        string senderSteamId = m.Groups[2].Value;
                        string senderName = m.Groups[3].Value;
                        string text = m.Groups[4].Value;
                        string rawTime = m.Groups[6].Value;
                        bool isOnline = m.Groups[7].Success && m.Groups[7].Value == "1";

                        string formattedTime = rawTime;
                        if (DateTime.TryParse(rawTime, out DateTime parsedUtc))
                        {
                            DateTime localTime = parsedUtc.ToLocalTime();
                            string timeFmt = (BOTPlugin.Config12HourTime != null && BOTPlugin.Config12HourTime.Value) ? "hh:mm tt" : "HH:mm";
                            formattedTime = localTime.ToString(timeFmt);
                        }

                        if (!_isFirstFetch && _maxKnownId > 0 && msgId > _maxKnownId && senderSteamId != myIdStr)
                        {
                            if (EnableNotification)
                            {
                                Utility.ForceMessage($"<color=#00FFFF>[Chat]</color> <b>{senderName}</b>: {text}");
                            }
                        }

                        if (msgId > highestIdInBatch) highestIdInBatch = msgId;

                        newList.Add(new CloudChatMessage
                        {
                            Id = msgId,
                            SteamId = senderSteamId,
                            UserName = senderName,
                            Message = text,
                            IsPlus = false,
                            Time = formattedTime,
                            IsOnline = isOnline
                        });
                    }

                    _maxKnownId = highestIdInBatch;
                    _isFirstFetch = false;
                    Messages = newList;
                }
            }
            catch { }
        }

        public static void SendChatMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg) || IsSending) return;
            IsSending = true;

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    string myName = BOTPlugin.ConfigCustomName != null ? BOTPlugin.ConfigCustomName.Value : "User";

                    string jsonPayload = $"{{\"steamId\":\"{myId}\",\"userName\":\"{myName}\",\"message\":\"{msg.Replace("\"", "\\\"")}\",\"isPlus\":0}}";

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        client.UploadString($"{BASE_URL}/send", "POST", jsonPayload);
                    }

                    FetchMessagesAsync();
                }
                catch { }
                finally { IsSending = false; }
            });
        }
    }

    public class CloudConfigItem
    {
        public int Id;
        public string Name;
        public string Author;
        public string AuthorSteamId;
        public string Description;
        public int Downloads;
        public int Likes;
        public bool Liked;
        public string Date;
        public bool IsExpanded = false;
    }

    public static class CloudConfigManager
    {
        private static string BASE_URL => CloudEndpoints.ConfigBase;
        public static List<CloudConfigItem> CloudConfigs = new List<CloudConfigItem>();
        public static bool IsLoading = false;

        public static void RefreshCloudList()
        {
            if (IsLoading) return;
            if (string.IsNullOrEmpty(BASE_URL))
            {
                Utility.ForceMessage(MenuUtility.T("<color=#FFAA00>Cloud endpoints are not configured.</color>", "<color=#FFAA00>云端地址未配置。</color>", "<color=#FFAA00>Адреса облака не заданы.</color>", "<color=#FFAA00>Bulut adresleri ayarlanmamış.</color>", "<color=#FFAA00>Endereços da nuvem não configurados.</color>"));
                return;
            }
            IsLoading = true;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        string json = client.DownloadString($"{BASE_URL}/configs?steamId={myId}");
                        if (!string.IsNullOrEmpty(json))
                        {
                            List<CloudConfigItem> list = new List<CloudConfigItem>();

                            string payload = json.Trim();
                            if (payload.Length >= 2 && payload.StartsWith("[") && payload.EndsWith("]"))
                                payload = payload.Substring(1, payload.Length - 2);

                            string[] blocks = payload.Split(new string[] { "},{" }, StringSplitOptions.None);
                            foreach (string rawBlock in blocks)
                            {
                                string block = rawBlock.Trim();
                                if (block.Length == 0) continue;
                                if (!block.StartsWith("{")) block = "{" + block;
                                if (!block.EndsWith("}")) block = block + "}";

                                try
                                {
                                    Match m = Regex.Match(block, @"\{""id"":(\d+),""config_name"":""([^""]*)"",""author_name"":""([^""]*)"",""author_steam_id"":""([^""]*)"",""description"":""([^""]*)"",""downloads"":(\d+),""likes"":(\d+),""liked"":(1|0|true|false),""date_str"":""([^""]*)""\}");
                                    if (!m.Success) continue;

                                    string likedRaw = m.Groups[8].Value.ToLower();
                                    bool isLiked = likedRaw == "1" || likedRaw == "true";

                                    list.Add(new CloudConfigItem
                                    {
                                        Id = int.Parse(m.Groups[1].Value),
                                        Name = m.Groups[2].Value,
                                        Author = m.Groups[3].Value,
                                        AuthorSteamId = m.Groups[4].Value,
                                        Description = m.Groups[5].Value,
                                        Downloads = int.Parse(m.Groups[6].Value),
                                        Likes = int.Parse(m.Groups[7].Value),
                                        Liked = isLiked,
                                        Date = m.Groups[9].Value
                                    });
                                }
                                catch { }
                            }

                            if (list.Count == 0)
                                Utility.Log($"Cloud config parse returned 0 items, keeping previous {CloudConfigs.Count} entries.");
                            else
                                CloudConfigs = list;
                        }
                    }
                }
                catch { }
                finally { IsLoading = false; }
            });
        }

        public static void UploadConfig(string configName, string description)
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            string myName = BOTPlugin.ConfigCustomName != null ? BOTPlugin.ConfigCustomName.Value : "User";
            string activePath = BOTPlugin.Instance.Config.ConfigFilePath;

            if (!File.Exists(activePath)) return;

            string configRawText = File.ReadAllText(activePath);
            string safeData = configRawText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\n").Replace("\n", "\\n");
            string jsonPayload = $"{{\"configName\":\"{configName}\",\"authorName\":\"{myName}\",\"authorSteamId\":\"{myId}\",\"description\":\"{description}\",\"configData\":\"{safeData}\"}}";

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        string response = client.UploadString($"{BASE_URL}/upload", "POST", jsonPayload);
                        if (response.Contains("success"))
                        {
                            Utility.ForceMessage(MenuUtility.T(
                                $"<color=#00FF44>Profile [{configName}] published successfully!</color>",
                                $"<color=#00FF44>配置 [{configName}] 上传成功！</color>",
                                $"<color=#00FF44>Профиль [{configName}] опубликован!</color>",
                                $"<color=#00FF44>Ayar [{configName}] başarıyla yüklendi!</color>",
                                $"<color=#00FF44>Perfil [{configName}] publicado com sucesso!</color>"
                            ));
                            RefreshCloudList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utility.ForceMessage(MenuUtility.T(
                        $"<color=red>Upload Failed: {ex.Message}</color>",
                        $"<color=red>上传失败: {ex.Message}</color>",
                        $"<color=red>Ошибка загрузки: {ex.Message}</color>",
                        $"<color=red>Yükleme Başarısız: {ex.Message}</color>",
                        $"<color=red>Falha no envio: {ex.Message}</color>"
                    ));
                }
            });
        }

        public static void DeleteConfig(int configId, string configName)
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (myId == 0) return;

            string jsonPayload = $"{{\"steamId\":\"{myId}\"}}";

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        string response = client.UploadString($"{BASE_URL}/delete/{configId}", "POST", jsonPayload);

                        Utility.ForceMessage(MenuUtility.T(
                            $"<color=#FF4444>Config [{configName}] deleted!</color>",
                            $"<color=#FF4444>配置 [{configName}] 已从云端删除！</color>",
                            $"<color=#FF4444>Профиль [{configName}] удален!</color>",
                            $"<color=#FF4444>Ayar [{configName}] silindi!</color>",
                            $"<color=#FF4444>Perfil [{configName}] excluído!</color>"
                        ));
                        RefreshCloudList();
                    }
                }
                catch (Exception ex)
                {
                    Utility.ForceMessage(MenuUtility.T(
                        $"<color=red>Delete Failed: {ex.Message}</color>",
                        $"<color=red>删除失败: {ex.Message}</color>",
                        $"<color=red>Ошибка удаления: {ex.Message}</color>",
                        $"<color=red>Silme Başarısız: {ex.Message}</color>",
                        $"<color=red>Falha ao excluir: {ex.Message}</color>"
                    ));
                }
            });
        }

        public static void ToggleLike(int configId, string configName, bool currentlyLiked)
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (myId == 0) return;

            string jsonPayload = $"{{\"steamId\":\"{myId}\",\"like\":{(!currentlyLiked ? "true" : "false")}}}";

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        client.UploadString($"{BASE_URL}/like/{configId}", "POST", jsonPayload);
                    }

                    RefreshCloudList();
                }
                catch (Exception ex)
                {
                    Utility.ForceMessage(MenuUtility.T(
                        $"<color=red>Like failed: {ex.Message}</color>",
                        $"<color=red>点赞失败：{ex.Message}</color>",
                        $"<color=red>Ошибка лайка: {ex.Message}</color>",
                        $"<color=red>Beğeni başarısız: {ex.Message}</color>",
                        $"<color=red>Falha no like: {ex.Message}</color>"
                    ));
                }
            });
        }

        private static string UnescapeConfigData(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            StringBuilder sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c != '\\' || i + 1 >= raw.Length)
                {
                    sb.Append(c);
                    continue;
                }

                char next = raw[i + 1];
                if (next == '\\') { sb.Append('\\'); i++; }
                else if (next == '"') { sb.Append('"'); i++; }
                else if (next == 'n') { sb.Append('\n'); i++; }
                else if (next == 'r') { sb.Append('\r'); i++; }
                else if (next == 't') { sb.Append('\t'); i++; }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static int CountConfigEntries(string path)
        {
            try
            {
                if (!File.Exists(path)) return 0;
                int count = 0;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length == 0) continue;
                    if (t.StartsWith("[") || t.StartsWith("#") || t.StartsWith(";")) continue;
                    if (t.IndexOf('=') > 0) count++;
                }
                return count;
            }
            catch { return 0; }
        }

        public static void DownloadAndApply(int configId, string configName)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        string json = client.DownloadString($"{BASE_URL}/download/{configId}");
                        var match = Regex.Match(json, @"""data"":""([\s\S]+?)""\s*\}");
                        if (match.Success)
                        {
                            string unescapedData = UnescapeConfigData(match.Groups[1].Value);

                            string activePath = BOTPlugin.Instance.Config.ConfigFilePath;
                            string tmpPath = activePath + ".tmp";
                            string bakPath = activePath + ".bak";

                            File.WriteAllText(tmpPath, unescapedData);

                            int entryCount = CountConfigEntries(tmpPath);
                            if (entryCount <= 0)
                            {
                                try { File.Delete(tmpPath); } catch { }
                                Utility.Log("Cloud config download validation failed: parsed 0 entries.");
                                Utility.ForceMessage(MenuUtility.T(
                                    $"<color=red>Profile [{configName}] is invalid, kept current config.</color>",
                                    $"<color=red>配置 [{configName}] 校验失败，已保留原配置。</color>",
                                    $"<color=red>Профиль [{configName}] недействителен, текущий конфиг сохранён.</color>",
                                    $"<color=red>Ayar [{configName}] geçersiz, mevcut ayar korundu.</color>",
                                    $"<color=red>Perfil [{configName}] inválido, configuração atual mantida.</color>"
                                ));
                                return;
                            }

                            if (File.Exists(activePath)) File.Copy(activePath, bakPath, true);

                            try
                            {
                                File.Copy(tmpPath, activePath, true);
                                try { File.Delete(tmpPath); } catch { }
                            }
                            catch (Exception applyEx)
                            {
                                if (File.Exists(bakPath))
                                {
                                    try { File.Copy(bakPath, activePath, true); } catch { }
                                }
                                try { File.Delete(tmpPath); } catch { }
                                Utility.Log("Cloud config apply failed, rolled back: " + applyEx.Message);
                                Utility.ForceMessage(MenuUtility.T(
                                    $"<color=red>Apply failed, rolled back: {applyEx.Message}</color>",
                                    $"<color=red>应用失败，已回滚：{applyEx.Message}</color>",
                                    $"<color=red>Не удалось применить, откат: {applyEx.Message}</color>",
                                    $"<color=red>Uygulanamadı, geri alındı: {applyEx.Message}</color>",
                                    $"<color=red>Falha ao aplicar, revertido: {applyEx.Message}</color>"
                                ));
                                return;
                            }

                            BOTPlugin.Instance.Config.Reload();
                            BOTPlugin.SyncConfigsToMemory();

                            Utility.ForceMessage(MenuUtility.T(
                                $"<color=#00FF44>Successfully loaded profile: [{configName}]!</color>",
                                $"<color=#00FF44>成功加载配置: [{configName}]！</color>",
                                $"<color=#00FF44>Успешно загружен профиль: [{configName}]!</color>",
                                $"<color=#00FF44>Ayar başarıyla yüklendi: [{configName}]!</color>",
                                $"<color=#00FF44>Perfil carregado com sucesso: [{configName}]!</color>"
                            ));
                            RefreshCloudList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utility.ForceMessage(MenuUtility.T(
                        $"<color=red>Download Failed: {ex.Message}</color>",
                        $"<color=red>下载应用失败: {ex.Message}</color>",
                        $"<color=red>Ошибка загрузки: {ex.Message}</color>",
                        $"<color=red>İndirme Başarısız: {ex.Message}</color>",
                        $"<color=red>Falha no download: {ex.Message}</color>"
                    ));
                }
            });
        }
    }
}
