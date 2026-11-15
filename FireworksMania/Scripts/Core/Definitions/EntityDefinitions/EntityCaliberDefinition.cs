using System.Globalization;
using UnityEngine;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    /// <summary>
    /// The caliber of a cake's or a single shot's tubes, in millimeters (#1497). Reference one of the
    /// base game's assets (15mm ... 125mm) rather than making your own.
    /// </summary>
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
    [CreateAssetMenu(fileName = "New Entity Caliber Definition", menuName = "Fireworks Mania/Definitions/Internal/Entity Caliber Definition")]
#endif
    public class EntityCaliberDefinition : EntityClassificationDefinition
    {
        [SerializeField]
        [Tooltip("Caliber in millimeters")]
        private float _caliber;

        /// <summary>
        /// Caliber in millimeters
        /// </summary>
        public float Caliber => _caliber;

        public override string DisplayName => _caliber.ToString(CultureInfo.InvariantCulture) + "mm";

        public override float SortValue => _caliber;
    }
}
