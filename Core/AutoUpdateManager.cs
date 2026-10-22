using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using BepInEx;

namespace BOTMenuMod
{
    public static class AutoUpdateManager
    {
        private const string VERSION_URL = "";

        public static bool HasUpdate = false;
        public static string LatestVersion = "";
        public static string DownloadUrl = "";
        public static string Changelog = "";
        public static bool IsDownloading = false;

        private static volatile string _pendingMessage = null;
        private static volatile bool _pendingQuit = false;

        public static void Tick()
        {
            try
            {
                if (_pendingQuit)
                {
                    _pendingQuit = false;
                    IsDownloading = false;
                    Application.Quit();
                    return;
                }

                string msg = _pendingMessage;
                if (msg == null) return;
                _pendingMessage = null;
                Utility.ForceMessage(msg);
            }
            catch { }
        }

        public static void CheckUpdate()
        {
            if (string.IsNullOrEmpty(VERSION_URL)) return;
            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CrabMod/1.0";
                        client.Encoding = Encoding.UTF8;

                        string json = client.DownloadString(VERSION_URL);
                        if (string.IsNullOrEmpty(json)) return;

                        var verMatch = Regex.Match(json, @"""latestVersion""\s*:\s*""([^""]+)""");
                        var urlMatch = Regex.Match(json, @"""downloadUrl""\s*:\s*""([^""]+)""");
                        var logMatch = Regex.Match(json, @"""changelog""\s*:\s*""([^""]+)""");

                        if (verMatch.Success && urlMatch.Success)
                        {
                            LatestVersion = verMatch.Groups[1].Value;
                            DownloadUrl = urlMatch.Groups[1].Value;
                            if (logMatch.Success) Changelog = logMatch.Groups[1].Value.Replace("\\n", "\n");

                            if (IsNewerVersion(LatestVersion, BOTPlugin.CurrentBotVersion))
                            {
                                HasUpdate = true;
                                _pendingMessage = MenuUtility.T(
                                $"<color=#00FF44>[Update] New version v{LatestVersion} available! Click update in menu.</color>",
                                $"<color=#00FF44>[云更新] 发现新版本 v{LatestVersion}！可在菜单左上角一键更新。</color>",
                                $"<color=#00FF44>[Обновление] Найдена версия v{LatestVersion}! Нажмите обновить в меню.</color>",
                                $"<color=#00FF44>[Güncelleme] Yeni v{LatestVersion} sürümü mevcut! Menüden güncelleyin.</color>",
                                $"<color=#00FF44>[Atualização] Nova versão v{LatestVersion} disponível! Atualize no menu.</color>"
                            );
                            }
                        }
                    }
                }
                catch { }
            });
        }

        private static bool IsNewerVersion(string serverVer, string localVer)
        {
            try
            {
                Version s = new Version(serverVer);
                Version l = new Version(localVer);
                return s > l;
            }
            catch
            {
                return false;
            }
        }

        public static void StartUpdate()
        {
            if (IsDownloading || string.IsNullOrEmpty(DownloadUrl)) return;
            IsDownloading = true;
            Utility.ForceMessage(MenuUtility.T(
                "<color=cyan>Downloading latest update, please wait...</color>",
                "<color=cyan>正在极速下载最新版本，请稍候...</color>",
                "<color=cyan>Загрузка обновления, пожалуйста подождите...</color>",
                "<color=cyan>Yeni sürüm indiriliyor, lütfen bekleyin...</color>",
                "<color=cyan>Baixando nova versão, por favor aguarde...</color>"
            ));

            _ = Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)4032 | SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = false;

                    string currentDllPath = typeof(BOTPlugin).Assembly.Location;
                    string pluginsFolder = Path.GetDirectoryName(currentDllPath);
                    string tempDllPath = Path.Combine(pluginsFolder, "BOTMenuMod_new.tmp");
                    string batScriptPath = Path.Combine(Paths.GameRootPath, "bot_updater.bat");

                    DownloadFileWithRedirect(DownloadUrl, tempDllPath);

                    FileInfo fi = new FileInfo(tempDllPath);
                    if (!fi.Exists || fi.Length < 10240)
                    {
                        throw new Exception("下载到的文件损坏或链接无效，请检查网络！");
                    }

                    int currentPid = Process.GetCurrentProcess().Id;
                    StringBuilder bat = new StringBuilder();
                    bat.AppendLine("@echo off");
                    bat.AppendLine("chcp 65001 >nul");
                    bat.AppendLine($":waitloop");
                    bat.AppendLine($"tasklist /FI \"PID eq {currentPid}\" 2>NUL | find /I /N \"{currentPid}\">NUL");
                    bat.AppendLine($"if \"%ERRORLEVEL%\"==\"0\" (timeout /t 1 /nobreak >nul & goto waitloop)");
                    bat.AppendLine("timeout /t 1 /nobreak >nul");
                    bat.AppendLine($"move /y \"{tempDllPath}\" \"{currentDllPath}\" >nul");
                    bat.AppendLine("(goto) 2>nul & del \"%~f0\"");

                    File.WriteAllText(batScriptPath, bat.ToString(), Encoding.Default);

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = batScriptPath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);

                    IsDownloading = false;
                    _pendingQuit = true;
                }
                catch (Exception ex)
                {
                    IsDownloading = false;
                    _pendingMessage = $"<color=red>更新失败: {ex.Message}</color>";
                }
            });
        }

        private static void DownloadFileWithRedirect(string url, string savePath)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 10;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CrabMod/1.0";
            request.Timeout = 30000;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException($"HTTP 错误: {(int)response.StatusCode} {response.StatusDescription}");
                }

                using (Stream stream = response.GetResponseStream())
                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }
        }
    }

    public class AutoUpdatePump : MonoBehaviour
    {
        public AutoUpdatePump(IntPtr ptr) : base(ptr) { }
        private void Update() { AutoUpdateManager.Tick(); }
    }
}
