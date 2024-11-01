using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    public enum SoundBusGroups
    {
        Default    = 3,
        Ambient    = 0,
        UI         = 1,
        Explosion  = 2
    }

    [CreateAssetMenu(fileName = "New Game Sound", menuName = "Fireworks Mania/Definitions/Game Sound Definition")]
    public class GameSoundDefinition : ScriptableObject
    {
        [Header("General")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField]
        private bool _loop = false;

        [SerializeField]
        [Tooltip("Determine the type of sound. Default: Used for most sounds, Ambient: Used for ambient sounds and are forced to be 2D, UI: Used for UI sounds and are forced to be 2D, Explosions: Used for loud explosions as sounds of this type will duck other sounds for a short while to emphasize how loud it is.")]
        private SoundBusGroups _soundBus = SoundBusGroups.Default;

        [Header("Audio")]
        [Tooltip("Typically only one clip is added with the actual sound. However, in some cases it adds a lot to the experience of the sound if it is not always the exact same playing. This is were variations comes in. As an example, you have recorded the same type of rocket explode 3 times. Even though the rocket is the same type, the sound will be slightly different.")]
        [SerializeField]
        private AudioClip[] _audioVariationClips;

        [Header("Distance")]
        [SerializeField]
        [Tooltip("If the player is closer to the sound than this value (in meters), the sound will not be lowered.")]
        private float _minDistance = 0;

        [SerializeField]
        [Tooltip("If the player is further away than this value (in meters) the sound will not be heard.")]
        private float _maxDistance = 100;

        [Header("Custom Fade")]
        [SerializeField]
        [Tooltip("Fade time in seconds for when the audio is played.")]
        [Range(0f, 10f)]
        private float _fadeInTime = 0f;

        [SerializeField]
        [Tooltip("Fade time in seconds for when the audio is stopped.")]
        [Range(0f, 10f)]
        private float _fadeOutTime = 0f;


        [Header("Custom Pitch")]
        [SerializeField]
        [Tooltip("The minimum random pitch.")]
        [Range(-3f, 3f)]
        private float _randomPitchMin = -0.1f;

        [SerializeField]
        [Tooltip("The maximum random pitch.")]
        [Range(-3f, 3f)]
        private float _randomPitchMax = 0.1f;


#if !UNITY_EDITOR
        public IEnumerable<AudioClip> AudioVariationClips   => _audioVariationClips;
        public float MinDistance                            => _minDistance;
        public float MaxDistance                            => _maxDistance;
        public float FadeInTime                             => _fadeInTime;
        public float FadeOutTime                            => _fadeOutTime;
        public float RandomPitchMin                         => _randomPitchMin;
        public float RandomPitchMax                         => _randomPitchMax;
        public float Volume                                 => _volume;
        public bool Loop                                    => _loop;
        public SoundBusGroups SoundBus                      => _soundBus;
#else
        public AudioClip[] AudioVariationClips
        {
            get => _audioVariationClips;
            set => _audioVariationClips = value;
        }

        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        public float FadeInTime
        {
            get => _fadeInTime;
            set => _fadeInTime = value;
        }

        public float FadeOutTime
        {
            get => _fadeOutTime;
            set => _fadeOutTime = value;
        }

        public float RandomPitchMin
        {
            get => _randomPitchMin;
            set => _randomPitchMin = value;
        }

        public float RandomPitchMax
        {
            get => _randomPitchMax;
            set => _randomPitchMax = value;
        }

        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }

        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        public SoundBusGroups SoundBus
        {
            get => _soundBus;
            set => _soundBus = value;
        }
#endif
    }
}
