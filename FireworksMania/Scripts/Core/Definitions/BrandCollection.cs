using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New Brand Collection Definition", menuName = "Fireworks Mania/Definitions/Brand Collection Definition")]
    public class BrandCollection : ScriptableObject
    {
        [Tooltip("Global unique definition id for this brand collection definition.")]
        [SerializeField]
        private string _id;

        [Tooltip("Human readable name of this brand collection.")]
        [SerializeField]
        private string _name;

        [Tooltip("Description of the collection.")]
        [SerializeField]
        [Multiline]
        private string _description;

        [Tooltip("Icon representing this brand collection.")]
        [SerializeField]
        private Sprite _icon;

        [Tooltip("Brand definition this collection belongs to.")]
        [SerializeField]
        private BrandDefinition _brandDefinition;

        [ContextMenu("Set Id to filename")]
        private void SetIdToFilename()
        {
            this._id = this.name;
        }

        public string Id => _id;
        public string Name => _name;
        public Sprite Icon => _icon;
        public BrandDefinition BrandDefinition => _brandDefinition;
        public string Description => _description;
    }
}
