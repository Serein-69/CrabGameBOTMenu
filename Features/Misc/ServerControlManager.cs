using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public static class ServerControlManager
    {

        public static KeyCode SavePosKey = KeyCode.Q;
        public static KeyCode LoadPosKey = KeyCode.F;
        public static bool IsBindingSave = false;
        public static bool IsBindingLoad = false;

        private static bool _hasSaved = false;
        private static Vector3 _savedPos;
        private static Vector3 _savedVelocity;
        private static Quaternion _savedRotation;

        private static bool _isCreating = false;
        private static float _timeoutTimer = 0f;

        public static string InputLobbyName = "";
        public static string InputFakeVersion = "";
        public static string InputThumbnailUrl = "";

        public static void Init()
        {
            Utility.Log("[ServerControl] 引擎就绪！");
        }

        public static bool IsPracticeMode()
        {
            try
            {
                if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return false;
                return LobbyManager.Instance.gameMode.modeName == "Practice" || LobbyManager.Instance.gameMode.id == 13;
            }
            catch { return false; }
        }

        public static void Update()
        {
            HandleQuickCreation();
            HandleSaveStateInput();
            HandleKeybindCapture();
        }

        public static void CreatePlaygroundPractice()
        {
            if (_isCreating) return;
            Utility.ForceMessage(MenuUtility.T(
                "<color=cyan>Entering practice arena, please wait...</color>",
                "<color=cyan>正在进入操场练习房，请稍候...</color>",
                "<color=cyan>Вход в тренировку, подождите...</color>",
                "<color=cyan>Alıştırma alanına giriliyor, lütfen bekleyin...</color>",
                "<color=cyan>Entrando na arena de treino, aguarde...</color>"
            ));

            if (SteamManager.Instance != null && SteamManager.Instance.currentLobby.m_SteamID != 0 && SteamManager.Instance.IsLobbyOwner())
            {
                ServerSend.LoadMap(0, 13);
                return;
            }

            if (SteamManager.Instance != null && SteamManager.Instance.currentLobby.m_SteamID != 0)
            {
                try { SteamManager.Instance.LeaveLobby(); } catch { }
            }

            _isCreating = true;
            _timeoutTimer = 0f;

            try
            {
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 16);
            }
            catch (Exception ex)
            {
                _isCreating = false;
                Utility.ForceMessage($"<color=red>{ex.Message}</color>");
            }
        }

        public static void ReturnToNormalLobby()
        {
            if (SteamManager.Instance == null || !SteamManager.Instance.IsLobbyOwner())
            {
                Utility.ForceMessage(MenuUtility.T(
                    "<color=red>Only the host can return all players to lobby!</color>",
                    "<color=red>只有房主才能带领全员返回大厅</color>",
                    "<color=red>Только хост может вернуть всех в лобби!</color>",
                    "<color=red>Yalnızca kurucu herkesi lobiye döndürebilir!</color>",
                    "<color=red>Apenas o anfitrião pode retornar todos ao lobby!</color>"
                ));
                return;
            }

            try
            {
                ServerSend.LoadMap(6, 0);
                Utility.ForceMessage(MenuUtility.T(
                    "<color=#00FF44>[Server] Returning all players to lobby...</color>",
                    "<color=#00FF44>[服务器] 正在带全员返回准备大厅...</color>",
                    "<color=#00FF44>[Сервер] Возврат всех игроков в лобби...</color>",
                    "<color=#00FF44>[Sunucu] Herkes lobiye döndürülüyor...</color>",
                    "<color=#00FF44>[Servidor] Retornando todos ao lobby...</color>"
                ));
            }
            catch (Exception ex)
            {
                Utility.ForceMessage($"<color=red>{ex.Message}</color>");
            }
        }

        private static void HandleQuickCreation()
        {
            if (!_isCreating) return;
            _timeoutTimer += Time.deltaTime;

            if (SteamManager.Instance != null &&
                SteamManager.Instance.currentLobby.m_SteamID != 0 &&
                SteamManager.Instance.IsLobbyOwner())
            {
                if (_timeoutTimer >= 0.5f)
                {
                    _isCreating = false;
                    _timeoutTimer = 0f;
                    ServerSend.LoadMap(0, 13);
                    Utility.ForceMessage(MenuUtility.T(
                        "<color=#00FF44>[Practice] Practice arena ready!</color>",
                        "<color=#00FF44>[练习房] 操场身法房已就绪</color>",
                        "<color=#00FF44>[Тренировка] Тренировочная комната готова!</color>",
                        "<color=#00FF44>[Alıştırma] Alıştırma alanı hazır!</color>",
                        "<color=#00FF44>[Treino] Arena de treino pronta!</color>"
                    ));
                }
                return;
            }

            if (_timeoutTimer > 8.0f)
            {
                _isCreating = false;
                _timeoutTimer = 0f;
                Utility.ForceMessage(MenuUtility.T(
                    "<color=red>Creation timed out, please retry!</color>",
                    "<color=red>创建超时，请重试！</color>",
                    "<color=red>Время создания истекло, повторите!</color>",
                    "<color=red>Oluşturma zaman aşımına uğradı, tekrar deneyin!</color>",
                    "<color=red>Tempo esgotado, tente novamente!</color>"
                ));
            }
        }

        private static void HandleSaveStateInput()
        {
            if (!IsPracticeMode()) return;
            if (Utility.IsChatOpen()) return;

            var player = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            if (player == null || player.dead) return;
            var rb = player.GetComponent<Rigidbody>();
            if (rb == null) return;

            if (Input.GetKeyDown(SavePosKey))
            {
                _savedPos = rb.position;
                _savedVelocity = rb.velocity;
                if (player.playerCam != null) _savedRotation = player.playerCam.transform.rotation;
                _hasSaved = true;
            }

            if (_hasSaved && Input.GetKeyDown(LoadPosKey))
            {
                rb.position = _savedPos;
                rb.velocity = _savedVelocity;
                if (player.playerCam != null) player.playerCam.transform.rotation = _savedRotation;
            }
        }

        private static void HandleKeybindCapture()
        {
            if (!IsBindingSave && !IsBindingLoad) return;

            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(k))
                {
                    if (k == KeyCode.Escape)
                    {
                        IsBindingSave = false;
                        IsBindingLoad = false;
                        return;
                    }

                    if (IsBindingSave)
                    {
                        SavePosKey = k;
                        IsBindingSave = false;
                    }
                    else if (IsBindingLoad)
                    {
                        LoadPosKey = k;
                        IsBindingLoad = false;
                    }
                    return;
                }
            }
        }

        public static void SetLobbyPublic()
        {
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;
            var lobby = SteamManager.Instance.currentLobby;

            SteamMatchmaking.SetLobbyType(lobby, ELobbyType.k_ELobbyTypePublic);
            SteamMatchmaking.SetLobbyJoinable(lobby, true);
            SteamMatchmaking.SetLobbyData(lobby, "Modes", "11111111111111111");
            SteamMatchmaking.SetLobbyData(lobby, "AllMaps", "False");
            SteamMatchmaking.SetLobbyData(lobby, "Maps", "111111111111111111111111111111111111111111111111111111111111111");
            SteamMatchmaking.SetLobbyData(lobby, "LobbyKeyId", "LobbyValId");
            SteamMatchmaking.SetLobbyData(lobby, "Voice Chat", "True");
            SteamMatchmaking.SetLobbyData(lobby, "LobbyState", "Lobby");

            CustomServerListManager.ApplyServerSettings();
            Utility.ForceMessage(MenuUtility.T(
                "<color=#00FF44>[Server] Room broadcasted to public list!</color>",
                "<color=#00FF44>[服务器] 房间已广播至【公共服务器列表】</color>",
                "<color=#00FF44>[Сервер] Комната опубликована в общем списке!</color>",
                "<color=#00FF44>[Sunucu] Oda genel listede yayınlandı!</color>",
                "<color=#00FF44>[Servidor] Sala publicada na lista pública!</color>"
            ));
        }

        public static void SetLobbyInvisible()
        {
            if (SteamManager.Instance == null || SteamManager.Instance.currentLobby.m_SteamID == 0) return;
            var lobby = SteamManager.Instance.currentLobby;
            SteamMatchmaking.SetLobbyType(lobby, ELobbyType.k_ELobbyTypeInvisible);
            SteamMatchmaking.SetLobbyJoinable(lobby, false);
            Utility.ForceMessage(MenuUtility.T(
                "<color=yellow>[Server] Room set to private/hidden.</color>",
                "<color=yellow>[服务器] 房间已设为【隐藏/私密】</color>",
                "<color=yellow>[Сервер] Комната скрыта (приватная).</color>",
                "<color=yellow>[Sunucu] Oda gizli/özel yapıldı.</color>",
                "<color=yellow>[Servidor] Sala definida como privada/oculta.</color>"
            ));
        }

        public static void ApplyColoredLobbyName(string hexColor)
        {
            string rawName = string.IsNullOrEmpty(InputLobbyName) ? "Crab Game Lobby" : InputLobbyName.Trim();
            string coloredName = $"<color={hexColor}>{rawName}</color>";

            CustomServerListManager.CustomName = coloredName;
            CustomServerListManager.SaveConfig();
            CustomServerListManager.ApplyServerSettings();
            Utility.ForceMessage(MenuUtility.T(
                $"<color=green>[Server] Colored name applied: </color>{coloredName}",
                $"<color=green>[服务器] 彩色房名已生效: </color>{coloredName}",
                $"<color=green>[Сервер] Цветное имя применено: </color>{coloredName}",
                $"<color=green>[Sunucu] Renkli isim uygulandı: </color>{coloredName}",
                $"<color=green>[Servidor] Nome colorido aplicado: </color>{coloredName}"
            ));
        }

        public static void ApplyPlainLobbyName()
        {
            string rawName = string.IsNullOrEmpty(InputLobbyName) ? "Crab Game Lobby" : InputLobbyName.Trim();
            CustomServerListManager.CustomName = rawName;
            CustomServerListManager.SaveConfig();
            CustomServerListManager.ApplyServerSettings();
            Utility.ForceMessage(MenuUtility.T(
                $"<color=green>[Server] Room name updated to: </color>{rawName}",
                $"<color=green>[服务器] 房名已更新为: </color>{rawName}",
                $"<color=green>[Сервер] Имя комнаты обновлено: </color>{rawName}",
                $"<color=green>[Sunucu] Oda adı güncellendi: </color>{rawName}",
                $"<color=green>[Servidor] Nome da sala atualizado: </color>{rawName}"
            ));
        }

        public static void ApplyVersion(string ver)
        {
            if (string.IsNullOrEmpty(ver)) ver = "1.362";
            InputFakeVersion = ver;
            CustomServerListManager.CustomVersion = ver;
            CustomServerListManager.SaveConfig();
            CustomServerListManager.ApplyServerSettings();
            Utility.ForceMessage(MenuUtility.T(
                $"<color=green>[Server] Version changed to: </color>{ver}",
                $"<color=green>[服务器] 版本号已修改为: </color>{ver}",
                $"<color=green>[Сервер] Версия изменена на: </color>{ver}",
                $"<color=green>[Sunucu] Sürüm değiştirildi: </color>{ver}",
                $"<color=green>[Servidor] Versão alterada para: </color>{ver}"
            ));
        }

        public static void PasteThumbnail()
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clip) && clip.StartsWith("http"))
            {
                InputThumbnailUrl = clip;
                CustomServerListManager.CustomIcon = clip;
                CustomServerListManager.SaveConfig();
                CustomServerListManager.ApplyServerSettings();
                Utility.ForceMessage(MenuUtility.T(
                    "<color=green>[Server] Thumbnail URL updated!</color>",
                    "<color=green>[服务器] 封面图直链已更新</color>",
                    "<color=green>[Сервер] URL иконки обновлен!</color>",
                    "<color=green>[Sunucu] İkon URL'si güncellendi!</color>",
                    "<color=green>[Servidor] URL do ícone atualizado!</color>"
                ));
            }
            else
            {
                Utility.ForceMessage(MenuUtility.T(
                    "<color=red>URL must start with http!</color>",
                    "<color=red>直链必须以 http 开头</color>",
                    "<color=red>URL должен начинаться с http!</color>",
                    "<color=red>URL http ile başlamalıdır!</color>",
                    "<color=red>O URL deve começar com http!</color>"
                ));
            }
        }

        public static void ResetLobby()
        {
            InputLobbyName = "";
            InputFakeVersion = "";
            CustomServerListManager.CustomName = "";
            CustomServerListManager.CustomPrefix = "";
            CustomServerListManager.CustomIcon = "";
            CustomServerListManager.CustomVersion = "";
            CustomServerListManager.SaveConfig();
            CustomServerListManager.ApplyServerSettings();
            Utility.ForceMessage(MenuUtility.T(
                "<color=yellow>[Server] Restored default lobby settings.</color>",
                "<color=yellow>[服务器] 已恢复原版大厅</color>",
                "<color=yellow>[Сервер] Восстановлены стандартные настройки лобби.</color>",
                "<color=yellow>[Sunucu] Varsayılan lobi ayarları geri yüklendi.</color>",
                "<color=yellow>[Servidor] Configurações padrão do lobby restauradas.</color>"
            ));
        }

        public static void KickPlayer(ulong steamId)
        {
            if (LobbyManager.Instance != null && steamId != 0)
            {
                LobbyManager.Instance.KickPlayer(steamId);
                Utility.ForceMessage(MenuUtility.T(
                    "<color=red>[Admin] Player kicked!</color>",
                    "<color=red>[管理] 已踢出该玩家</color>",
                    "<color=red>[Админ] Игрок кикнут!</color>",
                    "<color=red>[Yönetici] Oyuncu atıldı!</color>",
                    "<color=red>[Admin] Jogador expulso!</color>"
                ));
            }
        }

        public static void ToggleWhitelist(ulong steamId)
        {
            if (TagServerManager.WhitelistIDs.Contains(steamId))
            {
                TagServerManager.RemoveFromWhitelist(steamId);
                Utility.ForceMessage(MenuUtility.T(
                    "<color=yellow>[Whitelist] Removed player from whitelist.</color>",
                    "<color=yellow>[白名单] 已将该玩家移出白名单</color>",
                    "<color=yellow>[Вайтлист] Игрок удален из вайтлиста.</color>",
                    "<color=yellow>[Beyaz Liste] Oyuncu beyaz listeden çıkarıldı.</color>",
                    "<color=yellow>[Whitelist] Jogador removido da whitelist.</color>"
                ));
            }
            else
            {
                TagServerManager.AddToWhitelist(steamId);
                Utility.ForceMessage(MenuUtility.T(
                    "<color=#00FF44>[Whitelist] Added player to whitelist!</color>",
                    "<color=#00FF44>[白名单] 已将该玩家加入白名单</color>",
                    "<color=#00FF44>[Вайтлист] Игрок добавлен в вайтлист!</color>",
                    "<color=#00FF44>[Beyaz Liste] Oyuncu beyaz listeye eklendi!</color>",
                    "<color=#00FF44>[Whitelist] Jogador adicionado à whitelist!</color>"
                ));
            }
        }

        public static void KickAllNonWhitelisted()
        {
            if (SteamManager.Instance == null || !SteamManager.Instance.IsLobbyOwner()) return;

            var lobby = SteamManager.Instance.currentLobby;
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (member.m_SteamID == 0 || member.m_SteamID == myId) continue;
                if (TagServerManager.WhitelistIDs.Contains(member.m_SteamID)) continue;

                KickPlayer(member.m_SteamID);
            }
            Utility.ForceMessage(MenuUtility.T(
                "<color=red>[Admin] Cleared all non-whitelisted players!</color>",
                "<color=red>[管理] 已清理全部非白名单玩家</color>",
                "<color=red>[Админ] Кикнуты все кроме вайтлиста!</color>",
                "<color=red>[Yönetici] Beyaz listede olmayan tüm oyuncular atıldı!</color>",
                "<color=red>[Admin] Todos os jogadores fora da whitelist foram expulsos!</color>"
            ));
        }
    }
}
