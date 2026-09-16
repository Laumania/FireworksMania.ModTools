using System.Collections.Generic;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// Warnings about how a piece of content is built - a firework effect that loops forever, a particle system
    /// too big to observe - written for whoever built it, reading their own game's log while they try it out.
    /// <para>
    /// Each one is logged once per item per session, however many copies of that item get placed or launched:
    /// the second copy is the same content and has nothing new to say (#2359, #2856).
    /// </para>
    /// <para>
    /// A dedicated server logs none of them. Nobody building content reads a server.log, and the one person who
    /// does - whoever runs the server - can't fix a mod's particle systems (#2841, #2856).
    /// </para>
    /// </summary>
    internal static class ContentWarnings
    {
        private static readonly HashSet<string> _alreadyLogged = new HashSet<string>();

        //A once-per-session suppression - and with Domain Reload disabled it would otherwise keep swallowing the
        //same warning for every later play session in this editor run (#2612)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => _alreadyLogged.Clear();

        /// <summary>
        /// False on a dedicated server. Check it before doing any of the work a warning needs: resolving the item
        /// walks the hierarchy and allocates, and a server spawns fireworks for as long as it runs.
        /// </summary>
#if DEDICATED_SERVER
        internal static bool AreLogged => false;
#else
        internal static bool AreLogged => true;
#endif

        /// <summary>
        /// True the first time <paramref name="warning"/> comes up for this part of this item in the session, and
        /// never on a dedicated server. It counts as logged from then on, so only ask when about to log.
        /// </summary>
        /// <param name="warning">Which warning this is, so two different ones about the same part don't swallow each other.</param>
        /// <param name="itemName">The item, from <see cref="ResolveItemName"/> or given explicitly.</param>
        /// <param name="partName">The GameObject inside the item the warning is about.</param>
        internal static bool ShouldLog(string warning, string itemName, string partName)
            => AreLogged && _alreadyLogged.Add($"{warning}:{itemName}/{WithoutCloneSuffix(partName)}");

        /// <summary>
        /// The id of the entity <paramref name="part"/> belongs to, which is the same for every instance of it.
        /// Falls back to the hierarchy path for a part that isn't in an entity at all, so it still says something.
        /// <para>
        /// Only right while the part still hangs under its own item. A shell effect loaded into a mortar tube
        /// hangs under the tube, so that one has to be named explicitly or the tube is blamed for the shell.
        /// </para>
        /// <para>
        /// Every candidate is checked rather than just the nearest one: <see cref="SaveableEntity"/> implements
        /// this too and only picks up its definition from its sibling behavior in Awake, so whichever of them
        /// happens to be asked first may still be empty while a firework is being built.
        /// </para>
        /// </summary>
        internal static string ResolveItemName(Component part)
        {
            foreach (var entity in part.GetComponentsInParent<IHaveBaseEntityDefinition>(true))
            {
                if (entity.EntityDefinition != null)
                    return entity.EntityDefinition.Id;
            }

            return part.gameObject.GetHierarchyPathAsString();
        }

        internal static string WithoutCloneSuffix(string name) => name.EndsWith("(Clone)") ? name.Substring(0, name.Length - "(Clone)".Length) : name;
    }
}
