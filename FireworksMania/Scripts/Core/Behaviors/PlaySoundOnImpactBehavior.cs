using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/PlaySoundOnImpactBehavior")]
    public class PlaySoundOnImpactBehavior : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("ImpactSound")]
        [GameSound]
        private string _sound;

        private float velocityThreshold          = .5f;
        private const double PLAY_SOUND_COOLDOWN = 0.3f; // Cooldown to prevent playing the sound too often
        private double _lastImpactTime           = 0f;
        private float  _velocityThresholdSqr;

        private MessengerEventPlaySoundStruct _playSoundEvent;

        private void Awake()
        {
            _velocityThresholdSqr = velocityThreshold * velocityThreshold;
            _playSoundEvent       = new MessengerEventPlaySoundStruct(_sound, this.transform);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!this.enabled)
                return;

            var now = Time.timeAsDouble;
            if (now - _lastImpactTime < PLAY_SOUND_COOLDOWN)
                return;

            if (collision.impulse.sqrMagnitude > _velocityThresholdSqr)
            {
                PlaySingleImpactSound();
                _lastImpactTime = now;
            }
        }

        public void PlaySingleImpactSound()
        {
            Messenger.Broadcast(_playSoundEvent);
        }
    }
}
