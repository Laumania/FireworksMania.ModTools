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
        [Tooltip("This will make the sound follow the transform. Only set to true if needed, as there is a performance cost of having the sound follow the transform. Typically if you have a short sound you don't need it to follow the transform as it will just be played at the position of the transform.")]
        [SerializeField]
        private bool _followTransform = false;

        private void Start()
        {
            if (_playOnStart)
                PlaySound();
        }

        public void PlaySound()
        {
            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform, followTransform: _followTransform));
        }

        private void OnDestroy()
        {
            StopSound();
        }

        public void StopSound()
        {
            Messenger.Broadcast(new MessengerEventStopSound(_sound, this.transform));
        }
    }
}
