using FireworksMania.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Entity Diameter Definition", menuName = "Fireworks Mania/Definitions/Internal/Entity Diameter Definition")]
#endif
    public class EntityDiameterDefinition : ScriptableObject
    {
        [SerializeField]
        [ReadOnly]
        [Tooltip("IMPORTANT: Do not change this id after it have initially been set to avoid breaking references")]
        private string _id;

        [SerializeField]
        [Tooltip("Diameter in inches")]
        private float _diameter;

        //Todo: Figure out a way to get a localized name here
        //public LocalizedString Name;

        [ContextMenu("Generate new unique id (based on file name)")]
        private void GenerateIdFromName()
        {
            this._id = this.name;
        }

        public string Id => _id;
        
        /// <summary>
        /// Diameter in inches
        /// </summary>
        public float Diameter => _diameter;
    }
}
