using System.Collections.Generic;
using FireworksMania.Core.Behaviors;
using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Definitions.EntityDefinitions;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    //Finds fireworks whose prefab never had its diameter, caliber or class set (#1497). The rules live here, with no uMod
    //types, so the EditMode tests can pin them down and the game's own report menu can share them; the Mod Tools build
    //check is ModToolsBuildClassificationProcessor.
    //Internal for the same reason as GameSoundDefinitionValidator: this folder ships to modders as source.
    internal static class EntityClassificationValidator
    {
        internal readonly struct Problem
        {
            public readonly BaseInventoryEntityDefinition Definition;
            public readonly GameObject                    Prefab;
            public readonly string                        Message;

            public Problem(BaseInventoryEntityDefinition definition, GameObject prefab, string message)
            {
                Definition = definition;
                Prefab     = prefab;
                Message    = message;
            }
        }

        /// <summary>
        /// One problem per definition whose prefab has a diameter, caliber or class to set and doesn't.
        /// Definitions with no prefab are skipped: ModToolsBuildAssetsProcessor already refuses those.
        /// </summary>
        internal static List<Problem> FindMissing(IEnumerable<BaseInventoryEntityDefinition> definitions)
        {
            var problems = new List<Problem>();

            foreach (var definition in definitions)
            {
                if (definition == null || definition.PrefabGameObject == null)
                    continue;

                var prefab    = definition.PrefabGameObject;
                var component = FindComponentMissingItsClassification(prefab);
                if (component == null)
                    continue;

                var (kind, examples) = Expected(component);
                problems.Add(new Problem(definition, prefab,
                    $"'{definition.name}': {component.GetType().Name} on its prefab '{prefab.name}' has no {kind} set. " +
                    $"Pick one of the game's {examples}, so the inventory and Shop can list it (#1497)."));
            }

            return problems;
        }

        private static Component FindComponentMissingItsClassification(GameObject prefab)
        {
            //Shells and mortars implement both interfaces over one field. A component written before #1497 - a mod's
            //own - may know only the diameter one.
            var carrier = prefab.GetComponent<IHaveEntityClassificationDefinition>();
            if (carrier != null)
                return carrier.ClassificationDefinition == null ? (Component)carrier : null;

            var diameterCarrier = prefab.GetComponent<IHaveEntityDiameterDefinition>();
            if (diameterCarrier != null && diameterCarrier.DiameterDefinition == null)
                return (Component)diameterCarrier;

            return null;
        }

        private static (string Kind, string Examples) Expected(Component component)
        {
            if (component is IHaveEntityDiameterDefinition)
                return ("Diameter", "diameters (1.75\" to 16\")");

            if (component is CakeBehavior || component is PreloadedTubeBehavior)
                return ("Caliber", "calibers (15mm to 125mm)");

            if (component is BaseFireworkBehavior)
                return ("Class", "classes (F2, F3 or F4)");

            return ("classification", "diameters, calibers or classes");
        }
    }
}
