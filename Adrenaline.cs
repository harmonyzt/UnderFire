using EFT;
using EFT.HealthSystem;
using EFTEffectManager;

namespace UnderFire
{
    public interface IAdrenaline : IHealthEffect
    {
    }

    public class Adrenaline : ActiveHealthController.Effect, IAdrenaline, IEffectTriggersUIPanel
    {
        private const float BurstStamina = 50f;

        private float _originalStaminaRestoreRate;
        private bool _applied;
        private bool _reverted;

        public override float DefaultWorkTime => Plugin.DurationSecs.Value;

        public override void Started()
        {
            Player player = EffectsManager.RaidPlayer;
            ActiveHealthController controller = HealthController;
            if (player == null || controller == null || !controller.IsAlive)
            {
                return;
            }

            float duration = Plugin.DurationSecs.Value;

            // After effects
            if (Plugin.TunnelVisionEnabled.Value)
            {
                controller.AddEffect<ActiveHealthController.TunnelVision>(EBodyPart.Head, delayTime: 0.5f,
                    workTime: duration, residueTime: 10f);
            }

            if (Plugin.PainKillerEnabled.Value)
            {
                controller.AddEffect<ActiveHealthController.PainKiller>(EBodyPart.Head, delayTime: 0.5f,
                    workTime: duration / 2f, residueTime: 10f);
            }

            if (Plugin.TremorEnabled.Value)
            {
                controller.AddEffect<ActiveHealthController.Tremor>(EBodyPart.Head, delayTime: 0.5f,
                    workTime: duration, residueTime: 10f);
            }

            if (Plugin.StaminaRestoreInc.Value > 0)
            {
                _originalStaminaRestoreRate =
                    EffectsManager.AddStaminaRestoreRate(player, Plugin.StaminaRestoreInc.Value);
                _applied = true;
            }

            if (Plugin.StaminaBurstEnabled.Value)
            {
                EffectsManager.AddStamina(player, BurstStamina);
            }
        }

        public override void Residue() => RevertStamina();

        public override void Removed() => RevertStamina();

        private void RevertStamina()
        {
            if (!_applied || _reverted)
            {
                return;
            }

            _reverted = true;
            EffectsManager.SetStaminaRestoreRate(EffectsManager.RaidPlayer, _originalStaminaRestoreRate);
        }
    }
}