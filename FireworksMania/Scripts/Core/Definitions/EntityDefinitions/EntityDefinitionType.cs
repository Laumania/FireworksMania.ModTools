using FireworksMania.Core.Attributes;
using UnityEngine;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    [CreateAssetMenu(fileName = "New Entity Definition Type", menuName = "Fireworks Mania/Definitions/Internal/Entity Definition Type")]
    public class EntityDefinitionType : ScriptableObject
    {
        [SerializeField]
        [ReadOnly]
        [Tooltip("IMPORTANT: Do not change this id after it have initially been set to avoid breaking references")]
        private string _id;

        //Todo: Figure out a way to get a localized name here
        //public LocalizedString Name;

        [ContextMenu("Generate new unique id (based on file name)")]
        private void GenerateIdFromName()
        {
            this._id = this.name;
        }


        public string Id => _id;
    }
}
