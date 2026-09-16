using System;
using System.Collections.Generic;
using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// How long each firework keeps firing - from launch until its last stage has gone off - estimated once
    /// from the prefab while the map loads and then looked up (#2657). One number with two readers: the inventory's item
    /// details show it, and the host's spawn limit counts it down to decide when the firework is spent
    /// (<see cref="BaseFireworkBehavior.IsSpent"/>, #2651) - so what the inventory promises is exactly when
    /// the slot comes back. Nothing is measured while the game is running.
    /// <para>
    /// Each behavior composes its own estimate in <see cref="BaseFireworkBehavior.EstimateDurationInSeconds"/>,
    /// so a rocket is thrust plus burst while a cake is just its effect. Keyed by entity definition, because
    /// a firework asks from one of its clones and a clone shares nothing identifiable with the prefab that
    /// was measured - and the UI asks with the definition it is showing.
    /// </para>
    /// <para>
    /// By <see cref="BaseEntityDefinition.Id"/>, the same identity the entity repository, blueprints and the
    /// mod lookups all use, and not by the ScriptableObject's instance id (#2792). An instance id only names
    /// the object that is loaded right now: uMod destroys a mod's definitions and builds them afresh when it
    /// reloads that mod - an update landing, or the player disabling it - so an entry that outlives one of
    /// those is left holding a key that means nothing, and that Unity is free to hand to something else. An
    /// id means the same firework for as long as the id exists. <see cref="Clear"/> keeps the catalog to the
    /// map being loaded on top of that, so the two together leave no entry to go stale.
    /// </para>
    /// </summary>
    public static class FireworkDurationCatalog
    {
        private static readonly Dictionary<string, float> _durationsByDefinitionId = new Dictionary<string, float>();

        /// <summary>
        /// Forgets every duration. Called as a map starts loading, before that map's entities are
        /// registered and re-measured, so the catalog holds what this map actually has rather than
        /// everything the session has ever loaded - a mod the player has disabled since would otherwise
        /// keep its entry for as long as the game runs.
        /// </summary>
        public static void Clear() => _durationsByDefinitionId.Clear();

        //Play mode enters without a Domain Reload, so this would otherwise still hold the last session's
        //durations before the first map load has a chance to clear them
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Clear();

        /// <summary>
        /// Estimates this entity's duration from its prefab and remembers it. Idempotent, and a no-op for an
        /// entity that is not a firework - a mortar, a rack, a prop - which is how the inventory knows to
        /// show no duration for those.
        /// </summary>
        public static void Warm(BaseEntityDefinition entityDefinition)
        {
            if (entityDefinition.OrNull() == null || entityDefinition.PrefabGameObject.OrNull() == null)
                return;

            //An entity that never got its unique id filled in has nothing to be remembered under, and every
            //one of them would otherwise share the one entry. OnValidate already calls that out as an error
            if (string.IsNullOrEmpty(entityDefinition.Id))
                return;

            var fireworkBehavior = entityDefinition.PrefabGameObject.GetComponent<BaseFireworkBehavior>();
            if (fireworkBehavior.OrNull() == null)
                return;

            float seconds;
            try
            {
                seconds = fireworkBehavior.EstimateDurationInSeconds();
            }
            catch (Exception exception)
            {
                //A mod's behavior may override the estimate, and its bug must not take the whole entity
                //repository down with it - that firework just shows no duration
                Debug.LogError($"Unable to estimate the duration of '{entityDefinition.Id}' - it will show none in the inventory: {exception}", entityDefinition);
                return;
            }

            //A zero is a real answer - a firecracker is a bang and fires for no time at all - so it is kept;
            //the inventory rounds it up to ~1s rather than hiding the row, which would read like a bug
            _durationsByDefinitionId[entityDefinition.Id] = Mathf.Max(0f, seconds);
        }

        /// <summary>
        /// This entity's estimated duration, if it is a firework the catalog has seen. Zero is a real answer:
        /// a bang fires for no time at all.
        /// </summary>
        public static bool TryGetDurationInSeconds(BaseEntityDefinition entityDefinition, out float seconds)
        {
            if (entityDefinition.OrNull() != null &&
                string.IsNullOrEmpty(entityDefinition.Id) == false &&
                _durationsByDefinitionId.TryGetValue(entityDefinition.Id, out seconds))
                return true;

            seconds = 0f;
            return false;
        }
    }
}
