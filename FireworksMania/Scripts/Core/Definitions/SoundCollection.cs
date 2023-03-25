using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New Sound Collection", menuName = "Fireworks Mania/Definitions/Internal/Sound Collection")]
    public class SoundCollection : ScriptableObject
    {
        [SerializeField]
        private List<string> _sounds;

        public List<string> Sounds => _sounds;
    }
}
