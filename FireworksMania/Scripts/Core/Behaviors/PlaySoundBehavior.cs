using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/PlaySoundBehavior")]
    public class PlaySoundBehavior : MonoBehaviour
    {
        [SerializeField]
        [GameSound]
        private string _sound;

        [SerializeField]
        private bool _playOnStart = false;

        private void Start()
        {
            if (_playOnStart)
                PlaySound();
        }

        public void PlaySound()
        {
            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform));
        }

        public void StopSound()
        {
            Messenger.Broadcast(new MessengerEventStopSound(_sound, this.transform));
        }
    }
}
