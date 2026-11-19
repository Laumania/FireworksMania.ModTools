using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    //Internal (#3434): a brand is only ever reached through a BrandCollection, which modders no longer see
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Brand Definition", menuName = "Fireworks Mania/Definitions/Brand Definition")]
#endif
    public class BrandDefinition : ScriptableObject
    {
        [Tooltip("Global unique definition id for this brand definition.")]
        [SerializeField]
        private string _id;

        [Tooltip("Human readable name of this brand.")]
        [SerializeField]
        private string _name;

        [Tooltip("Icon representing this brand.")]
        [SerializeField]
        private Sprite _icon;

        [ContextMenu("Set Id to filename")]
        private void SetIdToFilename()
        {
            this._id = this.name;
        }

        public string Id => _id;
        public string Name => _name;
        public Sprite Icon => _icon;
    }
}
