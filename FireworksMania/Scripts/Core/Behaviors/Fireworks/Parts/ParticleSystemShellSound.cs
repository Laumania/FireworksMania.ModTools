using System;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemShellSound")]
    public class ParticleSystemShellSound : ParticleSystemSound
    {
        [Header("Spawn Sound (In Mortar)")]
        [GameSound]
        [SerializeField]
        [Tooltip("Sound played for each <color=green>spawned</color> particle in the ParticleSystem - when loaded into a MortarTube. This script is specifically made for Shells.")]
        private string _particleSpawnedInMortarSound;

        [SerializeField]
        [Tooltip("Will only play the sound once and only at the first event. This is useful if the ParticleSystem spawns a lot of particles and you only want to play a single sound.")]
        private bool _playSingleSpawnedInMortarSound = false;

        private bool _havePlayedSpawnInMortarSound = false;

        protected override void Awake()
        {
            base.Awake();

            if (String.IsNullOrEmpty(_particleSpawnedInMortarSound))
                _particleSpawnedInMortarSound = _soundGroupNoneValue;
        }

        protected override void PlaySpawnedSound(Vector3 particlePosition)
        {
            if (IsInMortarTube)
            {
                if (_playSingleSpawnedInMortarSound && _havePlayedSpawnInMortarSound)
                    return;

                Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_particleSpawnedInMortarSound, particlePosition, delayBasedOnDistanceToListener: true));

                _havePlayedSpawnInMortarSound = true;
            }
            else
                base.PlaySpawnedSound(particlePosition);
        }

        public bool IsInMortarTube { get; set; } = false;
    }
}
