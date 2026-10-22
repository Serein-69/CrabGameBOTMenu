using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace BOTMenuMod
{
    public static class ConfigProfileManager
    {
        public static string ProfileFolder = Path.Combine(Variables.mainFolderPath, "Profiles");
        public static List<string> Profiles = new List<string>();
        public static int CurrentIndex = 0;
        private static string _currentProfileName = "Default";

        public static void Init()
        {
            if (!Directory.Exists(ProfileFolder)) Directory.CreateDirectory(ProfileFolder);
            try
            {
                if (BOTPlugin.Instance != null && BOTPlugin.Instance.Config != null)
                    BOTPlugin.Instance.Config.Save();
            }
            catch { }
            RefreshProfiles();
        }

        public static void RefreshProfiles()
        {
            Profiles.Clear();
            if (!Directory.Exists(ProfileFolder)) Directory.CreateDirectory(ProfileFolder);

            string defaultPath = Path.Combine(ProfileFolder, "Default.cfg");
            if (!File.Exists(defaultPath))
            {
                try
                {
                    if (BOTPlugin.Instance != null && BOTPlugin.Instance.Config != null && File.Exists(BOTPlugin.Instance.Config.ConfigFilePath))
                    {
                        File.Copy(BOTPlugin.Instance.Config.ConfigFilePath, defaultPath, true);
                    }
                }
                catch { }
            }

            try
            {
                var files = Directory.GetFiles(ProfileFolder, "*.cfg");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (!Profiles.Contains(name)) Profiles.Add(name);
                }
            }
            catch { }

            if (Profiles.Count == 0) Profiles.Add("Default");

            if (string.IsNullOrEmpty(_currentProfileName) || !Profiles.Contains(_currentProfileName))
                _currentProfileName = Profiles[0];

            CurrentIndex = Profiles.IndexOf(_currentProfileName);
            if (CurrentIndex < 0) CurrentIndex = 0;
        }

        public static string GetCurrentProfileName()
        {
            if (Profiles.Count == 0) return string.IsNullOrEmpty(_currentProfileName) ? "Default" : _currentProfileName;

            if (string.IsNullOrEmpty(_currentProfileName) || !Profiles.Contains(_currentProfileName))
                _currentProfileName = Profiles[0];

            CurrentIndex = Profiles.IndexOf(_currentProfileName);
            if (CurrentIndex < 0) CurrentIndex = 0;
            return _currentProfileName;
        }

        public static void CycleProfile()
        {
            if (Profiles.Count == 0) return;
            int idx = Profiles.IndexOf(_currentProfileName);
            if (idx < 0) idx = 0;
            idx = (idx + 1) % Profiles.Count;
            _currentProfileName = Profiles[idx];
            CurrentIndex = idx;
        }

        public static void SaveProfile(string name)
        {
            if (!Directory.Exists(ProfileFolder)) Directory.CreateDirectory(ProfileFolder);
            BOTPlugin.Instance.Config.Save();

            string activePath = BOTPlugin.Instance.Config.ConfigFilePath;
            string targetPath = Path.Combine(ProfileFolder, name + ".cfg");

            File.Copy(activePath, targetPath, true);
            RefreshProfiles();
            Utility.ForceMessage(MenuUtility.T($"Saved profile: [{name}]", $"配置已保存: [{name}]", $"Профиль сохранен: [{name}]", $"Ayar kaydedildi: [{name}]", $"Perfil salvo: [{name}]"));
        }

        public static void LoadProfile()
        {
            string name = GetCurrentProfileName();
            string targetPath = Path.Combine(ProfileFolder, name + ".cfg");
            if (!File.Exists(targetPath))
            {
                Utility.ForceMessage(MenuUtility.T("Profile file not found!", "配置文件不存在!", "Файл не найден!", "Profil dosyası bulunamadı!", "Arquivo de perfil não encontrado!"));
                return;
            }

            string activePath = BOTPlugin.Instance.Config.ConfigFilePath;
            File.Copy(targetPath, activePath, true);

            BOTPlugin.Instance.Config.Reload();
            BOTPlugin.SyncConfigsToMemory();

            Utility.ForceMessage(MenuUtility.T($"Loaded profile: [{name}]", $"配置已加载: [{name}]", $"Загружен профиль: [{name}]", $"Ayar yüklendi: [{name}]", $"Perfil carregado: [{name}]"));
        }

        public static void CreateNewFromClipboard()
        {
            string name = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 15)
            {
                Utility.ForceMessage(MenuUtility.T("Invalid name in clipboard!", "剪贴板文字不合法!", "Неверное имя в буфере!", "Panodaki isim geçersiz!", "Nome inválido na área de transferência!"));
                return;
            }
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "");
            SaveProfile(name);
        }

        public static void DeleteProfile()
        {
            string name = GetCurrentProfileName();
            if (name == "Default")
            {
                Utility.ForceMessage(MenuUtility.T("Cannot delete Default profile!", "默认配置不可删除!", "Нельзя удалить Default!", "Default profil silinemez!", "Não é possível excluir o perfil Default!"));
                return;
            }

            string targetPath = Path.Combine(ProfileFolder, name + ".cfg");
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                RefreshProfiles();
                Utility.ForceMessage(MenuUtility.T($"Deleted profile: [{name}]", $"已删除配置: [{name}]", $"Удален профиль: [{name}]", $"Profil silindi: [{name}]", $"Perfil excluído: [{name}]"));
            }
        }

        public static void OpenProfileFolder()
        {
            if (!Directory.Exists(ProfileFolder)) Directory.CreateDirectory(ProfileFolder);
            System.Diagnostics.Process.Start("explorer.exe", ProfileFolder);
            Utility.ForceMessage(MenuUtility.T("Opened profiles folder!", "配置文件夹已打开!", "Папка профилей открыта!", "Klasör açıldı!", "Pasta de perfis aberta!"));
        }
    }
}
