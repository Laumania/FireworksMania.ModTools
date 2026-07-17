using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Game Sound Name Collection", menuName = "Fireworks Mania/Definitions/Internal/Game Sound Name Collection")]
#endif
    public class GameSoundNameCollection : ScriptableObject
    {
        //Note: The InfoBox sits on a dummy field because Unity 6 applies field attributes on List<> fields
        //to the list ELEMENTS, so an InfoBox directly on _sounds never renders.
        [InfoBox("List of sound names modders can pick from in the 'GameSound' dropdown in the Mod Tools. " +
                 "It's just strings - the actual sounds live in GameSoundDefinition assets. " +
                 "Repopulate via the context menu (three dots in top right corner) -> 'Populate' after adding/removing GameSoundDefinitions, so modders see an up-to-date list.")]
        [SerializeField, ReadOnly]
        private bool _info;

        [SerializeField]
        private List<string> _sounds;

        public List<string> Sounds => _sounds;
    }
}
