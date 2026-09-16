using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    /// <summary>
    /// A named group of characters — what the Shop's Characters tab draws a header over (#2048).
    /// Deliberately its own asset rather than a <see cref="BrandCollection"/>: characters have no
    /// brand, and "brand collection" is the wrong word for "the police uniforms". The player only
    /// ever sees <see cref="Name"/> on a header. Additive to the modder-facing Core surface; a
    /// <see cref="CharacterDefinition"/> without one simply lands in the trailing "Others" group.
    /// </summary>
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Character Collection", menuName = "Fireworks Mania/Definitions/Character Collection")]
#endif
    public class CharacterCollection : ScriptableObject, IItemCollection
    {
        [Tooltip("Global unique definition id for this character collection.")]
        [SerializeField]
        private string _id;

        [Tooltip("Human readable name of this collection - the text on the group header.")]
        [SerializeField]
        private string _name;

        [Tooltip("Optional logo shown in front of the name on the group header.")]
        [SerializeField]
        private Sprite _icon;

        [ContextMenu("Set Id to filename")]
        private void SetIdToFilename()
        {
            this._id = this.name;
        }

        public string Id   => _id;
        public string Name => _name;
        public Sprite Icon => _icon;
    }
}
