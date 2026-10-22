using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using SteamworksNative;

namespace BOTMenuMod
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance;
        public static bool IsOpen = false;

        public static int currentLanguage = 0;
        public static List<MenuCategory> Categories = new List<MenuCategory>();
        public static Text headerText;
        public static Text leftColText;
        public static Text rightColText;
        public static bool focusOnLeft = true;
        public static int categoryIndex = 0;
        public static int itemIndex = 0;
        public static int scrollOffset = 0;
        public static bool scrollingMode = false;
        public static float UIScale = 1.0f;
        private Rect _windowRect = new Rect((Screen.width - 800) / 2f, (Screen.height - 520) / 2f, 800, 520);
        private Vector2 _scrollPos = Vector2.zero;
        private Vector2 _chatScrollPos = new Vector2(0, 99999);
        private Vector2 _cloudScrollPos = Vector2.zero;
        private Vector2 _localScrollPos = Vector2.zero;
        private string _searchQuery = "";
        private string _chatInputMessage = "";
        public static int _selectedNavTab = 0;
        private Vector2 _helpScrollPos = Vector2.zero;
        private List<MenuButton> _cachedVisibleButtons = new List<MenuButton>();
        private string _lastSearch = "___";
        private int _lastCategoryIndex = -1;

        private int _configSubTab = 0;
        private bool _showUploadModal = false;
        private string _uploadConfigName = "";
        private string _uploadConfigDesc = "";

        private bool _showLocalNewModal = false;
        private string _newLocalProfileName = "";
        private float _menuAnimProgress = 0f;
        private static MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique _freezeTargetCache = null;
        private static float _freezeTargetTimer = 0f;

        private class MenuStar
        {
            public Vector2 BasePos;
            public float Speed;
            public float Size;
            public Color Color;
        }
        private List<MenuStar> _menuStars = new List<MenuStar>();

        public MenuManager(IntPtr ptr) : base(ptr) { }

        public void RefreshTheme()
        {
            BOTUIStyles.RebuildTextures();
        }

        public static void RebuildTheme()
        {
            BOTUIStyles.RebuildTextures();
        }

        void Awake()
        {
            Instance = this;
            currentLanguage = PlayerPrefs.GetInt("BOT_Lang", 0);
            BOTPlugin.SyncConfigsToMemory();
            Categories = MenuUtility.BuildCategories();
            InitGalaxyStars();

            ConfigProfileManager.Init();
            ServerControlManager.Init();
            SuperKnockbackManager.Init();
            _ = Task.Run(() => CloudChatManager.FetchMessagesAsync());
            _ = Task.Run(() => CloudConfigManager.RefreshCloudList());
            AutoUpdateManager.CheckUpdate();
        }

        private void InitGalaxyStars()
        {
            _menuStars.Clear();
            for (int i = 0; i < 60; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = UnityEngine.Random.Range(30f, 380f);
                _menuStars.Add(new MenuStar
                {
                    BasePos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius),
                    Speed = UnityEngine.Random.Range(0.4f, 1.5f),
                    Size = UnityEngine.Random.Range(2f, 4.5f),
                    Color = (UnityEngine.Random.value > 0.5f) ? new Color(0.6f, 0.8f, 1f, 0.7f) : new Color(1f, 0.9f, 0.6f, 0.7f)
                });
            }
        }

        void Update()
        {
            BOTPlugin.FlushConfigIfDirty();
            if (IsOpen)
            {
                _menuAnimProgress = Mathf.Lerp(_menuAnimProgress, 1f, Time.unscaledDeltaTime * 22f);
            }
            else
            {
                _menuAnimProgress = Mathf.Lerp(_menuAnimProgress, 0f, Time.unscaledDeltaTime * 35f);
                if (_menuAnimProgress < 0.05f) _menuAnimProgress = 0f;
            }

            CloudChatManager.Update();

            BoxOpenerManager.Update();
            CrosshairManager.Update();
            AutoReadyManager.Update();
            WeaponSpawnerManager.Update();
            VisualsManager.Update();
            BridgeESPManager.Update();
            ButtonESPManager.Update();
            NetworkVisualsManager.Update();
            ThirdPersonManager.Update();
            LavaPunchManager.Update();
            SpectatorPOVManager.Update();
            OrbitManager.Update();
            FullbrightManager.Update();
            SkinManager.Update();
            WaterJumpManager.Update();
            WaterWalkManager.Update();
            PlayerNoClipManager.Update();
            TeleportManager.Update();
            ForceKillManager.Update();
            HillGodManager.Update();
            PlayerLauncherManager.Update();
            TilePainterManager.Update();
            TASManager.Update();
            SnowballManager.Update();
            GodModeManager.Update();
            KeyDisplayManager.Update();
            BridgeHelperManager.Update();
            AntiBoundManager.Update();
            BacktrackingManager.Update();
            ItemESPManager.Update();
            MiniGameTrollManager.Update();
            CinematicGraphicsManager.Update();
            BotTabManager.Update();
            DamageNotificationManager.Update();
            HitEffectManager.Update();
            ShinyIntensityManager.Update();
            AutoPlayDodgeball.Update();
            AutoPlaySnowBrawl.Update();
            AutoPlayBlockDrop.Update();
            AutoPlayDeathFromAbove.Update();
            AutoPlayCrabFight.Update();
            AutoPlayRedLight.Update();
            AutoPlaySteppingStones.Update();
            AutoPlayBustlingButtons.Update();
            AutoPlayTheFloorIsLava.Update();
            AutoPlayTileDrive.Update();
            AutoAdvertiser.Update();
            BOTMenuMod.AntroPort.AntroSkinManager.Update();
            EnemyScaleManager.Update();
            CustomServerListManager.Update();
            VoiceIndicatorManager.Update();
            WeaponInspectManager.Update();
            PerfectMicManager.Update();
            SmoothMovementManager.Update();
            SharedObjectDestroyer.Update();
            ServerListViewerManager.Update();
            GlowESPManager.Update();
            PlayerTrackerManager.Update();
            NetworkFreezeManager.Update();
            WeaponManager.Update();
            ShadowModePatches.Update();
            SteamTrackerManager.Update();
            SwarmMasterManager.Update();
            GlobalStatsManager.Update();
            ServerControlManager.Update();

            if (Variables.menuKey == KeyCode.None) Variables.menuKey = KeyCode.Insert;

            if (Input.GetKeyDown(Variables.menuKey) && !IsAnyBinding() && (IsOpen || !Utility.IsChatOpen()))
            {
                if (!BOTDisclaimerUI.IsAccepted)
                {
                    if (BOTDisclaimerUI.Instance != null) BOTDisclaimerUI.Instance.Show();
                    return;
                }

                IsOpen = !IsOpen;
                Variables.menuTrigger = IsOpen;

                bool inMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu";
                if (IsOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    if (inMenu) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                    else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                }
            }

            if (ShadowModePatches.bypassFreeze)
            {
                PersistentPlayerData.frozen = false;
                if (_freezeTargetCache == null || _freezeTargetTimer <= 0f)
                {
                    _freezeTargetCache = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
                    _freezeTargetTimer = 0.5f;
                }
                _freezeTargetTimer -= Time.unscaledDeltaTime;
                var me = _freezeTargetCache;
                if (me != null && !me.dead) { var rb = me.GetComponent<Rigidbody>(); if (rb != null && rb.isKinematic && !OrbitManager.enabled) rb.isKinematic = false; }
            }

            ExecuteBoundHotkeys();
        }

        void LateUpdate()
        {
            GunMasterManager.Update();
            VisualBlockManager.LateUpdate();
        }

        void OnGUI()
        {
            LegitESPManager.Render();
            ItemESPManager.Render();
            GunMasterManager.RenderFOV();

            ProcessKeybindCapture();

            if (_menuAnimProgress < 0.005f && !IsOpen) return;

            BOTUIStyles.EnsureTextures();
            GUI.backgroundColor = Color.clear;

            float currentScale = Mathf.Lerp(0.85f, 1.0f, _menuAnimProgress) * UIScale;
            Vector2 pivot = new Vector2(_windowRect.x + _windowRect.width / 2f, _windowRect.y + _windowRect.height / 2f);

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            GUI.color = new Color(1f, 1f, 1f, _menuAnimProgress);
            GUIUtility.ScaleAroundPivot(new Vector2(currentScale, currentScale), pivot);

            _windowRect = GUI.Window(7788, _windowRect, (Action<int>)DrawBOTWindow, "", GUIStyle.none);

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawBOTWindow(int windowId)
        {
            float w = _windowRect.width;
            float h = _windowRect.height;

            BOTUIStyles.DrawBox(new Rect(0, 0, w, h), BOTUIStyles.MainBgStyle);

            if (GalaxyBackground.enabled)
            {
                DrawGalaxyParticles(w, h);
            }

            BOTUIStyles.DrawBox(new Rect(0, 0, 185, h), BOTUIStyles.SidebarStyle);
            DrawSidebarContent(185, h);

            if (_selectedNavTab == 0)
            {
                DrawTopBarContent(195, 12, w - 205, 36);
                DrawModulesListContent(195, 52, w - 205, h - 60);
            }
            else if (_selectedNavTab == 1)
            {
                DrawGlobalChatContent(195, 12, w - 205, h - 24);
            }
            else if (_selectedNavTab == 2)
            {
                DrawConfigHubContent(195, 12, w - 205, h - 24);
            }
            else if (_selectedNavTab == 3)
            {
                DrawHelpContent(195, 12, w - 205, h - 24);
            }
            else if (_selectedNavTab == 4)
            {
                DrawServerControlContent(195, 12, w - 205, h - 24);
            }

            GUI.DragWindow(new Rect(0, 0, w, 28));
        }

        private void DrawGalaxyParticles(float w, float h)
        {
            float time = Time.unscaledTime;
            Vector2 center = new Vector2(w * 0.6f, h * 0.5f);
            Vector2 mousePos = Event.current.mousePosition;

            foreach (var star in _menuStars)
            {
                float rot = time * 0.1f * star.Speed;
                float cos = Mathf.Cos(rot);
                float sin = Mathf.Sin(rot);
                Vector2 pos = center + new Vector2(star.BasePos.x * cos - star.BasePos.y * sin, star.BasePos.x * sin + star.BasePos.y * cos);

                Vector2 toMouse = pos - mousePos;
                if (toMouse.magnitude < 80f && toMouse.magnitude > 0.1f)
                    pos += toMouse.normalized * (80f - toMouse.magnitude) * 0.4f;

                float twinkle = Mathf.PingPong(time * star.Speed * 2f, 1f);
                Color c = new Color(star.Color.r, star.Color.g, star.Color.b, star.Color.a * twinkle);
                GUI.color = c;
                GUI.DrawTexture(new Rect(pos.x, pos.y, star.Size, star.Size), BOTUIStyles.ActivePillTex);
            }
            GUI.color = Color.white;
        }

        private Vector2 _serverControlScroll = Vector2.zero;

        private void DrawServerControlContent(float startX, float startY, float width, float height)
        {
            GUI.Label(new Rect(startX, startY, width, 28), MenuUtility.T("Server & Host Control", "服务器与主持控制", "Управление сервером", "Sunucu Kontrolü", "Controle de Servidor"), BOTUIStyles.CardTitleStyle);

            bool isHost = (SteamManager.Instance != null && SteamManager.Instance.IsLobbyOwner());
            int memberCount = (SteamManager.Instance != null && SteamManager.Instance.currentLobby.m_SteamID != 0)
                ? SteamMatchmaking.GetNumLobbyMembers(SteamManager.Instance.currentLobby) : 0;

            float totalScrollH = 500f;
            if (isHost && memberCount > 0)
            {
                totalScrollH += 60f + memberCount * 30f;
            }

            Rect viewRect = new Rect(startX, startY + 50, width - 8, height - 54);
            _serverControlScroll = GUI.BeginScrollView(viewRect, _serverControlScroll, new Rect(0, 0, width - 26, totalScrollH), false, true);

            float cardW = width - 30f;
            float y = 2f;

            Rect card1 = new Rect(0, y, cardW, 85f);
            BOTUIStyles.DrawBox(card1, BOTUIStyles.CardNormalStyle);

            string practiceTitle = MenuUtility.T("Practice/Freeplay", "练习房", "Тренировка / Свободный режим", "Alıştırma / Serbest Oyun", "Treino / Modo Livre");
            GUI.Label(new Rect(card1.x + 16, card1.y + 10, 260, 20), $"<b>{practiceTitle}</b>", BOTUIStyles.CardTitleStyle);

            string practiceDesc = MenuUtility.T(
                "Quickly enter practice room or return to the lobby.",
                "快速进入练习房或返回原版准备大厅。",
                "Быстрый вход в тренировку или возврат в лобби.",
                "Alıştırma odasına hızlıca girin veya lobiye dönün.",
                "Entre rapidamente no treino ou volte ao lobby."
            );
            GUI.Label(new Rect(card1.x + 16, card1.y + 32, card1.width - 290, 18), $"<color=#777788>{practiceDesc}</color>", BOTUIStyles.CardDescStyle);

            if (GUI.Button(new Rect(card1.x + card1.width - 275, card1.y + 24, 140, 36), MenuUtility.T("Enter Practice", "进入 练习房", "Войти в тренировку", "Alıştırmaya Gir", "Entrar no Treino"), BOTUIStyles.ActivePillTextStyle))
            {
                ServerControlManager.CreatePlaygroundPractice();
            }

            GUI.color = new Color(0.9f, 0.4f, 0.4f);
            if (GUI.Button(new Rect(card1.x + card1.width - 130, card1.y + 24, 115, 36), MenuUtility.T("Back to Lobby", "返回准备大厅", "В лобби", "Lobiye Dön", "Voltar ao Lobby"), BOTUIStyles.InactivePillTextStyle))
            {
                ServerControlManager.ReturnToNormalLobby();
            }
            GUI.color = Color.white;

            y += 95f;

            Rect card2 = new Rect(0, y, cardW, 85f);
            BOTUIStyles.DrawBox(card2, BOTUIStyles.CardNormalStyle);

            string posTitle = MenuUtility.T("Position Recorder", "位置记录", "Запись позиции", "Konum Kaydı", "Gravador de Posição");
            GUI.Label(new Rect(card2.x + 16, card2.y + 10, 260, 20), $"<b>{posTitle}</b>", BOTUIStyles.CardTitleStyle);
            GUI.Label(new Rect(card2.x + 16, card2.y + 32, card1.width - 320, 18), $"<color=#777788>{MenuUtility.T("Click buttons to customize your save and teleport keys.", "点击右侧按钮自定义您的位置存档与传送按键（静默回溯）。", "Нажмите для смены клавиш.", "Tuşları değiştirmek için tıklayın.", "Clique para personalizar as teclas.")}</color>", BOTUIStyles.CardDescStyle);

            string saveKeyText = ServerControlManager.IsBindingSave ? "PRESS KEY..." : $"Save: [{ServerControlManager.SavePosKey}]";
            GUI.color = ServerControlManager.IsBindingSave ? Color.red : Color.white;
            if (GUI.Button(new Rect(card2.x + card2.width - 245, card2.y + 24, 115, 32), saveKeyText, BOTUIStyles.KeybindBtnStyle))
            {
                ServerControlManager.IsBindingSave = true;
                ServerControlManager.IsBindingLoad = false;
            }

            string loadKeyText = ServerControlManager.IsBindingLoad ? "PRESS KEY..." : $"Load: [{ServerControlManager.LoadPosKey}]";
            GUI.color = ServerControlManager.IsBindingLoad ? Color.red : Color.white;
            if (GUI.Button(new Rect(card2.x + card2.width - 120, card2.y + 24, 110, 32), loadKeyText, BOTUIStyles.KeybindBtnStyle))
            {
                ServerControlManager.IsBindingLoad = true;
                ServerControlManager.IsBindingSave = false;
            }
            GUI.color = Color.white;

            y += 95f;

            Rect card3 = new Rect(0, y, cardW, 70f);
            BOTUIStyles.DrawBox(card3, BOTUIStyles.CardNormalStyle);

            string visibilityTitle = MenuUtility.T("Server Visibility", "服务器可见性", "Видимость сервера", "Sunucu Görünürlüğü", "Visibilidade do Servidor");
            GUI.Label(new Rect(card3.x + 16, card3.y + 10, 260, 20), $"<b>{visibilityTitle}</b>", BOTUIStyles.CardTitleStyle);

            string visibilityDesc = MenuUtility.T(
                "Publish the server to the public list or hide it again as private",
                "将服务器发布至全网列表或重新隐藏为私密",
                "Опубликовать сервер в публичном списке или снова скрыть его как приватный",
                "Sunucuyu genel listede yayınla veya tekrar gizleyerek özel yap",
                "Publique o servidor na lista pública ou oculte-o"
            );
            GUI.Label(new Rect(card3.x + 16, card3.y + 32, card3.width - 260, 18), $"<color=#777788>{visibilityDesc}</color>", BOTUIStyles.CardDescStyle);

            if (GUI.Button(new Rect(card3.x + card3.width - 250, card3.y + 16, 115, 34), MenuUtility.T("Make Public", "发布到公共列表", "Публичный", "Herkese Aç", "Tornar Pública"), BOTUIStyles.ActivePillTextStyle))
            {
                ServerControlManager.SetLobbyPublic();
            }
            if (GUI.Button(new Rect(card3.x + card3.width - 125, card3.y + 16, 115, 34), MenuUtility.T("Make Private", "设为私密隐藏", "Приватный", "Gizli Yap", "Tornar Privada"), BOTUIStyles.InactivePillTextStyle))
            {
                ServerControlManager.SetLobbyInvisible();
            }

            y += 80f;

            Rect card4 = new Rect(0, y, cardW, 200f);
            BOTUIStyles.DrawBox(card4, BOTUIStyles.CardNormalStyle);

            GUI.Label(new Rect(card4.x + 16, card4.y + 8, 260, 20), $"<b>{MenuUtility.T("Host Customization", "服务器主持", "Настройки хоста", "Sunucu Yönetimi", "Gerenciamento de Host")}</b>", BOTUIStyles.CardTitleStyle);

            if (GUI.Button(new Rect(card4.x + card4.width - 240, card4.y + 6, 120, 24), MenuUtility.T("Paste Icon URL", "粘贴封面直链", "Вставить иконку", "İkon Yapıştır", "Colar Ícone"), BOTUIStyles.SmallSubBtnStyle))
            {
                ServerControlManager.PasteThumbnail();
            }
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUI.Button(new Rect(card4.x + card4.width - 110, card4.y + 6, 95, 24), MenuUtility.T("Reset", "恢复默认", "Сброс", "Sıfırla", "Resetar"), BOTUIStyles.SmallSubBtnStyle))
            {
                ServerControlManager.ResetLobby();
            }
            GUI.color = Color.white;

            GUI.Label(new Rect(card4.x + 16, card4.y + 36, 75, 20), $"<b>{MenuUtility.T("Lobby Name", "房间名称", "Имя комнаты", "Oda Adı", "Nome da Sala")}:</b>", BOTUIStyles.LabelStyle);
            ServerControlManager.InputLobbyName = GUI.TextField(new Rect(card4.x + 85, card4.y + 35, card4.width - 190, 22), ServerControlManager.InputLobbyName, BOTUIStyles.SearchTextStyle);

            if (string.IsNullOrEmpty(ServerControlManager.InputLobbyName))
            {
                string namePlaceholder = MenuUtility.T("Enter custom name...", "输入自定义房间名...", "Введите имя комнаты...", "Özel oda adı girin...", "Digite o nome da sala...");
                GUI.Label(new Rect(card4.x + 90, card4.y + 35, 200, 22), namePlaceholder, BOTUIStyles.SearchPlaceholderStyle);
            }

            if (GUI.Button(new Rect(card4.x + card4.width - 95, card4.y + 34, 80, 24), MenuUtility.T("Apply Raw", "应用原色", "Применить", "Uygula", "Aplicar"), BOTUIStyles.SmallSubBtnStyle))
            {
                ServerControlManager.ApplyPlainLobbyName();
            }

            string colorLabel = MenuUtility.T("Color Preset:", "一键上色:", "Цвета:", "Renkler:", "Cores:");
            GUI.Label(new Rect(card4.x + 16, card4.y + 68, 75, 20), $"<b>{colorLabel}</b>", BOTUIStyles.LabelStyle);

            string[] colors = { "#FF3333", "#00FF66", "#00DDFF", "#FFCC00", "#CC33FF", "#FF55AA", "#FFFFFF" };
            string[] colorNames = {
                MenuUtility.T("R", "红", "К", "K", "V"),
                MenuUtility.T("G", "绿", "З", "Y", "V"),
                MenuUtility.T("C", "青", "Г", "M", "C"),
                MenuUtility.T("Y", "金", "Ж", "S", "D"),
                MenuUtility.T("P", "紫", "Ф", "M", "R"),
                MenuUtility.T("Pk", "粉", "Р", "P", "R"),
                MenuUtility.T("W", "白", "Б", "B", "B")
            };
            Color[] uiColors = { new Color(1f, 0.3f, 0.3f), new Color(0.2f, 1f, 0.4f), new Color(0f, 0.9f, 1f), new Color(1f, 0.8f, 0.1f), new Color(0.8f, 0.3f, 1f), new Color(1f, 0.4f, 0.7f), Color.white };

            float colorBtnX = card4.x + 85;
            for (int i = 0; i < colors.Length; i++)
            {
                GUI.color = uiColors[i];
                if (GUI.Button(new Rect(colorBtnX + i * 36, card4.y + 66, 30, 24), colorNames[i], BOTUIStyles.SmallSubBtnStyle))
                {
                    ServerControlManager.ApplyColoredLobbyName(colors[i]);
                }
            }
            GUI.color = Color.white;

            string verLabel = MenuUtility.T("Fake Version:", "版本号修改:", "Версия:", "Sahte Sürüm:", "Forjar Versão:");
            GUI.Label(new Rect(card4.x + 16, card4.y + 104, 85, 20), $"<b>{verLabel}</b>", BOTUIStyles.LabelStyle);
            ServerControlManager.InputFakeVersion = GUI.TextField(new Rect(card4.x + 105, card4.y + 103, 120, 22), ServerControlManager.InputFakeVersion, BOTUIStyles.SearchTextStyle);

            if (string.IsNullOrEmpty(ServerControlManager.InputFakeVersion))
            {
                string verPlaceholder = MenuUtility.T("Version (1.362)...", "版本号 (如 1.362)...", "Версия (1.362)...", "Sürüm (1.362)...", "Versão (1.362)...");
                GUI.Label(new Rect(card4.x + 110, card4.y + 103, 160, 22), verPlaceholder, BOTUIStyles.SearchPlaceholderStyle);
            }

            if (GUI.Button(new Rect(card4.x + 235, card4.y + 102, 70, 24), MenuUtility.T("Apply", "应用", "Применить", "Uygula", "Aplicar"), BOTUIStyles.ActivePillTextStyle))
            {
                ServerControlManager.ApplyVersion(ServerControlManager.InputFakeVersion);
            }

            GUI.Label(new Rect(card4.x + 16, card4.y + 145, 120, 20), $"<b>Kick Code: {CustomKickBanManager.kickReason}</b>", BOTUIStyles.LabelStyle);
            CustomKickBanManager.kickReason = (int)GUI.HorizontalSlider(new Rect(card4.x + 130, card4.y + 151, 90, 10), CustomKickBanManager.kickReason, 0, 100);

            GUI.Label(new Rect(card4.x + 240, card4.y + 145, 120, 20), $"<b>Ban Code: {CustomKickBanManager.banReason}</b>", BOTUIStyles.LabelStyle);
            CustomKickBanManager.banReason = (int)GUI.HorizontalSlider(new Rect(card4.x + 350, card4.y + 151, 90, 10), CustomKickBanManager.banReason, 0, 100);

            y += 210f;

            if (isHost && memberCount > 0)
            {
                float dynamicCardH = 45f + memberCount * 28f;
                Rect card5 = new Rect(0, y, cardW, dynamicCardH);
                BOTUIStyles.DrawBox(card5, BOTUIStyles.CardNormalStyle);

                GUI.Label(new Rect(card5.x + 16, card5.y + 8, 200, 20), $"<b>{MenuUtility.T("Players & Whitelist", "玩家管理与白名单", "Игроки и вайтлист", "Oyuncular & Beyaz Liste", "Jogadores & Lista Branca")} ({memberCount})</b>", BOTUIStyles.CardTitleStyle);

                GUI.color = new Color(1f, 0.3f, 0.3f);
                if (GUI.Button(new Rect(card5.x + card5.width - 190, card5.y + 6, 175, 24), MenuUtility.T("Kick Non-Whitelisted", "一键清理非白名单玩家", "Кикнуть всех кроме WL", "WL Dışındakileri At", "Expulsar Fora da WL"), BOTUIStyles.ActivePillTextStyle))
                {
                    ServerControlManager.KickAllNonWhitelisted();
                }
                GUI.color = Color.white;

                float pY = card5.y + 36f;
                var lobby = SteamManager.Instance.currentLobby;
                ulong myId = SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;

                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                    if (member.m_SteamID == 0) continue;

                    string pName = Utility.StripColorTags(SteamFriends.GetFriendPersonaName(member));
                    bool isMe = (member.m_SteamID == myId);
                    bool isWL = TagServerManager.WhitelistIDs.Contains(member.m_SteamID);

                    string hostTag = MenuUtility.T("[Host/You]", "[房主/你]", "[Хост/Вы]", "[Kurucu/Sen]", "[Host/Você]");
                    string nameTag = isMe ? $"<color=#00FF44>{hostTag}</color> {pName}" : pName;
                    GUI.Label(new Rect(card5.x + 16, pY, 180, 20), nameTag, BOTUIStyles.LabelStyle);

                    if (!isMe)
                    {
                        string wlText = isWL ? "WL [ON]" : "WL [OFF]";
                        GUI.color = isWL ? Color.yellow : Color.white;
                        if (GUI.Button(new Rect(card5.x + 210, pY, 80, 22), wlText, BOTUIStyles.SmallSubBtnStyle))
                        {
                            ServerControlManager.ToggleWhitelist(member.m_SteamID);
                        }
                        GUI.color = Color.white;

                        GUI.color = new Color(1f, 0.4f, 0.4f);
                        if (GUI.Button(new Rect(card5.x + 300, pY, 55, 22), "Kick", BOTUIStyles.SmallSubBtnStyle))
                        {
                            ServerControlManager.KickPlayer(member.m_SteamID);
                        }
                        GUI.color = Color.white;
                    }

                    pY += 28f;
                }
            }

            GUI.EndScrollView();
        }

        private void DrawSidebarContent(float width, float height)
        {
            GUI.Label(new Rect(16, 14, 150, 20), "<b>BOTMenu</b>", BOTUIStyles.LogoTitleStyle);
            GUI.Label(new Rect(16, 32, 150, 16), $"<color=#666677><size=10>v{BOTPlugin.CurrentBotVersion} / Undetected</size></color>", BOTUIStyles.LabelStyle);

            float navStartY = 68f;
            if (AutoUpdateManager.HasUpdate)
            {
                Rect updateBtnRect = new Rect(10, 52, width - 20, 24);
                string updateText = AutoUpdateManager.IsDownloading
                    ? MenuUtility.T("Downloading...", "正在下载...", "Загрузка...", "İndiriliyor...", "Baixando...")
                    : MenuUtility.T(
                        $"Update to v{AutoUpdateManager.LatestVersion}",
                        $"发现 v{AutoUpdateManager.LatestVersion} (点击更新)",
                        $"Обновить v{AutoUpdateManager.LatestVersion}",
                        $"v{AutoUpdateManager.LatestVersion} Güncelle",
                        $"Atualizar v{AutoUpdateManager.LatestVersion}"
                    );

                GUI.color = new Color(0f, 1f, 0.4f);
                if (GUI.Button(updateBtnRect, updateText, BOTUIStyles.ActivePillTextStyle))
                {
                    AutoUpdateManager.StartUpdate();
                }
                GUI.color = Color.white;
                navStartY = 82f;
            }

            int msgCount = CloudChatManager.Messages.Count;
            string chatBadge = msgCount > 99 ? " <color=#00FF44><size=9>99+</size></color>" : (msgCount > 0 ? $" <color=#00FF44><size=9>{msgCount}</size></color>" : "");

            string[] navItems = {
                MenuUtility.T("Modules", "功能模块", "Модули", "Modüller", "Módulos"),
                MenuUtility.T("Chat", "聊天", "Чат", "Sohbet", "Chat") + chatBadge,
                MenuUtility.T("Cloud / Local", "云端 / 本地", "Облако / Локально", "Bulut / Yerel", "Nuvem / Local"),
                MenuUtility.T("Manual", "使用说明", "Инструкция", "Kılavuz", "Manual"),
                MenuUtility.T("Server Control", "服务器控制", "Управление сервером", "Sunucu Kontrolü", "Controle do Servidor") 
            };

            for (int i = 0; i < navItems.Length; i++)
            {
                bool isTabActive = (_selectedNavTab == i);
                Rect navRect = new Rect(10, navStartY + i * 36, width - 20, 32);

                if (isTabActive) BOTUIStyles.DrawBox(navRect, BOTUIStyles.ActiveNavPillStyle);

                GUIStyle style = isTabActive ? BOTUIStyles.ActiveNavTextStyle : BOTUIStyles.NavTextStyle;
                if (GUI.Button(navRect, navItems[i], style))
                {
                    _selectedNavTab = i;
                }
            }

            int onlineCount = GlobalStatsManager.OnlineUsers;
            string onlineText = MenuUtility.T($"{onlineCount} Online", $"{onlineCount} 人在线", $"{onlineCount} Онлайн", $"{onlineCount} Çevrimiçi", $"{onlineCount} Online");
            GUI.Label(new Rect(18, navStartY + navItems.Length * 36 + 6, width - 36, 20), $"<color=#00FF44>●</color>  <color=#888899><size=11>{onlineText}</size></color>", BOTUIStyles.LabelStyle);

            Rect userCardRect = new Rect(10, height - 68, width - 20, 56);
            BOTUIStyles.DrawBox(userCardRect, BOTUIStyles.UserCardStyle);

            string myName = BOTPlugin.ConfigCustomName != null ? BOTPlugin.ConfigCustomName.Value : "User";
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;

            GUI.Label(new Rect(18, height - 62, width - 36, 18), $"<b>{myName}</b>", BOTUIStyles.LabelStyle);
            GUI.Label(new Rect(18, height - 44, width - 36, 16), $"<color=#666677><size=10>steamid: {myId}</size></color>", BOTUIStyles.LabelStyle);

            if (GUI.Button(new Rect(18, height - 28, 22, 18), "🔄", BOTUIStyles.IconBtnStyle)) { BOTPlugin.Instance.Config.Reload(); BOTPlugin.SyncConfigsToMemory(); }
        }

        private string _helpSearch = "";
        private HashSet<string> _expandedHelpItems = new HashSet<string>();
        private HashSet<string> _expandedHelpCategories = new HashSet<string>();

        private void DrawHelpContent(float startX, float startY, float width, float height)
        {
            GUI.Label(new Rect(startX, startY, width, 28), MenuUtility.T("Feature Manual", "功能使用说明", "Инструкция функций", "Özellik Kılavuzu", "Manual de Funções"), BOTUIStyles.CardTitleStyle);
            GUI.Label(new Rect(startX, startY + 27, width, 22), MenuUtility.T("Open a module, then open any feature to see what it does.", "打开模块后，再点击具体功能查看它的作用。", "Откройте модуль, затем функцию, чтобы узнать назначение.", "Önce modülü, sonra özelliği açarak ne yaptığını görün.", "Abra um módulo e depois uma função para ver o que ela faz."), BOTUIStyles.LabelStyle);

            Rect searchRect = new Rect(startX, startY + 53, width - 8, 26);
            BOTUIStyles.DrawBox(searchRect, BOTUIStyles.SearchBoxStyle);
            _helpSearch = GUI.TextField(new Rect(startX + 10, startY + 56, width - 28, 20), _helpSearch, BOTUIStyles.SearchTextStyle);
            if (string.IsNullOrEmpty(_helpSearch)) GUI.Label(new Rect(startX + 10, startY + 56, width - 28, 20), MenuUtility.T("Search feature or purpose...", "搜索功能名称或用途...", "Поиск функции или назначения...", "Özellik veya amaç ara...", "Pesquisar função ou objetivo..."), BOTUIStyles.SearchPlaceholderStyle);

            string search = (_helpSearch ?? "").Trim().ToLower();
            List<ManualCategory> manuals = new List<ManualCategory>();
            foreach (MenuCategory category in Categories)
            {
                ManualCategory manual = new ManualCategory(Utility.StripColorTags(category.Name), "category_" + category.Name);
                foreach (MenuButton button in category.Buttons) AddManualEntries(manual.Items, button, 0);
                if (!string.IsNullOrEmpty(search)) manual.Items.RemoveAll(e => !e.Name.ToLower().Contains(search) && !e.Instruction.ToLower().Contains(search));
                if (manual.Items.Count > 0) manuals.Add(manual);
            }

            Rect viewRect = new Rect(startX, startY + 86, width - 8, height - 86);
            GUIStyle manualStyle = new GUIStyle(BOTUIStyles.LabelStyle) { wordWrap = true, richText = false };
            float totalHeight = 0f;
            foreach (ManualCategory category in manuals)
            {
                bool categoryOpen = string.IsNullOrEmpty(search) ? _expandedHelpCategories.Contains(category.Id) : true;
                totalHeight += 32f;
                if (categoryOpen)
                    foreach (ManualEntry item in category.Items) totalHeight += _expandedHelpItems.Contains(item.Id) ? 64f : 30f;
            }
            Rect contentRect = new Rect(0, 0, viewRect.width - 18, Mathf.Max(viewRect.height, totalHeight + 4f));
            _helpScrollPos = GUI.BeginScrollView(viewRect, _helpScrollPos, contentRect, false, true);

            float y = 0f;
            foreach (ManualCategory category in manuals)
            {
                bool categoryOpen = string.IsNullOrEmpty(search) ? _expandedHelpCategories.Contains(category.Id) : true;
                Rect categoryRow = new Rect(0, y, contentRect.width, 30);
                if (GUI.Button(categoryRow, (categoryOpen ? "▼ " : "▶ ") + category.Name, manualStyle))
                {
                    if (_expandedHelpCategories.Contains(category.Id)) _expandedHelpCategories.Remove(category.Id); else _expandedHelpCategories.Add(category.Id);
                }
                y += 32f;
                if (!categoryOpen) continue;

                foreach (ManualEntry entry in category.Items)
                {
                    bool expanded = _expandedHelpItems.Contains(entry.Id);
                    Rect row = new Rect(12, y, contentRect.width - 16, 28);
                    if (GUI.Button(row, (expanded ? "▼ " : "▶ ") + entry.Name, manualStyle))
                    {
                        if (expanded) _expandedHelpItems.Remove(entry.Id); else _expandedHelpItems.Add(entry.Id);
                    }
                    y += 30f;
                    if (expanded)
                    {
                        GUI.Label(new Rect(28, y, contentRect.width - 34, 30), entry.Instruction, manualStyle);
                        y += 34f;
                    }
                }
            }
            GUI.EndScrollView();
        }
        private class ManualCategory
        {
            public string Name;
            public string Id;
            public List<ManualEntry> Items = new List<ManualEntry>();
            public ManualCategory(string name, string id) { Name = name; Id = id; }
        }

        private class ManualEntry
        {
            public string Name;
            public string Instruction;
            public int Level;
            public bool IsCategory;
            public string Id;
            public ManualEntry(string name, string instruction, bool isCategory, int level = 0, string id = "")
            {
                Name = name; Instruction = instruction; IsCategory = isCategory; Level = level; Id = id;
            }
        }

        private void AddManualEntries(List<ManualEntry> entries, MenuButton button, int level)
        {
            string cleanName = Utility.StripColorTags(button.Label).Trim();
            if (string.IsNullOrEmpty(cleanName)) cleanName = button.ID;
            entries.Add(new ManualEntry(cleanName, GetManualInstruction(button), false, level, button.ID + "_" + level));
            if (button.Children != null)
            {
                foreach (MenuButton child in button.Children)
                    AddManualEntries(entries, child, level + 1);
            }
        }

        private string GetManualInstruction(MenuButton button)
        {
            string name = Utility.StripColorTags(button.Label).Trim();
            if (button.IsScrollable)
                return MenuUtility.T("Purpose: adjusts the value used by this feature.", "作用：调整这个功能使用的参数数值。", "Назначение: изменяет параметр этой функции.", "Amacı: bu özelliğin kullandığı değeri ayarlar.", "Objetivo: ajusta o valor usado por esta função.");
            if (button.Children != null && button.Children.Count > 0)
                return MenuUtility.T("Purpose: opens a group of related settings.", "作用：打开一组相关设置。", "Назначение: открывает группу связанных настроек.", "Amacı: ilgili ayar grubunu açar.", "Objetivo: abre um grupo de configurações relacionadas.");
            if (button.Action == null)
                return MenuUtility.T("Purpose: displays information about this item.", "作用：显示这个项目的相关信息。", "Назначение: показывает информацию об этом пункте.", "Amacı: bu öğe hakkında bilgi gösterir.", "Objetivo: mostra informações sobre este item.");

            return GetSpecificManualInstruction(button.ID, name);
        }

        private string GetSpecificManualInstruction(string id, string name)
        {
            switch (id)
            {
                case "AutoAim": return MenuUtility.T("Purpose: helps aim the weapon toward valid targets.", "作用：帮助枪械瞄准有效目标。", "Назначение: помогает наводить оружие на цели.", "Amacı: silahı geçerli hedeflere yönlendirmeye yardım eder.", "Objetivo: ajuda a apontar a arma para alvos válidos.");
                case "AutoFire": return MenuUtility.T("Purpose: fires the weapon automatically when it can shoot.", "作用：枪械可以射击时自动开火。", "Назначение: автоматически стреляет, когда оружие готово.", "Amacı: silah ateş edebildiğinde otomatik ateş eder.", "Objetivo: dispara automaticamente quando a arma está pronta.");
                case "NoCD": return MenuUtility.T("Purpose: removes the weapon firing cooldown, allowing shots to be fired again immediately.", "作用：取消武器射击之间的冷却时间，让武器可以立即再次射击。", "Назначение: убирает задержку между выстрелами оружия.", "Amacı: silah atışları arasındaki bekleme süresini kaldırır.", "Objetivo: remove o tempo de recarga entre disparos.");
                case "InfAmmo": return MenuUtility.T("Purpose: prevents the weapon's ammunition from being depleted.", "作用：防止武器弹药被消耗。", "Назначение: предотвращает расход боеприпасов.", "Amacı: silah mühimmatının azalmasını engeller.", "Objetivo: impede que a munição seja consumida.");
                case "NoRecoil": return MenuUtility.T("Purpose: removes or reduces camera shake and weapon recoil while shooting.", "作用：射击时取消或减小镜头抖动和武器后坐力。", "Назначение: убирает тряску камеры и отдачу.", "Amacı: ateş ederken kamera sarsıntısını ve geri tepmeyi azaltır.", "Objetivo: remove ou reduz o recuo e a vibração da câmara.");
                case "AimThru": return MenuUtility.T("Purpose: allows aim assistance to detect targets through walls.", "作用：允许瞄准辅助检测墙后的目标。", "Назначение: позволяет прицеливанию видеть цели сквозь стены.", "Amacı: nişan yardımının duvar arkasındaki hedefleri algılamasını sağlar.", "Objetivo: permite que a assistência de mira detecte alvos através de paredes.");
                case "ShowFov": return MenuUtility.T("Purpose: displays the area in which aim assistance searches for targets.", "作用：显示瞄准辅助搜索目标的范围。", "Назначение: показывает область поиска целей.", "Amacı: nişan yardımının hedef aradığı alanı gösterir.", "Objetivo: mostra a área onde a assistência procura alvos.");
                case "FovRadius": return MenuUtility.T("Purpose: sets how far from the crosshair aim assistance can search for targets.", "作用：设置瞄准辅助可以在准星周围多大范围内搜索目标。", "Назначение: задаёт радиус поиска целей вокруг прицела.", "Amacı: nişan çevresindeki hedef arama mesafesini ayarlar.", "Objetivo: define o raio de procura de alvos à volta da mira.");
                case "RapidDel": return MenuUtility.T("Purpose: sets the delay between automatic shots.", "作用：设置自动连续射击之间的间隔时间。", "Назначение: задаёт задержку между автоматическими выстрелами.", "Amacı: otomatik atışlar arasındaki gecikmeyi ayarlar.", "Objetivo: define o atraso entre disparos automáticos.");
                case "ClickTP": return MenuUtility.T("Purpose: teleports the player to the selected click position.", "作用：将玩家传送到点击选择的位置。", "Назначение: телепортирует игрока в выбранную точку.", "Amacı: oyuncuyu tıklanan konuma ışınlar.", "Objetivo: teletransporta o jogador para a posição clicada.");
                case "KillAura": return MenuUtility.T("Automatically attacks nearby valid players; configure range and target mode below.", "自动攻击附近有效玩家，可在下方设置范围和目标模式。", "Автоматически атакует игроков рядом.", "Yakındaki geçerli oyunculara saldırır.", "Ataca jogadores válidos próximos.");
                case "SilentKill": return MenuUtility.T("Purpose: automatically attacks valid players within the configured silent range.", "作用：自动攻击设置范围内的有效玩家。", "Назначение: автоматически атакует игроков в указанном радиусе.", "Amacı: ayarlanan menzildeki geçerli oyunculara otomatik saldırır.", "Objetivo: ataca automaticamente jogadores válidos dentro do alcance definido.");
                case "SilentRange": return MenuUtility.T("Purpose: sets the maximum distance for the silent attack system.", "作用：设置静默攻击系统的最大距离。", "Назначение: задаёт максимальную дистанцию тихой атаки.", "Amacı: sessiz saldırı sisteminin maksimum mesafesini ayarlar.", "Objetivo: define a distância máxima da morte silenciosa.");
                case "AtkRange": return MenuUtility.T("Purpose: sets the distance at which the attack system can reach targets.", "作用：设置攻击系统能够攻击到目标的距离。", "Назначение: задаёт дальность атаки по целям.", "Amacı: saldırı sisteminin hedeflere ulaşma mesafesini ayarlar.", "Objetivo: define o alcance de ataque aos alvos.");
                case "TargetMode": return MenuUtility.T("Purpose: chooses whether attacks target the nearest, selected, or all valid players.", "作用：选择攻击最近目标、选定目标或所有有效玩家。", "Назначение: выбирает ближайшую, выбранную или все цели.", "Amacı: en yakın, seçili veya tüm geçerli hedefleri seçer.", "Objetivo: escolhe o alvo mais próximo, selecionado ou todos os válidos.");
                case "LockOn": return MenuUtility.T("Purpose: keeps the attack system focused on the current target.", "作用：让攻击系统持续锁定当前目标。", "Назначение: удерживает фокус на текущей цели.", "Amacı: saldırı sistemini mevcut hedefte tutar.", "Objetivo: mantém o sistema focado no alvo atual.");
                case "Trail": return MenuUtility.T("Purpose: leaves a visible trail behind the player while moving.", "作用：玩家移动时在身后留下可见拖尾。", "Назначение: оставляет видимый след за игроком.", "Amacı: hareket ederken oyuncunun arkasında iz bırakır.", "Objetivo: deixa um rasto visível atrás do jogador.");
                case "JumpRing": return MenuUtility.T("Creates an expanding ring when you jump.", "跳跃时在脚下生成扩散波纹。", "Создаёт расширяющееся кольцо при прыжке.", "Zıpladığında genişleyen halka oluşturur.", "Cria um anel expansivo ao saltar.");
                case "WalkRing": return MenuUtility.T("Purpose: creates expanding rings under the player while walking.", "作用：玩家走路时在脚下生成逐渐变大并消失的扩散圈。", "Назначение: создаёт расширяющиеся кольца под игроком при ходьбе.", "Amacı: yürürken oyuncunun altında büyüyen halkalar oluşturur.", "Objetivo: cria ondas expansivas sob o jogador enquanto caminha.");
                case "Aura": return MenuUtility.T("Purpose: displays animated particles around the player.", "作用：在玩家周围显示动态粒子效果。", "Назначение: показывает анимированные частицы вокруг игрока.", "Amacı: oyuncunun etrafında hareketli parçacıklar gösterir.", "Objetivo: mostra partículas animadas ao redor do jogador.");
                case "ClkTP": return MenuUtility.T("Enable it, then use the configured click action to teleport.", "开启后使用对应鼠标点击操作进行传送。", "Включите и используйте назначенное нажатие для телепорта.", "Açın ve atanmış tıklama ile ışınlanın.", "Ative e use o clique configurado para teleportar.");
                case "WallNoClip": return MenuUtility.T("Purpose: disables world collision so the player can pass through walls.", "作用：关闭世界碰撞，使玩家可以穿过墙体。", "Назначение: отключает столкновения и позволяет проходить сквозь стены.", "Amacı: dünya çarpışmasını kapatıp duvarlardan geçmeyi sağlar.", "Objetivo: desativa a colisão do mundo para atravessar paredes.");
                case "Blink": return MenuUtility.T("Purpose: enables the blink/freecam movement mode for temporary repositioning.", "作用：开启瞬移/自由移动模式，用于临时改变位置。", "Назначение: включает режим мигания/свободного перемещения.", "Amacı: geçici konum değiştirme için blink/freecam modunu açar.", "Objetivo: ativa o modo blink/freecam para reposicionamento temporário.");
                case "FakeLag": return MenuUtility.T("Purpose: delays movement updates to create a backtracking effect.", "作用：延迟移动同步，产生回潮/回溯效果。", "Назначение: задерживает обновления движения для эффекта backtracking.", "Amacı: geri izleme etkisi için hareket güncellemelerini geciktirir.", "Objetivo: atrasa atualizações de movimento para criar backtracking.");
                case "WaterWalk": return MenuUtility.T("Purpose: creates an invisible support surface so the player can walk on water.", "作用：生成隐形支撑面，让玩家可以在水面行走。", "Назначение: создаёт опору для ходьбы по воде.", "Amacı: oyuncunun su üzerinde yürümesi için görünmez destek oluşturur.", "Objetivo: cria uma superfície invisível para caminhar sobre a água.");
                case "WtrJmp": return MenuUtility.T("Purpose: lets the player jump or bounce from the water surface.", "作用：让玩家可以从水面跳跃或弹起。", "Назначение: позволяет прыгать от поверхности воды.", "Amacı: oyuncunun su yüzeyinden zıplamasını sağlar.", "Objetivo: permite saltar ou impulsionar-se a partir da água.");
                case "AutoBot": return MenuUtility.T("Enables the supported mini-game automation routines. Select specific helpers below when available.", "开启支持的小游戏自动化；有单独辅助项时可在下方配置。", "Включает автоматизацию поддерживаемых мини-игр.", "Desteklenen mini oyun otomasyonlarını açar.", "Ativa as rotinas automáticas dos minijogos suportados.");
                case "AutoRdy": return MenuUtility.T("Purpose: automatically marks the player as ready when the lobby allows it.", "作用：大厅允许时自动将玩家标记为准备状态。", "Назначение: автоматически отмечает игрока готовым.", "Amacı: lobi izin verdiğinde oyuncuyu otomatik hazırlar.", "Objetivo: marca automaticamente o jogador como pronto quando permitido.");
                case "AutoBox": return MenuUtility.T("Purpose: automatically opens available boxes; the limit and delay control how many and how fast.", "作用：自动开启可用箱子；数量限制和延迟控制开启数量与速度。", "Назначение: автоматически открывает кейсы; лимит и задержка задают количество и скорость.", "Amacı: kutuları otomatik açar; limit ve gecikme miktarı ve hızı belirler.", "Objetivo: abre caixas automaticamente; limite e atraso controlam quantidade e velocidade.");
                case "AutoAd": return MenuUtility.T("Purpose: periodically sends the configured advertisement in public chat.", "作用：定时在公屏发送设置好的广告内容。", "Назначение: периодически отправляет рекламу в общий чат.", "Amacı: ayarlanan reklamı genel sohbete düzenli gönderir.", "Objetivo: envia periodicamente o anúncio configurado no chat público.");
                case "AutoPickup": return MenuUtility.T("Purpose: automatically picks up nearby spawned weapons.", "作用：自动拾取附近生成的武器。", "Назначение: автоматически подбирает появившееся оружие.", "Amacı: yakındaki oluşturulmuş silahları otomatik toplar.", "Objetivo: recolhe automaticamente armas próximas que surgiram.");
                case "GodM": return MenuUtility.T("Continuously attempts to keep the player alive; it may not work in every game mode.", "持续尝试保持玩家存活，并非所有模式都保证有效。", "Пытается сохранять жизнь игрока; не гарантируется во всех режимах.", "Oyuncuyu hayatta tutmaya çalışır; her modda garanti değildir.", "Tenta manter o jogador vivo; pode não funcionar em todos os modos.");
                case "Void": return MenuUtility.T("Purpose: attempts to move the player back to safety when falling into the void.", "作用：玩家掉入虚空时尝试将其拉回安全位置。", "Назначение: пытается вернуть игрока в безопасное место при падении в пустоту.", "Amacı: oyuncu boşluğa düşerken onu güvenli yere geri taşımayı dener.", "Objetivo: tenta levar o jogador de volta à segurança ao cair no vazio.");
                case "Rlgl": return MenuUtility.T("Purpose: attempts to prevent elimination during the Red Light, Green Light game.", "作用：在红灯绿灯模式中尝试防止玩家被淘汰。", "Назначение: пытается предотвратить выбывание в RLGL.", "Amacı: Kırmızı Işık Yeşil Işık oyununda elenmeyi önlemeye çalışır.", "Objetivo: tenta evitar a eliminação no jogo Luz Vermelha, Luz Verde.");
                case "Hill": return MenuUtility.T("Purpose: automatically helps move or hold the player in the hill/zone objective.", "作用：在山丘/占领区域目标中自动辅助移动或保持位置。", "Назначение: помогает удерживать позицию в режиме холма.", "Amacı: tepe/bölge hedefinde konumu korumaya yardım eder.", "Objetivo: ajuda a manter a posição no objetivo da colina/zona.");
                case "AKick": return MenuUtility.T("Attempts to block kick/ban-related effects on this client; results depend on the server.", "尝试阻止客户端受到踢出或封禁影响，效果取决于服务器。", "Пытается блокировать кик/бан; зависит от сервера.", "Kick/ban etkilerini engellemeyi dener; sunucuya bağlıdır.", "Tenta bloquear efeitos de kick/ban; depende do servidor.");
                case "Hide": return MenuUtility.T("Purpose: attempts to hide this player from the in-game player list.", "作用：尝试让本玩家不显示在游戏玩家列表中。", "Назначение: пытается скрыть игрока из списка игроков.", "Amacı: oyuncuyu oyun listesinden gizlemeyi dener.", "Objetivo: tenta ocultar o jogador da lista de jogadores.");
                case "ByFrz": return MenuUtility.T("Purpose: attempts to keep movement working while the client is frozen.", "作用：客户端被冻结时尝试保持移动功能。", "Назначение: пытается сохранить движение при заморозке клиента.", "Amacı: istemci dondurulmuşken hareketi sürdürmeye çalışır.", "Objetivo: tenta manter o movimento enquanto o cliente está congelado.");
                case "AntiSpc": return MenuUtility.T("Purpose: attempts to prevent other players from spectating this player.", "作用：尝试阻止其他玩家观战本玩家。", "Назначение: пытается запретить другим наблюдать за игроком.", "Amacı: diğer oyuncuların bu oyuncuyu izlemesini engellemeye çalışır.", "Objetivo: tenta impedir que outros jogadores assistam este jogador.");
                case "Lang": return MenuUtility.T("Click to cycle the menu language. The manual updates to the selected language.", "点击切换菜单语言，说明书也会同步切换。", "Нажмите для смены языка меню; инструкция изменится тоже.", "Menü dilini değiştirmek için tıklayın; kılavuz da değişir.", "Clique para mudar o idioma; o manual também será atualizado.");
                default: return MenuUtility.T("Purpose: controls the " + name + " function or action.", "作用：控制“" + name + "”对应的功能或操作。", "Назначение: управляет функцией или действием «" + name + "».", "Amacı: " + name + " özelliğini veya işlemini kontrol eder.", "Objetivo: controla a função ou ação " + name + ".");
            }
        }

        private void DrawLegacyHelpContent(float startX, float startY, float width, float height)
        {
            GUI.Label(new Rect(startX, startY, width, 30), MenuUtility.T("Feature Manual", "功能使用说明", "Инструкция функций", "Özellik Kılavuzu", "Manual de Funções"), BOTUIStyles.CardTitleStyle);
            GUI.Label(new Rect(startX, startY + 28, width, 22), MenuUtility.T("Click a module in Modules to enable or configure it.", "在 Modules 中点击功能即可开启或配置。", "Нажмите функцию в Modules для включения или настройки.", "Modules bölümünden özelliğe tıklayarak açın veya ayarlayın.", "Clique numa função em Modules para ativar ou configurar."), BOTUIStyles.LabelStyle);

            Rect viewRect = new Rect(startX, startY + 56, width - 8, height - 56);
            float contentHeight = 0f;
            string[][] sections = new string[][]
            {
                new string[] { MenuUtility.T("Combat", "战斗", "Бой", "Savaş", "Combate"), MenuUtility.T("Aimbot / Auto Fire: automatically aim or fire at targets.", "自瞄 / 自动开火：自动瞄准或开火。", "Аимбот / автоогонь: автоматическая помощь при прицеливании и стрельбе.", "Aimbot / Otomatik Ateş: hedefe nişan alır veya ateş eder.", "Aimbot / Fogo Automático: mira ou dispara automaticamente."), MenuUtility.T("Kill Aura / Silent Kill: attack nearby targets automatically.", "杀戮光环 / 静默击杀：自动攻击附近目标。", "Аура убийства / тихое убийство: автоматическая атака целей рядом.", "Ölüm Halesi / Sessiz Öldürme: yakındaki hedeflere saldırır.", "Aura de Morte / Morte Silenciosa: ataca alvos próximos."), MenuUtility.T("Infinite Ammo / No Cooldown: remove ammo or weapon cooldown limits.", "无限弹药 / 无冷却：移除弹药或武器冷却限制。", "Бесконечные патроны / без перезарядки: убирает ограничения.", "Sonsuz Mermi / Bekleme Yok: mühimmat ve bekleme sınırlarını kaldırır.", "Munição Infinita / Sem Recarga: remove limites de munição e recarga.") },
                new string[] { MenuUtility.T("Visuals", "视觉", "Визуал", "Görseller", "Visuais"), MenuUtility.T("ESP: displays players, items, buttons, bridges, or other objects.", "ESP：显示玩家、物品、按钮、玻璃桥等对象。", "ESP: показывает игроков, предметы, кнопки и мосты.", "ESP: oyuncuları, eşyaları, düğmeleri ve köprüleri gösterir.", "ESP: mostra jogadores, itens, botões e pontes."), MenuUtility.T("Player Visuals: trail, jump ring, walking ripple, aura, and shared effects.", "玩家特效：拖尾、跳跃波纹、行走扩散圈、光环和共享效果。", "Визуал игрока: след, кольцо прыжка, кольцо ходьбы и аура.", "Oyuncu Efektleri: iz, zıplama halkası, yürüme dalgası ve aura.", "Efeitos do Jogador: rastro, anel de pulo, onda ao caminhar e aura."), MenuUtility.T("Camera / Graphics: third person, zoom, FOV, fullbright, mirror, and cinematic effects.", "相机 / 画面：第三人称、缩放、FOV、全亮、后视镜和电影效果。", "Камера / графика: вид от третьего лица, зум, FOV и эффекты.", "Kamera / Grafik: üçüncü şahıs, yakınlaştırma, FOV ve efektler.", "Câmera / Gráficos: terceira pessoa, zoom, FOV e efeitos.") },
                new string[] { MenuUtility.T("Movement", "移动", "Движение", "Hareket", "Movimento"), MenuUtility.T("NoClip / Wall NoClip: disable player or world collision.", "穿人 / 穿墙：关闭玩家或世界碰撞。", "NoClip: отключает столкновения игрока или мира.", "NoClip: oyuncu veya dünya çarpışmasını kapatır.", "NoClip: desativa colisões do jogador ou do mundo."), MenuUtility.T("Teleport / Orbit / Launcher: move or orbit around selected positions and players.", "传送 / 环绕 / 发射：移动、环绕或快速接近目标。", "Телепорт / орбита / запуск: перемещение и вращение вокруг целей.", "Işınlanma / Yörünge / Fırlatıcı: hedeflere hareket eder veya etrafında döner.", "Teleporte / Órbita / Lançador: move-se ou orbita em torno dos alvos."), MenuUtility.T("Water Walk / Water Jump: move across or jump from water surfaces.", "水上行走 / 水上跳跃：在水面移动或跳跃。", "Ходьба по воде / прыжок из воды.", "Su Yürüyüşü / Su Zıplaması: su üzerinde hareket veya zıplama.", "Caminhar na Água / Salto na Água: move-se ou salta sobre a água.") },
                new string[] { MenuUtility.T("Protection & Misc", "保护与杂项", "Защита и прочее", "Koruma ve Diğer", "Proteção e Diversos"), MenuUtility.T("Anti-Kick / Shadow Mode: reduce player-list visibility and kick risks.", "防踢 / 隐身模式：降低玩家列表可见性和被踢风险。", "Анти-кик / скрытый режим: уменьшает видимость и риск кика.", "Anti-Kick / Gizli Mod: görünürlüğü ve atılma riskini azaltır.", "Anti-Kick / Modo Oculto: reduz visibilidade e risco de expulsão."), MenuUtility.T("God Mode / Rescue: attempts to prevent death or rescue the player from the void.", "无敌 / 救援：尝试防止死亡或从虚空中救回玩家。", "Бессмертие / спасение: пытается предотвратить смерть или падение в пустоту.", "God Mode / Kurtarma: ölümü veya boşluğa düşmeyi önlemeye çalışır.", "God Mode / Resgate: tenta evitar a morte ou queda no vazio."), MenuUtility.T("Automation: runs supported mini-games, auto-ready, auto-advertising, and other routines.", "自动化：运行小游戏、自动准备、自动广告等功能。", "Автоматизация: мини-игры, автоготовность, реклама и другое.", "Otomasyon: mini oyunlar, otomatik hazır, reklam ve diğer rutinler.", "Automação: minijogos, pronto automático, anúncios e outras rotinas.") }
            };

            foreach (string[] section in sections)
            {
                contentHeight += 26f + (section.Length - 1) * 44f + 10f;
            }

            Rect contentRect = new Rect(0, 0, viewRect.width - 12, Mathf.Max(viewRect.height, contentHeight));
            _helpScrollPos = GUI.BeginScrollView(viewRect, _helpScrollPos, contentRect, false, true);
            float y = 0f;
            foreach (string[] section in sections)
            {
                GUI.Label(new Rect(0, y, contentRect.width, 24), section[0], BOTUIStyles.CardTitleStyle);
                y += 26f;
                for (int i = 1; i < section.Length; i++)
                {
                    GUI.Label(new Rect(8, y, contentRect.width - 20, 42), "• " + section[i], BOTUIStyles.LabelStyle);
                    y += 44f;
                }
                y += 10f;
            }
            GUI.EndScrollView();
        }

        private void DrawTopBarContent(float startX, float startY, float width, float height)
        {
            float searchW = 115f;
            float searchX = startX + width - searchW - 5f;

            float availablePillsWidth = width - searchW - 15f;
            int catCount = Categories.Count > 0 ? Categories.Count : 6;
            float pillW = (availablePillsWidth - (catCount - 1) * 4f) / catCount;

            float currentPillX = startX;
            for (int i = 0; i < Categories.Count; i++)
            {
                bool isSelected = (categoryIndex == i);
                string cleanCatName = Utility.StripColorTags(Categories[i].Name).Replace("[", "").Replace("]", "").Trim();

                Rect pillRect = new Rect(currentPillX, startY + 3, pillW, 26);

                if (isSelected) BOTUIStyles.DrawBox(pillRect, BOTUIStyles.ActivePillBoxStyle);
                else BOTUIStyles.DrawBox(pillRect, BOTUIStyles.InactivePillBoxStyle);

                GUIStyle pStyle = isSelected ? BOTUIStyles.ActivePillTextStyle : BOTUIStyles.InactivePillTextStyle;
                pStyle.fontSize = cleanCatName.Length > 8 ? 9 : 10;

                if (GUI.Button(pillRect, cleanCatName, pStyle))
                {
                    categoryIndex = i;
                }

                currentPillX += pillW + 4f;
            }

            Rect searchRect = new Rect(searchX, startY + 3, searchW, 26);
            BOTUIStyles.DrawBox(searchRect, BOTUIStyles.SearchBoxStyle);

            _searchQuery = GUI.TextField(new Rect(searchX + 16, startY + 6, searchW - 20, 20), _searchQuery, BOTUIStyles.SearchTextStyle);
            if (string.IsNullOrEmpty(_searchQuery))
            {
                GUI.Label(new Rect(searchX + 16, startY + 6, searchW - 20, 20), "Search...", BOTUIStyles.SearchPlaceholderStyle);
            }
        }

        private void DrawModulesListContent(float startX, float startY, float width, float height)
        {
            if (Categories.Count == 0 || categoryIndex >= Categories.Count) return;

            string searchLower = _searchQuery.ToLower().Trim();

            if (_lastCategoryIndex != categoryIndex || _lastSearch != searchLower)
            {
                _lastCategoryIndex = categoryIndex;
                _lastSearch = searchLower;
                _cachedVisibleButtons.Clear();

                var currentButtons = Categories[categoryIndex].Buttons;
                for (int i = 0; i < currentButtons.Count; i++)
                {
                    var b = currentButtons[i];
                    if (b.ID != "Separator" &&
                        (string.IsNullOrEmpty(searchLower) || Utility.StripColorTags(b.Label).ToLower().Contains(searchLower)))
                    {
                        _cachedVisibleButtons.Add(b);
                    }
                }
            }

            float totalContentHeight = 0f;
            for (int i = 0; i < _cachedVisibleButtons.Count; i++)
            {
                var b = _cachedVisibleButtons[i];
                float cardH = 50f;
                if (b.IsExpanded)
                {
                    int childCount = (b.Children != null ? b.Children.Count : 0) + (b.IsScrollable ? 1 : 0) + (b.Action != null ? 1 : 0);
                    cardH += 10f + childCount * 32f;
                }
                totalContentHeight += cardH + 6f;
            }

            Rect viewRect = new Rect(startX, startY, width - 8, height);
            Rect scrollContentRect = new Rect(0, 0, width - 24, Mathf.Max(height, totalContentHeight + 10f));

            _scrollPos = GUI.BeginScrollView(viewRect, _scrollPos, scrollContentRect, false, false);

            float currentCardY = 2f;
            float cardWidth = width - 26;

            for (int i = 0; i < _cachedVisibleButtons.Count; i++)
            {
                var btn = _cachedVisibleButtons[i];
                float cardHeight = 50f;
                if (btn.IsExpanded)
                {
                    int settingCount = (btn.Children != null ? btn.Children.Count : 0) + (btn.IsScrollable ? 1 : 0) + (btn.Action != null ? 1 : 0);
                    cardHeight += 10f + settingCount * 32f;
                }

                DrawBOTCard(new Rect(0, currentCardY, cardWidth, cardHeight), btn, 50f);
                currentCardY += cardHeight + 6f;
            }

            GUI.EndScrollView();
        }

        private void DrawBOTCard(Rect fullRect, MenuButton btn, float headerHeight)
        {
            string statusStr = btn.Status != null ? btn.Status() : "";
            bool isEnabled = statusStr.Contains("ON") || statusStr.Contains("ACTIVE") || statusStr.Contains("开启") || statusStr.Contains("ВКЛ") || statusStr.Contains("LIGADO");

            Rect headRect = new Rect(fullRect.x, fullRect.y, fullRect.width, headerHeight);
            BOTUIStyles.DrawBox(headRect, isEnabled ? BOTUIStyles.CardActiveStyle : BOTUIStyles.CardNormalStyle);

            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && headRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    btn.Action?.Invoke();
                    MenuSoundManager.PlayClick();
                    if (btn.ID.Contains("Theme") || btn.ID.Contains("Alpha") || btn.ID.Contains("Lang"))
                        BOTUIStyles.RebuildTextures();
                    e.Use();
                }
                else if (e.button == 1)
                {
                    btn.IsExpanded = !btn.IsExpanded;
                    MenuSoundManager.PlayClick();
                    e.Use();
                }
                else if (e.button == 2)
                {
                    if (btn.Action != null && btn.ID != "Lang" && btn.ID != "Separator")
                    {
                        if (btn.IsBinding)
                        {
                            btn.BoundKey = KeyCode.None;
                            btn.IsBinding = false;
                            KeybindManager.SaveAllBinds();
                            MenuSoundManager.PlayClick();
                            Utility.ForceMessage(MenuUtility.T("Keybind cleared (NONE).", "已重置为无绑定 (NONE)。", "Привязка удалена.", "Tuş ataması temizlendi.", "Atalho removido."));
                        }
                        else
                        {
                            btn.IsBinding = true;
                            MenuSoundManager.PlayClick();
                            Utility.ForceMessage(MenuUtility.T("Press key to bind (Middle-click again to CLEAR)...", "请按下按键绑定 (再次点击中键清空/无绑定)...", "Нажмите клавишу (СКМ для очистки)...", "Tuşa basın (Temizlemek için tekrar Orta Tık)...", "Pressione tecla (Clique do meio para limpar)..."));
                        }
                    }
                    e.Use();
                }
            }

            Rect checkCircleRect = new Rect(headRect.x + 14, headRect.y + 15, 20, 20);
            GUI.DrawTexture(checkCircleRect, isEnabled ? BOTUIStyles.CheckedCircleTex : BOTUIStyles.UncheckedCircleTex);

            string cleanTitle = Utility.StripColorTags(btn.Label);
            GUI.Label(new Rect(headRect.x + 44, headRect.y + 7, headRect.width - 90, 18), $"<b>{cleanTitle}</b>", BOTUIStyles.CardTitleStyle);

            string defaultDesc = MenuUtility.T(
                 "Click to toggle or right-click to adjust.",
                 "点击切换或右键展开调节。",
                 "Нажмите для переключения или ПКМ для настройки.",
                 "Aç/kapat için tıkla veya ayar için sağ tıkla.",
                 "Clique para alternar ou clique direito para ajustar."
             );
            string descText = string.IsNullOrEmpty(statusStr) ? defaultDesc : statusStr;
            GUI.Label(new Rect(headRect.x + 44, headRect.y + 26, headRect.width - 90, 16), $"<color=#777788>{descText}</color>", BOTUIStyles.CardDescStyle);

            Rect dotsRect = new Rect(headRect.x + headRect.width - 28, headRect.y + 14, 20, 20);
            if (GUI.Button(dotsRect, "⋮", BOTUIStyles.DotsBtnStyle))
            {
                btn.IsExpanded = !btn.IsExpanded;
                MenuSoundManager.PlayClick();
            }

            if (btn.IsExpanded)
            {
                Rect trayRect = new Rect(fullRect.x, fullRect.y + headerHeight, fullRect.width, fullRect.height - headerHeight);
                BOTUIStyles.DrawBox(trayRect, BOTUIStyles.TrayStyle);

                float settingY = trayRect.y + 6f;

                if (btn.Action != null && btn.ID != "Lang" && btn.ID != "Separator")
                {
                    GUI.Label(new Rect(trayRect.x + 20, settingY, 150, 22), "<b>Keybind</b>", BOTUIStyles.LabelStyle);
                    string keyText = btn.IsBinding ? "PRESS KEY..." : (btn.BoundKey == KeyCode.None ? "NONE" : btn.BoundKey.ToString());

                    GUI.color = btn.IsBinding ? Color.red : Color.white;
                    if (GUI.Button(new Rect(trayRect.x + trayRect.width - 110, settingY, 95, 22), keyText, BOTUIStyles.KeybindBtnStyle))
                    {
                        if (btn.IsBinding)
                        {
                            btn.BoundKey = KeyCode.None;
                            btn.IsBinding = false;
                            KeybindManager.SaveAllBinds();
                            Utility.ForceMessage(MenuUtility.T("Keybind cleared.", "已重置为无绑定。", "Привязка удалена.", "Tuş ataması temizlendi.", "Atalho removido."));
                        }
                        else
                        {
                            btn.IsBinding = true;
                        }
                    }
                    GUI.color = Color.white;
                    settingY += 28f;
                }

                if (btn.IsScrollable && btn.Getter != null && btn.Setter != null)
                {
                    int currentVal = btn.Getter();
                    GUI.Label(new Rect(trayRect.x + 20, settingY, 160, 20), $"<b>{cleanTitle}</b>", BOTUIStyles.LabelStyle);
                    GUI.Label(new Rect(trayRect.x + trayRect.width - 80, settingY, 65, 20), $"<b>{currentVal}</b>", BOTUIStyles.ValueRightStyle);

                    float newVal = GUI.HorizontalSlider(new Rect(trayRect.x + 180, settingY + 5, trayRect.width - 270, 10), currentVal, btn.ScrollMin(), btn.ScrollMax());
                    if (Mathf.Abs(newVal - currentVal) > 0.5f)
                    {
                        btn.Setter((int)newVal);
                        if (btn.ID.Contains("Alpha")) BOTUIStyles.RebuildTextures();
                    }

                    settingY += 28f;
                }

                if (btn.Children != null)
                {
                    foreach (var child in btn.Children)
                    {
                        string childName = Utility.StripColorTags(child.Label);
                        Rect subRowRect = new Rect(trayRect.x, settingY, trayRect.width, 26f);

                        if (e != null && e.type == EventType.MouseDown && subRowRect.Contains(e.mousePosition) && e.button == 2)
                        {
                            if (child.Action != null && child.ID != "Lang" && child.ID != "Separator")
                            {
                                if (child.IsBinding)
                                {
                                    child.BoundKey = KeyCode.None;
                                    child.IsBinding = false;
                                    KeybindManager.SaveAllBinds();
                                    MenuSoundManager.PlayClick();
                                    Utility.ForceMessage(MenuUtility.T($"Cleared keybind for {childName} (NONE)", $"已清除 {childName} 的快捷键绑定 (无绑定)", $"Привязка удалена: {childName}", $"Tuş ataması silindi: {childName}", $"Atalho removido: {childName}"));
                                }
                                else
                                {
                                    child.IsBinding = true;
                                    MenuSoundManager.PlayClick();
                                    Utility.ForceMessage(MenuUtility.T($"Press key to bind: {childName} (Middle-click again to CLEAR)", $"请按下按键绑定: {childName} (再次点击中键清空)", $"Клавиша для: {childName} (СКМ для очистки)", $"Tuş ata: {childName} (Temizlemek için tekrar Orta Tık)", $"Pressione tecla: {childName} (Clique do meio para limpar)"));
                                }
                            }
                            e.Use();
                        }

                        if (child.IsScrollable && child.Getter != null && child.Setter != null)
                        {
                            int currentVal = child.Getter();
                            GUI.Label(new Rect(trayRect.x + 20, settingY, 160, 20), $"<b>{childName}</b>", BOTUIStyles.LabelStyle);
                            GUI.Label(new Rect(trayRect.x + trayRect.width - 80, settingY, 65, 20), $"<b>{currentVal}</b>", BOTUIStyles.ValueRightStyle);

                            float newVal = GUI.HorizontalSlider(new Rect(trayRect.x + 180, settingY + 5, trayRect.width - 270, 10), currentVal, child.ScrollMin(), child.ScrollMax());
                            if (Mathf.Abs(newVal - currentVal) > 0.5f)
                            {
                                child.Setter((int)newVal);
                                if (child.ID.Contains("Alpha")) BOTUIStyles.RebuildTextures();
                            }
                        }
                        else
                        {
                            GUI.Label(new Rect(trayRect.x + 20, settingY, 200, 20), $"<b>{childName}</b>", BOTUIStyles.LabelStyle);

                            string cStatus = child.IsBinding ? "PRESS KEY..." : (child.Status != null ? child.Status() : "EXEC");
                            if (child.BoundKey != KeyCode.None && !child.IsBinding) cStatus += $" [{child.BoundKey}]";

                            GUI.color = child.IsBinding ? Color.red : Color.white;
                            Rect btnRect = new Rect(trayRect.x + trayRect.width - 110, settingY, 95, 22);

                            if (GUI.Button(btnRect, cStatus, BOTUIStyles.SmallSubBtnStyle))
                            {
                                if (Event.current.button == 0)
                                {
                                    child.Action?.Invoke();
                                    if (child.ID.Contains("Theme") || child.ID.Contains("Color"))
                                        BOTUIStyles.RebuildTextures();
                                }
                            }
                            GUI.color = Color.white;
                        }
                        settingY += 28f;
                    }
                }
            }
        }

        private void DrawGlobalChatContent(float startX, float startY, float width, float height)
        {
            string globalTitle = MenuUtility.T("Global Chat", "全局跨服聊天", "Глобальный chat", "Genel Sohbet", "Chat Global");
            GUI.Label(new Rect(startX + 10, startY + 4, 180, 24), $"<b>{globalTitle}</b>", BOTUIStyles.LogoTitleStyle);

            string notifyBtnText = CloudChatManager.EnableNotification
                ? MenuUtility.T("Notify: ON", "消息提示: 开启", "Уведомления: ВКЛ", "Bildirim: AÇIK", "Notificação: LIGADA")
                : MenuUtility.T("Notify: OFF", "消息提示: 关闭", "Уведомления: ВЫКЛ", "Bildirim: KAPALI", "Notificação: DESLIGADA");

            Rect notifyBtnRect = new Rect(startX + width - 140, startY + 2, 130, 26);
            if (GUI.Button(notifyBtnRect, notifyBtnText, CloudChatManager.EnableNotification ? BOTUIStyles.ActivePillTextStyle : BOTUIStyles.InactivePillTextStyle))
            {
                CloudChatManager.EnableNotification = !CloudChatManager.EnableNotification;
                if (BOTPlugin.ConfigChatNotifications != null)
                {
                    BOTPlugin.ConfigChatNotifications.Value = CloudChatManager.EnableNotification;
                    BOTPlugin.SaveConfigDebounced();
                }
            }

            Rect channelPill = new Rect(startX + 10, startY + 36, 110, 32);
            GUI.DrawTexture(channelPill, BOTUIStyles.ActiveNavPillTex);

            int msgCount = CloudChatManager.Messages.Count;
            string liveBadge = msgCount > 99 ? "99+" : (msgCount > 0 ? msgCount.ToString() : "Live");
            GUI.Label(new Rect(startX + 22, startY + 42, 100, 20), $"Global <size=9><color=#00FF44>{liveBadge}</color></size>", BOTUIStyles.LabelStyle);

            float chatBoxX = startX + 130;
            float chatBoxW = width - 140;
            float chatBoxH = height - 55;

            if (BOTUIStyles.ChatBoxBgTex != null)
                GUI.DrawTexture(new Rect(chatBoxX, startY + 36, chatBoxW, chatBoxH), BOTUIStyles.ChatBoxBgTex);
            else
                GUI.DrawTexture(new Rect(chatBoxX, startY + 36, chatBoxW, chatBoxH), BOTUIStyles.CardNormalBgTex);

            Rect chatScrollRect = new Rect(chatBoxX + 8, startY + 42, chatBoxW - 16, chatBoxH - 12);

            float totalMsgHeight = 0f;
            float msgWidth = chatBoxW - 60f;
            foreach (var msg in CloudChatManager.Messages)
            {
                float textH = BOTUIStyles.ChatMsgStyle != null
                    ? BOTUIStyles.ChatMsgStyle.CalcHeight(new GUIContent(msg.Message), msgWidth)
                    : 20f;
                totalMsgHeight += 20f + textH + 8f;
            }

            _chatScrollPos = GUI.BeginScrollView(
                chatScrollRect,
                _chatScrollPos,
                new Rect(0, 0, chatBoxW - 35, Mathf.Max(chatBoxH - 20, totalMsgHeight + 10f)),
                false,
                true
            );

            if (CloudChatManager.Messages.Count == 0)
            {
                string emptyText = MenuUtility.T("No cloud messages yet...", "暂无云端消息，正在连接服务器...", "Нет сообщений...", "Henüz mesaj yok...", "Sem mensagens...");
                GUI.Label(new Rect(10, 10, 300, 20), $"<color=grey>{emptyText}</color>", BOTUIStyles.LabelStyle);
            }
            else
            {
                float msgY = 2f;
                foreach (var msg in CloudChatManager.Messages)
                {

                    string statusDot = msg.IsOnline ? "<color=#00FF44>●</color>" : "<color=#FF4444>●</color>";

                    GUI.Label(new Rect(0, msgY, chatBoxW - 40, 18), $"{statusDot} <b>{msg.UserName}</b> <color=#666677><size=9>{msg.Time}</size></color>", BOTUIStyles.LabelStyle);

                    float textH = BOTUIStyles.ChatMsgStyle != null
                        ? BOTUIStyles.ChatMsgStyle.CalcHeight(new GUIContent(msg.Message), msgWidth)
                        : 20f;

                    GUI.Label(new Rect(14, msgY + 18, msgWidth, textH), msg.Message, BOTUIStyles.ChatMsgStyle ?? BOTUIStyles.LabelStyle);

                    msgY += 20f + textH + 8f;
                }
            }

            GUI.EndScrollView();

            float inputY = startY + height - 12;
            Rect inputRect = new Rect(chatBoxX, inputY, chatBoxW - 40, 26);
            GUI.DrawTexture(inputRect, BOTUIStyles.SearchBoxTex);

            _chatInputMessage = GUI.TextField(new Rect(chatBoxX + 8, inputY + 4, chatBoxW - 55, 20), _chatInputMessage, BOTUIStyles.SearchTextStyle);
            if (string.IsNullOrEmpty(_chatInputMessage))
            {
                string msgPlaceholder = MenuUtility.T("Message the global channel...", "发送给全网BOT用户...", "Написать сообщение...", "Genel kanala yaz...", "Enviar mensagem...");
                GUI.Label(new Rect(chatBoxX + 8, inputY + 4, 200, 20), msgPlaceholder, BOTUIStyles.SearchPlaceholderStyle);
            }

            bool isSendTriggered = GUI.Button(new Rect(chatBoxX + chatBoxW - 35, inputY, 35, 26), "➤", BOTUIStyles.SmallSubBtnStyle) ||
                                  (Event.current.isKey && Event.current.keyCode == KeyCode.Return && !string.IsNullOrEmpty(_chatInputMessage));

            if (isSendTriggered && !string.IsNullOrEmpty(_chatInputMessage))
            {
                CloudChatManager.SendChatMessage(_chatInputMessage);
                _chatInputMessage = "";
                _chatScrollPos.y = 999999f;
            }
        }

        private void DrawConfigHubContent(float startX, float startY, float width, float height)
        {
            string tabCloudText = MenuUtility.T("Community Cloud", "云端社区", "Сообщество", "Bulut", "Nuvem Comunitária");
            string tabLocalText = MenuUtility.T("Local Profiles", "本地私密", "Локальные", "Yerel", "Perfis Locais");

            Rect subTabCloudRect = new Rect(startX + 10, startY + 2, 130, 28);
            Rect subTabLocalRect = new Rect(startX + 145, startY + 2, 130, 28);

            if (_configSubTab == 0) BOTUIStyles.DrawBox(subTabCloudRect, BOTUIStyles.ActivePillBoxStyle);
            else BOTUIStyles.DrawBox(subTabCloudRect, BOTUIStyles.InactivePillBoxStyle);

            if (_configSubTab == 1) BOTUIStyles.DrawBox(subTabLocalRect, BOTUIStyles.ActivePillBoxStyle);
            else BOTUIStyles.DrawBox(subTabLocalRect, BOTUIStyles.InactivePillBoxStyle);

            if (GUI.Button(subTabCloudRect, tabCloudText, _configSubTab == 0 ? BOTUIStyles.ActivePillTextStyle : BOTUIStyles.InactivePillTextStyle))
            {
                _configSubTab = 0;
            }
            if (GUI.Button(subTabLocalRect, tabLocalText, _configSubTab == 1 ? BOTUIStyles.ActivePillTextStyle : BOTUIStyles.InactivePillTextStyle))
            {
                _configSubTab = 1;
                ConfigProfileManager.RefreshProfiles();
            }

            if (_configSubTab == 0)
            {
                DrawCloudConfigContent(startX, startY + 34, width, height - 34);
            }
            else
            {
                DrawLocalConfigContent(startX, startY + 34, width, height - 34);
            }
        }

        private void DrawCloudConfigContent(float startX, float startY, float width, float height)
        {
            string uploadBtnText = MenuUtility.T("Upload", "上传配置", "Загрузить", "Yükle", "Enviar");
            string refreshBtnText = MenuUtility.T("Refresh", "刷新列表", "Обновить", "Yenile", "Atualizar");

            if (GUI.Button(new Rect(startX + width - 210, startY - 32, 100, 26), uploadBtnText, BOTUIStyles.ActivePillTextStyle))
            {
                _showUploadModal = !_showUploadModal;
            }
            if (GUI.Button(new Rect(startX + width - 100, startY - 32, 90, 26), refreshBtnText, BOTUIStyles.InactivePillTextStyle))
            {
                CloudConfigManager.RefreshCloudList();
            }

            if (_showUploadModal)
            {
                Rect modalRect = new Rect(startX + 10, startY + 2, width - 20, 110);
                if (BOTUIStyles.TrayStyle != null) BOTUIStyles.DrawBox(modalRect, BOTUIStyles.TrayStyle);
                else GUI.DrawTexture(modalRect, BOTUIStyles.TrayBgTex);

                GUI.Label(new Rect(modalRect.x + 15, modalRect.y + 10, 80, 20), $"<b>{MenuUtility.T("Name", "配置名称", "Имя", "İsim", "Nome")}:</b>", BOTUIStyles.LabelStyle);
                _uploadConfigName = GUI.TextField(new Rect(modalRect.x + 90, modalRect.y + 8, 200, 22), _uploadConfigName, BOTUIStyles.SearchTextStyle);

                if (string.IsNullOrEmpty(_uploadConfigName))
                {
                    GUI.Label(new Rect(modalRect.x + 94, modalRect.y + 8, 200, 22), MenuUtility.T("Enter profile name...", "输入配置名称...", "Введите имя...", "Profil adı...", "Nome do perfil..."), BOTUIStyles.SearchPlaceholderStyle);
                }

                GUI.Label(new Rect(modalRect.x + 15, modalRect.y + 40, 80, 20), $"<b>{MenuUtility.T("Hints", "按键说明", "Описание", "Açıklama", "Dicas")}:</b>", BOTUIStyles.LabelStyle);
                _uploadConfigDesc = GUI.TextField(new Rect(modalRect.x + 90, modalRect.y + 38, modalRect.width - 110, 22), _uploadConfigDesc, BOTUIStyles.SearchTextStyle);

                if (string.IsNullOrEmpty(_uploadConfigDesc))
                {
                    GUI.Label(new Rect(modalRect.x + 94, modalRect.y + 38, modalRect.width - 110, 22), MenuUtility.T("Optional hints (e.g. F=Fly, X=Aura)...", "选填按键说明 (例如: F=飞行, X=光环)...", "Описание (необязательно)...", "Açıklama / İpuçları...", "Dicas opcionais..."), BOTUIStyles.SearchPlaceholderStyle);
                }

                if (GUI.Button(new Rect(modalRect.x + modalRect.width - 120, modalRect.y + 70, 105, 26), MenuUtility.T("Publish", "确认发布云端", "Опубликовать", "Yayınla", "Publicar"), BOTUIStyles.ActivePillTextStyle))
                {
                    string finalName = string.IsNullOrWhiteSpace(_uploadConfigName) ? "Custom Profile" : _uploadConfigName.Trim();
                    CloudConfigManager.UploadConfig(finalName, _uploadConfigDesc);
                    _uploadConfigName = "";
                    _uploadConfigDesc = "";
                    _showUploadModal = false;
                }
                if (GUI.Button(new Rect(modalRect.x + modalRect.width - 200, modalRect.y + 70, 70, 26), MenuUtility.T("Cancel", "取消", "Отмена", "İptal", "Cancelar"), BOTUIStyles.InactivePillTextStyle))
                {
                    _showUploadModal = false;
                }
            }

            float listY = _showUploadModal ? startY + 120 : startY + 2;
            float listH = _showUploadModal ? height - 125 : height - 6;
            Rect cloudListRect = new Rect(startX + 10, listY, width - 20, listH);

            float totalCardHeight = 0f;
            float cardWidth = width - 42f;
            foreach (var cfg in CloudConfigManager.CloudConfigs)
            {
                float singleH = 54f;
                if (cfg.IsExpanded)
                {
                    string hintContent = string.IsNullOrEmpty(cfg.Description) ? MenuUtility.T("No keybind hints provided.", "作者未填写特别说明。", "Нет описания.", "Açıklama yok.", "Sem dicas.") : cfg.Description;
                    float textH = BOTUIStyles.ChatMsgStyle != null ? BOTUIStyles.ChatMsgStyle.CalcHeight(new GUIContent(hintContent), cardWidth - 30f) : 20f;
                    singleH += 28f + textH + 12f;
                }
                totalCardHeight += singleH + 6f;
            }

            _cloudScrollPos = GUI.BeginScrollView(cloudListRect, _cloudScrollPos, new Rect(0, 0, width - 38, Mathf.Max(listH, totalCardHeight)), false, true);

            if (CloudConfigManager.CloudConfigs.Count == 0)
            {
                string emptyCloud = MenuUtility.T("No cloud configs yet. Click 'Upload' to share yours!", "暂无云端分享配置，点击右上角【上传配置】发布你的配置！", "Нет профилей...", "Henüz profil yok...", "Sem perfis na nuvem...");
                GUI.Label(new Rect(10, 20, 380, 20), $"<color=grey>{emptyCloud}</color>", BOTUIStyles.LabelStyle);
            }
            else
            {
                ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                string myIdStr = myId.ToString();
                float cardY = 2f;
                Event e = Event.current;

                foreach (var cfg in CloudConfigManager.CloudConfigs)
                {
                    Rect cRect = new Rect(0, cardY, cardWidth, 54f);

                    if (e != null && e.type == EventType.MouseDown && cRect.Contains(e.mousePosition) && e.button == 1)
                    {
                        cfg.IsExpanded = !cfg.IsExpanded;
                        MenuSoundManager.PlayClick();
                        e.Use();
                    }

                    if (BOTUIStyles.CardNormalStyle != null) BOTUIStyles.DrawBox(cRect, BOTUIStyles.CardNormalStyle);
                    else GUI.DrawTexture(cRect, BOTUIStyles.CardNormalBgTex);

                    GUI.Label(new Rect(cRect.x + 15, cRect.y + 8, cRect.width - 230, 20), $"<b>{cfg.Name}</b>  <color=#00FF44><size=10>By: {cfg.Author}</size></color>", BOTUIStyles.CardTitleStyle);

                    string rightClickTip = MenuUtility.T("[Right-Click for Hints]", "[右键查看按键说明]", "[ПКМ для описания]", "[Açıklama için Sağ Tık]", "[Clique direito para dicas]");
                    GUI.Label(new Rect(cRect.x + 15, cRect.y + 28, cRect.width - 230, 18), $"<color=#777788>{cfg.Downloads} ⬇  |  {cfg.Likes} ♥  |  {cfg.Date}  <color=#5599FF>{rightClickTip}</color></color>", BOTUIStyles.CardDescStyle);

                    bool isMyConfig = (!string.IsNullOrEmpty(cfg.AuthorSteamId) && cfg.AuthorSteamId == myIdStr);

                    float rightBtnOffset = 0f;

                    float applyW = 90f;
                    float applyX = cRect.x + cRect.width - 95f;
                    if (GUI.Button(new Rect(applyX, cRect.y + 12, applyW, 30), MenuUtility.T("Apply", "下载并应用", "Загрузить", "Uygula", "Aplicar"), BOTUIStyles.ActivePillTextStyle))
                    {
                        CloudConfigManager.DownloadAndApply(cfg.Id, cfg.Name);
                    }
                    rightBtnOffset += 100f;

                    string heartIcon = cfg.Liked ? "♥" : "♡";
                    float likeW = 45f;
                    float likeX = cRect.x + cRect.width - rightBtnOffset - likeW;
                    GUI.color = cfg.Liked ? new Color(1f, 0.3f, 0.4f) : Color.white;
                    if (GUI.Button(new Rect(likeX, cRect.y + 12, likeW, 30), heartIcon, BOTUIStyles.InactivePillTextStyle))
                    {
                        CloudConfigManager.ToggleLike(cfg.Id, cfg.Name, cfg.Liked);
                    }
                    GUI.color = Color.white;
                    rightBtnOffset += 50f;

                    if (isMyConfig)
                    {
                        float delW = 50f;
                        float delX = cRect.x + cRect.width - rightBtnOffset - delW;
                        if (GUI.Button(new Rect(delX, cRect.y + 12, delW, 30), MenuUtility.T("Delete", "删除", "Удалить", "Sil", "Excluir"), BOTUIStyles.InactivePillTextStyle))
                        {
                            CloudConfigManager.DeleteConfig(cfg.Id, cfg.Name);
                        }
                    }

                    cardY += 54f;

                    if (cfg.IsExpanded)
                    {
                        string hintContent = string.IsNullOrEmpty(cfg.Description) ? MenuUtility.T("No keybind hints provided.", "作者未填写特别说明。", "Нет описания.", "Açıklama yok.", "Sem dicas.") : cfg.Description;
                        float textH = BOTUIStyles.ChatMsgStyle != null ? BOTUIStyles.ChatMsgStyle.CalcHeight(new GUIContent(hintContent), cardWidth - 30f) : 20f;
                        float trayH = 26f + textH + 10f;

                        Rect trayRect = new Rect(0, cardY, cardWidth, trayH);
                        if (BOTUIStyles.TrayStyle != null) BOTUIStyles.DrawBox(trayRect, BOTUIStyles.TrayStyle);
                        else GUI.DrawTexture(trayRect, BOTUIStyles.TrayBgTex);

                        GUI.Label(new Rect(trayRect.x + 14, trayRect.y + 6, trayRect.width - 28, 18), $"<b><color=#00DDFF>{MenuUtility.T("Keybind Hints & Details:", "按键说明与详情:", "Описание и бинды:", "Tuş İpuçları & Detaylar:", "Dicas e Detalhes:")}</color></b>", BOTUIStyles.LabelStyle);

                        GUI.Label(new Rect(trayRect.x + 14, trayRect.y + 24, trayRect.width - 28, textH), hintContent, BOTUIStyles.ChatMsgStyle ?? BOTUIStyles.LabelStyle);

                        cardY += trayH + 6f;
                    }
                    else
                    {
                        cardY += 6f;
                    }
                }
            }

            GUI.EndScrollView();
        }

        private void DrawLocalConfigContent(float startX, float startY, float width, float height)
        {
            string newBtnText = MenuUtility.T("+ New Profile", "+ 新建本地配置", "+ Создать", "+ Yeni Profil", "+ Novo Perfil");
            string openFolderText = MenuUtility.T("Folder", "打开文件夹", "Папка", "Klasör", "Pasta");
            string refreshBtnText = MenuUtility.T("Refresh", "刷新列表", "Обновить", "Yenile", "Atualizar");

            if (GUI.Button(new Rect(startX + width - 320, startY - 32, 110, 26), newBtnText, BOTUIStyles.ActivePillTextStyle))
            {
                _showLocalNewModal = !_showLocalNewModal;
            }
            if (GUI.Button(new Rect(startX + width - 200, startY - 32, 95, 26), openFolderText, BOTUIStyles.InactivePillTextStyle))
            {
                ConfigProfileManager.OpenProfileFolder();
            }
            if (GUI.Button(new Rect(startX + width - 95, startY - 32, 85, 26), refreshBtnText, BOTUIStyles.InactivePillTextStyle))
            {
                ConfigProfileManager.RefreshProfiles();
            }

            if (_showLocalNewModal)
            {
                Rect modalRect = new Rect(startX + 10, startY + 2, width - 20, 85);
                BOTUIStyles.DrawBox(modalRect, BOTUIStyles.TrayStyle);

                GUI.Label(new Rect(modalRect.x + 15, modalRect.y + 12, 80, 20), $"<b>{MenuUtility.T("Name", "配置名称", "Имя", "İsim", "Nome")}:</b>", BOTUIStyles.LabelStyle);
                _newLocalProfileName = GUI.TextField(new Rect(modalRect.x + 90, modalRect.y + 10, 220, 22), _newLocalProfileName, BOTUIStyles.SearchTextStyle);

                if (string.IsNullOrEmpty(_newLocalProfileName))
                {
                    GUI.Label(new Rect(modalRect.x + 94, modalRect.y + 10, 220, 22), MenuUtility.T("e.g. Rage_Hacks...", "例如: 暴力杀戮配置...", "Имя профиля...", "Örn: Legit...", "Ex: Rage..."), BOTUIStyles.SearchPlaceholderStyle);
                }

                if (GUI.Button(new Rect(modalRect.x + modalRect.width - 120, modalRect.y + 45, 105, 26), MenuUtility.T("Save", "保存当前配置", "Сохранить", "Kaydet", "Salvar"), BOTUIStyles.ActivePillTextStyle))
                {
                    if (!string.IsNullOrWhiteSpace(_newLocalProfileName))
                    {
                        ConfigProfileManager.SaveProfile(_newLocalProfileName.Trim());
                        _newLocalProfileName = "";
                        _showLocalNewModal = false;
                    }
                }
                if (GUI.Button(new Rect(modalRect.x + modalRect.width - 200, modalRect.y + 45, 70, 26), MenuUtility.T("Cancel", "取消", "Отмена", "İptal", "Cancelar"), BOTUIStyles.InactivePillTextStyle))
                {
                    _showLocalNewModal = false;
                }
            }

            float listY = _showLocalNewModal ? startY + 95 : startY + 2;
            float listH = _showLocalNewModal ? height - 100 : height - 6;

            Rect localListRect = new Rect(startX + 10, listY, width - 20, listH);
            float totalCardHeight = ConfigProfileManager.Profiles.Count * 62f;

            _localScrollPos = GUI.BeginScrollView(localListRect, _localScrollPos, new Rect(0, 0, width - 38, Mathf.Max(listH, totalCardHeight)), false, true);

            if (ConfigProfileManager.Profiles.Count == 0)
            {
                string emptyText = MenuUtility.T("No local profiles found. Click '+ New Profile' to save your current setup!", "暂无本地私密配置，点击右上角【+ 新建本地配置】保存你当前的专属设置！", "Нет профилей...", "Yerel profil yok...", "Sem perfis locais...");
                GUI.Label(new Rect(10, 20, 420, 20), $"<color=grey>{emptyText}</color>", BOTUIStyles.LabelStyle);
            }
            else
            {
                float cardY = 2f;
                for (int i = 0; i < ConfigProfileManager.Profiles.Count; i++)
                {
                    string profileName = ConfigProfileManager.Profiles[i];
                    Rect cRect = new Rect(0, cardY, width - 42, 54);
                    BOTUIStyles.DrawBox(cRect, BOTUIStyles.CardNormalStyle);

                    GUI.Label(new Rect(cRect.x + 15, cRect.y + 8, cRect.width - 240, 20), $"<b>{profileName}</b>  <color=#00DDFF><size=10>[ Private Local ]</size></color>", BOTUIStyles.CardTitleStyle);
                    GUI.Label(new Rect(cRect.x + 15, cRect.y + 28, cRect.width - 240, 18), $"<color=#777788>{profileName}.cfg  |  {MenuUtility.T("Local Storage", "仅存本机", "Локально", "Yerel Depolama", "Armazenamento Local")}</color>", BOTUIStyles.CardDescStyle);

                    if (GUI.Button(new Rect(cRect.x + cRect.width - 240, cRect.y + 12, 75, 30), MenuUtility.T("Save", "覆盖", "Записать", "Üzerine", "Salvar"), BOTUIStyles.InactivePillTextStyle))
                    {
                        ConfigProfileManager.SaveProfile(profileName);
                    }

                    if (profileName != "Default")
                    {
                        if (GUI.Button(new Rect(cRect.x + cRect.width - 160, cRect.y + 12, 45, 30), MenuUtility.T("Delete", "删除", "Удалить", "Sil", "Excluir"), BOTUIStyles.InactivePillTextStyle))
                        {
                            string targetPath = Path.Combine(ConfigProfileManager.ProfileFolder, profileName + ".cfg");
                            if (File.Exists(targetPath))
                            {
                                File.Delete(targetPath);
                                ConfigProfileManager.RefreshProfiles();
                                Utility.ForceMessage(MenuUtility.T($"Deleted profile: [{profileName}]", $"已删除本地配置: [{profileName}]", $"Удален: [{profileName}]", $"Silindi: [{profileName}]", $"Excluído: [{profileName}]"));
                            }
                        }
                    }

                    float applyX = (profileName != "Default") ? (cRect.x + cRect.width - 110) : (cRect.x + cRect.width - 160);
                    float applyW = (profileName != "Default") ? 100 : 150;

                    if (GUI.Button(new Rect(applyX, cRect.y + 12, applyW, 30), MenuUtility.T("Apply", "应用配置", "Применить", "Uygula", "Aplicar"), BOTUIStyles.ActivePillTextStyle))
                    {
                        string targetPath = Path.Combine(ConfigProfileManager.ProfileFolder, profileName + ".cfg");
                        if (File.Exists(targetPath))
                        {
                            string activePath = BOTPlugin.Instance.Config.ConfigFilePath;
                            File.Copy(targetPath, activePath, true);
                            BOTPlugin.Instance.Config.Reload();
                            BOTPlugin.SyncConfigsToMemory();
                            Utility.ForceMessage(MenuUtility.T($"Loaded local profile: [{profileName}]", $"已成功加载本地配置: [{profileName}]", $"Загружен: [{profileName}]", $"Yüklendi: [{profileName}]", $"Perfil carregado: [{profileName}]"));
                        }
                    }

                    cardY += 60f;
                }
            }

            GUI.EndScrollView();
        }

        private void ProcessKeybindCapture()
        {
            Event e = Event.current;
            if (e == null || !e.isKey || e.type != EventType.KeyDown || e.keyCode == KeyCode.None) return;

            Action<List<MenuButton>> processList = null;
            processList = (buttons) =>
            {
                if (buttons == null) return;
                foreach (var btn in buttons)
                {
                    if (btn.IsBinding)
                    {
                        if (e.keyCode == KeyCode.Escape)
                        {
                            btn.BoundKey = KeyCode.None;
                            Utility.ForceMessage(MenuUtility.T("Keybind cleared (NONE).", "已重置为无绑定 (NONE)。", "Привязка удалена.", "Tuş ataması temizlendi.", "Atalho removido."));
                        }
                        else
                        {
                            btn.BoundKey = e.keyCode;
                            Utility.ForceMessage(MenuUtility.T($"Bound to: {e.keyCode}", $"已成功绑定按键: {e.keyCode}", $"Привязано к: {e.keyCode}", $"Atandı: {e.keyCode}", $"Atribuído a: {e.keyCode}"));
                        }
                        btn.IsBinding = false;
                        ApplySpecialBind(btn);
                        KeybindManager.SaveAllBinds();
                        e.Use();
                        return;
                    }

                    if (btn.Children != null && btn.Children.Count > 0)
                        processList(btn.Children);
                }
            };

            foreach (var cat in Categories)
            {
                if (cat == null) continue;
                processList(cat.Buttons);
            }
        }

        private void ExecuteBoundHotkeys()
        {
            if (Utility.IsChatOpen()) return;
            if (GUIUtility.keyboardControl != 0) return;

            Action<List<MenuButton>> checkExecution = null;
            checkExecution = (buttons) =>
            {
                if (buttons == null) return;
                foreach (var btn in buttons)
                {
                    if (btn.ID == "WpnInsp" || btn.ID == "MenuKey" || btn.ID == "SvrListKey")
                    {
                        if (btn.Children != null && btn.Children.Count > 0) checkExecution(btn.Children);
                        continue;
                    }

                    if (btn.BoundKey != KeyCode.None && !btn.IsBinding && btn.Action != null)
                    {
                        if (Input.GetKeyDown(btn.BoundKey))
                        {
                            btn.Action.Invoke();
                        }
                    }

                    if (btn.Children != null && btn.Children.Count > 0)
                        checkExecution(btn.Children);
                }
            };

            foreach (var cat in Categories)
            {
                if (cat == null) continue;
                checkExecution(cat.Buttons);
            }
        }

        public static void ApplyMenuKey(KeyCode key)
        {
            if (key == KeyCode.None) key = KeyCode.Insert;
            Variables.menuKey = key;
            try
            {
                if (BOTPlugin.ConfigMenuKey != null)
                {
                    BOTPlugin.ConfigMenuKey.Value = key;
                    BOTPlugin.SaveConfigDebounced();
                }
            }
            catch { }
        }

        public static void ApplySpecialBind(MenuButton btn)
        {
            if (btn == null) return;
            if (btn.ID == "MenuKey")
            {
                ApplyMenuKey(btn.BoundKey);
            }
            else if (btn.ID == "SvrListKey")
            {
                BOTMenuMod.ServerListViewerManager.ToggleKey = btn.BoundKey;
            }
        }

        public static void ApplyAllSpecialBinds()
        {
            if (Categories == null) return;
            Action<List<MenuButton>> walk = null;
            walk = (buttons) =>
            {
                if (buttons == null) return;
                foreach (var btn in buttons)
                {
                    if (btn == null) continue;
                    ApplySpecialBind(btn);
                    if (btn.Children != null && btn.Children.Count > 0) walk(btn.Children);
                }
            };
            foreach (var cat in Categories)
            {
                if (cat == null) continue;
                walk(cat.Buttons);
            }
        }

        public static bool IsAnyBinding()
        {
            if (Categories == null) return false;
            bool result = false;
            Action<List<MenuButton>> walk = null;
            walk = (buttons) =>
            {
                if (buttons == null) return;
                foreach (var btn in buttons)
                {
                    if (btn == null) continue;
                    if (btn.IsBinding) { result = true; return; }
                    if (btn.Children != null && btn.Children.Count > 0) walk(btn.Children);
                }
            };
            foreach (var cat in Categories)
            {
                if (cat == null) continue;
                walk(cat.Buttons);
            }
            return result;
        }
    }
}
