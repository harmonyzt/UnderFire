using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.HealthSystem;
using EFTEffectManager;
using UnityEngine;

namespace UnderFire
{
    [BepInPlugin("com.harmonyzt.underfire", "Under Fire", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logSource;

        internal static ConfigEntry<bool> SuppressEnabled { get; set; }
        internal static ConfigEntry<bool> HitEnabled { get; set; }
        internal static ConfigEntry<int> SuppressDistance { get; set; }
        internal static ConfigEntry<int> SnipeDistance { get; set; }
        internal static ConfigEntry<float> DurationSecs { get; set; }
        internal static ConfigEntry<float> CooldownSecs { get; set; }
        internal static ConfigEntry<bool> TunnelVisionEnabled { get; set; }
        internal static ConfigEntry<bool> PainKillerEnabled { get; set; }
        internal static ConfigEntry<bool> TremorEnabled { get; set; }
        internal static ConfigEntry<bool> StaminaBurstEnabled { get; set; }
        internal static ConfigEntry<int> StaminaRestoreInc { get; set; }

        private static EffectRegistration _adrenaline;
        private static float _lastAdrenaline = float.NegativeInfinity;

        private void SetupConfigOptions()
        {
            string mainSetting = "1. Main Settings";
            ConfigDefinition enableOnSuppressDef =
                new ConfigDefinition(mainSetting, "Enable Adrenaline On Suppression");
            ConfigDescription enableOnSuppressDesc = new ConfigDescription(
                "Enables Adrenaline when the player is being suppressed (shot at).",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 10 });
            ConfigDefinition enableOnHitDef = new ConfigDefinition(mainSetting, "Enable Adrenaline On Hit");
            ConfigDescription enableOnHitDesc = new ConfigDescription("Enables Adrenaline when the player is shot."
                , null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 9 });

            string suppSetting = "2. Suppression Settings";
            ConfigDefinition suppressDistanceDef =
                new ConfigDefinition(suppSetting, "Max Suppression Distance for Bullets in Meters");
            ConfigDescription suppressDistanceDesc = new ConfigDescription(
                "How close the bullets have to fly by in order for the Adrenaline to activate from the suppression effect (a value of 5 means the bullets have to fly within 5 meters for suppression to activate).",
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 8 });
            ConfigDefinition snipeDistanceDef = new ConfigDefinition(suppSetting, "Max Shooter Distance in Meters");
            ConfigDescription snipeDistanceDesc = new ConfigDescription(
                "How close the shooter has to be in order for the Adrenaline to activate from the suppression effect (this is useful if you don't like being suppressed from snipers).",
                new AcceptableValueRange<int>(0, 300),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 7 });

            string effectSetting = "3. Adrenaline Effects Settings";
            ConfigDefinition adrenalineDurationDef =
                new ConfigDefinition(effectSetting, "Duration of Adrenaline Effect in Secs");
            ConfigDescription adrenalineDurationDesc = new ConfigDescription(
                "How long the effects of adrenaline lasts.",
                new AcceptableValueRange<float>(0f, 1200f),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 6 });
            ConfigDefinition adrenalineCooldownDef =
                new ConfigDefinition(effectSetting, "Cooldown of Adrenaline Effect in Secs");
            ConfigDescription adrenalineCooldownDesc = new ConfigDescription(
                "How long after the adrenaline effect is triggered before it can be triggered again.",
                new AcceptableValueRange<float>(0f, 1200f),
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 5 });
            ConfigDefinition tunnelVisionEnableDef = new ConfigDefinition(effectSetting, "Enable Tunnel Vision Effect");
            ConfigDescription tunnelVisionEnableDesc = new ConfigDescription(
                "Enables the tunnel vision effect when Adrenaline is activated.",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 });
            ConfigDefinition painKillerEnableDef = new ConfigDefinition(effectSetting, "Enable Pain Killer Effect");
            ConfigDescription painKillerEnableDesc = new ConfigDescription(
                "Enables the pain killer effect when Adrenaline is activated (it lasts for Duration / 2).",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 });
            ConfigDefinition tremorEnableDef = new ConfigDefinition(effectSetting, "Enable Tremor Effect");
            ConfigDescription tremorEnableDesc = new ConfigDescription(
                "Enables the tremor effect when Adrenaline is activated.",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 });
            ConfigDefinition staminaBurstEnableDef = new ConfigDefinition(effectSetting, "Enable Stamina Burst Effect");
            ConfigDescription staminaBurstEnableDesc = new ConfigDescription(
                "Enables the stamina burst effect (this adds some stamina) when Adrenaline is activated.",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 });
            ConfigDefinition staminaRestoreIncDef =
                new ConfigDefinition(effectSetting, "Stamina Restore Rate Increase");
            ConfigDescription staminaRestoreIncDesc = new ConfigDescription(
                "The rate of stamina points to increase the restore rate by (set to 0 to disable).",
                new AcceptableValueRange<int>(0, 20),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 });

            SuppressEnabled = Config.Bind(enableOnSuppressDef, true, enableOnSuppressDesc);
            HitEnabled = Config.Bind(enableOnHitDef, false, enableOnHitDesc);
            SuppressDistance = Config.Bind(suppressDistanceDef, 5, suppressDistanceDesc);
            SnipeDistance = Config.Bind(snipeDistanceDef, 80, snipeDistanceDesc);
            DurationSecs = Config.Bind(adrenalineDurationDef, 60f, adrenalineDurationDesc);
            CooldownSecs = Config.Bind(adrenalineCooldownDef, 180f, adrenalineCooldownDesc);
            TunnelVisionEnabled = Config.Bind(tunnelVisionEnableDef, true, tunnelVisionEnableDesc);
            PainKillerEnabled = Config.Bind(painKillerEnableDef, true, painKillerEnableDesc);
            TremorEnabled = Config.Bind(tremorEnableDef, true, tremorEnableDesc);
            StaminaBurstEnabled = Config.Bind(staminaBurstEnableDef, true, staminaBurstEnableDesc);
            StaminaRestoreInc = Config.Bind(staminaRestoreIncDef, 2, staminaRestoreIncDesc);
        }

        private void Awake()
        {
            logSource = Logger;
            SetupConfigOptions();

            _adrenaline = EffectsManager.RegisterEffect<Adrenaline>(typeof(Plugin).Assembly, new EffectOptions
            {
                DisplayName = "Adrenaline",
                IconFilePath = "Adrenaline.png",
                NetworkEffectType = typeof(Network.Adrenaline),
                PersistAcrossRaids = false,
            });

            EffectsManager.OnHit += OnHit;
            EffectsManager.OnBulletWhizz += OnBulletWhizz;
        }

        private static void OnHit(HitInfo hit)
        {
            if (!HitEnabled.Value) return;
            if (!hit.IsBallistic || hit.Damage <= 0f) return;

            TryStartAdrenaline();
        }

        private static void OnBulletWhizz(WhizzInfo whizz)
        {
            if (!SuppressEnabled.Value) return;
            if (whizz.ShooterDistance > SnipeDistance.Value) return; // dont do adrenaline for sniping
            if (whizz.Distance >= SuppressDistance.Value) return;

            TryStartAdrenaline();
        }

        private static void TryStartAdrenaline()
        {
            Player player = EffectsManager.RaidPlayer;
            ActiveHealthController controller = player.ActiveHealthController;

            if (!EffectsManager.IsLocalController(controller)) return;
            if (Time.time - _lastAdrenaline < CooldownSecs.Value) return;
            if (EffectsManager.HasEffect<Adrenaline>(controller)) return;

            _lastAdrenaline = Time.time;
            _adrenaline.Add(controller, EBodyPart.Head);
        }
    }
}