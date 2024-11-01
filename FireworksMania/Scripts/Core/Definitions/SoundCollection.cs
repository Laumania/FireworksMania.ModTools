using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Sound Collection", menuName = "Fireworks Mania/Definitions/Internal/Sound Collection")]
#endif
    public class SoundCollection : ScriptableObject
    {
        [SerializeField]
        private List<string> _sounds;

        public List<string> Sounds => _sounds;
    }
}
