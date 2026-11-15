using System.Globalization;
using UnityEngine;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Entity Diameter Definition", menuName = "Fireworks Mania/Definitions/Internal/Entity Diameter Definition")]
#endif
    public class EntityDiameterDefinition : EntityClassificationDefinition
    {
        [SerializeField]
        [Tooltip("Diameter in inches")]
        private float _diameter;

        //Todo: Figure out a way to get a localized name here
        //public LocalizedString Name;

        /// <summary>
        /// Diameter in inches
        /// </summary>
        public float Diameter => _diameter;

        //Invariant culture: the default ToString() writes 1,75 on some OS locales.
        public override string DisplayName => _diameter.ToString(CultureInfo.InvariantCulture) + "\"";

        public override float SortValue => _diameter;
    }
}
