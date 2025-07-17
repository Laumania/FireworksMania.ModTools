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

        private void OnCollisionEnter(Collision collision)
        {
            if (this.enabled)
            {
                if (Time.timeAsDouble - _lastImpactTime >= PLAY_SOUND_COOLDOWN && collision.impulse.magnitude > velocityThreshold)
                {
                    PlaySingleImpactSound();
                    _lastImpactTime = Time.timeAsDouble;
                }
            }
        }

        public void PlaySingleImpactSound()
        {
            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform));
        }
    }
}
