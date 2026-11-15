using FireworksMania.Core.Attributes;
using UnityEngine;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    /// <summary>
    /// What a firework is classified by: the size or class the inventory and Shop list under each
    /// category, so a player can narrow, say, Cakes down to 25mm (#1497). Shells and mortars are
    /// classified by <see cref="EntityDiameterDefinition"/>, cakes and single shots by
    /// <see cref="EntityCaliberDefinition"/>, and everything else by <see cref="EntityClassDefinition"/>.
    /// <para>
    /// The base game ships one asset per value (3", 25mm, F3 ...) and every firework references
    /// one of them, so two items share a classification by sharing an asset.
    /// </para>
    /// </summary>
    public abstract class EntityClassificationDefinition : ScriptableObject
    {
        //Moved here from EntityDiameterDefinition (#1497). Unity reads serialized fields by name, so
        //existing diameter assets - the game's and those inside mods - keep their id.
        [SerializeField]
        [ReadOnly]
        [Tooltip("IMPORTANT: Do not change this id after it have initially been set to avoid breaking references")]
        private string _id;

        [ContextMenu("Generate new unique id (based on file name)")]
        private void GenerateIdFromName()
        {
            this._id = this.name;
        }

        public string Id => _id;

        /// <summary>The text on the row that lists it, e.g. 3", 25mm or F3.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Rows of the same kind of classification are listed in ascending order of this.</summary>
        public abstract float SortValue { get; }
    }
}
