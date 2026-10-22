using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using UnhollowerRuntimeLib;
using SteamworksNative;
using UnityEngine.UI;

namespace BOTMenuMod
{
    [BepInPlugin("BOT.Menu", "BOT Menu", "3.15.0")]
    public class BOTPlugin : BasePlugin
    {
        public static BOTPlugin Instance;
        public static GameObject __MenuObject = null;
        public static GameObject __NetPumpObject = null;
        public static bool HasModNet => ModNet.Enabled;
        public static readonly string CurrentBotVersion = ResolveBotVersion();
        private static ulong _lastLobbyId = 0;
        private static bool _cfgDirty = false;
        private static float _cfgDirtyAt = 0f;

        public static void SaveConfigDebounced()
        {
            _cfgDirty = true;
            _cfgDirtyAt = Time.realtimeSinceStartup;
        }

        public static void FlushConfigIfDirty()
        {
            if (!_cfgDirty) return;
            if (Time.realtimeSinceStartup - _cfgDirtyAt < 0.4f) return;
            FlushConfig();
        }

        public static void FlushConfig()
        {
            _cfgDirty = false;
            try
            {
                var inst = Instance;
                if (inst != null && inst.Config != null) inst.Config.Save();
            }
            catch { }
        }

        private void OnApplicationQuit()
        {
            FlushConfig();
        }

        public static ConfigEntry<bool> ConfigShinyEnabled;
        public static ConfigEntry<float> ConfigShinyValue;
        public static ConfigEntry<bool> ConfigAntiVoidEnabled;
        public static ConfigEntry<bool> ConfigGunAutoAim;
        public static ConfigEntry<bool> ConfigGunAutoShoot;
        public static ConfigEntry<bool> ConfigGunNoRecoil;
        public static ConfigEntry<bool> ConfigBlockLoadMap;
        public static ConfigEntry<KeyCode> ConfigMenuKey;
        public static ConfigEntry<bool> ConfigAntiKick;
        public static ConfigEntry<bool> ConfigHideList;
        public static ConfigEntry<bool> ConfigBypassFreeze;
        public static ConfigEntry<bool> ConfigFakePing;
        public static ConfigEntry<int> ConfigPingValue;
        public static ConfigEntry<bool> ConfigPingJitter;
        public static ConfigEntry<int> ConfigPingMin;
        public static ConfigEntry<int> ConfigPingMax;
        public static ConfigEntry<int> ConfigPingInterval;
        public static ConfigEntry<bool> ConfigShowMarker;
        public static ConfigEntry<int> ConfigAutoBoxCount;
        public static ConfigEntry<int> ConfigAutoBoxDelay;
        public static ConfigEntry<bool> ConfigCrosshairDot;
        public static ConfigEntry<int> ConfigCrosshairDotSize;
        public static ConfigEntry<int> ConfigCrosshairGap;
        public static ConfigEntry<int> ConfigCrosshairLength;
        public static ConfigEntry<int> ConfigCrosshairWidth;
        public static ConfigEntry<string> ConfigCrosshairColor;
        public static ConfigEntry<bool> ConfigAutoReady;
        public static ConfigEntry<bool> ConfigRespawnOnDeath;
        public static ConfigEntry<bool> ConfigItemSpawner;
        public static ConfigEntry<int> ConfigItemSpawnerMultiplier;
        public static ConfigEntry<bool> ConfigNoShake;
        public static ConfigEntry<bool> ConfigFreecam;
        public static ConfigEntry<bool> ConfigEnableTrail;
        public static ConfigEntry<string> ConfigTrailColor;
        public static ConfigEntry<bool> ConfigEnableJumpRing;
        public static ConfigEntry<string> ConfigJumpRingColor;
        public static ConfigEntry<bool> ConfigEnableWalkRing;
        public static ConfigEntry<string> ConfigWalkRingColor;
        public static ConfigEntry<bool> ConfigEnableAura;
        public static ConfigEntry<string> ConfigAuraColor;
        public static ConfigEntry<bool> ConfigAuraDrag;
        public static ConfigEntry<bool> ConfigEnableBridgeESP;
        public static ConfigEntry<bool> ConfigEnableButtonESP;
        public static ConfigEntry<bool> ConfigShareVisuals;
        public static ConfigEntry<bool> ConfigThirdPerson;
        public static ConfigEntry<float> ConfigThirdPersonDist;
        public static ConfigEntry<bool> ConfigEnableLavaPunch;
        public static ConfigEntry<bool> ConfigSpectatorPOV;
        public static ConfigEntry<bool> ConfigMagicBullet;
        public static ConfigEntry<bool> ConfigFullbright;
        public static ConfigEntry<int> ConfigLavaAttackMode;
        public static ConfigEntry<bool> ConfigLavaLockOn;
        public static ConfigEntry<float> ConfigLavaRange;
        public static ConfigEntry<float> ConfigLavaCooldown;
        public static ConfigEntry<int> ConfigLavaMultiplier;
        public static ConfigEntry<bool> ConfigSpinbot;
        public static ConfigEntry<int> ConfigSpinMode;
        public static ConfigEntry<float> ConfigSpinSpeed;
        public static ConfigEntry<float> ConfigFunnyBaseScale;
        public static ConfigEntry<float> ConfigFunnyIntensity;
        public static ConfigEntry<float> ConfigFunnySpeed;
        public static ConfigEntry<float> ConfigOrbitRadius;
        public static ConfigEntry<float> ConfigOrbitSpeed;
        public static ConfigEntry<float> ConfigOrbitHeight;
        public static ConfigEntry<bool> ConfigZoomEnabled;
        public static ConfigEntry<KeyCode> ConfigZoomKey;
        public static ConfigEntry<float> ConfigZoomFOV;
        public static ConfigEntry<bool> ConfigOverrideFOV;
        public static ConfigEntry<float> ConfigBaseFOV;
        public static ConfigEntry<bool> ConfigWaterWalkEnabled;
        public static ConfigEntry<float> ConfigWaterWalkOffset;
        public static ConfigEntry<bool> ConfigWaterJumpEnabled;
        public static ConfigEntry<float> ConfigWaterJumpLevel;
        public static ConfigEntry<float> ConfigWaterJumpBouncePower;
        public static ConfigEntry<bool> ConfigHillGodEnabled;
        public static ConfigEntry<bool> ConfigPlayerLauncherEnabled;
        public static ConfigEntry<bool> ConfigGodMode;
        public static ConfigEntry<float> ConfigKeyDisplayX;
        public static ConfigEntry<float> ConfigKeyDisplayY;
        public static ConfigEntry<bool> ConfigHitOverlayEnabled;
        public static ConfigEntry<float> ConfigHitOverlayX;
        public static ConfigEntry<float> ConfigHitOverlayY;
        public static ConfigEntry<float> ConfigNotifX;
        public static ConfigEntry<float> ConfigNotifY;
        public static ConfigEntry<float> ConfigNotifSpeed;
        public static ConfigEntry<float> ConfigNotifDuration;
        public static ConfigEntry<bool> ConfigLegitESPEnabled;
        public static ConfigEntry<string> ConfigLegitESPColor;
        public static ConfigEntry<bool> ConfigPlayerNoClip;
        public static ConfigEntry<bool> ConfigKeyDisplayEnabled;
        public static ConfigEntry<bool> ConfigSkinChangerEnabled;
        public static ConfigEntry<int> ConfigSkinTargetId;
        public static ConfigEntry<bool> ConfigTeleportThroughWalls;
        public static ConfigEntry<bool> ConfigWallNoClipEnabled;
        public static ConfigEntry<bool> ConfigSnowAutoShoot;
        public static ConfigEntry<bool> ConfigSnowAutoAim;
        public static ConfigEntry<bool> ConfigSnowNoDelay;
        public static ConfigEntry<bool> ConfigSnowHide;
        public static ConfigEntry<int> ConfigSnowLoop;
        public static ConfigEntry<bool> ConfigGunAimThroughWalls;
        public static ConfigEntry<string> ConfigCustomName;
        public static ConfigEntry<bool> ConfigIsNameSet;
        public static ConfigEntry<bool> ConfigItemESPEnabled;
        public static ConfigEntry<bool> ConfigFovCircleEnabled;
        public static ConfigEntry<float> ConfigFovRadius;
        public static ConfigEntry<bool> ConfigGodModeRescue;
        public static ConfigEntry<bool> ConfigInfAmmo;
        public static ConfigEntry<bool> ConfigNoCD;
        public static ConfigEntry<bool> ConfigNoWeaponPush;
        public static ConfigEntry<bool> ConfigAntiSpectate;
        public static ConfigEntry<bool> ConfigAutoPickup;
        public static ConfigEntry<float> ConfigBacktrackInterval;
        public static ConfigEntry<bool> ConfigRlglGodmode;
        public static ConfigEntry<bool> ConfigGlassGod;
        public static ConfigEntry<bool> ConfigCinematicFX;
        public static ConfigEntry<bool> ConfigMirrorCam;
        public static ConfigEntry<bool> ConfigWeaponSpawnerInfinite;
        public static ConfigEntry<bool> ConfigClickRipple;
        public static ConfigEntry<bool> ConfigRlglGod;
        public static ConfigEntry<bool> ConfigSilentKill;
        public static ConfigEntry<float> ConfigSilentKillRange;
        public static ConfigEntry<bool> ConfigAntiHost;
        public static ConfigEntry<bool> ConfigClickTPMain;
        public static ConfigEntry<bool> ConfigPlayerLauncher;
        public static ConfigEntry<bool> ConfigSnowPickup;
        public static ConfigEntry<bool> ConfigDamageUI;
        public static ConfigEntry<bool> ConfigTabColorEnabled;
        public static ConfigEntry<string> ConfigTabColorHex;
        public static ConfigEntry<bool> ConfigUncensoredChat;
        public static ConfigEntry<int> ConfigCrashMode;
        public static ConfigEntry<float> ConfigKnockbackMult;
        public static ConfigEntry<bool> ConfigKnockbackMultEnabled;
        public static ConfigEntry<bool> ConfigActiveListBg;
        public static ConfigEntry<bool> ConfigSmoothMovement;
        public static ConfigEntry<bool> ConfigGlobalMagicBullet;
        public static ConfigEntry<float> ConfigRapidFireDelay;
        public static ConfigEntry<KeyCode> ConfigServerListKey;
        public static ConfigEntry<bool> ConfigCustomKickBan;
        public static ConfigEntry<int> ConfigKickReason;
        public static ConfigEntry<int> ConfigBanReason;
        public static ConfigEntry<bool> ConfigKnockbackDeflect;
        public static ConfigEntry<float> ConfigDeflectAngle;
        public static ConfigEntry<bool> ConfigGlowESP;
        public static ConfigEntry<int> ConfigGlowMode;
        public static ConfigEntry<int> ConfigThemeMode;
        public static ConfigEntry<float> ConfigBgAlpha;
        public static ConfigEntry<float> ConfigBgNoiseRemoval;
        public static ConfigEntry<float> ConfigVoiceDuckLevel;
        public static ConfigEntry<bool> ConfigWhitelistMode;
        public static ConfigEntry<int> ConfigActiveListColorMode;
        public static ConfigEntry<bool> ConfigKillAura;
        public static ConfigEntry<bool> ConfigCrashPlayer;
        public static ConfigEntry<bool> ConfigVisualBlock;
        public static ConfigEntry<bool> ConfigGibsonEnabled;
        public static ConfigEntry<int> ConfigGibsonMode;
        public static ConfigEntry<float> ConfigGibsonDelay;
        public static ConfigEntry<bool> ConfigEnemyScaleEnabled;
        public static ConfigEntry<float> ConfigEnemyScaleMult;
        public static ConfigEntry<bool> ConfigHitHeartFX;
        public static ConfigEntry<bool> ConfigFreecamEnabled;
        public static ConfigEntry<bool> ConfigBacktrackingEnabled;
        public static ConfigEntry<bool> ConfigPoseEnabled;
        public static ConfigEntry<float> ConfigPoseYaw;
        public static ConfigEntry<bool> ConfigOrbitEnabled;
        public static ConfigEntry<bool> ConfigOrbitAuto;
        public static ConfigEntry<float> ConfigOrbitInterval;
        public static ConfigEntry<bool> ConfigFunnyScaleEnabled;
        public static ConfigEntry<bool> ConfigDemonModeEnabled;
        public static ConfigEntry<float> ConfigDemonPitch;
        public static ConfigEntry<bool> ConfigAutoBotEnabled;
        public static ConfigEntry<bool> ConfigAutoBoxEnabled;
        public static ConfigEntry<bool> ConfigAutoAdEnabled;
        public static ConfigEntry<string> ConfigAutoAdMessage;
        public static ConfigEntry<string> ConfigCloudChatApi;
        public static ConfigEntry<string> ConfigCloudConfigApi;
        public static ConfigEntry<string> ConfigMusicApiBase;
        public static ConfigEntry<string> ConfigMusicReferer;
        public static ConfigEntry<bool> ConfigFreezeClientEnabled;
        public static ConfigEntry<int> ConfigFreezePower;
        public static ConfigEntry<bool> ConfigLipSyncEnabled;
        public static ConfigEntry<bool> ConfigStrongCrash;
        public static ConfigEntry<bool> ConfigFakeBoxEnabled;
        public static ConfigEntry<bool> Config12HourTime;
        public static ConfigEntry<bool> ConfigDemonDynamicEnabled;
        public static ConfigEntry<float> ConfigDemonDynamicSpeed;
        public static ConfigEntry<float> ConfigDemonDynamicRange;
        public static ConfigEntry<bool> ConfigDemon360Enabled;
        public static ConfigEntry<bool> ConfigGalaxyBgEnabled;
        public static ConfigEntry<bool> ConfigChatNotifications;
        public static ConfigEntry<float> ConfigUIScale;

        public override void Load()
        {
            Instance = this;

            if (!Directory.Exists(Variables.mainFolderPath))
                Directory.CreateDirectory(Variables.mainFolderPath);

            ConfigMenuKey = Config.Bind("General", "MenuKey", KeyCode.Insert, "Key to toggle the menu.");
            KeybindManager.LoadBinds();

            ConfigCustomName = Config.Bind("General", "Username", "User", "Your BOT Mod Identity.");
            ConfigIsNameSet = Config.Bind("General", "IsNameSet", false, "Has the user set their name for the first time?");
            Config12HourTime = Config.Bind("UI", "12HourTime", false, "Display time in 12-hour format with AM/PM.");

            ConfigAntiKick = Config.Bind("Settings", "AntiKick", true, "Prevent being kicked.");
            ConfigHideList = Config.Bind("Settings", "HideFromList", true, "Hide from player list.");
            ConfigBypassFreeze = Config.Bind("Settings", "BypassFreeze", false, "Move while frozen.");
            ConfigBlockLoadMap = Config.Bind("Settings", "BlockLoadMap", false, "Prevent map from changing.");
            ConfigUncensoredChat = Config.Bind("Settings", "UncensoredChat", false, "Remove chat filter.");
            ConfigAutoReady = Config.Bind("Settings", "AutoReady", false, "Automatically press ready button.");
            ConfigAntiSpectate = Config.Bind("Settings", "AntiSpectate", false, "Prevent others from spectating you.");

            ConfigFakePing = Config.Bind("Ping", "FakePing", false, "Enable fake ping.");
            ConfigPingValue = Config.Bind("Ping", "PingValue", 0, "Fixed fake ping value.");
            ConfigPingJitter = Config.Bind("Ping", "PingJitter", false, "Enable random ping fluctuation.");
            ConfigPingMin = Config.Bind("Ping", "PingMin", 10, "Min ping for jitter.");
            ConfigPingMax = Config.Bind("Ping", "PingMax", 100, "Max ping for jitter.");
            ConfigPingInterval = Config.Bind("Ping", "PingInterval", 200, "Ms delay between ping changes.");

            ConfigAutoBoxCount = Config.Bind("AutoBox", "Count", 0, "0 = Open All, >0 = Limit");
            ConfigAutoBoxDelay = Config.Bind("AutoBox", "DelayMs", 500, "Delay in ms between boxes");

            ConfigCrosshairDot = Config.Bind("Crosshair", "EnableDot", false, "Enable center dot");
            ConfigCrosshairDotSize = Config.Bind("Crosshair", "DotSize", 2, "Size of the dot");
            ConfigCrosshairGap = Config.Bind("Crosshair", "GapSize", 6, "Gap between lines");
            ConfigCrosshairLength = Config.Bind("Crosshair", "Length", 10, "Length of lines");
            ConfigCrosshairWidth = Config.Bind("Crosshair", "Width", 4, "Width of lines");
            ConfigCrosshairColor = Config.Bind("Crosshair", "Color", "#FFFFFF", "Crosshair Color (Hex)");

            ConfigItemSpawner = Config.Bind("Cheats", "ItemSpawner", false, "Spam weapon packets to spawn items.");
            ConfigItemSpawnerMultiplier = Config.Bind("Cheats", "ItemSpawnerMultiplier", 20, "Spam multiplier per tick.");
            ConfigNoShake = Config.Bind("Cheats", "NoShake", false, "Remove camera shake and recoil.");
            ConfigFreecam = Config.Bind("Cheats", "Freecam", false, "Freeze network position while moving.");
            ConfigGodModeRescue = Config.Bind("Cheats", "GodModeRescue", true, "Auto teleport to sky when falling to void");
            ConfigAutoPickup = Config.Bind("Cheats", "AutoPickup", false, "Auto pickup spawned weapons");
            ConfigWeaponSpawnerInfinite = Config.Bind("Cheats", "SpawnerInfinite", false, "SpawnerInfinite");
            ConfigRespawnOnDeath = Config.Bind("Cheats", "RespawnOnDeath", false, "Respawn as a ghost when dead.");

            ConfigEnableTrail = Config.Bind("Visuals", "EnableTrail", false, "Enable player movement trail");
            ConfigTrailColor = Config.Bind("Visuals", "TrailColor", "#00FFFF", "Trail color in Hex");
            ConfigEnableJumpRing = Config.Bind("Visuals", "EnableJumpRing", false, "Enable jump ripple effect");
            ConfigJumpRingColor = Config.Bind("Visuals", "JumpRingColor", "#000000", "Jump ring color in Hex");
            ConfigEnableWalkRing = Config.Bind("Visuals", "EnableWalkRing", false, "Enable walking expanding ring effect");
            ConfigWalkRingColor = Config.Bind("Visuals", "WalkRingColor", "#FFFFFF", "Walking ring color in Hex");
            ConfigEnableAura = Config.Bind("Visuals", "EnableAura", false, "Enable particle aura");
            ConfigAuraColor = Config.Bind("Visuals", "AuraColor", "#00FFFF", "Aura color in Hex");
            ConfigAuraDrag = Config.Bind("Visuals", "AuraDrag", true, "Particles trail behind movement");
            ConfigEnableBridgeESP = Config.Bind("Visuals", "Stepping stones ESP", false, "Reveal safe platforms (Glass/Stone)");
            ConfigEnableButtonESP = Config.Bind("Visuals", "EnableButtonESP", false, "Highlight Bustling Buttons (Black=Bad, White=Good)");
            ConfigShareVisuals = Config.Bind("Visuals", "ShareVisuals", true, "Share your Trail/Aura/Rings with other mod users.");
            ConfigThirdPerson = Config.Bind("Visuals", "ThirdPerson", false, "Enable 3rd Person View");
            ConfigThirdPersonDist = Config.Bind("Visuals", "ThirdPersonDistance", 4.0f, "Distance of the camera");
            ConfigSpectatorPOV = Config.Bind("Visuals", "SpectatorPOV", false, "First Person view when spectating");
            ConfigFullbright = Config.Bind("Visuals", "Fullbright", false, "Fullbright");
            ConfigShowMarker = Config.Bind("Visuals", "ShowMarker", true, "Show BOT marker to others.");
            ConfigZoomEnabled = Config.Bind("Visuals", "ZoomEnabled", false, "Enable Zoom feature");
            ConfigZoomKey = Config.Bind("Visuals", "ZoomKey", KeyCode.C, "Keybind for Zoom");
            ConfigZoomFOV = Config.Bind("Visuals", "ZoomFOV", 20f, "Target FOV when zooming");
            ConfigOverrideFOV = Config.Bind("Visuals", "OverrideFOV", false, "Enable FOV Override");
            ConfigBaseFOV = Config.Bind("Visuals", "BaseFOV", 100f, "Custom Base FOV");
            ConfigLegitESPEnabled = Config.Bind("Visuals", "LegitESP_Enabled", false, "Enable visibility-checked corner ESP.");
            ConfigLegitESPColor = Config.Bind("Visuals", "LegitESP_Color", "#00FFFF", "Hex color code for Legit ESP border.");
            ConfigSkinChangerEnabled = Config.Bind("Visuals", "SkinChanger_Enabled", false, "Enable weapon skin changer.");
            ConfigSkinTargetId = Config.Bind("Visuals", "SkinChanger_Id", 15, "The target Skin ID to apply.");
            ConfigItemESPEnabled = Config.Bind("Visuals", "ItemESP", false, "Enable Item/Weapon ESP");
            ConfigCinematicFX = Config.Bind("Visuals", "CinematicFX", false, "Enable cinematic graphics");
            ConfigMirrorCam = Config.Bind("Visuals", "MirrorCam", false, "Enable rear view mirror");
            ConfigGlowESP = Config.Bind("Visuals", "GlowESP", false, "CS:GO style Chams ESP through walls.");
            ConfigGlowMode = Config.Bind("Visuals", "GlowMode", 1, "Mode for Glow ESP.");
            ConfigEnemyScaleEnabled = Config.Bind("Visuals", "EnemyScaleEnabled", false, "Giant Enemies");
            ConfigEnemyScaleMult = Config.Bind("Visuals", "EnemyScaleMult", 3.0f, "Enemy Size");
            ConfigHitHeartFX = Config.Bind("Visuals", "HitHeartFX", false, "Hit Heart Effect");
            ConfigShinyEnabled = Config.Bind("Visuals", "ShinyGlowEnabled", false, "Enable custom shiny glow multiplier.");
            ConfigShinyValue = Config.Bind("Visuals", "ShinyGlowValue", 64f, "Shiny glow intensity value (1-200).");

            ConfigEnableLavaPunch = Config.Bind("Combat", "LavaAutoPunch", false, "Auto punch nearest player in Floor Is Lava mode");
            ConfigLavaAttackMode = Config.Bind("Combat", "AttackMode", 0, "0=Nearest, 1=Selected, 2=All");
            ConfigLavaLockOn = Config.Bind("Combat", "LockOn", false, "Stick to target");
            ConfigLavaRange = Config.Bind("Combat", "Range", 4.0f, "Attack Range");
            ConfigLavaCooldown = Config.Bind("Combat", "Cooldown", 0.25f, "Attack Speed");
            ConfigLavaMultiplier = Config.Bind("Combat", "Multiplier", 1, "Punch Multiplier");
            ConfigGunAutoAim = Config.Bind("Combat", "GunAutoAim", false, "Snap crosshair to nearest target.");
            ConfigGunAutoShoot = Config.Bind("Combat", "GunAutoShoot", false, "Fire automatically when holding gun.");
            ConfigGunNoRecoil = Config.Bind("Combat", "GunNoRecoil", false, "Remove spread and recoil.");
            ConfigGunAimThroughWalls = Config.Bind("Combat", "GunAimThroughWalls", false, "Aim at enemies even behind walls.");
            ConfigFovCircleEnabled = Config.Bind("Combat", "FovCircle", false, "Show Aimbot FOV Circle");
            ConfigFovRadius = Config.Bind("Combat", "FovRadius", 150f, "Aimbot FOV Radius");
            ConfigMagicBullet = Config.Bind("Combat", "MagicBullet", false, "Magic Bullet");
            ConfigInfAmmo = Config.Bind("Combat", "InfAmmo", false, "Infinite Ammo");
            ConfigNoCD = Config.Bind("Combat", "NoCooldown", false, "No Weapon Cooldown");
            ConfigNoWeaponPush = Config.Bind("Combat", "NoWeaponPush", false, "Disable weapon push back");
            ConfigSilentKill = Config.Bind("Combat", "SilentKill", false, "SilentKill");
            ConfigSilentKillRange = Config.Bind("Combat", "SilentKillRange", 5f, "Lethal range");
            ConfigAntiHost = Config.Bind("Combat", "AntiHost", true, "Do not attack the homeowner");
            ConfigCrashMode = Config.Bind("Combat", "CrashMode", 0, "0=MinValue Flood, 1=Fatal ID Overflow");
            ConfigKnockbackMult = Config.Bind("Combat", "KnockbackMult", 1.0f, "Multiplier for weapon knockback.");
            ConfigKnockbackMultEnabled = Config.Bind("Combat", "KnockbackMultEnabled", false, "Enable weapon knockback multiplier.");
            ConfigGlobalMagicBullet = Config.Bind("Combat", "GlobalMagicBullet", false, "Kill everyone instantly when shooting.");
            ConfigRapidFireDelay = Config.Bind("Combat", "RapidFireDelay", 0.08f, "Delay between each shot (lower = faster).");
            ConfigKnockbackDeflect = Config.Bind("Combat", "KnockbackDeflect", false, "Deflect the knockback direction of your attacks.");
            ConfigDeflectAngle = Config.Bind("Combat", "DeflectAngle", 90f, "Angle to deflect knockback (90 = Right, -90 = Left, 180 = Pull).");
            ConfigKillAura = Config.Bind("Combat", "KillAura", false, "Kill Aura");
            ConfigCrashPlayer = Config.Bind("Combat", "CrashPlayer", false, "Crash Player on hit");
            ConfigVisualBlock = Config.Bind("Combat", "VisualBlock", false, "Visual Block");
            ConfigGibsonEnabled = Config.Bind("Combat", "GibsonEnabled", false, "Gibson Strike");
            ConfigGibsonMode = Config.Bind("Combat", "GibsonMode", 0, "Gibson Mode");
            ConfigGibsonDelay = Config.Bind("Combat", "GibsonDelay", 0.2f, "Gibson Delay");
            ConfigStrongCrash = Config.Bind("Combat", "StrongCrash", false, "Enable PlayerReload NaN (may affect other players)");

            ConfigSpinbot = Config.Bind("Movement", "Spinbot", false, "Enable Spinbot");
            ConfigSpinMode = Config.Bind("Movement", "SpinMode", 1, "0=Heli, 1=Jitter, 2=Random");
            ConfigSpinSpeed = Config.Bind("Movement", "SpinSpeed", 2000f, "Rotation Speed");
            ConfigOrbitRadius = Config.Bind("Movement", "OrbitRadius", 3f, "Orbit radius");
            ConfigOrbitSpeed = Config.Bind("Movement", "OrbitSpeed", 3f, "Rotation speed");
            ConfigOrbitHeight = Config.Bind("Movement", "OrbitHeight", 2.5f, "Height offset");
            ConfigPlayerNoClip = Config.Bind("Movement", "PlayerNoClip", false, "Disable collision with other players.");
            ConfigTeleportThroughWalls = Config.Bind("Movement", "TPThroughWalls", false, "Allow Click TP to ignore obstacles.");
            ConfigWallNoClipEnabled = Config.Bind("Movement", "WallNoClip", false, "Allow player to walk through walls and objects.");
            ConfigAntiVoidEnabled = Config.Bind("Movement", "AntiVoid", false, "Prevent dying when falling into the kill zone.");
            ConfigBacktrackInterval = Config.Bind("Movement", "BacktrackInterval", 0.4f, "Fake lag interval");
            ConfigClickTPMain = Config.Bind("Movement", "ClickTP", false, "Click the teleport master switch");
            ConfigSmoothMovement = Config.Bind("Movement", "SmoothMovement", false, "Force 30 tickrate sync for smoother movement to others.");
            ConfigFreecamEnabled = Config.Bind("Movement", "FreecamEnabled", false, "Freecam");
            ConfigBacktrackingEnabled = Config.Bind("Movement", "BacktrackingEnabled", false, "Backtrack");
            ConfigPoseEnabled = Config.Bind("Movement", "PoseEnabled", false, "Demon Pose");
            ConfigPoseYaw = Config.Bind("Movement", "PoseYaw", 180f, "Pose Angle");
            ConfigOrbitEnabled = Config.Bind("Movement", "OrbitEnabled", false, "Orbit Walk");
            ConfigOrbitAuto = Config.Bind("Movement", "OrbitAuto", false, "Orbit Auto Cycle");
            ConfigOrbitInterval = Config.Bind("Movement", "OrbitInterval", 4.0f, "Orbit Interval");
            ConfigFunnyScaleEnabled = Config.Bind("Movement", "FunnyScaleEnabled", false, "Funny Scale");
            ConfigDemonModeEnabled = Config.Bind("Movement", "DemonModeEnabled", false, "Demon Mode");
            ConfigDemonPitch = Config.Bind("Movement", "DemonPitch", 270f, "Demon Pitch");
            ConfigDemonDynamicEnabled = Config.Bind("Movement", "DemonDynamicEnabled", false, "Dynamic Demon Swing");
            ConfigDemonDynamicSpeed = Config.Bind("Movement", "DemonDynamicSpeed", 10f, "Dynamic Demon Speed");
            ConfigDemonDynamicRange = Config.Bind("Movement", "DemonDynamicRange", 40f, "Dynamic Demon Range");
            ConfigDemon360Enabled = Config.Bind("Movement", "Demon360Enabled", false, "360 Continuous Flip");

            ConfigWaterWalkEnabled = Config.Bind("WaterWalk", "Enabled", false, "Enable Water Walk (create invisible floors)");
            ConfigWaterWalkOffset = Config.Bind("WaterWalk", "Offset", 0.1f, "Floor offset above liquid surface");
            ConfigWaterJumpEnabled = Config.Bind("WaterJump", "Enabled", false, "Enable Water Jump");
            ConfigWaterJumpLevel = Config.Bind("WaterJump", "Level", -3.5f, "Fixed water level for jumping");
            ConfigWaterJumpBouncePower = Config.Bind("WaterJump", "BouncePower", 25f, "Bounce power");

            ConfigFunnyBaseScale = Config.Bind("Fun", "FunnyBaseScale", 0.4f, "Base scale size");
            ConfigFunnyIntensity = Config.Bind("Fun", "FunnyIntensity", 0.15f, "Jitter intensity");
            ConfigFunnySpeed = Config.Bind("Fun", "FunnySpeed", 25f, "Jitter speed");
            ConfigPlayerLauncher = Config.Bind("Fun", "PlayerLauncher", false, "Random teleport to other players with high speed");
            ConfigPlayerLauncherEnabled = ConfigPlayerLauncher;

            ConfigHillGodEnabled = Config.Bind("Modes", "HillGod", false, "Auto-teleport to ScoreZone and freeze");
            ConfigRlglGodmode = Config.Bind("Modes", "RlglGod", false, "Red Light Green Light Godmode");
            ConfigGlassGod = Config.Bind("Modes", "GlassGod", false, "Prevent glass from breaking");
            ConfigRlglGod = Config.Bind("Modes", "RlglGod", false, "RlglGod");

            ConfigGodMode = Config.Bind("Misc", "GodMode", false, "Infinite health (constantly reset player status)");
            ConfigFreezeClientEnabled = Config.Bind("Misc", "FreezeClientEnabled", false, "Client P2P Freeze");
            ConfigFreezePower = Config.Bind("Misc", "FreezePower", 50, "Client P2P Freeze Power");

            ConfigKeyDisplayX = Config.Bind("UI", "KeyDisplayX", 50f, "X position of KeyDisplay");
            ConfigKeyDisplayY = Config.Bind("UI", "KeyDisplayY", 150f, "Y position of KeyDisplay");
            ConfigHitOverlayEnabled = Config.Bind("UI", "HitOverlayEnabled", true, "Show target info when hit");
            ConfigHitOverlayX = Config.Bind("UI", "HitOverlayX", 500f, "X position of HitOverlay");
            ConfigHitOverlayY = Config.Bind("UI", "HitOverlayY", 600f, "Y position of HitOverlay");
            ConfigNotifX = Config.Bind("UI", "NotifX", 30f, "X position of Notification");
            ConfigNotifY = Config.Bind("UI", "NotifY", 40f, "Y position of Notification");
            ConfigNotifSpeed = Config.Bind("UI", "NotifSpeed", 10f, "Animation speed of Notification");
            ConfigNotifDuration = Config.Bind("UI", "NotifDuration", 3.5f, "How long the notification stays");
            ConfigKeyDisplayEnabled = Config.Bind("UI", "KeyDisplayEnabled", false, "Enable keystrokes display overlay.");
            ConfigClickRipple = Config.Bind("UI", "ClickRipple", false, "ClickRipple");
            ConfigDamageUI = Config.Bind("UI", "DamageFeed", false, "Show damage feedback list");
            ConfigTabColorEnabled = Config.Bind("UI", "TabColorEnabled", false, "Enable colored name in TAB.");
            ConfigTabColorHex = Config.Bind("UI", "TabColorHex", "#FFFF00", "TAB Name Color Hex.");
            ConfigActiveListBg = Config.Bind("UI", "ActiveListBg", false, "Show background for active list.");
            ConfigServerListKey = Config.Bind("UI", "ServerListKey", KeyCode.F2, "Keybind to open the server list anywhere.");
            ConfigThemeMode = Config.Bind("UI", "ThemeMode", 0, "Menu Theme Index.");
            ConfigBgAlpha = Config.Bind("UI", "BgAlpha", 0.90f, "Background Opacity.");
            ConfigActiveListColorMode = Config.Bind("UI", "ActiveListColorMode", 2, "0=ThemeWave, 1=ThemeStatic, 2=ItemRGB, 3=TextRainbow");
            ConfigGalaxyBgEnabled = Config.Bind("UI", "GalaxyBgEnabled", true, "Enable Galaxy Dynamic Background");

            ConfigSnowAutoShoot = Config.Bind("Snowball", "AutoShoot", false, "Auto throw snowballs.");
            ConfigSnowAutoAim = Config.Bind("Snowball", "AutoAim", false, "Teleport snowball to target head.");
            ConfigSnowNoDelay = Config.Bind("Snowball", "NoDelay", false, "Remove throwing cooldown.");
            ConfigSnowHide = Config.Bind("Snowball", "HideUnderPile", false, "Hide player under the snowball pile.");
            ConfigSnowLoop = Config.Bind("Snowball", "LoopBoost", 1, "Logic execution multiplier per frame.");
            ConfigSnowPickup = Config.Bind("Snowball", "AutoPickup", false, "automatically pick up snowballs");

            ConfigAutoBotEnabled = Config.Bind("Automation", "AutoBotEnabled", false, "Auto Play BOTs");
            ConfigAutoBoxEnabled = Config.Bind("Automation", "AutoBoxEnabled", false, "Auto Box Opener");
            ConfigAutoAdEnabled = Config.Bind("Automation", "AutoAdEnabled", false, "Auto Advertiser");
            ConfigAutoAdMessage = Config.Bind("Automation", "AutoAdMessage", "", "Text sent to public chat by the auto advertiser (empty = feature disabled)");
            ConfigCloudChatApi = Config.Bind("Network", "CloudChatApi", "", "Base URL of the cloud chat API (empty = global chat disabled)");
            ConfigCloudConfigApi = Config.Bind("Network", "CloudConfigApi", "", "Base URL of the cloud config API (empty = cloud configs disabled)");
            ConfigMusicApiBase = Config.Bind("Network", "MusicApiBase", "", "Base URL of the music search/stream API (empty = music player disabled)");
            ConfigMusicReferer = Config.Bind("Network", "MusicReferer", "", "Referer header sent to the music API (optional)");
            ConfigFakeBoxEnabled = Config.Bind("Automation", "FakeBoxEnabled", false, "Enable Fake Box Replayer");

            ConfigCustomKickBan = Config.Bind("Server", "CustomKickBan", false, "Send custom numbers when kicking/banning players.");
            ConfigKickReason = Config.Bind("Server", "KickReason", 1488, "Custom number for Kick reason.");
            ConfigBanReason = Config.Bind("Server", "BanReason", 69, "Custom number for Ban reason.");
            ConfigWhitelistMode = Config.Bind("Server", "WhitelistMode", false, "Only allow whitelisted players.");

            ConfigBgNoiseRemoval = Config.Bind("MicSettings", "BgNoiseRemoval", 0f, "Background Sound Removal Amount.");
            ConfigVoiceDuckLevel = Config.Bind("MicSettings", "VoiceDuckLevel", 0.4f, "Voice Duck Level.");
            ConfigLipSyncEnabled = Config.Bind("Audio", "LipSyncEnabled", true, "Lip sync and head swing when others speak");
            ConfigChatNotifications = Config.Bind("UI", "ChatNotifications", true, "Enable global chat pop-up notifications.");
            ConfigUIScale = Config.Bind("UI", "UIScale", 1.0f, "Menu UI Scale Multiplier (0.7 - 1.5).");

            RegisterIL2CPPTypes();
            InitAllManagers();
            ApplyHarmonyPatches();
            SpawnNetPump();

            Utility.CreateFolder(Variables.mainFolderPath);
            Utility.CreateFile(Variables.logFilePath);
            Utility.CreateFile(Variables.configFilePath);
            Utility.SetConfigFile(Variables.configFilePath);

            ConfigProfileManager.Init();
            SyncConfigsToMemory();

            Utility.Log("BOT Menu Loaded.");
        }

        private static void SpawnNetPump()
        {
            try
            {
                if (__NetPumpObject == null || !__NetPumpObject)
                {
                    __NetPumpObject = new GameObject("BOT_NetPump");
                    UnityEngine.Object.DontDestroyOnLoad(__NetPumpObject);
                }
                if (!__NetPumpObject.GetComponent<BOTMenuMod.Net.ModNetPump>())
                    __NetPumpObject.AddComponent<BOTMenuMod.Net.ModNetPump>();
                Utility.Log("ModNet pump started.");
            }
            catch { }
        }

        private void RegisterIL2CPPTypes()
        {
            ClassInjector.RegisterTypeInIl2Cpp<BOTNotification>();
            ClassInjector.RegisterTypeInIl2Cpp<MenuManager>();
            ClassInjector.RegisterTypeInIl2Cpp<Draggable>();
            ClassInjector.RegisterTypeInIl2Cpp<BOTStatusIsland>();
            ClassInjector.RegisterTypeInIl2Cpp<BOTActiveList>();
            ClassInjector.RegisterTypeInIl2Cpp<GalaxyBackground>();
            ClassInjector.RegisterTypeInIl2Cpp<ClickRippleManager>();
            ClassInjector.RegisterTypeInIl2Cpp<SpectatorWarningManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BOTNameSetup>();
            ClassInjector.RegisterTypeInIl2Cpp<ItemBrowserManager>();
            ClassInjector.RegisterTypeInIl2Cpp<UltimateDropSpoofer>();
            ClassInjector.RegisterTypeInIl2Cpp<FakeBoxManager>();
            ClassInjector.RegisterTypeInIl2Cpp<MusicPlayerManager>();
            ClassInjector.RegisterTypeInIl2Cpp<FunnyScaleModule>();
            ClassInjector.RegisterTypeInIl2Cpp<CameraManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BodyRotationManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BodyInverseManager>();
            ClassInjector.RegisterTypeInIl2Cpp<WeaponAttackManager>();
            ClassInjector.RegisterTypeInIl2Cpp<ItemSkinChangerManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BlacklistManager>();
            ClassInjector.RegisterTypeInIl2Cpp<BOTDisclaimerUI>();
            ClassInjector.RegisterTypeInIl2Cpp<AntroPort.BoneSynchronizer>();
            ClassInjector.RegisterTypeInIl2Cpp<AntroPort.SkinsMenu>();
            ClassInjector.RegisterTypeInIl2Cpp<AutoUpdatePump>();
            ClassInjector.RegisterTypeInIl2Cpp<BOTMenuMod.Net.ModNetPump>();
        }

        private void InitAllManagers()
        {
            FunnyScaleModule.InitNetwork();
            TagServerManager.LoadWhitelist();
            JoinGameManager.Init();
            GlobalStatsManager.Init();
            AntiCrashManager.Init();
            VoiceIndicatorManager.Init();
            ChatHistoryManager.Init();
            PerfectMicManager.Init();
            SmoothMovementManager.Init();
            WeaponAttackManager.Init();
            ServerListViewerManager.Init();
            CustomKickBanManager.Init();
            GlowESPManager.Init();
            PlayerTrackerManager.Init();
            SteamTrackerManager.Init();
            SwarmMasterManager.Init();
            AntroPort.AntroSkinManager.InitFolders();
            AntroPort.AntroConfigManager.Init(this.Config);

            if (HasModNet)
            {
                RegisterNetworkHandlers();
                NetworkVisualsManager.Init();
                SkinManager.Init();
                AntroPort.AntroSkinManager.InitNetwork();
                BotTabManager.InitNetwork();
            }
        }

        private void ApplyHarmonyPatches()
        {
            Harmony.CreateAndPatchAll(typeof(BOTPlugin));
            Harmony.CreateAndPatchAll(typeof(ShadowModePatches));
            Harmony.CreateAndPatchAll(typeof(CrosshairManager));
            Harmony.CreateAndPatchAll(typeof(JumpVisualPatch));
            Harmony.CreateAndPatchAll(typeof(TagModePatches));
            Harmony.CreateAndPatchAll(typeof(CustomServerListManager.SetUIPatch));
            Harmony.CreateAndPatchAll(typeof(SpectatorPOVManager.CamBobPatch));
            Harmony.CreateAndPatchAll(typeof(FakeBoxPatches));
            Harmony.CreateAndPatchAll(typeof(ChatFontPatch));
        }

        public static void SyncConfigsToMemory()
        {
            Variables.menuKey = ConfigMenuKey.Value;
            ShinyIntensityManager.Enabled = ConfigShinyEnabled.Value;
            ShinyIntensityManager.ShinyValue = ConfigShinyValue.Value;
            ShadowModePatches.preventKickOnBan = ConfigAntiKick.Value;
            ShadowModePatches.hideFromPlayerList = ConfigHideList.Value;
            ShadowModePatches.bypassFreeze = ConfigBypassFreeze.Value;
            ShadowModePatches.enableFakePing = ConfigFakePing.Value;
            ShadowModePatches.fakePingValue = ConfigPingValue.Value;
            ShadowModePatches.enablePingJitter = ConfigPingJitter.Value;
            ShadowModePatches.pingMin = ConfigPingMin.Value;
            ShadowModePatches.pingMax = ConfigPingMax.Value;
            ShadowModePatches.pingInterval = ConfigPingInterval.Value;
            ShadowModePatches.autoBoxCount = ConfigAutoBoxCount.Value;
            ShadowModePatches.autoBoxDelayMs = ConfigAutoBoxDelay.Value;
            ShadowModePatches.enableAutoReady = ConfigAutoReady.Value;
            ShadowModePatches.blockLoadMap = ConfigBlockLoadMap.Value;
            ShadowModePatches.noShake = ConfigNoShake.Value;
            ShadowModePatches.enableSpinbot = ConfigSpinbot.Value;
            ShadowModePatches.spinMode = ConfigSpinMode.Value;
            ShadowModePatches.spinSpeed = ConfigSpinSpeed.Value;
            ShadowModePatches.enableAntiSpectate = ConfigAntiSpectate.Value;
            ShadowModePatches.enableNoWeaponPush = ConfigNoWeaponPush.Value;

            MarkerManager.enabled = ConfigShowMarker.Value;
            CustomKickBanManager.enabled = ConfigCustomKickBan.Value;
            CustomKickBanManager.kickReason = ConfigKickReason.Value;
            CustomKickBanManager.banReason = ConfigBanReason.Value;

            CrosshairManager.enableDot = ConfigCrosshairDot.Value;
            CrosshairManager.dotSize = ConfigCrosshairDotSize.Value;
            CrosshairManager.gapSize = ConfigCrosshairGap.Value;
            CrosshairManager.length = ConfigCrosshairLength.Value;
            CrosshairManager.width = ConfigCrosshairWidth.Value;
            CrosshairManager.colorHtml = ConfigCrosshairColor.Value;

            CloudChatManager.EnableNotification = ConfigChatNotifications.Value;
            MenuManager.UIScale = ConfigUIScale.Value;

            WeaponSpawnerManager.IsActive = ConfigItemSpawner.Value;
            WeaponSpawnerManager.Multiplier = ConfigItemSpawnerMultiplier.Value;
            WeaponSpawnerManager.ShouldPickUp = ConfigAutoPickup.Value;
            WeaponSpawnerManager.IsInfinite = ConfigWeaponSpawnerInfinite.Value;
            WeaponAttackManager.enableKnockbackDeflect = ConfigKnockbackDeflect.Value;
            WeaponAttackManager.knockbackDeflectAngle = ConfigDeflectAngle.Value;
            WeaponAttackManager.knockbackMult = ConfigKnockbackMult.Value;
            WeaponAttackManager.enableKnockbackMult = ConfigKnockbackMultEnabled.Value;
            WeaponAttackManager.rapidFireDelay = ConfigRapidFireDelay.Value;
            WeaponAttackManager.enableInfiniteAmmo = ConfigInfAmmo.Value;
            WeaponAttackManager.enableNoCooldown = ConfigNoCD.Value;

            FreecamManager.enabled = ConfigFreecamEnabled.Value;
            BacktrackingManager.interval = ConfigBacktrackInterval.Value;
            BacktrackingManager.enabled = ConfigBacktrackingEnabled.Value;

            VisualsManager.enableTrail = ConfigEnableTrail.Value;
            if (ColorUtility.TryParseHtmlString(ConfigTrailColor.Value, out Color savedTrailColor))
                VisualsManager.TrailColor = savedTrailColor;

            VisualsManager.enableJumpRing = ConfigEnableJumpRing.Value;
            if (ColorUtility.TryParseHtmlString(ConfigJumpRingColor.Value, out Color savedRingColor))
                VisualsManager.JumpRingColor = savedRingColor;

            VisualsManager.enableWalkRing = ConfigEnableWalkRing.Value;
            if (ColorUtility.TryParseHtmlString(ConfigWalkRingColor.Value, out Color savedWalkRingColor))
                VisualsManager.WalkRingColor = savedWalkRingColor;

            AuraManager.enabled = ConfigEnableAura.Value;
            AuraManager.enableDrag = ConfigAuraDrag.Value;
            if (ColorUtility.TryParseHtmlString(ConfigAuraColor.Value, out Color savedAuraColor))
                AuraManager.AuraColor = savedAuraColor;

            BridgeESPManager.enabled = ConfigEnableBridgeESP.Value;
            ButtonESPManager.enabled = ConfigEnableButtonESP.Value;
            GlowESPManager.enabled = ConfigGlowESP.Value;
            GlowESPManager.currentMode = (GlowMode)ConfigGlowMode.Value;
            NetworkVisualsManager.ShareVisuals = ConfigShareVisuals.Value;

            ThirdPersonManager.enabled = ConfigThirdPerson.Value;
            ThirdPersonManager.cameraDistance = ConfigThirdPersonDist.Value;
            SpectatorPOVManager.enabled = ConfigSpectatorPOV.Value;
            FakeBoxManager.fakeBox = ConfigFakeBoxEnabled.Value;

            LavaPunchManager.enableLavaLegit = ConfigEnableLavaPunch.Value;
            LavaPunchManager.attackMode = ConfigLavaAttackMode.Value;
            LavaPunchManager.lockOnTarget = ConfigLavaLockOn.Value;
            LavaPunchManager.attackRange = ConfigLavaRange.Value;
            LavaPunchManager.attackCooldown = ConfigLavaCooldown.Value;
            LavaPunchManager.punchMultiplier = ConfigLavaMultiplier.Value;
            LavaPunchManager.antiHost = ConfigAntiHost.Value;
            LavaPunchManager.enableKillAura = ConfigKillAura.Value;

            FunnyScaleModule.LocalBaseScale = ConfigFunnyBaseScale.Value;
            FunnyScaleModule.LocalIntensity = ConfigFunnyIntensity.Value;
            FunnyScaleModule.LocalSpeed = ConfigFunnySpeed.Value;
            FunnyScaleModule._isLocalActive = ConfigFunnyScaleEnabled.Value;

            OrbitManager.radius = ConfigOrbitRadius.Value;
            OrbitManager.speed = ConfigOrbitSpeed.Value;
            OrbitManager.height = ConfigOrbitHeight.Value;
            OrbitManager.enabled = ConfigOrbitEnabled.Value;
            OrbitManager.autoCycle = ConfigOrbitAuto.Value;
            OrbitManager.cycleInterval = ConfigOrbitInterval.Value;

            CameraManager.EnableZoom = ConfigZoomEnabled.Value;
            CameraManager.ZoomKey = ConfigZoomKey.Value;
            CameraManager.ZoomFOV = ConfigZoomFOV.Value;
            CameraManager.OverrideFOV = ConfigOverrideFOV.Value;
            CameraManager.BaseFOV = ConfigBaseFOV.Value;

            WaterWalkManager.enabled = ConfigWaterWalkEnabled.Value;
            WaterWalkManager.floorOffset = ConfigWaterWalkOffset.Value;
            WaterJumpManager.enabled = ConfigWaterJumpEnabled.Value;
            WaterJumpManager.waterLevel = ConfigWaterJumpLevel.Value;
            WaterJumpManager.bouncePower = ConfigWaterJumpBouncePower.Value;

            HillGodManager.enabled = ConfigHillGodEnabled.Value;
            PlayerLauncherManager.enabled = ConfigPlayerLauncher.Value;
            GodModeManager.enabled = ConfigGodMode.Value;
            GodModeManager.autoRescue = ConfigGodModeRescue.Value;

            KeyDisplayManager.enabled = ConfigKeyDisplayEnabled.Value;
            KeyDisplayManager.posX = ConfigKeyDisplayX.Value;
            KeyDisplayManager.posY = ConfigKeyDisplayY.Value;

            BOTNotification.PosX = ConfigNotifX.Value;
            BOTNotification.PosY = ConfigNotifY.Value;
            BOTNotification.AnimSpeed = ConfigNotifSpeed.Value;
            BOTNotification.Duration = ConfigNotifDuration.Value;

            LegitESPManager.enabled = ConfigLegitESPEnabled.Value;
            if (ColorUtility.TryParseHtmlString(ConfigLegitESPColor.Value, out Color savedEspCol))
                LegitESPManager.espColor = savedEspCol;

            PlayerNoClipManager.enabled = ConfigPlayerNoClip.Value;
            PlayerNoClipManager.wallsEnabled = ConfigWallNoClipEnabled.Value;

            ItemSkinChangerManager.itemSkinChanger = ConfigSkinChangerEnabled.Value;
            ItemSkinChangerManager.itemSkinId = ConfigSkinTargetId.Value;
            TeleportManager.ignoreWalls = ConfigTeleportThroughWalls.Value;
            TeleportManager.enabled = ConfigClickTPMain.Value;

            SnowballManager.autoShoot = ConfigSnowAutoShoot.Value;
            SnowballManager.autoAim = ConfigSnowAutoAim.Value;
            SnowballManager.noShootDelay = ConfigSnowNoDelay.Value;
            SnowballManager.loopBoost = ConfigSnowLoop.Value;
            SnowballManager.autoPickup = ConfigSnowPickup.Value;

            GunMasterManager.autoAim = ConfigGunAutoAim.Value;
            GunMasterManager.autoShoot = ConfigGunAutoShoot.Value;
            GunMasterManager.aimThroughWalls = ConfigGunAimThroughWalls.Value;
            GunMasterManager.showFovCircle = ConfigFovCircleEnabled.Value;
            GunMasterManager.fovRadius = ConfigFovRadius.Value;

            ItemESPManager.enabled = ConfigItemESPEnabled.Value;
            AntiBoundManager.enabled = ConfigAntiVoidEnabled.Value;
            ClickRippleManager.IsActive = ConfigClickRipple.Value;
            CinematicGraphicsManager.isEnabled = ConfigCinematicFX.Value;
            BridgeHelperManager.noGlassBreak = ConfigGlassGod.Value;
            MiniGameTrollManager.rlglGodMode = ConfigRlglGod.Value;
            ForceKillManager.killAuraEnabled = ConfigSilentKill.Value;
            ForceKillManager.auraRange = ConfigSilentKillRange.Value;
            MirrorCamManager.enabled = ConfigMirrorCam.Value;

            VisualBlockManager.enabled = ConfigVisualBlock.Value;
            WeaponManager.Enabled = ConfigGibsonEnabled.Value;
            WeaponManager.PacketMode = ConfigGibsonMode.Value;
            WeaponManager.PacketSendDelay = ConfigGibsonDelay.Value;
            EnemyScaleManager.enabled = ConfigEnemyScaleEnabled.Value;
            EnemyScaleManager.scaleMultiplier = ConfigEnemyScaleMult.Value;
            HitEffectManager.enabled = ConfigHitHeartFX.Value;
            BodyRotationManager.IsActive = ConfigPoseEnabled.Value;
            BodyRotationManager.FixedAngle = ConfigPoseYaw.Value;
            BodyInverseManager.IsActive = ConfigDemonModeEnabled.Value;
            BodyInverseManager.targetPitch = ConfigDemonPitch.Value;
            BodyInverseManager.IsDynamic = ConfigDemonDynamicEnabled.Value;
            BodyInverseManager.DynamicSpeed = ConfigDemonDynamicSpeed.Value;
            BodyInverseManager.DynamicRange = ConfigDemonDynamicRange.Value;
            BodyInverseManager.Is360Spin = ConfigDemon360Enabled.Value;
            AutoAdvertiser.enabled = ConfigAutoAdEnabled.Value;
            CloudEndpoints.ChatBase = ConfigCloudChatApi != null ? ConfigCloudChatApi.Value : "";
            CloudEndpoints.ConfigBase = ConfigCloudConfigApi != null ? ConfigCloudConfigApi.Value : "";
            NetworkFreezeManager.IsEnabled = ConfigFreezeClientEnabled.Value;
            NetworkFreezeManager.FreezePower = ConfigFreezePower.Value;
            LipSyncPatch.Enabled = ConfigLipSyncEnabled.Value;
            ShadowModePatches.crashPlayerOnHit = ConfigCrashPlayer.Value;
            ShadowModePatches.respawnOnDeath = ConfigRespawnOnDeath.Value;
            ShadowModePatches.enableUncensoredChat = ConfigUncensoredChat.Value;
            ShadowModePatches.crashMode = ConfigCrashMode.Value;
            ShadowModePatches.enableStrongCrash = ConfigStrongCrash.Value;

            TagServerManager.WhitelistModeEnabled = ConfigWhitelistMode.Value;
            BotTabManager.enableTabColor = ConfigTabColorEnabled.Value;
            BotTabManager.tabColor = ConfigTabColorHex.Value;
            BOTActiveList.ShowBackground = ConfigActiveListBg.Value;
            BOTActiveList.ColorMode = ConfigActiveListColorMode.Value;
            DamageNotificationManager.enabled = ConfigDamageUI.Value;
            FullbrightManager.enabled = ConfigFullbright.Value;
            GalaxyBackground.enabled = ConfigGalaxyBgEnabled.Value;
            MenuThemeManager.ThemeIndex = ConfigThemeMode.Value;
            MenuThemeManager.BgAlpha = ConfigBgAlpha.Value;
            SmoothMovementManager.enabled = ConfigSmoothMovement.Value;
            PerfectMicManager.BgNoiseRemoval = ConfigBgNoiseRemoval.Value;
            PerfectMicManager.VoiceDuckLevel = ConfigVoiceDuckLevel.Value;
            ServerListViewerManager.ToggleKey = ConfigServerListKey.Value;

            CrosshairManager.ApplySettings();
        }

        public static void BroadcastIdentity()
        {
            if (!HasModNet || !ConfigIsNameSet.Value || SteamManager.Instance == null) return;
            ulong currentLobby = SteamManager.Instance.currentLobby.m_SteamID;
            if (currentLobby == 0 || currentLobby == _lastLobbyId) return;

            _lastLobbyId = currentLobby;
            Packet p = new Packet();
            p.Write(ConfigCustomName.Value);
            ModNet.SendMessage("BOT:UserJoin", p, Utility.GetAllLobbyMembers());
        }

        private void RegisterNetworkHandlers()
        {
            ModNet.RegisterMessageHandler("BOT:Marker", (senderId, packet) =>
            {
                ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
                if (senderId == myId) return;

                bool active = packet.ReadBool();
                int type = 1;
                try
                {
                    if (packet.UnreadLength() >= 4) type = packet.ReadInt();
                }
                catch { }

                MarkerManager.SetMarker(senderId, active, type);
            });
        }

        [HarmonyPatch(typeof(GameUIClass), "Awake")]
        [HarmonyPatch(typeof(MonoBehaviourPublicGalomeGacrsemiGaupObUnique), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(Component __instance)
        {
            if (__MenuObject != null && UnhollowerBaseLib.IL2CPP.Il2CppObjectBaseToPtr(__MenuObject) != IntPtr.Zero) return;

            SpawnUIObject<BOTStatusIsland>("BOT_StatusIsland", __instance.transform);
            SpawnUIObject<BOTActiveList>("BOT_ActiveList", __instance.transform);
            SpawnUIObject<SpectatorWarningManager>("BOT_SpectatorWarning", null);
            SpawnUIObject<ClickRippleManager>("BOT_ClickRipple", null);
            SpawnUIObject<AntroPort.SkinsMenu>("AntroSkinsSystem", null);
            SpawnUIObject<FakeBoxManager>("BOT_CombinedMod_Manager", null);
            SpawnUIObject<MusicPlayerManager>("SecretBOT_MusicManager", null);
            SpawnUIObject<AutoUpdatePump>("BOT_UpdatePump", null);

            KeybindManager.LoadBinds();

            GameObject pluginObj = new GameObject("BOTMenu_UI");
            pluginObj.transform.SetParent(__instance.transform, false);

            GameObject galaxyBg = new GameObject("BOT_GalaxyBackground");
            galaxyBg.transform.SetParent(pluginObj.transform, false);
            galaxyBg.transform.SetAsFirstSibling();
            galaxyBg.AddComponent<GalaxyBackground>();

            RectTransform rt = pluginObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900, 580);

            CanvasGroup cg = pluginObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.ignoreParentGroups = true;

            Image bg = pluginObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.98f);
            pluginObj.AddComponent<Draggable>();

            GameObject specInfoObj = new GameObject("SpectatorInfo_UI");
            specInfoObj.transform.SetParent(__instance.transform, false);
            RectTransform specRect = specInfoObj.AddComponent<RectTransform>();
            specRect.anchorMin = specRect.anchorMax = specRect.pivot = new Vector2(0.5f, 0f);
            specRect.anchoredPosition = new Vector2(0, 100);
            specRect.sizeDelta = new Vector2(800, 50);
            Text specText = specInfoObj.AddComponent<Text>();
            specText.font = GetUIFont();
            specText.fontSize = 24; specText.fontStyle = FontStyle.Bold;
            specText.alignment = TextAnchor.MiddleCenter; specText.color = Color.yellow;
            specText.supportRichText = true;
            specInfoObj.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            specInfoObj.SetActive(false);
            SpectatorPOVManager.SpectatorText = specText;

            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(pluginObj.transform, false);
            Text headerText = headerObj.AddComponent<Text>();
            headerText.font = GetUIFont();
            headerText.fontSize = 18; headerText.fontStyle = FontStyle.Bold;
            headerText.supportRichText = true; headerText.alignment = TextAnchor.UpperCenter;
            headerText.color = Color.white;
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1); headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1); headerRt.anchoredPosition = new Vector2(0, -10);
            headerRt.sizeDelta = new Vector2(-20, 80);
            headerObj.AddComponent<Outline>().effectDistance = new Vector2(1, -1);

            GameObject leftBgObj = new GameObject("LeftBG");
            leftBgObj.transform.SetParent(pluginObj.transform, false);
            Image leftBg = leftBgObj.AddComponent<Image>();
            leftBg.color = new Color(0.04f, 0.04f, 0.04f, 1f);
            RectTransform leftBgRt = leftBgObj.GetComponent<RectTransform>();
            leftBgRt.anchorMin = new Vector2(0, 0); leftBgRt.anchorMax = new Vector2(0, 1);
            leftBgRt.pivot = new Vector2(0, 0); leftBgRt.anchoredPosition = new Vector2(10, 10);
            leftBgRt.sizeDelta = new Vector2(240, -100);

            GameObject leftColObj = new GameObject("LeftColumn");
            leftColObj.transform.SetParent(pluginObj.transform, false);
            Text leftText = leftColObj.AddComponent<Text>();
            leftText.font = GetUIFont();
            leftText.fontSize = 18; leftText.fontStyle = FontStyle.Bold;
            leftText.supportRichText = true; leftText.alignment = TextAnchor.UpperLeft;
            leftText.horizontalOverflow = HorizontalWrapMode.Overflow;
            leftText.verticalOverflow = VerticalWrapMode.Overflow;
            leftText.color = Color.white;
            RectTransform leftRt = leftColObj.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0); leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0, 0); leftRt.anchoredPosition = new Vector2(20, 20);
            leftRt.sizeDelta = new Vector2(220, -110);
            leftColObj.AddComponent<Outline>().effectDistance = new Vector2(1, -1);

            GameObject rightColObj = new GameObject("RightColumn");
            rightColObj.transform.SetParent(pluginObj.transform, false);
            Text rightText = rightColObj.AddComponent<Text>();
            rightText.font = GetUIFont();
            rightText.fontSize = 18; rightText.fontStyle = FontStyle.Bold;
            rightText.supportRichText = true; rightText.alignment = TextAnchor.UpperLeft;
            rightText.horizontalOverflow = HorizontalWrapMode.Overflow;
            rightText.verticalOverflow = VerticalWrapMode.Overflow;
            rightText.lineSpacing = 1.15f; rightText.color = Color.white;
            RectTransform rightRt = rightColObj.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0, 0); rightRt.anchorMax = new Vector2(1, 1);
            rightRt.pivot = new Vector2(0, 0);
            rightRt.offsetMin = new Vector2(260, 20);
            rightRt.offsetMax = new Vector2(-20, -90);
            rightColObj.AddComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);

            pluginObj.AddComponent<MenuManager>();
            MenuManager.headerText = headerText;
            MenuManager.leftColText = leftText;
            MenuManager.rightColText = rightText;

            pluginObj.AddComponent<FunnyScaleModule>();
            pluginObj.AddComponent<WeaponAttackManager>();
            pluginObj.AddComponent<CameraManager>();
            pluginObj.AddComponent<BodyRotationManager>();
            pluginObj.AddComponent<BodyInverseManager>();
            pluginObj.AddComponent<ItemSkinChangerManager>();
            pluginObj.AddComponent<BlacklistManager>();
            pluginObj.AddComponent<ItemBrowserManager>();
            pluginObj.AddComponent<UltimateDropSpoofer>();

            __MenuObject = pluginObj;
            GameObject notifyObj = new GameObject("BOT_Notification");
            notifyObj.transform.SetParent(__instance.transform, false);
            BOTNotification.Instance = notifyObj.AddComponent<BOTNotification>();
            MenuSoundManager.Init(notifyObj.transform);
            BOTDisclaimerUI.Create(__instance.transform);
            BOTNameSetup.Create(__instance.transform);
            BroadcastIdentity();
        }

        private static string ResolveBotVersion()
        {
            try
            {
                object[] attrs = typeof(BOTPlugin).GetCustomAttributes(false);
                if (attrs != null)
                {
                    foreach (object attr in attrs)
                    {
                        if (attr == null) continue;
                        Type attrType = attr.GetType();
                        if (attrType.Name != "BepInPlugin") continue;
                        string ver = null;
                        var versionProp = attrType.GetProperty("Version");
                        if (versionProp != null) ver = versionProp.GetValue(attr, null) as string;
                        if (string.IsNullOrEmpty(ver))
                        {
                            var versionField = attrType.GetField("Version");
                            if (versionField != null) ver = versionField.GetValue(attr) as string;
                        }
                        if (!string.IsNullOrEmpty(ver)) return ver;
                    }
                }
            }
            catch { }
            return "3.15.0";
        }

        private static Font GetUIFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            }
            if (font == null)
            {
                try
                {
                    var allFonts = Resources.FindObjectsOfTypeAll(Il2CppType.Of<Font>());
                    foreach (var o in allFonts)
                    {
                        var f = o.TryCast<Font>();
                        if (f != null) { font = f; break; }
                    }
                }
                catch { }
            }
            return font;
        }

        private static void SpawnUIObject<T>(string name, Transform parent) where T : MonoBehaviour
        {
            if (GameObject.Find(name) != null) return;
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            else UnityEngine.Object.DontDestroyOnLoad(obj);
            obj.AddComponent<T>();
        }
    }
}
