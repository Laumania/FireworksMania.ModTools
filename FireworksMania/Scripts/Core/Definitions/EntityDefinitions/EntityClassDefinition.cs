using UnityEngine;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    /// <summary>
    /// The F-class of a firework - F2, F3 or F4 - for everything that is not classified by its
    /// diameter or caliber: rockets, firecrackers, roman candles, fountains, smoke and novelties
    /// (#1497). Reference one of the base game's assets rather than making your own.
    /// </summary>
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Entity Class Definition", menuName = "Fireworks Mania/Definitions/Internal/Entity Class Definition")]
#endif
    public class EntityClassDefinition : EntityClassificationDefinition
    {
        [SerializeField]
        [Tooltip("The number after the F: 3 for F3")]
        private int _class;

        /// <summary>
        /// The number after the F: 3 for F3
        /// </summary>
        public int Class => _class;

        public override string DisplayName => "F" + _class;

        public override float SortValue => _class;
    }
}
