using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New Game Sound", menuName = "Fireworks Mania/Definitions/Game Sound Definition")]
    public class GameSoundDefinition : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Typically only one clip is added with the actual sound. However, in some cases it adds a lot to the experience of the sound if it is not always the exact same playing. This is were variations comes in. As an example, you have recorded the same type of rocket explode 3 times. Even though the rocket is the same type, the sound will be slightly different.")]
        [SerializeField]
        private AudioClip[] _audioVariationClips;

        [SerializeField]
        [Range(0f, 1f)]
        private float _volume = 1f;

        [Header("Distance")]
        [SerializeField]
        [Tooltip("If the player is closer to the sound than this value (in meters), the sound will not be lowered.")]
        private int _minDistance = 0;

        [SerializeField]
        [Tooltip("If the player is further away than this value (in meters) the sound will not be heard.")]
        private int _maxDistance = 100;

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


        public IEnumerable<AudioClip> AudioVariationClips => _audioVariationClips;
        public int MinDistance                            => _minDistance;
        public int MaxDistance                            => _maxDistance;
        public float FadeInTime                           => _fadeInTime;
        public float FadeOutTime                          => _fadeOutTime;
        public float RandomPitchMin                       => _randomPitchMin;
        public float RandomPitchMax                       => _randomPitchMax;
        public float Volume                               => _volume;
    }
}
