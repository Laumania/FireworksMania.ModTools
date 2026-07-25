using System;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
using UnityEngine;

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

        private bool _havePlayedDestroySound = false;
        private bool _havePlayedSpawnSound   = false;

        protected virtual void Awake()
        {
            _particleObserver = this.GetComponent<ParticleSystemObserver>();
            Preconditions.CheckNotNull(_particleObserver, $"Missing {nameof(ParticleSystemObserver)} on {nameof(ParticleSystemSound)}", this);
            
            if (String.IsNullOrEmpty(_particleSpawnedSound))
                _particleSpawnedSound = GameSoundAttribute.SoundGroupNoneValue;

            if (String.IsNullOrEmpty(_particleDestroyedSound))
                _particleDestroyedSound = GameSoundAttribute.SoundGroupNoneValue;
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
            if(_particleObserver == null)
                return;

            if (_particleSpawnedSound != GameSoundAttribute.SoundGroupNoneValue)
                _particleObserver.OnParticleSpawned += PlaySpawnedSound;

            if (_particleDestroyedSound != GameSoundAttribute.SoundGroupNoneValue)
                _particleObserver.OnParticleDestroyed += PlayDestroyedSound;
        }

        private void OnDisable()
        {
            if (_particleObserver == null)
                return;

            _particleObserver.OnParticleSpawned -= PlaySpawnedSound;
            _particleObserver.OnParticleDestroyed -= PlayDestroyedSound;
        }

        protected virtual void PlayDestroyedSound(Vector3 particlePosition)
        {
            if (_playSingleDestroySound && _havePlayedDestroySound)
                return;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(_particleDestroyedSound, particlePosition, delayBasedOnDistanceToListener: true));

            _havePlayedDestroySound = true;
        }

        protected virtual void PlaySpawnedSound(Vector3 particlePosition)
        {
            if (_playSingleSpawnSound && _havePlayedSpawnSound)
                return;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(_particleSpawnedSound, particlePosition, delayBasedOnDistanceToListener: true));

            _havePlayedSpawnSound = true;
        }
    }
}
