using UnityEngine;

namespace FireworksMania.Core.Attributes
{
    public class GameSoundAttribute : PropertyAttribute
    {
        public const string SoundGroupNoneValue = "[None]";

        /// <summary>
        /// True when the field says "no sound here". <see cref="SoundGroupNoneValue"/> is what the picker
        /// writes when someone deliberately chooses none, and an untouched field is simply empty - both mean
        /// the same thing, and neither is a sound group name, so nothing downstream should go looking for it.
        /// </summary>
        public static bool IsNoSound(string soundGroupName)
        {
            return string.IsNullOrWhiteSpace(soundGroupName) || soundGroupName == SoundGroupNoneValue;
        }
    }
}
