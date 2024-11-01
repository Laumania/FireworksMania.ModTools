using System;
using FireworksMania.Core.Attributes;
using UnityEngine;
using Messenger = FireworksMania.Core.Messaging.Messenger;
using MessengerEventPlaySoundAtVector3 = FireworksMania.Core.Messaging.MessengerEventPlaySoundAtVector3;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemSound")]
    public class ParticleSystemSound : MonoBehaviour
    {
        [Header("Spawn Sound")]
        [GameSound]
        [SerializeField]
        [Tooltip("Sound played for each <color=green>spawned</color> particle in the ParticleSystem")]
        private string _particleSpawnedSound;
        
        [SerializeField]
        [Tooltip("Will only play the sound once and only at the first event. This is useful if the ParticleSystem spawns a lot of particles and you only want to play a single sound.")]
        private bool _playSingleSpawnSound = false;


        [Header("Destroy Sound")]
        [GameSound]
        [SerializeField]
        [Tooltip("Sound played for each <color=red>destroyed / death</color> particle in the ParticleSystem")]
        private string _particleDestroyedSound;

        [SerializeField]
        [Tooltip("Will only play the sound once and only at the first event. This is useful if the ParticleSystem spawns a lot of particles and you only want to play a single sound.")]
        private bool _playSingleDestroySound = false;


        private ParticleSystemObserver _particleObserver;
        protected const string _soundGroupNoneValue = "[None]";

        private bool _havePlayedDestroySound = false;
        private bool _havePlayedSpawnSound   = false;

        protected virtual void Awake()
        {
            _particleObserver = this.GetComponent<ParticleSystemObserver>();
            if (_particleObserver == null)
                Debug.LogError($"Missing {nameof(ParticleSystemObserver)} on {nameof(ParticleSystemSound)}", this);

            if (String.IsNullOrEmpty(_particleSpawnedSound))
                _particleSpawnedSound = _soundGroupNoneValue;

            if (String.IsNullOrEmpty(_particleDestroyedSound))
                _particleDestroyedSound = _soundGroupNoneValue;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this.GetComponent<ParticleSystemObserver>() == null)
                Debug.LogError($"{nameof(ParticleSystemSound)} is missing ParticleSystemObserver on '{this.gameObject.name}' else it will not work", this);
        }

        private void OnEnable()
        {
            if (_particleSpawnedSound != _soundGroupNoneValue)
                _particleObserver.OnParticleSpawned += PlaySpawnedSound;

            if (_particleDestroyedSound != _soundGroupNoneValue)
                _particleObserver.OnParticleDestroyed += PlayDestroyedSound;
        }

        private void OnDisable()
        {
            _particleObserver.OnParticleSpawned -= PlaySpawnedSound;
            _particleObserver.OnParticleDestroyed -= PlayDestroyedSound;
        }

        protected virtual void PlayDestroyedSound(Vector3 particlePosition)
        {
            if (_playSingleDestroySound && _havePlayedDestroySound)
                return;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_particleDestroyedSound, particlePosition, delayBasedOnDistanceToListener: true));

            _havePlayedDestroySound = true;
        }

        protected virtual void PlaySpawnedSound(Vector3 particlePosition)
        {
            if (_playSingleSpawnSound && _havePlayedSpawnSound)
                return;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_particleSpawnedSound, particlePosition, delayBasedOnDistanceToListener: true));

            _havePlayedSpawnSound = true;
        }
    }
}
