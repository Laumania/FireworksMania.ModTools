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

        private float velocityThreshold = .5f;

        private void OnCollisionEnter(Collision collision)
        {
            if (this.enabled)
            {
                if (collision.impulse.magnitude > velocityThreshold)
                {
                    PlaySingleImpactSound();
                }
            }
        }

        public void PlaySingleImpactSound()
        {
            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform));
        }
    }
}
