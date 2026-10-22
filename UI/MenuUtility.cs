using System;
using System.Collections.Generic;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{
    public class MenuUtility
    {
        public static string T(string en, string cn, string ru = "", string tr = "", string pt = "")
        {
            if (MenuManager.currentLanguage == 1) return cn;
            if (MenuManager.currentLanguage == 2 && !string.IsNullOrEmpty(ru)) return ru;
            if (MenuManager.currentLanguage == 3 && !string.IsNullOrEmpty(tr)) return tr;
            if (MenuManager.currentLanguage == 4 && !string.IsNullOrEmpty(pt)) return pt;
            return en;
        }

        public static string TOFF() => $"<color=#FF4444>[ {T("OFF", "关闭", "ВЫКЛ", "KAPALI", "DESLIGADO")} ]</color>";
        public static string TState(bool state) => state
        ? $"<color={MenuThemeManager.CurrentAccentHex}>[ {T("ON", "开启", "ВКЛ", "AÇIK", "LIGADO")} ]</color>"
        : $"<color=#FF4444>[ {T("OFF", "关闭", "ВЫКЛ", "KAPALI", "DESLIGADO")} ]</color>";
        public static string TFolder() => $"<color={MenuThemeManager.CurrentAccentHex}>[ {T("FOLDER", "展开", "ПАПКА", "KLASÖR", "PASTA")} ]</color>";
        public static string TChange() => "[ " + T("CHANGE", "切换", "ИЗМЕНИТЬ", "DEĞIŞTIR", "ALTERAR") + " ]";
        public static string TExec() => "[ " + T("EXECUTE", "点击执行", "ВЫПОЛНИТЬ", "ÇALIŞTIR", "EXECUTAR") + " ]";

        public static MenuButton CreateBtn(string id, string label, Action action, Func<string> status)
        {
            Action wrappedAction = null;
            if (action != null)
            {
                wrappedAction = () => {
                    action.Invoke();
                    string cleanLabel = Utility.StripColorTags(label);
                    if (status != null) Utility.ForceMessage(cleanLabel + " " + status.Invoke());
                    else Utility.ForceMessage(cleanLabel + " " + T("Executed", "已执行", "Выполнено", "Çalıştırıldı", "Executado"));
                };
            }

            var btn = new MenuButton(id, label, wrappedAction, status);
            if (KeybindManager.SavedBinds.ContainsKey(id)) btn.BoundKey = KeybindManager.SavedBinds[id];
            return btn;
        }

        public static List<MenuCategory> BuildCategories()
        {
            List<MenuCategory> cats = new List<MenuCategory>();

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Combat ]", "[ 战斗武装 ]", "[ Боевые ]", "[ Savaş ]", "[ Combate ]"), Buttons = new List<MenuButton>() };

                var f1 = CreateBtn("GunFolder", T("Gun Master", "枪械大师", "Оружейный мастер", "Silah Uzmanı", "Mestre das Armas"), null, TFolder);
                var c1 = CreateBtn("AutoAim", T("Auto Aim", "自动锁定", "Автоприцел", "Oto Nişan", "Mira Automática"), () => { GunMasterManager.autoAim = !GunMasterManager.autoAim; BOTPlugin.ConfigGunAutoAim.Value = GunMasterManager.autoAim; BOTPlugin.SaveConfigDebounced(); }, () => TState(GunMasterManager.autoAim));
                var c2 = CreateBtn("AimThru", T("Aim Through Walls", "瞄准穿墙", "Аим сквозь стены", "Duvardan Nişan", "Mirar Através de Paredes"), () => { GunMasterManager.aimThroughWalls = !GunMasterManager.aimThroughWalls; BOTPlugin.ConfigGunAimThroughWalls.Value = GunMasterManager.aimThroughWalls; BOTPlugin.SaveConfigDebounced(); }, () => TState(GunMasterManager.aimThroughWalls));
                var c3 = CreateBtn("ShowFov", T("Show FOV Circle", "显示自瞄圈", "Показать круг FOV", "FOV Çemberi", "Círculo de FOV"), () => { GunMasterManager.showFovCircle = !GunMasterManager.showFovCircle; BOTPlugin.ConfigFovCircleEnabled.Value = GunMasterManager.showFovCircle; BOTPlugin.SaveConfigDebounced(); }, () => TState(GunMasterManager.showFovCircle));
                var c4 = new MenuButton("FovRadius", T("FOV Radius", "自瞄范围", "Радиус FOV", "FOV Yarıçapı", "Raio de FOV"), true, (v) => { GunMasterManager.fovRadius = v; BOTPlugin.ConfigFovRadius.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)GunMasterManager.fovRadius, () => 30, () => 500, 10);
                var c5 = CreateBtn("AutoFire", T("Auto Fire", "自动开火", "Автоогонь", "Oto Ateş", "Tiro Automático"), () => { GunMasterManager.autoShoot = !GunMasterManager.autoShoot; BOTPlugin.ConfigGunAutoShoot.Value = GunMasterManager.autoShoot; BOTPlugin.SaveConfigDebounced(); }, () => TState(GunMasterManager.autoShoot));

                var c5_a = new MenuButton("RapidDel", T("Rapid Delay (ms)", "连发间隔 (毫秒)", "Задержка (мс)", "Ateş Aralığı", "Atraso do Tiro (ms)"), true, (val) => {
                    WeaponAttackManager.rapidFireDelay = val / 1000f;
                    BOTPlugin.ConfigRapidFireDelay.Value = WeaponAttackManager.rapidFireDelay;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)(WeaponAttackManager.rapidFireDelay * 1000), () => 10, () => 1000, 10);
                var c6 = CreateBtn("NoRecoil", T("No Recoil", "无后座", "Без отдачи", "Sekmeme", "Sem Recuo"), () => { ShadowModePatches.noShake = !ShadowModePatches.noShake; BOTPlugin.ConfigGunNoRecoil.Value = ShadowModePatches.noShake; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.noShake));
                var c7 = CreateBtn("MagicB", T("Magic Bullet", "魔法子弹 (隔墙/无限距)", "Волшебная пуля", "Sihirli Mermi", "Bala Mágica"), () => { GunMasterManager.magicBullet = !GunMasterManager.magicBullet; BOTPlugin.ConfigMagicBullet.Value = GunMasterManager.magicBullet; BOTPlugin.SaveConfigDebounced(); }, () => TState(GunMasterManager.magicBullet));
                var c8 = CreateBtn("GlobalMag", T("<color=red>Global Wipe</color>", "<color=red>全图魔法子弹清场</color>", "<color=red>Глобальная чистка</color>", "<color=red>Tüm Haritayı Temizle</color>", "<color=red>Limpeza Global</color>"), () => {
                    GunMasterManager.globalMagicBullet = !GunMasterManager.globalMagicBullet;
                    BOTPlugin.ConfigGlobalMagicBullet.Value = GunMasterManager.globalMagicBullet;
                    BOTPlugin.SaveConfigDebounced();
                }, () => GunMasterManager.globalMagicBullet ? "<color=red>[ ACTIVE ]</color>" : TOFF());

                c1.IsChild = c2.IsChild = c3.IsChild = c4.IsChild = c5.IsChild = c5_a.IsChild = c6.IsChild = c7.IsChild = c8.IsChild = true;
                f1.Children.AddRange(new[] { c1, c2, c3, c4, c5, c5_a, c6, c7, c8 });

                var f2 = CreateBtn("KillSystem", T("Kill System", "杀戮系统", "Система убийств", "Öldürme Sistemi", "Sistema de Morte"), null, TFolder);
                var k1 = CreateBtn("SelectTarget", T("Select Target", "选择攻击目标", "Выбрать цель", "Hedef Seç", "Selecionar Alvo"), () => { JoinGameManager.CyclePlayerInLobby(); }, () => "[ " + JoinGameManager.GetSelectedPlayerName() + " ]");
                var k2 = CreateBtn("KillAura", T("Kill Aura", "暴力杀戮", "Аура убийства", "Ölüm Halesi", "Aura de Morte"), () => {
                    LavaPunchManager.enableKillAura = !LavaPunchManager.enableKillAura;
                    BOTPlugin.ConfigKillAura.Value = LavaPunchManager.enableKillAura;
                    BOTPlugin.SaveConfigDebounced();
                }, () => LavaPunchManager.enableKillAura ? "<color=red>[ ACTIVE ]</color>" : TOFF());
                var k3 = CreateBtn("CrashPlayer", T("Crash", "闪退打击", "Краш игрока", "Oyuncuyu Çökert", "Travar Jogador"), () => {
                    ShadowModePatches.crashPlayerOnHit = !ShadowModePatches.crashPlayerOnHit;
                    BOTPlugin.ConfigCrashPlayer.Value = ShadowModePatches.crashPlayerOnHit;
                    BOTPlugin.SaveConfigDebounced();
                }, () => ShadowModePatches.crashPlayerOnHit ? "<color=red>[ FATAL ]</color>" : TOFF());
                var k3_strong = CreateBtn("StrongCrash", T("Strong Crash", "强力崩溃", "Сильный краш", "Güçlü Çökme", "Super Travamento"), () => {
                    ShadowModePatches.enableStrongCrash = !ShadowModePatches.enableStrongCrash;
                    BOTPlugin.ConfigStrongCrash.Value = ShadowModePatches.enableStrongCrash;
                    BOTPlugin.SaveConfigDebounced();
                }, () => ShadowModePatches.enableStrongCrash ? "<color=red>[ ON ]</color>" : TOFF());
                var k4 = CreateBtn("ForceTPKill", T("Force TP Kill", "一键传送杀", "ТП Убийство", "TP Kill", "Morte por TP"), () => { ForceKillManager.Execute(); }, TExec);
                var k5 = CreateBtn("SilentKill", T("Silent Killing", "静默杀戮", "Тихое убийство", "Sessiz Ölüm", "Morte Silenciosa"), () => { ForceKillManager.killAuraEnabled = !ForceKillManager.killAuraEnabled; BOTPlugin.ConfigSilentKill.Value = ForceKillManager.killAuraEnabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(ForceKillManager.killAuraEnabled));
                var k6 = new MenuButton("SilentRange", T("Silent Range", "静默杀戮范围", "Дистанция", "Sessiz Menzil", "Alcance Silencioso"), true, (val) => { ForceKillManager.auraRange = val; BOTPlugin.ConfigSilentKillRange.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)ForceKillManager.auraRange, () => 1, () => 50, 1);
                var k7 = CreateBtn("AntiHost", T("Anti-Host", "不攻击房主", "Не бить хоста", "Kurucuya Vurma", "Proteger Host"), () => { LavaPunchManager.antiHost = !LavaPunchManager.antiHost; BOTPlugin.ConfigAntiHost.Value = LavaPunchManager.antiHost; BOTPlugin.SaveConfigDebounced(); }, () => TState(LavaPunchManager.antiHost));
                var k8 = CreateBtn("TargetMode", T("Target Mode", "切换目标模式", "Режим цели", "Hedef Modu", "Modo de Alvo"), () => { LavaPunchManager.attackMode = (LavaPunchManager.attackMode + 1) % 3; BOTPlugin.ConfigLavaAttackMode.Value = LavaPunchManager.attackMode; BOTPlugin.SaveConfigDebounced(); }, () => { if (LavaPunchManager.attackMode == 0) return "[ " + T("Nearest", "最近", "Ближайший", "En Yakın", "Mais Próximo") + " ]"; else if (LavaPunchManager.attackMode == 1) return "[ " + T("Selected", "选定", "Выбранный", "Seçili", "Selecionado") + " ]"; else return "[ " + T("All", "全体", "Все", "Hepsi", "Todos") + " ]"; });
                var k9 = CreateBtn("LockOn", T("Lock On", "黏着模式", "Захват цели", "Hedefe Kilitlen", "Travar no Alvo"), () => { LavaPunchManager.lockOnTarget = !LavaPunchManager.lockOnTarget; BOTPlugin.ConfigLavaLockOn.Value = LavaPunchManager.lockOnTarget; BOTPlugin.SaveConfigDebounced(); }, () => TState(LavaPunchManager.lockOnTarget));
                var k10 = new MenuButton("AtkRange", T("Attack Range", "暴力攻击范围", "Радиус атаки", "Saldırı Menzili", "Alcance de Ataque"), true, (v) => { LavaPunchManager.attackRange = v; BOTPlugin.ConfigLavaRange.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)LavaPunchManager.attackRange, () => 1, () => 2000, 10);
                var k11 = new MenuButton("PunchMult", T("Punch Mult", "攻击倍率", "Множитель удара", "Vuruş Çarpanı", "Multiplicador de Soco"), true, (v) => { LavaPunchManager.punchMultiplier = v; BOTPlugin.ConfigLavaMultiplier.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => LavaPunchManager.punchMultiplier, () => 1, () => 50, 1);
                var k12 = CreateBtn("KillCrab", T("Insta-Kill Crab", "一键秒杀巨蟹", "Убить краба", "Yengeci Öldür", "Matar Caranguejo"), () => { CrabKillerManager.ExecuteKill(); }, TExec);

                k1.IsChild = k2.IsChild = k3.IsChild = k3_strong.IsChild = k4.IsChild = k5.IsChild = k6.IsChild = k7.IsChild = k8.IsChild = k9.IsChild = k10.IsChild = k11.IsChild = k12.IsChild = true;
                f2.Children.AddRange(new[] { k1, k2, k3, k3_strong, k4, k5, k6, k7, k8, k9, k10, k11, k12 });

                var f3 = CreateBtn("WpnMods", T("Weapon Mods", "武器修改", "Моды на оружие", "Silah Modları", "Mods de Armas"), null, TFolder);
                var w1 = CreateBtn("InfAmmo", T("Infinite", "无限子弹", "Беск. патроны", "Sınırsız Mermi", "Munição Infinita"), () => { WeaponAttackManager.enableInfiniteAmmo = !WeaponAttackManager.enableInfiniteAmmo; BOTPlugin.ConfigInfAmmo.Value = WeaponAttackManager.enableInfiniteAmmo; BOTPlugin.SaveConfigDebounced(); }, () => TState(WeaponAttackManager.enableInfiniteAmmo));
                var w2 = CreateBtn("NoCD", T("No CD", "无冷却", "Без перезарядки", "Bekleme Yok", "Sem Cooldown"), () => { WeaponAttackManager.enableNoCooldown = !WeaponAttackManager.enableNoCooldown; BOTPlugin.ConfigNoCD.Value = WeaponAttackManager.enableNoCooldown; BOTPlugin.SaveConfigDebounced(); }, () => TState(WeaponAttackManager.enableNoCooldown));
                var w3 = CreateBtn("NoWpnPush", T("No Weapon Push", "武器无推力", "Без отталкивания", "Geri Tepme Yok", "Sem Empurrão"), () => { ShadowModePatches.enableNoWeaponPush = !ShadowModePatches.enableNoWeaponPush; BOTPlugin.ConfigNoWeaponPush.Value = ShadowModePatches.enableNoWeaponPush; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enableNoWeaponPush));
                var w4 = CreateBtn("VisBlock", T("Visual Block", "右键格挡动画", "Анимация блока", "Görsel Engelleme", "Bloqueio Visual"), () => { VisualBlockManager.enabled = !VisualBlockManager.enabled; BOTPlugin.ConfigVisualBlock.Value = VisualBlockManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(VisualBlockManager.enabled));

                var w5 = new MenuButton("WpnInsp", T("Weapon Inspect", "武器检视", "Осмотр оружия", "Silah İncele", "Inspecionar Arma"), () => { }, null, false);
                w5.Action = () => { w5.IsBinding = true; };
                if (KeybindManager.SavedBinds.ContainsKey("WpnInsp"))
                {
                    w5.BoundKey = KeybindManager.SavedBinds["WpnInsp"];
                    WeaponInspectManager.InspectKey = w5.BoundKey;
                }
                w5.Status = () => {
                    WeaponInspectManager.InspectKey = w5.BoundKey;
                    return w5.BoundKey == KeyCode.None ? "<color=grey>[ NONE ]</color>" : $"<color=yellow>[ {w5.BoundKey} ]</color>";
                };

                var w6_enable = CreateBtn("EnKnockMult", T("Enable Knockback Mult", "开启击退倍率", "Включить множитель", "Geri Tepme Çarpanını Aç", "Ativar Multiplicador"), () => {
                    WeaponAttackManager.enableKnockbackMult = !WeaponAttackManager.enableKnockbackMult;
                    BOTPlugin.ConfigKnockbackMultEnabled.Value = WeaponAttackManager.enableKnockbackMult;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(WeaponAttackManager.enableKnockbackMult));

                var w6 = new MenuButton("KnockMult", T("Knockback Mult", "修改武器击退倍率", "Отдача", "Geri Tepme", "Multiplicador de Repulsão"), true, (v) => {
                    WeaponAttackManager.knockbackMult = v / 10f;
                    BOTPlugin.ConfigKnockbackMult.Value = WeaponAttackManager.knockbackMult;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)(WeaponAttackManager.knockbackMult * 10), () => -500, () => 500, 5);

                var w7 = CreateBtn("KbDeflect", T("Deflect Knockback", "击退方向偏转", "Отражение отбрасывания", "Geri Tepme Saptırıcı", "Desviar Repulsão"), () => {
                    WeaponAttackManager.enableKnockbackDeflect = !WeaponAttackManager.enableKnockbackDeflect;
                    BOTPlugin.ConfigKnockbackDeflect.Value = WeaponAttackManager.enableKnockbackDeflect;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(WeaponAttackManager.enableKnockbackDeflect));

                var w7_a = new MenuButton("DefAngle", T("Deflect Angle", "偏转角度 (180=拉扯)", "Угол отражения", "Sapma Açısı", "Ângulo de Desvio"), true, (val) => {
                    WeaponAttackManager.knockbackDeflectAngle = val;
                    BOTPlugin.ConfigDeflectAngle.Value = val;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)WeaponAttackManager.knockbackDeflectAngle, () => -180, () => 180, 15);

                w1.IsChild = w2.IsChild = w3.IsChild = w4.IsChild = w5.IsChild = w6_enable.IsChild = w6.IsChild = w7.IsChild = w7_a.IsChild = true;
                f3.Children.AddRange(new[] { w1, w2, w3, w4, w5, w6_enable, w6, w7, w7_a });

                var f4 = CreateBtn("WpnSpawn", T("Weapon Spawner", "武器生成", "Спавнер оружия", "Silah Çıkarıcı", "Gerador de Armas"), null, TFolder);
                var ws1 = CreateBtn("EnSpawner", T("Enable Spawner", "生成武器", "Включить спавнер", "Çıkarıcıyı Aç", "Ativar Gerador"), () => { WeaponSpawnerManager.IsActive = !WeaponSpawnerManager.IsActive; BOTPlugin.ConfigItemSpawner.Value = WeaponSpawnerManager.IsActive; BOTPlugin.SaveConfigDebounced(); }, () => TState(WeaponSpawnerManager.IsActive));
                var ws2 = CreateBtn("AutoPickup", T("Auto Pickup", "拿起武器", "Автоподбор", "Oto Toplama", "Pegar Automático"), () => { WeaponSpawnerManager.ShouldPickUp = !WeaponSpawnerManager.ShouldPickUp; BOTPlugin.ConfigAutoPickup.Value = WeaponSpawnerManager.ShouldPickUp; BOTPlugin.SaveConfigDebounced(); }, () => TState(WeaponSpawnerManager.ShouldPickUp));
                var ws3 = CreateBtn("InfMode", T("Infinite Mode", "无限生成", "Беск. режим", "Sınırsız Mod", "Modo Infinito"), () => { WeaponSpawnerManager.IsInfinite = !WeaponSpawnerManager.IsInfinite; BOTPlugin.ConfigWeaponSpawnerInfinite.Value = WeaponSpawnerManager.IsInfinite; BOTPlugin.SaveConfigDebounced(); }, () => WeaponSpawnerManager.IsInfinite ? "<color=magenta>[ ACTIVE ]</color>" : TOFF());
                var ws4 = CreateBtn("SelWeapon", T("Select", "选择武器", "Выбрать оружие", "Silah Seç", "Selecionar Arma"), () => { WeaponSpawnerManager.SelectedWeaponIdx = (WeaponSpawnerManager.SelectedWeaponIdx + 1) % 5; }, () => "[ " + WeaponSpawnerManager.GetWeaponName() + " ]");
                var ws5 = new MenuButton("WpnMult", T("Multiplier", "生成倍率", "Множитель спавна", "Çarpan", "Multiplicador"), true, (v) => { WeaponSpawnerManager.Multiplier = v; BOTPlugin.ConfigItemSpawnerMultiplier.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => WeaponSpawnerManager.Multiplier, () => 1, () => 50, 1);

                ws1.IsChild = ws2.IsChild = ws3.IsChild = ws4.IsChild = ws5.IsChild = true;
                f4.Children.AddRange(new[] { ws1, ws2, ws3, ws4, ws5 });

                cat.Buttons.Add(f1); cat.Buttons.Add(f2); cat.Buttons.Add(f3); cat.Buttons.Add(f4);

                var m_fold = CreateBtn("GibsonFold", T("Gibson Weapon Spoofer", "神罚武器打击", "Оружие", "Silah Vuruşu", "Golpe de Arma Gibson"), null, TFolder);
                var m_exec = CreateBtn("GibsonExec", T("   Enable Attack", "   开启打击循环", "   Включить", "   Açık", "   Ativar Ataque"), () => { WeaponManager.Enabled = !WeaponManager.Enabled; BOTPlugin.ConfigGibsonEnabled.Value = WeaponManager.Enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(WeaponManager.Enabled));
                var m_mode = CreateBtn("GibsonMode", T("   Attack Mode", "   打击模式", "   Режим", "   Mod", "   Modo de Ataque"), () => {
                    WeaponManager.PacketMode = (WeaponManager.PacketMode + 1) % 2;
                    BOTPlugin.ConfigGibsonMode.Value = WeaponManager.PacketMode;
                    BOTPlugin.SaveConfigDebounced();
                }, () => WeaponManager.PacketMode == 0 ? "<color=cyan>[ Flung (打飞升天) ]</color>" : "<color=red>[ Kill (陨石下砸) ]</color>");

                var m_delay = new MenuButton("GibsonDelay", T("   Send Delay", "   发包冷却 (秒)", "   Задержка", "   Gecikme", "   Atraso de Envio"), true, (val) => {
                    WeaponManager.PacketSendDelay = val / 100f;
                    BOTPlugin.ConfigGibsonDelay.Value = WeaponManager.PacketSendDelay;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)(WeaponManager.PacketSendDelay * 100), () => 1, () => 100, 5);

                m_exec.IsChild = m_mode.IsChild = m_delay.IsChild = true;
                m_fold.Children.AddRange(new[] { m_exec, m_mode, m_delay });

                var swarmFold = CreateBtn("SwarmFold",
                    T("Swarm Legion", "傀儡军团系统", "Легион марионеток", "Kukla Ordusu", "Legião de Marionetes"),
                    null,
                    () => $"<color=green>[ {SwarmMasterManager.OnlineSlaves.Count} 傀儡在线 ]</color>"
                );

                var sw1 = CreateBtn("SwarmSelTgt", T("Target", "选择目标"), () => {
                    JoinGameManager.CyclePlayerInLobby();
                }, () => "[ " + JoinGameManager.GetSelectedPlayerName() + " ]");

                var sw2 = CreateBtn("SwarmAtk", T("Focus Fire", "命令全军集火"), () => {

                    if (SwarmMasterManager.IsAttackingActive)
                    {
                        SwarmMasterManager.CommandStop();
                    }
                    else
                    {
                        if (JoinGameManager.SelectedPlayerUid != 0)
                        {
                            SwarmMasterManager.CommandAttack(JoinGameManager.SelectedPlayerUid);
                        }
                        else
                        {
                            Utility.ForceMessage("<color=red>请先选择目标！</color>");
                        }
                    }
                }, () => SwarmMasterManager.IsAttackingActive ? "<color=red>[ 正在集火 (点击停止) ]</color>" : TOFF());

                var sw3 = CreateBtn("SwarmStop", T("Stop Attack", "命令停火挂机"), () => {
                    SwarmMasterManager.CommandStop();
                }, () => TExec());

                var sw4 = CreateBtn("SwarmFollow", T("Escort", "命令全军护驾"), () => {
                    SwarmMasterManager.CommandFollow();
                }, () => TExec());

                var sw5 = CreateBtn("SwarmSuicide", T("Mass Suicide", "命令傀儡自爆"), () => {
                    SwarmMasterManager.CommandSuicide();
                }, () => "<color=red>[ 自爆 ]</color>");

                sw1.IsChild = sw2.IsChild = sw3.IsChild = sw4.IsChild = sw5.IsChild = true;
                swarmFold.Children.AddRange(new[] { sw1, sw2, sw3, sw4, sw5 });

                cat.Buttons.Add(swarmFold);
                cat.Buttons.Add(m_fold);
                cats.Add(cat);
            }

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Visuals ]", "[ 视觉透视 ]", "[ Визуал ]", "[ Görseller ]", "[ Visuais ]"), Buttons = new List<MenuButton>() };

                var themeFold = CreateBtn("ThemeFold", T("Menu Theme & Background", "菜单主题与背景壁纸", "Темы меню", "Menü Teması", "Tema e Fundo do Menu"), null, TFolder);
                var t_galaxy = CreateBtn("GalaxyBg", T("   Galaxy Background", "   动态银河背景", "   Галактический фон", "   Galaksi Arka Planı", "   Fundo de Galáxia"), () => {
                    GalaxyBackground.enabled = !GalaxyBackground.enabled;
                    BOTPlugin.ConfigGalaxyBgEnabled.Value = GalaxyBackground.enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(GalaxyBackground.enabled));

                var t_mode = CreateBtn("ThemeMode", T("   Theme Style", "   切换强调色 (Breeze)", "   Стиль темы", "   Tema Stili", "   Estilo do Tema"), () => {
                    MenuThemeManager.ThemeIndex = (MenuThemeManager.ThemeIndex + 1) % MenuThemeManager.ThemeNames.Length;
                    BOTPlugin.ConfigThemeMode.Value = MenuThemeManager.ThemeIndex;
                    BOTPlugin.SaveConfigDebounced();
                    if (MenuManager.Instance != null) MenuManager.Instance.RefreshTheme();
                }, () => $"<color={MenuThemeManager.CurrentAccentHex}>[ {MenuThemeManager.ThemeNames[MenuThemeManager.ThemeIndex]} ]</color>");

                var t_alpha = new MenuButton("ThemeAlpha", T("  Background Opacity", "   背景透明度", " Прозрачность фона", " Arka plan saydamlığı", " Opacidade do Fundo"), true, (val) => {
                    MenuThemeManager.BgAlpha = val / 100f;
                    BOTPlugin.ConfigBgAlpha.Value = MenuThemeManager.BgAlpha;
                    BOTPlugin.SaveConfigDebounced();
                    if (MenuManager.Instance != null) MenuManager.Instance.RefreshTheme();
                }, () => (int)(MenuThemeManager.BgAlpha * 100), () => 10, () => 100, 5);

                t_galaxy.IsChild = t_mode.IsChild = t_alpha.IsChild = true;
                themeFold.Children.AddRange(new[] { t_galaxy, t_mode, t_alpha });
                cat.Buttons.Add(themeFold);

                var f1 = CreateBtn("EspFolder", T("ESP Options", "可视化 ESP", "Настройки ESP", "ESP Ayarları", "Opções de ESP"), null, TFolder);
                var e1 = CreateBtn("LegitESP", T("Box ESP", "方框透视 ", "ESP Боксы", "Kutu ESP", "ESP Caixa"), () => { LegitESPManager.enabled = !LegitESPManager.enabled; BOTPlugin.ConfigLegitESPEnabled.Value = LegitESPManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(LegitESPManager.enabled));
                var e2 = CreateBtn("EspColor", T("Box Color", "切换方框颜色", "Цвет боксов", "Kutu Rengi", "Cor da Caixa"), () => {
                    Color[] c = { Color.cyan, Color.green, Color.red, Color.yellow, Color.white, Color.magenta };
                    int currentIdx = Array.IndexOf(c, LegitESPManager.espColor);
                    if (currentIdx == -1) currentIdx = 0;
                    LegitESPManager.espColor = c[(currentIdx + 1) % c.Length];
                    BOTPlugin.ConfigLegitESPColor.Value = "#" + ColorUtility.ToHtmlStringRGB(LegitESPManager.espColor);
                    BOTPlugin.SaveConfigDebounced();
                }, () => "<color=#" + ColorUtility.ToHtmlStringRGB(LegitESPManager.espColor) + ">[ COLOR ]</color>");

                var e6 = CreateBtn("GlowESP", T("Chams (Glow)", "CSGO发光透视 ", "Свечение игроков", "Parlayan Oyuncular", "Chams (Brilho)"), () => { GlowESPManager.enabled = !GlowESPManager.enabled; BOTPlugin.ConfigGlowESP.Value = GlowESPManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(GlowESPManager.enabled));
                var e7 = CreateBtn("GlowMode", T("Glow Mode", "切换发光模式", "Режим свечения", "Parlaklık Modu", "Modo de Brilho"), () => {
                    int nextMode = ((int)GlowESPManager.currentMode + 1) % Enum.GetValues(typeof(GlowMode)).Length;
                    GlowESPManager.currentMode = (GlowMode)nextMode;
                    BOTPlugin.ConfigGlowMode.Value = nextMode;
                    BOTPlugin.SaveConfigDebounced();
                }, () => $"<color=yellow>[ {GlowESPManager.currentMode} ]</color>");

                e1.IsChild = e2.IsChild = e6.IsChild = e7.IsChild = true;
                f1.Children.AddRange(new[] { e1, e2, e6, e7 });

                var e3 = CreateBtn("ItemESP", T("Item ESP", "物品透视", "ESP предметов", "Eşya ESP", "ESP de Itens"), () => { ItemESPManager.enabled = !ItemESPManager.enabled; BOTPlugin.ConfigItemESPEnabled.Value = ItemESPManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(ItemESPManager.enabled));
                var e4 = CreateBtn("ButtonESP", T("Button ESP", "按钮透视", "ESP кнопок", "Buton ESP", "ESP de Botões"), () => { ButtonESPManager.enabled = !ButtonESPManager.enabled; BOTPlugin.ConfigEnableButtonESP.Value = ButtonESPManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(ButtonESPManager.enabled));
                var e5 = CreateBtn("BridgeESP", T("Bridge ESP", "玻璃透视", "ESP моста", "Köprü ESP", "ESP da Ponte"), () => { BridgeESPManager.enabled = !BridgeESPManager.enabled; BOTPlugin.ConfigEnableBridgeESP.Value = BridgeESPManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(BridgeESPManager.enabled));

                var f2 = CreateBtn("PVisuals", T("Player Visuals", "玩家特效", "Визуал игрока", "Oyuncu Efektleri", "Efeitos do Jogador"), null, TFolder);
                string[] colorHexes = { "#00FFFF", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#FF8800", "#8800FF", "#FF0088", "#00FF88", "#FFFFFF", "#000000" };

                var p1 = CreateBtn("Trail", T("Movement Trail", "移动拖尾", "След движения", "Hareket İzi", "Rastro de Movimento"), () => { VisualsManager.enableTrail = !VisualsManager.enableTrail; BOTPlugin.ConfigEnableTrail.Value = VisualsManager.enableTrail; BOTPlugin.SaveConfigDebounced(); NetworkVisualsManager.SendState(); }, () => TState(VisualsManager.enableTrail));
                var p1_c = CreateBtn("TrailCol", T("Trail Color", "切换拖尾颜色", "Цвет следа", "İz Rengi", "Cor do Rastro"), () => {
                    string currentHex = "#" + ColorUtility.ToHtmlStringRGB(VisualsManager.TrailColor);
                    int idx = Array.IndexOf(colorHexes, currentHex);
                    string nextHex = colorHexes[(idx + 1) % colorHexes.Length];
                    if (ColorUtility.TryParseHtmlString(nextHex, out Color c)) VisualsManager.TrailColor = c;
                    BOTPlugin.ConfigTrailColor.Value = nextHex; BOTPlugin.SaveConfigDebounced(); NetworkVisualsManager.SendState();
                }, () => $"<color=#{ColorUtility.ToHtmlStringRGB(VisualsManager.TrailColor)}>[ COLOR ]</color>");

                var p2 = CreateBtn("JumpRing", T("Jump Ring", "跳跃波纹", "Кольцо прыжка", "Zıplama Halkası", "Anel de Pulo"), () => { VisualsManager.enableJumpRing = !VisualsManager.enableJumpRing; BOTPlugin.ConfigEnableJumpRing.Value = VisualsManager.enableJumpRing; BOTPlugin.SaveConfigDebounced(); }, () => TState(VisualsManager.enableJumpRing));
                var p2_c = CreateBtn("RingCol", T("Ring Color", "切换波纹颜色", "Цвет кольца", "Halka Rengi", "Cor do Anel"), () => {
                    string currentHex = "#" + ColorUtility.ToHtmlStringRGB(VisualsManager.JumpRingColor);
                    int idx = Array.IndexOf(colorHexes, currentHex);
                    string nextHex = colorHexes[(idx + 1) % colorHexes.Length];
                    if (ColorUtility.TryParseHtmlString(nextHex, out Color c)) VisualsManager.JumpRingColor = c;
                    BOTPlugin.ConfigJumpRingColor.Value = nextHex; BOTPlugin.SaveConfigDebounced();
                }, () => $"<color=#{ColorUtility.ToHtmlStringRGB(VisualsManager.JumpRingColor)}>[ COLOR ]</color>");

                var walkRing = CreateBtn("WalkRing", T("Walking Ripple", "行走扩散圈", "Кольцо при ходьбе", "Yürüme Dalgalanması", "Onda ao Caminhar"), () => { VisualsManager.enableWalkRing = !VisualsManager.enableWalkRing; BOTPlugin.ConfigEnableWalkRing.Value = VisualsManager.enableWalkRing; BOTPlugin.SaveConfigDebounced(); }, () => TState(VisualsManager.enableWalkRing));
                var walkRingCol = CreateBtn("WalkRingCol", T("Walking Ripple Color", "行走扩散圈颜色", "Цвет кольца ходьбы", "Yürüme Dalga Rengi", "Cor da Onda ao Caminhar"), () => {
                    string currentHex = "#" + ColorUtility.ToHtmlStringRGB(VisualsManager.WalkRingColor);
                    int idx = Array.IndexOf(colorHexes, currentHex);
                    string nextHex = colorHexes[(idx + 1) % colorHexes.Length];
                    if (ColorUtility.TryParseHtmlString(nextHex, out Color c)) VisualsManager.WalkRingColor = c;
                    BOTPlugin.ConfigWalkRingColor.Value = nextHex; BOTPlugin.SaveConfigDebounced();
                }, () => $"<color=#{ColorUtility.ToHtmlStringRGB(VisualsManager.WalkRingColor)}>[ COLOR ]</color>");

                var p3 = CreateBtn("Aura", T("Aura", "光环", "Аура", "Hale", "Aura"), () => { AuraManager.enabled = !AuraManager.enabled; BOTPlugin.ConfigEnableAura.Value = AuraManager.enabled; BOTPlugin.SaveConfigDebounced(); NetworkVisualsManager.SendState(); }, () => TState(AuraManager.enabled));
                var p3_d = CreateBtn("AuraDrag", T("Aura Inertia", "动态跟随", "Инерция ауры", "Hale İvmesi", "Inércia da Aura"), () => { AuraManager.enableDrag = !AuraManager.enableDrag; BOTPlugin.ConfigAuraDrag.Value = AuraManager.enableDrag; BOTPlugin.SaveConfigDebounced(); }, () => TState(AuraManager.enableDrag));
                var p3_c = CreateBtn("AuraCol", T("Aura Color", "切换光环颜色", "Цвет ауры", "Hale Rengi", "Cor da Aura"), () => {
                    string currentHex = "#" + ColorUtility.ToHtmlStringRGB(AuraManager.AuraColor);
                    int idx = Array.IndexOf(colorHexes, currentHex);
                    string nextHex = colorHexes[(idx + 1) % colorHexes.Length];
                    if (ColorUtility.TryParseHtmlString(nextHex, out Color c)) AuraManager.AuraColor = c;
                    BOTPlugin.ConfigAuraColor.Value = nextHex; BOTPlugin.SaveConfigDebounced(); NetworkVisualsManager.SendState();
                }, () => $"<color=#{ColorUtility.ToHtmlStringRGB(AuraManager.AuraColor)}>[ COLOR ]</color>");
                var p4 = CreateBtn("ShareVis", T("Share Visuals", "共享视觉效果", "Поделиться визуалом", "Görselleri Paylaş", "Compartilhar Visuais"), () => { NetworkVisualsManager.ShareVisuals = !NetworkVisualsManager.ShareVisuals; BOTPlugin.ConfigShareVisuals.Value = NetworkVisualsManager.ShareVisuals; BOTPlugin.SaveConfigDebounced(); NetworkVisualsManager.SendState(); }, () => { if (!BOTPlugin.HasModNet) return "<color=red>[ Network Offline ]</color>"; return TState(NetworkVisualsManager.ShareVisuals); });
                var ge1 = CreateBtn("GiantEny", T("Giant Enemies", "本地敌人巨大化", "Огромные враги", "Dev Düşmanlar", "Inimigos Gigantes"), () => { EnemyScaleManager.enabled = !EnemyScaleManager.enabled; BOTPlugin.ConfigEnemyScaleEnabled.Value = EnemyScaleManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(EnemyScaleManager.enabled));
                var ge2 = new MenuButton("GiantSize", T("Size Multiplier", "放大倍率", "Размер", "Boyut", "Multiplicador de Tamanho"), true, (val) => { EnemyScaleManager.scaleMultiplier = val / 10f; BOTPlugin.ConfigEnemyScaleMult.Value = EnemyScaleManager.scaleMultiplier; BOTPlugin.SaveConfigDebounced(); }, () => (int)(EnemyScaleManager.scaleMultiplier * 10), () => 10, () => 100, 5);

                var sh1 = CreateBtn("ShinyBoost", T("Shiny Glow Boost", "炫彩发光增强", "Усиление блеска", "Parlaklık Artırma", "Aumentar Brilho Shiny"), () => {
                    ShinyIntensityManager.Enabled = !ShinyIntensityManager.Enabled;
                    BOTPlugin.ConfigShinyEnabled.Value = ShinyIntensityManager.Enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(ShinyIntensityManager.Enabled));

                var sh2 = new MenuButton("ShinyVal", T("Shiny Multiplier", "发光强度倍率", "Интенсивность", "Parlaklık Değeri", "Intensidade do Brilho"), true, (val) => {
                    ShinyIntensityManager.ShinyValue = val;
                    BOTPlugin.ConfigShinyValue.Value = val;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)ShinyIntensityManager.ShinyValue, () => 1, () => 200, 5);

                sh1.IsChild = sh2.IsChild = true;
                p1.IsChild = p1_c.IsChild = p2.IsChild = p2_c.IsChild = walkRing.IsChild = walkRingCol.IsChild = p3.IsChild = p3_d.IsChild = p3_c.IsChild = p4.IsChild = ge1.IsChild = ge2.IsChild = sh1.IsChild = sh2.IsChild = true;
                f2.Children.AddRange(new[] { p1, p1_c, p2, p2_c, walkRing, walkRingCol, p3, p3_d, p3_c, p4, ge1, ge2, sh1, sh2 });

                var f3 = CreateBtn("CamFov", T("Camera & FOV", "相机与视野", "Камера и FOV", "Kamera & FOV", "Câmera e FOV"), null, () => { string k = CameraManager.ZoomKey == KeyCode.None ? "NONE" : CameraManager.ZoomKey.ToString(); return CameraManager.EnableZoom ? "<color=green>[ ON ]</color> <color=yellow>(Hold " + k + ")</color>" : TOFF(); });
                var c1 = CreateBtn("ThirdP", T("3rd Person", "第三人称", "3-е лицо", "3. Şahıs", "3ª Pessoa"), () => { ThirdPersonManager.enabled = !ThirdPersonManager.enabled; BOTPlugin.ConfigThirdPerson.Value = ThirdPersonManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(ThirdPersonManager.enabled));
                var c2 = new MenuButton("CamDist", T("Cam Distance", "相机距离", "Дистанция камеры", "Kamera Mesafesi", "Distância da Câmera"), true, (v) => { ThirdPersonManager.cameraDistance = v / 10f; BOTPlugin.ConfigThirdPersonDist.Value = ThirdPersonManager.cameraDistance; BOTPlugin.SaveConfigDebounced(); }, () => (int)(ThirdPersonManager.cameraDistance * 10), () => 10, () => 150, 5);
                var c3 = CreateBtn("SpecPOV", T("Spec POV", "第一人称观战", "POV наблюдателя", "İzleyici Açısı", "POV do Espectador"), () => { SpectatorPOVManager.enabled = !SpectatorPOVManager.enabled; BOTPlugin.ConfigSpectatorPOV.Value = SpectatorPOVManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(SpectatorPOVManager.enabled));
                var c4 = CreateBtn("Zoom", T("Zoom Mode", "放大镜倍率", "Зум", "Yakınlaştırma", "Modo Zoom"), () => { CameraManager.EnableZoom = !CameraManager.EnableZoom; BOTPlugin.ConfigZoomEnabled.Value = CameraManager.EnableZoom; BOTPlugin.SaveConfigDebounced(); }, () => TState(CameraManager.EnableZoom));
                var c5 = new MenuButton("ZoomFov", T("Zoom FOV", "放大倍率", "FOV зума", "Yakınlaştırma FOV", "FOV do Zoom"), true, (v) => { CameraManager.ZoomFOV = v; BOTPlugin.ConfigZoomFOV.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)CameraManager.ZoomFOV, () => 1, () => 60, 1);
                var c6 = CreateBtn("OverFov", T("Override FOV", "强制FOV开关", "Свой FOV", "Özel FOV", "Sobrescrever FOV"), () => { CameraManager.OverrideFOV = !CameraManager.OverrideFOV; BOTPlugin.ConfigOverrideFOV.Value = CameraManager.OverrideFOV; BOTPlugin.SaveConfigDebounced(); }, () => TState(CameraManager.OverrideFOV));
                var c7 = new MenuButton("BaseFov", T("Base FOV", "基础FOV数值", "Базовый FOV", "Temel FOV", "FOV Base"), true, (v) => { CameraManager.BaseFOV = v; BOTPlugin.ConfigBaseFOV.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)CameraManager.BaseFOV, () => 60, () => 170, 1);
                var c8 = CreateBtn("FullB", T("Fullbright", "去除黑夜", "Полная яркость", "Tam Parlaklık", "Brilho Total"), () => { FullbrightManager.enabled = !FullbrightManager.enabled; BOTPlugin.ConfigFullbright.Value = FullbrightManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(FullbrightManager.enabled));
                var c9 = CreateBtn("Mirror", T("Mirror Cam", "后视镜", "Зеркало заднего вида", "Dikiz Aynası", "Câmera Traseira"), () => { MirrorCamManager.Toggle(); BOTPlugin.ConfigMirrorCam.Value = MirrorCamManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(MirrorCamManager.enabled));
                var c10 = CreateBtn("CineFX", T("Cinematic FX", "电影级画质光影", "Кино-эффекты", "Sinematik FX", "Efeitos Cinemáticos"), () => { CinematicGraphicsManager.isEnabled = !CinematicGraphicsManager.isEnabled; BOTPlugin.ConfigCinematicFX.Value = CinematicGraphicsManager.isEnabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(CinematicGraphicsManager.isEnabled));
                c1.IsChild = c2.IsChild = c3.IsChild = c4.IsChild = c5.IsChild = c6.IsChild = c7.IsChild = c8.IsChild = c9.IsChild = c10.IsChild = true;
                f3.Children.AddRange(new[] { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10 });

                var f4 = CreateBtn("UiHud", T("UI Settings", "界面元素管理", "Интерфейс и HUD", "Arayüz Ayarları", "Configurações da UI"), null, TFolder);
                var u1 = CreateBtn("StatIsle", T("Status Island", "顶部状态栏", "Остров статуса", "Durum Çubuğu", "Ilha de Status"), () => { BOTStatusIsland.IsVisible = !BOTStatusIsland.IsVisible; }, () => TState(BOTStatusIsland.IsVisible));
                var u2 = CreateBtn("ActList", T("Active List", "右侧功能列表", "Активный список", "Aktif Liste", "Lista de Ativos"), () => { BOTActiveList.IsVisible = !BOTActiveList.IsVisible; }, () => TState(BOTActiveList.IsVisible));
                var u3 = CreateBtn("ListCol", T("Color Mode", "列表颜色模式", "Цветовой режим", "Renk Modu", "Modo de Cor"), () => {
                    BOTActiveList.ColorMode = (BOTActiveList.ColorMode + 1) % 4;
                    BOTPlugin.ConfigActiveListColorMode.Value = BOTActiveList.ColorMode;
                    BOTPlugin.SaveConfigDebounced();
                }, () => {
                    if (BOTActiveList.ColorMode == 0) return $"<color={MenuThemeManager.CurrentAccentHex}>[ Theme Wave 主题色滚动 ]</color>";
                    else if (BOTActiveList.ColorMode == 1) return $"<color={MenuThemeManager.CurrentAccentHex}>[ Theme Static 主题静态色 ]</color>";
                    else if (BOTActiveList.ColorMode == 2) return "<color=#FF00FF>[ RGB Wave 彩虹滚动 ]</color>";
                    else return "<color=#00FFFF>[ Text Rainbow 逐字彩虹 ]</color>";
                });

                var u3_bg = CreateBtn("ActBg", T("List Background", "右侧列表黑框底色", "Фон списка", "Liste Arka Planı", "Fundo da Lista"), () => {
                    BOTActiveList.ShowBackground = !BOTActiveList.ShowBackground;
                    BOTPlugin.ConfigActiveListBg.Value = BOTActiveList.ShowBackground;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(BOTActiveList.ShowBackground));

                var u4 = CreateBtn("KeyShow", T("Keystrokes", "按键显示", "按键回显", "Tuş Gösterimi", "Teclas na Tela"), () => { KeyDisplayManager.enabled = !KeyDisplayManager.enabled; BOTPlugin.ConfigKeyDisplayEnabled.Value = KeyDisplayManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => KeyDisplayManager.enabled ? "<color=green>[ ON ]</color>" : "<color=red>[ OFF ]</color>");
                var u5 = new MenuButton("KeyX", T("Keys X", "按键显示 X位置", "Позиция X клавиш", "Tuş X", "Posição X Teclas"), true, (val) => { KeyDisplayManager.posX = val; BOTPlugin.ConfigKeyDisplayX.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)KeyDisplayManager.posX, () => 0, () => Screen.width, 10);
                var u6 = new MenuButton("KeyY", T("Keys Y", "按键显示 Y位置", "Позиция Y клавиш", "Tuş Y", "Posição Y Teclas"), true, (val) => { KeyDisplayManager.posY = val; BOTPlugin.ConfigKeyDisplayY.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)KeyDisplayManager.posY, () => 0, () => Screen.height, 10);
                var u7 = new MenuButton("NotifX", T("Notif X", "提示框 X位置", "Позиция X увед.", "Bildirim X", "Posição X Notif"), true, (val) => { BOTNotification.PosX = val; BOTPlugin.ConfigNotifX.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)BOTNotification.PosX, () => 0, () => Screen.width, 10);
                var u8 = new MenuButton("NotifY", T("Notif Y", "提示框 Y位置", "Позиция Y увед.", "Bildirim Y", "Posição Y Notif"), true, (val) => { BOTNotification.PosY = val; BOTPlugin.ConfigNotifY.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)BOTNotification.PosY, () => 0, () => Screen.height, 10);
                var u9 = new MenuButton("NotifSpd", T("Notif Speed", "提示 动画速度", "Скорость увед.", "Bildirim Hızı", "Velocidade Notif"), true, (val) => { BOTNotification.AnimSpeed = val; BOTPlugin.ConfigNotifSpeed.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)BOTNotification.AnimSpeed, () => 1, () => 30, 1);
                var u10 = new MenuButton("NotifDur", T("Notif Duration", "提示 停留时长", "Время увед.", "Bildirim Süresi", "Duração Notif"), true, (val) => { BOTNotification.Duration = val / 10f; BOTPlugin.ConfigNotifDuration.Value = BOTNotification.Duration; BOTPlugin.SaveConfigDebounced(); }, () => (int)(BOTNotification.Duration * 10), () => 5, () => 100, 5);
                var u11 = CreateBtn("DmgFeed", T("Damage Feed", "受击伤害反馈列表", "Лента урона", "Hasar Bildirimi", "Feed de Dano"), () => { DamageNotificationManager.enabled = !DamageNotificationManager.enabled; BOTPlugin.ConfigDamageUI.Value = DamageNotificationManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(DamageNotificationManager.enabled));
                var u12 = CreateBtn("HitHeart", T("Hit Heart FX", "击中爆出粉色爱心", "Эффект сердечек", "Vuruş Kalbi", "Efeito Coração no Acerto"), () => {
                    HitEffectManager.enabled = !HitEffectManager.enabled;
                    BOTPlugin.ConfigHitHeartFX.Value = HitEffectManager.enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(HitEffectManager.enabled));
                var u13 = CreateBtn("ClkRip", T("Click Ripple", "鼠标点击波纹", "Рябь от клика", "Tıklama Efekti", "Onda de Clique"), () => { ClickRippleManager.IsActive = !ClickRippleManager.IsActive; BOTPlugin.ConfigClickRipple.Value = ClickRippleManager.IsActive; BOTPlugin.SaveConfigDebounced(); }, () => TState(ClickRippleManager.IsActive));

                var cr = CreateBtn("CrossF", T("Crosshair", "自定义准星", "Свой прицел", "Özel Nişangah", "Mira Personalizada"), () => { CrosshairManager.enableDot = !CrosshairManager.enableDot; BOTPlugin.ConfigCrosshairDot.Value = CrosshairManager.enableDot; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => TState(CrosshairManager.enableDot));
                var cr1 = new MenuButton("CrLen", T("Length", "线段长度", "Длина", "Uzunluk", "Comprimento"), true, (v) => { CrosshairManager.length = v; BOTPlugin.ConfigCrosshairLength.Value = v; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => CrosshairManager.length, () => 0, () => 100, 1);
                var cr2 = new MenuButton("CrWid", T("Width", "线段宽度", "Ширина", "Genişlik", "Largura"), true, (v) => { CrosshairManager.width = v; BOTPlugin.ConfigCrosshairWidth.Value = v; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => CrosshairManager.width, () => 0, () => 50, 1);
                var cr3 = new MenuButton("CrGap", T("Gap", "中心间距", "Зазор", "Boşluk", "Espaçamento"), true, (v) => { CrosshairManager.gapSize = v; BOTPlugin.ConfigCrosshairGap.Value = v; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => CrosshairManager.gapSize, () => 0, () => 100, 1);
                var cr4 = new MenuButton("CrDot", T("Dot Size", "中心点大小", "Размер точки", "Nokta Boyutu", "Tamanho do Ponto"), true, (v) => { CrosshairManager.dotSize = v; BOTPlugin.ConfigCrosshairDotSize.Value = v; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => CrosshairManager.dotSize, () => 0, () => 30, 1);
                var cr5 = CreateBtn("CrCol", T("Color", "切换准星颜色", "Цвет", "Renk", "Cor"), () => { string[] hex = { "#FFFFFF", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#FFFF00" }; CrosshairManager.colorHtml = hex[(Array.IndexOf(hex, CrosshairManager.colorHtml) + 1) % hex.Length]; BOTPlugin.ConfigCrosshairColor.Value = CrosshairManager.colorHtml; BOTPlugin.SaveConfigDebounced(); CrosshairManager.ApplySettings(); }, () => "<color=" + CrosshairManager.colorHtml + ">[ COLOR ]</color>");

                var time12 = CreateBtn("Time12", T("12-Hour Time", "12小时制时间", "12-часовой формат", "12 Saat Formatı", "Formato 12 Horas"), () => {
                    BOTPlugin.Config12HourTime.Value = !BOTPlugin.Config12HourTime.Value;
                    BOTPlugin.SaveConfigDebounced();
                    if (BOTStatusIsland.Instance != null) BOTStatusIsland.Instance.UpdateContent();
                }, () => TState(BOTPlugin.Config12HourTime.Value));

                cr1.IsChild = cr2.IsChild = cr3.IsChild = cr4.IsChild = cr5.IsChild = true; cr.Children.AddRange(new[] { cr1, cr2, cr3, cr4, cr5 });
                u1.IsChild = u2.IsChild = u3.IsChild = u3_bg.IsChild = u4.IsChild = u5.IsChild = u6.IsChild = u7.IsChild = u8.IsChild = u9.IsChild = u10.IsChild = u11.IsChild = u12.IsChild = u13.IsChild = cr.IsChild = time12.IsChild = true;
                f4.Children.AddRange(new[] { u1, u2, u3, u3_bg, u4, u5, u6, u7, u8, u9, u10, u11, u12, u13, cr, time12 });

                var f5 = CreateBtn("PingFold", T("Fake Ping", "伪造延迟", "Фейковый пинг", "Sahte Ping", "Ping Falso"), () => { ShadowModePatches.enableFakePing = !ShadowModePatches.enableFakePing; BOTPlugin.ConfigFakePing.Value = ShadowModePatches.enableFakePing; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enableFakePing));
                var ping1 = CreateBtn("Jitter", T("Jitter", "动态波动", "Колебание", "Dalgalanma", "Oscilação"), () => { ShadowModePatches.enablePingJitter = !ShadowModePatches.enablePingJitter; BOTPlugin.ConfigPingJitter.Value = ShadowModePatches.enablePingJitter; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enablePingJitter));
                var ping2 = new MenuButton("PingVal", T("Value", "延迟数值", "Значение", "Değer", "Valor do Ping"), true, (v) => { if (ShadowModePatches.enablePingJitter) { ShadowModePatches.pingMin = v; BOTPlugin.ConfigPingMin.Value = v; } else { ShadowModePatches.fakePingValue = v; BOTPlugin.ConfigPingValue.Value = v; } BOTPlugin.SaveConfigDebounced(); }, () => ShadowModePatches.enablePingJitter ? ShadowModePatches.pingMin : ShadowModePatches.fakePingValue, () => -500, () => 1000, 1);
                var ping3 = new MenuButton("PingInt", T("Interval (ms)", "波动间隔", "Интервал (мс)", "Aralık (ms)", "Intervalo (ms)"), true, (v) => { ShadowModePatches.pingInterval = v; BOTPlugin.ConfigPingInterval.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => ShadowModePatches.pingInterval, () => 10, () => 5000, 50);
                ping1.IsChild = ping2.IsChild = ping3.IsChild = true; f5.Children.AddRange(new[] { ping1, ping2, ping3 });

                var f6 = CreateBtn("SkinFold", T("Skin Mods", "皮肤管理", "Контроль скинов", "Kostüm Değişimi", "Gerenciador de Skins"), null, () => "<color=yellow>[ " + SkinManager.SavedBlacklist.Count + " ID ]</color>");
                var sk0 = CreateBtn("WpnSkin", T("Skin Changer", "武器皮肤", "Скин оружия", "Silah Kostümü", "Modificador de Skins"), () => { ItemSkinChangerManager.itemSkinChanger = !ItemSkinChangerManager.itemSkinChanger; BOTPlugin.ConfigSkinChangerEnabled.Value = ItemSkinChangerManager.itemSkinChanger; BOTPlugin.SaveConfigDebounced(); }, () => TState(ItemSkinChangerManager.itemSkinChanger));
                var sk0_a = new MenuButton("SkinID", T("Skin ID", "皮肤 ID", "ID скина", "Kostüm ID", "ID da Skin"), true, (val) => { ItemSkinChangerManager.itemSkinId = val; BOTPlugin.ConfigSkinTargetId.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => ItemSkinChangerManager.itemSkinId, () => 0, () => 14, 1);
                var sk1 = CreateBtn("SelPlayer", T("Select Player", "选择大厅玩家", "Выбрать игрока", "Oyuncu Seç", "Selecionar Jogador"), () => { JoinGameManager.CyclePlayerInLobby(); }, () => "[ " + JoinGameManager.GetSelectedPlayerName() + " ]");
                var sk2 = CreateBtn("ForceBlk", T("Toggle Auto", "强制变黑", "Сделать черным", "Siyah Yap", "Alternar Automático"), () => { SkinManager.ToggleAuto(!SkinManager.AutoApply); }, () => TState(SkinManager.AutoApply));
                var sk3 = CreateBtn("SaveBlk", T("Save Blacklist", "存入永久黑名单", "Сохранить в ЧС", "Kara Listeye Al", "Salvar na Lista Negra"), () => { SkinManager.AddCurrentToSavedList(); }, () => "[ SAVE ]");
                var sk5 = CreateBtn("CusAva", T("Custom Avatars", "自定义模型菜单", "Кастомные аватары", "Özel Modeller", "Avatares Personalizados"), () => { BOTMenuMod.AntroPort.SkinsMenu.IsWindowOpen = !BOTMenuMod.AntroPort.SkinsMenu.IsWindowOpen; }, () => TState(BOTMenuMod.AntroPort.SkinsMenu.IsWindowOpen));
                var sk3_del = CreateBtn("DelBlk", T("Remove from Blacklist", "从永久名单删除", "Удалить из ЧС", "Listeden Çıkar", "Remover da Lista Negra"), () => { SkinManager.RemoveCurrentFromSavedList(); }, () => "<color=red>[ DEL ]</color>");
                var sk4 = CreateBtn("RstSkin", T("Reset Target", "清除目标所有改动", "Сбросить всё", "Hedefi Sıfırla", "Redefinir Alvo"), () => { SkinManager.ResetSelectedPlayer(); }, () => "[ RESET TGT ]");
                var sk_me = CreateBtn("RstSelf", T("Reset Myself", "恢复我自己的皮肤", "Сбросить себя", "Kendimi Sıfırla", "Restaurar Minha Skin"), () => {
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    if (myId != 0) SkinManager.ResetSpecificPlayer(myId);
                    Utility.ForceMessage("<color=green>Skin restored successfully!</color>");
                }, () => "<color=green>[ RESET ME ]</color>");

                var myButtons = new List<MenuButton> { sk0, sk0_a, sk1, sk2, sk3, sk3_del, sk4, sk_me, sk5 };
                foreach (var btn in myButtons) btn.IsChild = true;
                f6.Children.AddRange(myButtons);

                var chatFold = CreateBtn("ChatFold", T("TAB Name Color", "TAB彩色名字", "Цвет в TAB", "TAB Renkleri", "Cor do Nome no TAB"), null, TFolder);
                var ct4 = CreateBtn("TabColOn", T("Enable TAB Color", "开启名字变色", "Включить цвет", "Rengi Aç", "Ativar Cor no TAB"), () => {
                    BotTabManager.enableTabColor = !BotTabManager.enableTabColor;
                    BOTPlugin.ConfigTabColorEnabled.Value = BotTabManager.enableTabColor;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(BotTabManager.enableTabColor));

                var ct5 = CreateBtn("TabCol", T("Select Color", "切换名字颜色", "Выбрать цвет", "Renk Seç", "Selecionar Cor"), () => {
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    List<string> hexList = new List<string> { "#FFFF00", "#00FFFF", "#FF00FF", "#00FF00", "#FF0000", "#FFFFFF" };

                    int nextIdx = (hexList.IndexOf(BotTabManager.tabColor) + 1) % hexList.Count;
                    BotTabManager.tabColor = hexList[nextIdx];
                    BOTPlugin.ConfigTabColorHex.Value = BotTabManager.tabColor;
                    BOTPlugin.SaveConfigDebounced();
                }, () => $"<color={BotTabManager.tabColor}>[ COLOR ]</color>");

                ct4.IsChild = ct5.IsChild = true;
                chatFold.Children.AddRange(new[] { ct4, ct5 });
                cat.Buttons.Add(chatFold);

                if (PlayerPrefs.HasKey("BOT_MarkerColor")) MarkerManager.myMarkerColorIndex = PlayerPrefs.GetInt("BOT_MarkerColor", 1);
                var mkrFold = CreateBtn("NetMarkerFold", T("Net Marker", "头顶标记", "Маркеры над головой", "İsim Etiketi", "Marcador de Rede"), null, TFolder);
                var mkr1 = CreateBtn("NetMkrSts", T("Marker Toggle", "头顶标记开关", "Вкл/Выкл маркер", "Etiketi Aç/Kapat", "Alternar Marcador"), () => {
                    MarkerManager.Toggle(!MarkerManager.enabled);
                    BOTPlugin.ConfigShowMarker.Value = MarkerManager.enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(MarkerManager.enabled));

                var mkr2 = CreateBtn("NetMkrCol", T("Change Color", "切换球颜色", "Сменить цвет", "Renk Değiştir", "Alterar Cor"), () => {
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    while (true)
                    {
                        MarkerManager.myMarkerColorIndex = (MarkerManager.myMarkerColorIndex % 9) + 1;
                        break;
                    }
                    PlayerPrefs.SetInt("BOT_MarkerColor", MarkerManager.myMarkerColorIndex);
                    PlayerPrefs.Save();
                    MarkerManager.Toggle(MarkerManager.enabled);
                }, () => "[ " + MarkerManager.GetColorName(MarkerManager.myMarkerColorIndex) + " ]");

                mkr1.IsChild = mkr2.IsChild = true;
                mkrFold.Children.AddRange(new[] { mkr1, mkr2 });

                cat.Buttons.Add(CreateBtn("LipSync", T("Lip Sync & Head Swing", "唇形同步 & 头部摆动", "Синх. губ и поворот", "Dudak Senkronu & Kafa Sallama", "Sincronia Labial e Balanço da Cabeça"), () => {
                    LipSyncPatch.Enabled = !LipSyncPatch.Enabled;
                    BOTPlugin.ConfigLipSyncEnabled.Value = LipSyncPatch.Enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(LipSyncPatch.Enabled)));

                cat.Buttons.Add(f1); cat.Buttons.Add(e3); cat.Buttons.Add(e4); cat.Buttons.Add(e5);
                cat.Buttons.Add(f2); cat.Buttons.Add(f3); cat.Buttons.Add(f4); cat.Buttons.Add(f5);
                cat.Buttons.Add(f6); cat.Buttons.Add(mkrFold);
                cats.Add(cat);
            }

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Movement ]", "[ 移动身法 ]", "[ Движение ]", "[ Hareket ]", "[ Movimento ]"), Buttons = new List<MenuButton>() };

                var f1 = CreateBtn("TPLag", T("Blink Backtracking", "瞬移/回潮", "Мигание с возвратом", "Göz Kırpma Geri İzleme", "Blink & Backtracking"), null, TFolder);
                var c1 = CreateBtn("ClkTP", T("Click TP", "鼠标中键瞬移", "Клик ТП", "Tıkla Işınlan", "Clique Teleporte"), () => { TeleportManager.enabled = !TeleportManager.enabled; BOTPlugin.ConfigClickTPMain.Value = TeleportManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(TeleportManager.enabled));
                var c2 = CreateBtn("WallTP", T("Through Walls", "穿墙模式", "Сквозь стены", "Duvardan Geç", "Através de Paredes"), () => { TeleportManager.ignoreWalls = !TeleportManager.ignoreWalls; BOTPlugin.ConfigTeleportThroughWalls.Value = TeleportManager.ignoreWalls; BOTPlugin.SaveConfigDebounced(); }, () => TState(TeleportManager.ignoreWalls));
                var c3 = CreateBtn("Blink", T("Blink", "瞬移", "Моргать", "göz kırpma", "Blink"), () => { FreecamManager.Toggle(); BOTPlugin.ConfigFreecamEnabled.Value = FreecamManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => FreecamManager.enabled ? "<color=green>[ ACTIVE ]</color>" : "<color=red>[ NORMAL ]</color>");
                var c4 = CreateBtn("FakeLag", T("Backtracking", "回潮开关", "Фейк lag", "Kırp", "Backtracking"), () => { BacktrackingManager.Toggle(); BOTPlugin.ConfigBacktrackingEnabled.Value = BacktrackingManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(BacktrackingManager.enabled));
                var c5 = new MenuButton("LagInt", T("Interval", "回潮间隔", "Интервал", "Aralık", "Intervalo"), true, (v) => { BacktrackingManager.interval = v / 10f; BOTPlugin.ConfigBacktrackInterval.Value = BacktrackingManager.interval; BOTPlugin.SaveConfigDebounced(); }, () => (int)(BacktrackingManager.interval * 10), () => 1, () => 15, 1);
                c1.IsChild = c2.IsChild = c3.IsChild = c4.IsChild = c5.IsChild = true;
                f1.Children.AddRange(new[] { c1, c2, c3, c4, c5 });

                var f2 = CreateBtn("NoClipFold", T("Player NoClip", "穿人模式", "Прохождение сквозь", "Duvardan Geçme", "NoClip Jogador"), () => { PlayerNoClipManager.enabled = !PlayerNoClipManager.enabled; BOTPlugin.ConfigPlayerNoClip.Value = PlayerNoClipManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(PlayerNoClipManager.enabled));
                var n2 = CreateBtn("WallNoClip", T("Wall NoClip", "穿墙飞行", "Сквозь стены (Полет)", "Duvardan Uç", "NoClip Paredes"), () => { PlayerNoClipManager.wallsEnabled = !PlayerNoClipManager.wallsEnabled; BOTPlugin.ConfigWallNoClipEnabled.Value = PlayerNoClipManager.wallsEnabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(PlayerNoClipManager.wallsEnabled));
                n2.IsChild = true; f2.Children.Add(n2);

                var f3 = CreateBtn("WaterFold", T("Water Control", "水面控制系统", "Контроль воды", "Su Kontrolü", "Controle de Água"), null, TFolder);
                var w1 = CreateBtn("WtrJmp", T("Water Jump", "水跳", "Прыжок по воде", "Suda Zıpla", "Pulo na Água"), () => { WaterJumpManager.enabled = !WaterJumpManager.enabled; BOTPlugin.ConfigWaterJumpEnabled.Value = WaterJumpManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(WaterJumpManager.enabled));
                var w1_a = new MenuButton("WtrLvl", T("Level", "固定水位", "Уровень", "Su Seviyesi", "Nível da Água"), true, (v) => { WaterJumpManager.waterLevel = v; BOTPlugin.ConfigWaterJumpLevel.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)WaterJumpManager.waterLevel, () => -100, () => 100, 1);
                var w1_b = new MenuButton("WtrBnc", T("Bounce", "弹跳力度", "Сила отскока", "Sıçrama Gücü", "Força de Pulo"), true, (v) => { WaterJumpManager.bouncePower = v; BOTPlugin.ConfigWaterJumpBouncePower.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)WaterJumpManager.bouncePower, () => 10, () => 100, 5);
                var w2 = CreateBtn("WtrWlk", T("Water Walk", "水上行走", "Хождение по воде", "Suda Yürü", "Andar na Água"), () => { WaterWalkManager.enabled = !WaterWalkManager.enabled; if (!WaterWalkManager.enabled) WaterWalkManager.Cleanup(); BOTPlugin.ConfigWaterWalkEnabled.Value = WaterWalkManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(WaterWalkManager.enabled));
                var w2_a = new MenuButton("WtrOff", T("Offset", "地板偏移", "Смещение", "Yükseklik", "Deslocamento"), true, (v) => { WaterWalkManager.floorOffset = v / 100f; BOTPlugin.ConfigWaterWalkOffset.Value = WaterWalkManager.floorOffset; BOTPlugin.SaveConfigDebounced(); }, () => (int)(WaterWalkManager.floorOffset * 100), () => -50, () => 100, 1);
                w1_a.IsChild = w1_b.IsChild = true; w1.Children.AddRange(new[] { w1_a, w1_b });
                w2_a.IsChild = true; w2.Children.Add(w2_a); w1.IsChild = w2.IsChild = true;
                f3.Children.AddRange(new[] { w1, w2 });

                var f4 = CreateBtn("CrazyMov", T("Pose", "身体姿态控制", "Безумное движение", "Çılgın Hareketler", "Controle de Postura"), () => { BodyRotationManager.IsActive = !BodyRotationManager.IsActive; BOTPlugin.ConfigPoseEnabled.Value = BodyRotationManager.IsActive; BOTPlugin.SaveConfigDebounced(); }, () => TState(BodyRotationManager.IsActive));
                var sp = CreateBtn("SpinB", T("Spinbot", "疯狂自转", "Спинбот", "Mevlana", "Spinbot"), () => { ShadowModePatches.enableSpinbot = !ShadowModePatches.enableSpinbot; BOTPlugin.ConfigSpinbot.Value = ShadowModePatches.enableSpinbot; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enableSpinbot));
                var sp1 = CreateBtn("SpinMod", T("Spin Mode", "模式切换", "Режим вращения", "Dönüş Modu", "Modo de Giro"), () => { ShadowModePatches.spinMode = (ShadowModePatches.spinMode + 1) % 3; BOTPlugin.ConfigSpinMode.Value = ShadowModePatches.spinMode; BOTPlugin.SaveConfigDebounced(); }, () => "[ " + ShadowModePatches.spinMode + " ]");
                var sp2 = new MenuButton("SpinSpd", T("Spin Speed", "旋转转速", "Скорость вращения", "Dönüş Hızı", "Velocidade de Giro"), true, (v) => { ShadowModePatches.spinSpeed = v; BOTPlugin.ConfigSpinSpeed.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)ShadowModePatches.spinSpeed, () => 100, () => 5000, 100);
                var orb = CreateBtn("Orbit", T("Orbit Walk", "绕人转圈", "Орбитальный шаг", "Etrafında Dön", "Andar em Órbita"), () => { OrbitManager.Toggle(!OrbitManager.enabled); BOTPlugin.ConfigOrbitEnabled.Value = OrbitManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(OrbitManager.enabled));
                var orb1 = CreateBtn("OrbTgt", T("Target", "切换公转目标", "Цель", "Hedef", "Alvo"), () => { OrbitManager.autoCycle = false; OrbitManager.CycleToNextLivingTarget(); }, () => "[ " + OrbitManager.GetTargetName() + " ]");
                var orb2 = CreateBtn("OrbAuto", T("Auto Cycle", "自动切换循环", "Автосмена", "Oto Hedef", "Troca Automática"), () => { OrbitManager.autoCycle = !OrbitManager.autoCycle; BOTPlugin.ConfigOrbitAuto.Value = OrbitManager.autoCycle; BOTPlugin.SaveConfigDebounced(); }, () => TState(OrbitManager.autoCycle));
                var orb3 = new MenuButton("OrbInt", T("Interval", "切换间隔", "Интервал", "Aralık", "Intervalo"), true, (v) => {
                    OrbitManager.cycleInterval = v / 100f;
                    BOTPlugin.ConfigOrbitInterval.Value = OrbitManager.cycleInterval;
                    BOTPlugin.SaveConfigDebounced();
                }, () => (int)(OrbitManager.cycleInterval * 100), () => 1, () => 2000, 5);
                var orb4 = new MenuButton("OrbRad", T("Radius", "公转半径", "Радиус", "Yarıçap", "Raio"), true, (v) => { OrbitManager.radius = v; BOTPlugin.ConfigOrbitRadius.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)OrbitManager.radius, () => 0, () => 30, 1);
                var orb5 = new MenuButton("OrbSpd", T("Speed", "公转速度", "Скорость", "Hız", "Velocidade"), true, (v) => { OrbitManager.speed = v; BOTPlugin.ConfigOrbitSpeed.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)OrbitManager.speed, () => 1, () => 100, 1);
                var orb6 = new MenuButton("OrbHgt", T("Height", "垂直高度", "Высота", "Yükseklik", "Altura"), true, (v) => { OrbitManager.height = v / 10f; BOTPlugin.ConfigOrbitHeight.Value = OrbitManager.height; BOTPlugin.SaveConfigDebounced(); }, () => (int)(OrbitManager.height * 10), () => -50, () => 150, 1);
                var fly = CreateBtn("Launcher", T("Player Launcher", "玩家发射器", "Запускатель игроков", "Fırlatıcı", "Lançador de Jogadores"), () => { if (!PlayerLauncherManager.enabled) PlayerLauncherManager.enabled = true; else PlayerLauncherManager.Disable(); BOTPlugin.ConfigPlayerLauncher.Value = PlayerLauncherManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => PlayerLauncherManager.enabled ? "<color=green>[ ACTIVE ]</color>" : "<color=red>[ OFF ]</color>");
                var fs = CreateBtn("Scale", T("Funny Scale", "鬼畜变形", "Смешной масштаб", "Boyut Değiştir", "Escala Engraçada"), () => { FunnyScaleModule.Instance.Toggle(); BOTPlugin.ConfigFunnyScaleEnabled.Value = FunnyScaleModule._isLocalActive; BOTPlugin.SaveConfigDebounced(); }, () => TState(FunnyScaleModule._isLocalActive));
                var fs1 = new MenuButton("SclSize", T("Scale", "大小", "Масштаб", "Boyut", "Escala"), true, (val) => { FunnyScaleModule.LocalBaseScale = val / 100f; BOTPlugin.ConfigFunnyBaseScale.Value = FunnyScaleModule.LocalBaseScale; BOTPlugin.SaveConfigDebounced(); FunnyScaleModule.Instance?.SyncParameters(); }, () => (int)(FunnyScaleModule.LocalBaseScale * 100), () => 10, () => 1000, 5);
                var fs2 = new MenuButton("SclInt", T("Intensity", "强度", "Интенсивность", "Titreme", "Intensidade"), true, (val) => { FunnyScaleModule.LocalIntensity = val / 100f; BOTPlugin.ConfigFunnyIntensity.Value = FunnyScaleModule.LocalIntensity; BOTPlugin.SaveConfigDebounced(); }, () => (int)(FunnyScaleModule.LocalIntensity * 100), () => 0, () => 100, 5);
                var fs3 = new MenuButton("SclSpd", T("Speed", "速度", "Скорость", "Hız", "Velocidade"), true, (val) => { FunnyScaleModule.LocalSpeed = val; BOTPlugin.ConfigFunnySpeed.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => (int)FunnyScaleModule.LocalSpeed, () => 1, () => 100, 5);
                var inv = CreateBtn("Demon", T("Demon Mode", "反转模式 (静止)", "Режим демона", "Şeytan Modu", "Modo Demônio"), () => { BodyInverseManager.IsActive = !BodyInverseManager.IsActive; BOTPlugin.ConfigDemonModeEnabled.Value = BodyInverseManager.IsActive; BOTPlugin.SaveConfigDebounced(); }, () => TState(BodyInverseManager.IsActive));
                var pt = new MenuButton("Pitch", T("Center Pitch", "停留/中心角度 (Pitch)", "Наклон", "Eğilme", "Inclinação Central (Pitch)"), true, (v) => { BodyInverseManager.targetPitch = v; BOTPlugin.ConfigDemonPitch.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)BodyInverseManager.targetPitch, () => 0, () => 360, 10);
                var invDyn = CreateBtn("DemonDyn", T("Dynamic Demon", "动态反转摇摆 (来回)", "Динам. демон", "Dinamik Şeytan", "Demônio Dinâmico"), () => { BodyInverseManager.IsDynamic = !BodyInverseManager.IsDynamic; BOTPlugin.ConfigDemonDynamicEnabled.Value = BodyInverseManager.IsDynamic; BOTPlugin.SaveConfigDebounced(); }, () => TState(BodyInverseManager.IsDynamic));
                var inv360 = CreateBtn("Demon360", T("360 Continuous Flip", "360度无限连续空翻", "360 Вращение", "360 Dönüş", "Giro Contínuo 360"), () => { BodyInverseManager.Is360Spin = !BodyInverseManager.Is360Spin; BOTPlugin.ConfigDemon360Enabled.Value = BodyInverseManager.Is360Spin; BOTPlugin.SaveConfigDebounced(); }, () => TState(BodyInverseManager.Is360Spin));
                var invSpd = new MenuButton("DemonSpd", T("Speed", "摇摆/旋转速度", "Скорость", "Hız", "Velocidade"), true, (v) => { BodyInverseManager.DynamicSpeed = v; BOTPlugin.ConfigDemonDynamicSpeed.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)BodyInverseManager.DynamicSpeed, () => 1, () => 50, 1);
                var invRng = new MenuButton("DemonRng", T("Swing Range", "摇摆幅度 (最高360°)", "Диапазон", "Aralık", "Alcance de Balanço"), true, (v) => { BodyInverseManager.DynamicRange = v; BOTPlugin.ConfigDemonDynamicRange.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)BodyInverseManager.DynamicRange, () => 5, () => 360, 10);
                var yw = new MenuButton("Yaw", T("Yaw", "固定角度 (Yaw)", "Поворот", "Dönüş", "Rotação Fixa (Yaw)"), true, (v) => { BodyRotationManager.FixedAngle = v; BOTPlugin.ConfigPoseYaw.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => (int)BodyRotationManager.FixedAngle, () => 0, () => 360, 45);
                var sm = CreateBtn("SmthMov", T("Smooth Movement", "高频平滑移动 (防卡顿)", "Плавное движение", "Akıcı Hareket", "Movimento Suave"), () => { SmoothMovementManager.enabled = !SmoothMovementManager.enabled; BOTPlugin.ConfigSmoothMovement.Value = SmoothMovementManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(SmoothMovementManager.enabled));

                sp.IsChild = sp1.IsChild = sp2.IsChild = orb.IsChild = orb1.IsChild = orb2.IsChild = orb3.IsChild = orb4.IsChild = orb5.IsChild = orb6.IsChild = fly.IsChild = fs.IsChild = fs1.IsChild = fs2.IsChild = fs3.IsChild = inv.IsChild = pt.IsChild = invDyn.IsChild = inv360.IsChild = invSpd.IsChild = invRng.IsChild = yw.IsChild = sm.IsChild = true;
                f4.Children.AddRange(new[] { sp, sp1, sp2, orb, orb1, orb2, orb3, orb4, orb5, orb6, fly, fs, fs1, fs2, fs3, inv, pt, invDyn, inv360, invSpd, invRng, yw, sm });

                cat.Buttons.Add(f1); cat.Buttons.Add(f2); cat.Buttons.Add(f3); cat.Buttons.Add(f4);
                cat.Buttons.Add(CreateBtn("FastTP", T("Map Fast finish TP", "地图快速完成传送", "ТП Победа", "Harita Bitirme TP", "TP Finalizar Mapa Rápido"), () => { WinManager.InstantWin(); }, TExec));
                cats.Add(cat);
            }

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Automation ]", "[ 自动机器 ]", "[ Автоматизация ]", "[ Otomasyon ]", "[ Automação ]"), Buttons = new List<MenuButton>() };

                var bots = CreateBtn("AutoBot", T("Auto play BOT", "自动游玩机器人", "Универсальный бот", "Oto Oynama Botu", "Bot de Jogo Automático"), () => {
                    bool newState = !AutoPlayDodgeball.enabled;
                    AutoPlayDodgeball.enabled = AutoPlaySnowBrawl.enabled = AutoPlayBlockDrop.enabled = AutoPlayDeathFromAbove.enabled = AutoPlayCrabFight.enabled = AutoPlayRedLight.enabled = AutoPlaySteppingStones.enabled = AutoPlayBustlingButtons.enabled = AutoPlayTheFloorIsLava.enabled = AutoPlayTileDrive.enabled = newState;
                    BOTPlugin.ConfigAutoBotEnabled.Value = newState;
                    BOTPlugin.SaveConfigDebounced();
                }, () => AutoPlayDodgeball.enabled ? "<color=green>[ ACTIVE ]</color>" : "<color=red>[ OFF ]</color>");

                var f1 = CreateBtn("SnowBot", T("Snowball Master", "雪球大师", "Снежный бот", "Kar Topu Botu", "Mestre das Bolas de Neve"), null, TFolder);
                var s1 = CreateBtn("SnowPick", T("Auto PickUp", "自动捡球", "Автоподбор", "Oto Toplama", "Coletar Automaticamente"), () => { SnowballManager.autoPickup = !SnowballManager.autoPickup; BOTPlugin.ConfigSnowPickup.Value = SnowballManager.autoPickup; BOTPlugin.SaveConfigDebounced(); }, () => TState(SnowballManager.autoPickup));
                var s2 = CreateBtn("SnowAim", T("Auto Aim", "自动瞄准", "Автоприцел", "Oto Nişan", "Mira Automática"), () => { SnowballManager.autoAim = !SnowballManager.autoAim; BOTPlugin.ConfigSnowAutoAim.Value = SnowballManager.autoAim; BOTPlugin.SaveConfigDebounced(); }, () => TState(SnowballManager.autoAim));
                var s3 = CreateBtn("SnowSht", T("Auto Shoot", "自动射击", "Автовыстрел", "Oto Fırlatma", "Disparo Automático"), () => { SnowballManager.autoShoot = !SnowballManager.autoShoot; BOTPlugin.ConfigSnowAutoShoot.Value = SnowballManager.autoShoot; BOTPlugin.SaveConfigDebounced(); }, () => TState(SnowballManager.autoShoot));
                var s4 = CreateBtn("SnowNoCD", T("No Delay", "无冷却", "Без задержки", "Sıfır Bekleme", "Sem Atraso"), () => { SnowballManager.noShootDelay = !SnowballManager.noShootDelay; BOTPlugin.ConfigSnowNoDelay.Value = SnowballManager.noShootDelay; BOTPlugin.SaveConfigDebounced(); }, () => TState(SnowballManager.noShootDelay));
                var s5 = new MenuButton("SnowMult", T("Loop Boost", "射击倍率", "Множитель", "Ateş Hızı", "Acelerador de Loop"), true, (val) => { SnowballManager.loopBoost = val; BOTPlugin.ConfigSnowLoop.Value = val; BOTPlugin.SaveConfigDebounced(); }, () => SnowballManager.loopBoost, () => 1, () => 50, 1);
                s1.IsChild = s2.IsChild = s3.IsChild = s4.IsChild = s5.IsChild = true;
                f1.Children.AddRange(new[] { s1, s2, s3, s4, s5 });

                var f2 = CreateBtn("Utils", T("Utilities", "实用小帮手", "Утилиты", "Araçlar", "Utilitários"), null, TFolder);
                var au1 = CreateBtn("AutoRdy", T("Auto Ready", "自动准备", "Авто-готовность", "Oto Hazır", "Pronto Automático"), () => { ShadowModePatches.enableAutoReady = !ShadowModePatches.enableAutoReady; BOTPlugin.ConfigAutoReady.Value = ShadowModePatches.enableAutoReady; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enableAutoReady));
                var au2 = CreateBtn("AutoBox", T("Auto Box", "自动开箱", "Авто-кейс", "Oto Kutu Açma", "Abrir Caixas Automático"), () => {
                    ShadowModePatches.enableAutoBox = !ShadowModePatches.enableAutoBox;
                    BOTPlugin.ConfigAutoBoxEnabled.Value = ShadowModePatches.enableAutoBox;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(ShadowModePatches.enableAutoBox));
                var au2_a = new MenuButton("BoxLim", T("Limit", "限制数量", "Лимит", "Kutu Limiti", "Limite de Caixas"), true, (v) => { ShadowModePatches.autoBoxCount = v; BOTPlugin.ConfigAutoBoxCount.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => ShadowModePatches.autoBoxCount, () => 0, () => 1000, 1);
                var au2_b = new MenuButton("BoxDel", T("Delay (ms)", "开箱速度 (ms)", "Задержка (мс)", "Gecikme (ms)", "Atraso (ms)"), true, (v) => { ShadowModePatches.autoBoxDelayMs = v; BOTPlugin.ConfigAutoBoxDelay.Value = v; BOTPlugin.SaveConfigDebounced(); }, () => ShadowModePatches.autoBoxDelayMs, () => 100, () => 2000, 50);
                var au3 = CreateBtn("Daily", T("Complete Daily", "一键完成每日任务", "Завершить дейлики", "Görevleri Bitir", "Concluir Missão Diária"), () => { DailyQuestManager.CompleteDaily(); }, TExec);
                var au4 = CreateBtn("Painter", T("Tile Painter", "自动涂满瓷砖", "Красильщик", "Oto Boyama", "Pintor de Pisos"), () => { TilePainterManager.Start(); }, () => TilePainterManager.isPainting ? "<color=green>[ PAINTING ]</color>" : "[ START ]");
                var au5 = CreateBtn("AutoAd", T("Auto Advertiser", "自动公屏打广告", "Авто-реклама", "Oto Reklam", "Divulgador Automático"), () => {
                    AutoAdvertiser.enabled = !AutoAdvertiser.enabled;
                    BOTPlugin.ConfigAutoAdEnabled.Value = AutoAdvertiser.enabled;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(AutoAdvertiser.enabled));

                au1.IsChild = au2.IsChild = au2_a.IsChild = au2_b.IsChild = au3.IsChild = au4.IsChild = au5.IsChild = true;
                f2.Children.AddRange(new[] { au1, au2, au2_a, au2_b, au3, au4, au5 });

                cat.Buttons.Add(bots); cat.Buttons.Add(f1); cat.Buttons.Add(f2);
                cat.Buttons.Add(CreateBtn("QuickRecycleCommon", T("Recycle White (Common) Items", "一键分解白色普通物品", "разобрать белые качественные предметы", "Beyaz Eşyaları Geri Dönüştür", "Reciclar Itens Comuns"), () => { QuickRecycleManager.RecycleAllCommonItems(); }, TExec));
                cats.Add(cat);
            }

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Protection ]", "[ 保护机制 ]", "[ Защита ]", "[ Koruma ]", "[ Proteção ]"), Buttons = new List<MenuButton>() };

                var f1 = CreateBtn("Gods", T("God Modes", "上帝防御", "Режимы Бога", "Ölümsüzlük Modu", "Modos Deus"), null, TFolder);
                var g0 = CreateBtn("RspwnDth", T("Shadow respawn", "死后化作影子复活", "Возрождение тени", "Gölge tekrar doğuyor", "Renascer das Sombras"), () => { ShadowModePatches.respawnOnDeath = !ShadowModePatches.respawnOnDeath; BOTPlugin.ConfigRespawnOnDeath.Value = ShadowModePatches.respawnOnDeath; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.respawnOnDeath));
                var g1 = CreateBtn("GodM", T("God Mode", "上帝模式", "Режим Бога", "Ölümsüzlük", "Modo Deus"), () => { GodModeManager.enabled = !GodModeManager.enabled; BOTPlugin.ConfigGodMode.Value = GodModeManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(GodModeManager.enabled));
                var g2 = CreateBtn("Void", T("Void Rescue", "掉落自动拉回", "Спасение из пустоты", "Boşluktan Kurtar", "Resgate do Vazio"), () => { GodModeManager.autoRescue = !GodModeManager.autoRescue; BOTPlugin.ConfigGodModeRescue.Value = GodModeManager.autoRescue; BOTPlugin.SaveConfigDebounced(); }, () => TState(GodModeManager.autoRescue));
                var g3 = CreateBtn("Rlgl", T("RLGL Godmode", "红绿灯不死", "Бессмертие RLGL", "Kırmızı Işık Hilesi", "Modo Deus no Batatinha"), () => { MiniGameTrollManager.rlglGodMode = !MiniGameTrollManager.rlglGodMode; BOTPlugin.ConfigRlglGod.Value = MiniGameTrollManager.rlglGodMode; BOTPlugin.SaveConfigDebounced(); }, () => TState(MiniGameTrollManager.rlglGodMode));
                var g4 = CreateBtn("Hill", T("Hill God", "山丘之王", "Бог Горы", "Tepe Kralı Hilesi", "Rei da Colina Deus"), () => { HillGodManager.enabled = !HillGodManager.enabled; BOTPlugin.ConfigHillGodEnabled.Value = HillGodManager.enabled; BOTPlugin.SaveConfigDebounced(); }, () => TState(HillGodManager.enabled));

                g0.IsChild = g1.IsChild = g2.IsChild = g3.IsChild = g4.IsChild = true;
                f1.Children.AddRange(new[] { g0, g1, g2, g3, g4 });

                var f2 = CreateBtn("BridgeH", T("Bridge Helper", "垫脚石辅助", "Помощник моста", "Köprü Asistanı", "Auxiliar de Ponte"), null, TFolder);
                var b1 = CreateBtn("Glass", T("Glass God", "踩假玻璃不碎", "Стеклянный бог", "Cam Kırılmaz", "Vidro Inquebrável"), () => { BridgeHelperManager.noGlassBreak = !BridgeHelperManager.noGlassBreak; BOTPlugin.ConfigGlassGod.Value = BridgeHelperManager.noGlassBreak; BOTPlugin.SaveConfigDebounced(); }, () => TState(BridgeHelperManager.noGlassBreak));
                var b2 = CreateBtn("TPSafe", T("TP to Safe", "传送到下一块安全处", "ТП в безопасность", "Güvenli Cama Işınlan", "TP para Vidro Seguro"), () => { BridgeTPManager.TeleportToNextSafe(); }, TExec);
                var b3 = CreateBtn("BrkFakeRemote", T("Remote Break Fakes", "瞬间远程引爆所有假玻璃", "Удаленный взрыв", "Uzaktan Kır", "Quebrar Falsos Remotamente"), () => { BridgeHelperManager.ExecuteRemoteBreak(); }, TExec);
                var b4 = CreateBtn("BrkFakeTP", T("TP Break Fakes", "瞬移逐个踩碎所有假玻璃", "ТП Взрыв", "TP ile Kır", "Quebrar Falsos por TP"), () => { BridgeHelperManager.ExecuteTeleportBreak(); }, TExec);

                b1.IsChild = b2.IsChild = b3.IsChild = b4.IsChild = true;
                f2.Children.AddRange(new[] { b1, b2, b3, b4 });

                var f3 = CreateBtn("AntiH", T("Anti-Hack", "反作弊对抗", "Анти-чит", "Anti-Hile Koruması", "Anti-Cheat"), null, TFolder);
                var h1 = CreateBtn("AKick", T("Anti-Kick ban", "防踢/防Ban", "Анти-кик", "Ban/Kick Koruması", "Anti-Expulsão/Ban"), () => { ShadowModePatches.preventKickOnBan = !ShadowModePatches.preventKickOnBan; BOTPlugin.ConfigAntiKick.Value = ShadowModePatches.preventKickOnBan; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.preventKickOnBan));
                var h2 = CreateBtn("Hide", T("Shadow Mode", "影子模式", "Скрыть в списке", "Gizli Mod", "Modo Sombra"), () => { ShadowModePatches.hideFromPlayerList = !ShadowModePatches.hideFromPlayerList; BOTPlugin.ConfigHideList.Value = ShadowModePatches.hideFromPlayerList; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.hideFromPlayerList));
                var h3 = CreateBtn("ByFrz", T("Bypass Freeze", "绕过冻结", "Обход заморозки", "Donmayı Engelle", "Burlar Congelamento"), () => { ShadowModePatches.bypassFreeze = !ShadowModePatches.bypassFreeze; BOTPlugin.ConfigBypassFreeze.Value = ShadowModePatches.bypassFreeze; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.bypassFreeze));
                var h4 = CreateBtn("BlkMap", T("Block Map", "阻止加载地图", "Блок загрузки карты", "Haritayı Engelle", "Bloquear Carregamento de Mapa"), () => { ShadowModePatches.blockLoadMap = !ShadowModePatches.blockLoadMap; BOTPlugin.ConfigBlockLoadMap.Value = ShadowModePatches.blockLoadMap; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.blockLoadMap));
                var h5 = CreateBtn("AntiSpc", T("Anti-Spectate", "禁止观战", "Анти-наблюдатель", "İzlenmeyi Engelle", "Anti-Espectador"), () => { ShadowModePatches.enableAntiSpectate = !ShadowModePatches.enableAntiSpectate; BOTPlugin.ConfigAntiSpectate.Value = ShadowModePatches.enableAntiSpectate; BOTPlugin.SaveConfigDebounced(); }, () => TState(ShadowModePatches.enableAntiSpectate));
                h1.IsChild = h2.IsChild = h3.IsChild = h4.IsChild = h5.IsChild = true;
                f3.Children.AddRange(new[] { h1, h2, h3, h4, h5 });

                cat.Buttons.Add(f1); cat.Buttons.Add(f2); cat.Buttons.Add(f3);
                cats.Add(cat);
            }

            {
                MenuCategory cat = new MenuCategory { Name = T("[ Misc / Settings ]", "[ 大厅杂项 ]", "[ Настройки ]", "[ Ayarlar ]", "[ Diversos / Config ]"), Buttons = new List<MenuButton>() };

                cat.Buttons.Add(CreateBtn("Lang", T("Menu Language", "Menu Language", "Язык меню", "Menü Dili", "Idioma do Menu"), () => {
                    MenuManager.currentLanguage = (MenuManager.currentLanguage + 1) % 5;
                    PlayerPrefs.SetInt("BOT_Lang", MenuManager.currentLanguage);
                    PlayerPrefs.Save();
                    MenuManager.Categories = BuildCategories();
                }, () => T("<color=yellow>[ English ]</color>", "<color=yellow>[ 中文 ]</color>", "<color=yellow>[ Русский ]</color>", "<color=yellow>[ Türkçe ]</color>", "<color=yellow>[ Português ]</color>")));

                var f1 = CreateBtn("PSnipe", T("Sniper", "玩家追踪狙击", "Снайпер игроков", "Oyuncu Takibi", "Rastreador de Jogadores"), () => { JoinGameManager.TryJoin(); }, () => { string id = JoinGameManager.GetCurrentId(); return string.IsNullOrEmpty(id) ? T("<color=grey>[ Empty ]</color>", "<color=grey>[ 列表空 ]</color>", "<color=grey>[ Пусто ]</color>", "<color=grey>[ Boş ]</color>", "<color=grey>[ Vazio ]</color>") : "<color=white>" + JoinGameManager.GetTargetName(id) + "</color> <color=yellow>[ JOIN ]</color>"; });
                var s0 = CreateBtn("CPYID", T("Copy SteamID", "点击获取当前选定人 SteamID", "Копировать ID", "SteamID Kopyala", "Copiar SteamID"), () => { PlayerInfoManager.OnClick(); }, () => "[ " + PlayerInfoManager.GetCurrentStatus() + " ]");
                var s0_next = CreateBtn("NxtPlyr", T("Next Player", "切换下一个大厅玩家", "След. игрок", "Sonraki Oyuncu", "Próximo Jogador"), () => { PlayerInfoManager.CycleNext(); }, () => "<color=cyan>[ NEXT ]</color>");
                var s1 = CreateBtn("GETTGT", T("Get Target", "将当前选定人设为狙击目标", "Получить цель", "Hedefi Al", "Definir Alvo"), () => { JoinGameManager.CyclePlayerInLobby(); }, () => "[ " + JoinGameManager.GetSelectedPlayerName() + " ]");
                var s2 = CreateBtn("SAVTGT", T("Save Target", "保存选定到狙击名单", "Сохранить цель", "Seçili Oyuncuyu Kaydet", "Salvar Alvo"), () => { JoinGameManager.SaveSelectedPlayer(); }, () => "[ SAVE ]");
                var s3 = CreateBtn("CYCTGT", T("Cycle Target", "切换狙击名单里的目标", "Сменить цель", "Listedeki Hedefi Değiştir", "Alternar Alvos Salvos"), () => { JoinGameManager.CycleTargetId(); }, () => { string id = JoinGameManager.GetCurrentId(); return string.IsNullOrEmpty(id) ? T("[ Empty ]", "[ 列表空 ]", "[ Пусто ]", "[ Boş ]", "[ Vazio ]") : "[ " + (JoinGameManager.currentIndex + 1) + "/" + JoinGameManager.savedIds.Count + " ]"; });
                var s4 = CreateBtn("PSTID", T("Paste ID", "粘贴剪贴板ID并保存", "Вставить ID", "ID Yapıştır/Kaydet", "Colar e Salvar ID"), () => { JoinGameManager.PasteAndSave(); }, () => "[ PASTE ]");
                var s5 = CreateBtn("STS", T("Status:", "加入状态:", "Статус:", "Bağlantı Durumu:", "Status:"), null, () => { JoinGameManager.CheckIfJoined(); string s = JoinGameManager.statusText; return s.Contains("Join") || s.Contains("Success") || s.Contains("成功") ? "<color=green>" + s + "</color>" : (s.Contains("Error") || s.Contains("Offline") || s.Contains("失败") ? "<color=red>" + s + "</color>" : "<color=grey>" + s + "</color>"); });
                var s6 = CreateBtn("RLD", T("Reload", "编辑/重载名单", "Перезагрузить", "Listeyi Yenile", "Recarregar Lista"), () => { JoinGameManager.LoadIdFromFile(); }, () => "[ RELOAD ]");
                var s7 = CreateBtn("DELTGT", T("Delete Target", "删除当前选中目标", "Удалить", "Seçili Hedefi Sil", "Excluir Alvo"), () => { JoinGameManager.DeleteCurrentTarget(); }, () => "<color=red>[ DELETE ]</color>");

                s0.IsChild = s0_next.IsChild = s1.IsChild = s2.IsChild = s3.IsChild = s4.IsChild = s5.IsChild = s6.IsChild = s7.IsChild = true;
                f1.Children.AddRange(new[] { s0, s0_next, s1, s2, s3, s4, s5, s6, s7 });

                var f2 = CreateBtn("LSnipe", T("Lobby Sniper", "房间工具", "Снайпер лобби", "Lobi Takibi", "Rastreador de Lobby"), () => { JoinGameManager.JoinByLobbyCode(); }, () => "<color=green>[ JOIN ]</color>");
                var r1 = CreateBtn("LCPY", T("Copy Code", "获取当前房间号", "Скопировать код", "Mevcut Odayı Kopyala", "Copiar Código do Lobby"), () => { JoinGameManager.FetchCurrentRoomCode(); }, () => "[ COPY ]");
                var r2 = CreateBtn("LPST", T("Paste/Save", "粘贴剪贴板ID", "Вставить/Сохранить", "Yapıştır/Kaydet", "Colar/Salvar"), () => { string c = GUIUtility.systemCopyBuffer; if (ulong.TryParse(c, out _)) { JoinGameManager.LobbyCodeInput = c; JoinGameManager.SaveCurrentLobbyCode(); } }, () => "[ PASTE ]");
                var r3 = CreateBtn("LSAV", T("Save Code", "保存该房间号", "Сохранить", "Odayı Kaydet", "Salvar Código"), () => { JoinGameManager.SaveCurrentLobbyCode(); }, () => "[ SAVE ]");
                var r4 = CreateBtn("LCYC", T("Cycle Code", "切换已存房间", "Сменить код", "Kayıtlı Odalar Arası Geç", "Alternar Lobbies Salvos"), () => { JoinGameManager.CycleSavedLobbies(); }, () => "[ CYCLE ]");
                var r5 = CreateBtn("LDEL", T("Del Code", "删除选定房间", "Удалить", "Seçili Odayı Sil", "Excluir Código"), () => { JoinGameManager.DeleteCurrentLobby(); }, () => "[ DEL ]");
                var r6 = CreateBtn("LJOIN", T("Join Code", "加入选定房间", "Присоединиться", "Odaya Katıl", "Entrar no Lobby"), () => { JoinGameManager.JoinByLobbyCode(); }, () => "<color=green>[ JOIN ]</color>");
                var r7 = CreateBtn("LCUR", T("Current Code:", "当前代码:", "Текущий код:", "Oda Kodu:", "Código Atual:"), null, () => string.IsNullOrEmpty(JoinGameManager.LobbyCodeInput) ? T("<color=grey>[ Empty ]</color>", "<color=grey>[ 空 ]</color>", "<color=grey>[ Пусто ]</color>", "<color=grey>[ Boş ]</color>", "<color=grey>[ Vazio ]</color>") : "<color=yellow>[ " + JoinGameManager.LobbyCodeInput + " ]</color>");
                r1.IsChild = r2.IsChild = r3.IsChild = r4.IsChild = r5.IsChild = r6.IsChild = r7.IsChild = true;
                f2.Children.AddRange(new[] { r1, r2, r3, r4, r5, r6, r7 });

                var f4 = CreateBtn("TASFold", T("TAS Manager", "动作记录系统", "TAS Менеджер", "TAS Yöneticisi (Kayıt)", "Gerenciador de TAS"), () => { if (!TASManager.isRecording) { TASManager.recordedPath.Clear(); TASManager.isRecording = true; } else TASManager.isRecording = false; }, () => TASManager.isRecording ? "<color=red>[ RECORDING ]</color>" : "[ REC ]");
                var t1 = CreateBtn("TPlay", T("Play", "播放", "Воспроизвести", "Rotayı Oynat", "Reproduzir"), () => { if (TASManager.recordedPath.Count > 0) { TASManager.isRecording = false; TASManager.isPlaying = !TASManager.isPlaying; } }, () => TASManager.isPlaying ? "<color=green>[ PLAYING ]</color>" : "[ PLAY ]");
                var t2 = CreateBtn("TSav", T("Save File", "快速保存文件", "Сохранить файл", "Dosyayı Kaydet", "Salvar Arquivo"), () => { string mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; TASManager.SaveCurrentPath(mapName + "_" + DateTime.Now.ToString("HH-mm")); }, () => "[ SAVE ]");
                var t3 = CreateBtn("TCyc", T("Cycle TAS", "循环选择文件", "Сменить файл", "Kayıt Seç", "Alternar Gravação"), () => { TASManager.RefreshFileList(); if (TASManager.savedFiles.Count > 0) { TASManager.selectedFileIndex = (TASManager.selectedFileIndex + 1) % TASManager.savedFiles.Count; TASManager.LoadSelectedPath(); } }, () => { if (TASManager.savedFiles.Count == 0) return T("[ No Files ]", "[ 无文件 ]", "[ Нет файлов ]", "[ Dosya Yok ]", "[ Sem Arquivos ]"); return "[ " + TASManager.savedFiles[TASManager.selectedFileIndex] + " ]"; });
                var t5 = CreateBtn("TDel", T("Delete", "删除", "Удалить", "Dosyayı Sil", "Excluir"), () => { TASManager.DeleteSelectedFile(); }, () => "<color=red>[ DELETE ]</color>");
                t1.IsChild = t2.IsChild = t3.IsChild = t5.IsChild = true;
                f4.Children.AddRange(new[] { t1, t2, t3, t5 });

                var f6 = CreateBtn("CfgFold", T("Config Profiles", "配置文件管理", "Профили настроек", "Profil Yöneticisi", "Perfis de Configuração"), () => { ConfigProfileManager.CycleProfile(); }, () => "[ " + ConfigProfileManager.GetCurrentProfileName() + " ]");
                var cf1 = CreateBtn("CfgLd", T("Load", "加载选中配置", "Загрузить", "Profili Yükle", "Carregar Perfil"), () => { ConfigProfileManager.LoadProfile(); }, () => "<color=green>[ LOAD ]</color>");
                var cf2 = CreateBtn("CfgOv", T("Overwrite", "覆盖保存至选中", "Перезаписать", "Üzerine Yaz", "Sobrescrever"), () => { ConfigProfileManager.SaveProfile(ConfigProfileManager.GetCurrentProfileName()); }, () => "<color=red>[ OVERWRITE ]</color>");
                var cf3 = CreateBtn("CfgNew", T("New Profile", "从剪贴板文字新建", "Создать", "Panodan Yeni Profil", "Novo Perfil da Área de Transferência"), () => { ConfigProfileManager.CreateNewFromClipboard(); }, () => "[ CREATE ]");
                var cf4 = CreateBtn("CfgDel", T("Delete", "删除当前选中配置", "Удалить", "Profili Sil", "Excluir Perfil"), () => { ConfigProfileManager.DeleteProfile(); }, () => "<color=red>[ DELETE ]</color>");
                var cf5 = CreateBtn("CfgOpn", T("Open Folder", "打开配置文件夹", "Открыть папку", "Klasörü Aç", "Abrir Pasta"), () => { ConfigProfileManager.OpenProfileFolder(); }, () => "<color=cyan>[ OPEN ]</color>");
                var cf6 = CreateBtn("CfgRef", T("Refresh List", "刷新文件列表", "Обновить", "Listeyi Yenile", "Atualizar Lista"), () => { ConfigProfileManager.RefreshProfiles(); Utility.ForceMessage("Profiles Refreshed!"); }, () => "[ REFRESH ]");
                cf1.IsChild = cf2.IsChild = cf3.IsChild = cf4.IsChild = cf5.IsChild = cf6.IsChild = true;
                f6.Children.AddRange(new[] { cf1, cf2, cf3, cf4, cf5, cf6 });

                cat.Buttons.Add(f1); cat.Buttons.Add(f2); cat.Buttons.Add(f4); cat.Buttons.Add(f6);

                var chatToolsFold = CreateBtn("ChatToolsFold", T("Chat Tools", "聊天功能工具箱", "Инструменты чата", "Sohbet Araçları", "Ferramentas de Chat"), null, TFolder);
                var ct1 = CreateBtn("UncenChatToggle", T("   Bypass Chat Filter", "   破解违禁词屏蔽", "   Обход цензуры", "   Sansürsüz Gönder", "   Burlar Filtro de Chat"), () => {
                    ShadowModePatches.enableUncensoredChat = !ShadowModePatches.enableUncensoredChat;
                    BOTPlugin.ConfigUncensoredChat.Value = ShadowModePatches.enableUncensoredChat;
                    BOTPlugin.SaveConfigDebounced();
                }, () => TState(ShadowModePatches.enableUncensoredChat));

                var ct2 = CreateBtn("DisableChatToggle", T("   Block Others Chat", "   屏蔽其他人看消息 (全服清屏)", "   Заблокировать чат другим", "   Diğerlerine Sohbeti Engelle", "   Bloquear Chat dos Outros"), () => {
                    ShadowModePatches.disableChat = !ShadowModePatches.disableChat;
                }, () => TState(ShadowModePatches.disableChat));

                ct1.IsChild = ct2.IsChild = true;
                chatToolsFold.Children.AddRange(new[] { ct1, ct2 });
                cat.Buttons.Add(chatToolsFold);

                cat.Buttons.Add(CreateBtn("KillMe", T("Suicide", "自杀", "Суицид", "İntihar", "Suicídio"), () => { SuicideManager.Execute(); }, () => ""));

                var desFold = CreateBtn("DesFold", T("Destroy Shared Object", "破坏地图共享物品", "Уничтожить объекты", "Nesne Yokedici", "Destruir Objetos Compartilhados"), null, TFolder);
                var d1 = CreateBtn("DesToggle", T("Enable Destroyer", "开启破坏", "Включить", "Yokediciyi Aç", "Ativar Destruidor"), () => { SharedObjectDestroyer.enabled = !SharedObjectDestroyer.enabled; }, () => TState(SharedObjectDestroyer.enabled));
                var d2 = CreateBtn("DesTarget", T("Target Player", "背锅玩家 (伪造伤害源)", "Цель", "Hedef Oyuncu", "Jogador Alvo"), () => { JoinGameManager.CyclePlayerInLobby(); }, () => "[ " + JoinGameManager.GetSelectedPlayerName() + " ]");
                var d3 = CreateBtn("DesMode", T("Destroy Mode", "破坏模式", "Режим", "Yok Etme Modu", "Modo de Destruição"), () => { SharedObjectDestroyer.destroyAll = !SharedObjectDestroyer.destroyAll; }, () => SharedObjectDestroyer.destroyAll ? "<color=red>[ DESTROY ALL ]</color>" : "<color=cyan>[ SINGLE TARGET ]</color>");
                var d4 = CreateBtn("DesCycle", T("Cycle Object", "切换目标物品", "Выбрать объект", "Nesne Seç", "Alternar Objeto"), () => { if (!SharedObjectDestroyer.destroyAll) SharedObjectDestroyer.CycleObject(); }, () => "[ " + SharedObjectDestroyer.GetCurrentObjectName() + " ]");

                d1.IsChild = d2.IsChild = d3.IsChild = d4.IsChild = true;
                desFold.Children.AddRange(new[] { d1, d2, d3, d4 });
                cat.Buttons.Add(desFold);

                var svrFold = CreateBtn("SvrFold", T("Server List Viewer", "全局服务器列表", "Список серверов", "Sunucu Listesi Görüntüleyici", "Visualizador de Servidores"), null, TFolder);
                var s_list = new MenuButton("SvrListKey", T("Hotkey", "呼出/关闭快捷键", "Горячая клавиша", "Kısayol Tuşu", "Tecla de Atalho"), () => { }, null, false);
                s_list.Action = () => { s_list.IsBinding = true; };
                s_list.Status = () => {
                    ServerListViewerManager.ToggleKey = s_list.BoundKey;
                    BOTPlugin.SaveConfigDebounced();
                    if (BOTPlugin.ConfigServerListKey != null) BOTPlugin.ConfigServerListKey.Value = s_list.BoundKey;
                    return s_list.BoundKey == KeyCode.None ? "<color=grey>[ NONE ]</color>" : $"<color=yellow>[ {s_list.BoundKey} ]</color>";
                };
                if (BOTPlugin.ConfigServerListKey != null && BOTPlugin.ConfigServerListKey.Value != KeyCode.None)
                {
                    s_list.BoundKey = BOTPlugin.ConfigServerListKey.Value;
                    ServerListViewerManager.ToggleKey = s_list.BoundKey;
                }
                s_list.IsChild = true;
                svrFold.Children.AddRange(new[] { s_list });
                cat.Buttons.Add(svrFold);

                var c_trollFold = CreateBtn("ClientTrollFold", T("Freeze client player", "冻结客户端玩家", "Заблокировать клиента игрока", "İstemci oyuncuyu dondur", "Congelar Cliente do Jogador"), null, TFolder);
                var ct_tgt = CreateBtn("CTrollTgt", T("   [Select Victim]", "   [选择受害者]", "   [Выбрать жертву]", "   [Kurban Seç]", "   [Selecionar Vítima]"), () => {
                    if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return;
                    ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                    List<ulong> alivePlayers = new List<ulong>();
                    foreach (var kvp in GameManager.Instance.activePlayers)
                    {
                        if (kvp.Key != myId && kvp.Value != null && !kvp.Value.dead) alivePlayers.Add(kvp.Key);
                    }
                    if (alivePlayers.Count > 0)
                    {
                        int nextIndex = (alivePlayers.IndexOf(JoinGameManager.SelectedPlayerUid) + 1) % alivePlayers.Count;
                        JoinGameManager.SelectedPlayerUid = alivePlayers[nextIndex];
                        NetworkFreezeManager.TargetId = JoinGameManager.SelectedPlayerUid;
                    }
                    else Utility.ForceMessage("<color=red>No alive players found!</color>");
                }, () => "<color=yellow>[ " + JoinGameManager.GetSelectedPlayerName() + " ]</color>");

                var ct_freeze = CreateBtn("CTrollFreeze", T("Send Freeze (can only be done before the turn starts)", "   发送冻结（只能在回合开始前进行）", "Отправить заморозку (можно только до начала хода)", "   Donma gönder (yalnızca turun başında yapılabilir)", "   Enviar Congelamento (apenas antes do turno)"), () => {
                    if (NetworkFreezeManager.TargetId != 0)
                    {
                        NetworkFreezeManager.IsEnabled = !NetworkFreezeManager.IsEnabled;
                        BOTPlugin.ConfigFreezeClientEnabled.Value = NetworkFreezeManager.IsEnabled;
                        BOTPlugin.SaveConfigDebounced();
                        if (NetworkFreezeManager.IsEnabled) SteamNetworking.AcceptP2PSessionWithUser(new CSteamID(NetworkFreezeManager.TargetId));
                    }
                    else Utility.ForceMessage("<color=red>Select a target first!</color>");
                }, () => NetworkFreezeManager.IsEnabled ? "<color=red>[ FROZEN ]</color>" : "<color=green>[ NORMAL ]</color>");

                var ct_power = new MenuButton("CFreezePower", T("   Freeze Power", "   压制发包强度", "   Сила", "   Güç", "   Poder de Congelamento"), true, (val) => {
                    NetworkFreezeManager.FreezePower = val;
                    BOTPlugin.ConfigFreezePower.Value = val;
                    BOTPlugin.SaveConfigDebounced();
                }, () => NetworkFreezeManager.FreezePower, () => 50, () => 1000, 50);

                ct_tgt.IsChild = ct_freeze.IsChild = ct_power.IsChild = true;
                c_trollFold.Children.AddRange(new[] { ct_tgt, ct_freeze, ct_power });
                cat.Buttons.Add(c_trollFold);

                var micFold = CreateBtn("MicFold", T("Microphone Quality", "麦克风音质调节", "Микрофон", "Mikrofon", "Qualidade do Microfone"), null, TFolder);
                var m_noise = new MenuButton("MicNoise", T("   Bg Noise Removal", "   环境降噪强度", "   Шумоподавление", "   Gürültü Engelleme", "   Redução de Ruído de Fundo"), true, (val) => {
                    PerfectMicManager.BgNoiseRemoval = val / 100f;
                    BOTPlugin.ConfigBgNoiseRemoval.Value = PerfectMicManager.BgNoiseRemoval;
                    BOTPlugin.SaveConfigDebounced();
                    PerfectMicManager.UpdateVoiceSettings();
                }, () => (int)(PerfectMicManager.BgNoiseRemoval * 100), () => 0, () => 100, 5);

                var m_duck = new MenuButton("MicDuck", T("   Voice Duck Level", "   说话时压低游戏背景音", "   Приглушение игры", "   Oyun Sesini Kıs", "   Nível de Abaixamento do Jogo"), true, (val) => {
                    PerfectMicManager.VoiceDuckLevel = val / 100f;
                    BOTPlugin.ConfigVoiceDuckLevel.Value = PerfectMicManager.VoiceDuckLevel;
                    BOTPlugin.SaveConfigDebounced();
                    PerfectMicManager.UpdateVoiceSettings();
                }, () => (int)(PerfectMicManager.VoiceDuckLevel * 100), () => 0, () => 100, 5);

                m_noise.IsChild = m_duck.IsChild = true;
                micFold.Children.AddRange(new[] { m_noise, m_duck });
                cat.Buttons.Add(micFold);

                var h_itemFold = CreateBtn("ItemFold", T("Item ID send", "饰品ID浏览器与发送", "Браузер и отправка ID украшений", "Eşya ID Gönder", "Navegador e Envio de Itens"), null, TFolder);
                var id1 = CreateBtn("OpenBrowser", T("   Open Visual Browser", "   打开可视化饰品浏览器", "   Открыть браузер", "   Arayüzü Aç", "   Abrir Navegador Visual"), () => {
                    ItemBrowserManager.ShowGUI = !ItemBrowserManager.ShowGUI;
                    if (ItemBrowserManager.ShowGUI) { Variables.menuTrigger = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                }, () => ItemBrowserManager.ShowGUI ? "<color=green>[ OPENED ]</color>" : "[ OPEN ]");

                var id2 = CreateBtn("DumpItems", T("   Dump IDs to TXT", "   一键导出所有ID到TXT", "   Дамп в TXT", "   TXT'ye Aktar", "   Exportar IDs para TXT"), () => { ItemBrowserManager.DumpItemsToFile(); }, TExec);
                var id3 = CreateBtn("Jackpot", T("  Send replacement accessories with one click", "   一键发送替换的饰品", " Отправить заменяющие детали одним нажатием", "  Tek tıkla yedek parça gönder", "  Enviar Acessórios Substitutos com 1 Clique"), () => { UltimateDropSpoofer.TriggerJackpot(); }, () => $"<color=yellow>[ {ItemBrowserManager.SelectedItemIDs.Count} Items ]</color>");

                id1.IsChild = id2.IsChild = id3.IsChild = true;
                h_itemFold.Children.AddRange(new[] { id1, id2, id3 });
                cat.Buttons.Add(h_itemFold);

                cat.Buttons.Add(CreateBtn("FakeBoxUI", T("Fake Box ", "假箱子控制", "Меню ложных", "Sahte Kutu", "Caixa Falsa"), () => {
                    FakeBoxManager.fakeBox = !FakeBoxManager.fakeBox;
                    BOTPlugin.ConfigFakeBoxEnabled.Value = FakeBoxManager.fakeBox;
                    BOTPlugin.SaveConfigDebounced();
                }, () => FakeBoxManager.fakeBox ? $"<color=green>{T("[ ON ]", "[ 开启 ]", "[ ВКЛ ]", "[ AÇIK ]", "[ LIGADO ]")}</color>" : $"<color=red>{T("[ OFF ]", "[ 关闭 ]", "[ ВЫКЛ ]", "[ KAPALI ]", "[ DESLIGADO ]")}</color>"));

                cat.Buttons.Add(CreateBtn("MusicPlayerUI", T("BOTMusic Player ", "BOT音乐播放器 ", "BOTМузыкальный плеер ", "BOTMüzik Çalar ", "BOT Tocador de Música "), () => {
                    MusicPlayerManager.menuVisible = !MusicPlayerManager.menuVisible;
                    if (MusicPlayerManager.menuVisible) { Variables.menuTrigger = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                    else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                }, () => MusicPlayerManager.menuVisible ? "<color=green>[ OPEN ]</color>" : "[ CLOSE ]"));

                var trackerFold = CreateBtn("TrackerFold", T("Steam Online Tracker", "Steam上线追踪器", "Отслеж. Steam", "Steam Takibi", "Rastreador Steam Online"), null, TFolder);
                var tr1 = CreateBtn("TrToggle", T("   Enable Tracker", "   开启上线通知", "   Включить", "   Açık", "   Ativar Rastreador"), () => { SteamTrackerManager.enabled = !SteamTrackerManager.enabled; }, () => TState(SteamTrackerManager.enabled));
                var tr2 = CreateBtn("TrAddSel", T("   Track Selected Player", "   添加选中玩家", "   Добавить", "   Ekle", "   Rastrear Jogador Selecionado"), () => { SteamTrackerManager.AddSelectedPlayer(); }, () => "<color=green>[ ADD ]</color>");
                var tr3 = CreateBtn("TrAddClip", T("   Add ID from Clipboard", "   粘贴剪贴板 SteamID", "   Из буфера", "   Panodan Ekle", "   Adicionar ID da Área de Transf."), () => { SteamTrackerManager.AddFromClipboard(); }, () => "[ PASTE ]");
                var tr4 = CreateBtn("TrRemSel", T("   Remove Selected", "   删除选中玩家", "   Удалить", "   Kaldır", "   Remover Selecionado"), () => { SteamTrackerManager.RemoveSelectedPlayer(); }, () => "<color=red>[ DEL ]</color>");
                var tr5 = CreateBtn("TrOpenTxt", T("   Open List TXT", "   打开名单 TXT", "   Файл", "   Dosya", "   Abrir Lista TXT"), () => { SteamTrackerManager.OpenConfigFile(); }, () => $"<color=yellow>[ {SteamTrackerManager.TrackedIDs.Count} IDs ]</color>");

                tr1.IsChild = tr2.IsChild = tr3.IsChild = tr4.IsChild = tr5.IsChild = true;
                trackerFold.Children.AddRange(new[] { tr1, tr2, tr3, tr4, tr5 });
                cat.Buttons.Add(trackerFold);

                cat.Buttons.Add(CreateBtn("IncognitoMode",
                    T("Incognito Mode", "在线匿名模式", "Режим инкогнито", "Gizli Mod", "Modo Anônimo"),
                    () => {
                        GlobalStatsManager.ToggleHiddenMode();
                    },
                    () => TState(GlobalStatsManager.hiddenMode)
                ));

                cat.Buttons.Add(new MenuButton("UIScale",
                    T("UI Scale ", "UI 大小缩放 ", "Масштаб UI ", "UI Boyutu ", "Escala da UI "),
                    true,
                    (val) => {
                        MenuManager.UIScale = val / 100f;
                        if (BOTPlugin.ConfigUIScale != null)
                        {
                            BOTPlugin.ConfigUIScale.Value = MenuManager.UIScale;
                            BOTPlugin.SaveConfigDebounced();
                        }
                    },
                    () => (int)(MenuManager.UIScale * 100),
                    () => 70,
                    () => 150,
                    5
                ));

                cat.Buttons.Add(new MenuButton("Separator", MenuConstants.SEPARATOR));

                var menuKeyBtn = new MenuButton("MenuKey", T("Menu Key", "菜单按键", "Кнопка меню", "Menü Tuşu", "Tecla do Menu"), () => { }, null, true);
                menuKeyBtn.Action = () => { menuKeyBtn.IsBinding = true; };
                if (KeybindManager.SavedBinds.ContainsKey("MenuKey")) menuKeyBtn.BoundKey = KeybindManager.SavedBinds["MenuKey"];
                if (menuKeyBtn.BoundKey != KeyCode.None) Variables.menuKey = menuKeyBtn.BoundKey;
                menuKeyBtn.Status = () => menuKeyBtn.IsBinding
                    ? "<color=yellow>[ PRESS KEY... ]</color>"
                    : $"<color=yellow>[ {menuKeyBtn.BoundKey} ]</color>";
                cat.Buttons.Add(menuKeyBtn);

                cats.Add(cat);
            }

            return cats;
        }
    }
}
