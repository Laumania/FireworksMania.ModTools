using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// Waking up whatever is resting on an object that is leaving the physics simulation.
    ///
    /// A body that has come to rest is asleep, and physics only wakes a sleeping body when it can feel
    /// something happen to it. Taking away what it is resting on is not one of those things - switching
    /// colliders off, hiding, pooling or shrinking an object away all happen without the sleeping body
    /// above ever being touched - so it stays exactly where it was, hanging in mid air (#2733).
    ///
    /// So anywhere something stops taking part in the simulation, call
    /// <see cref="WakeUpWhateverIsTouching"/> first, while there are still colliders to measure. It is
    /// deliberately about nothing but Rigidbodies and Colliders - it neither knows nor cares what the
    /// object is.
    /// </summary>
    public static class PhysicsWakeUp
    {
        /// <summary>
        /// How far outside the leaving object we look for what was touching it. Resting bodies sit a
        /// contact offset apart (0.01 by default) rather than exactly touching, so a bit of slack is
        /// needed - but not so much that half the neighborhood gets woken for nothing.
        /// </summary>
        public const float SearchMargin = 0.05f;

        /// <summary>
        /// Neighbors found around the leaving object. Grown rather than fixed, as a full buffer would
        /// silently leave somebody hanging - which is the whole bug this exists to fix. Entries are
        /// cleared as they are read, so it never keeps a destroyed collider alive in between (play mode
        /// skips the Domain Reload, so it lives on across play sessions too).
        /// </summary>
        private static Collider[] _overlapBuffer = new Collider[64];
        private const int MaxOverlapBufferSize   = 512;

        private static readonly List<Collider> _colliderBuffer = new List<Collider>();

        /// <summary>
        /// Wakes every sleeping body resting against <paramref name="gameObject"/>, so they get to fall
        /// once it stops holding them up. Call it BEFORE switching the object's own colliders off, while
        /// there is still something to measure - the object's own bodies are skipped either way.
        /// </summary>
        public static void WakeUpWhateverIsTouching(GameObject gameObject)
        {
            if (TryGetSimulatedBounds(gameObject, out var simulatedBounds) == false)
                return;

            var searchVolume = GetSearchVolume(simulatedBounds);
            var foundCount   = 0;

            while (true)
            {
                foundCount = Physics.OverlapBoxNonAlloc(searchVolume.center, searchVolume.extents, _overlapBuffer, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore);

                //A full buffer means there could be neighbors we never got to see, so make room and ask again
                if (foundCount < _overlapBuffer.Length || _overlapBuffer.Length >= MaxOverlapBufferSize)
                    break;

                _overlapBuffer = new Collider[_overlapBuffer.Length * 2];
            }

            for (int i = 0; i < foundCount; i++)
            {
                var neighborRigidbody = _overlapBuffer[i].attachedRigidbody;
                _overlapBuffer[i]     = null;

                if (neighborRigidbody == null || neighborRigidbody.transform.IsChildOf(gameObject.transform))
                    continue;

                if (ShouldWakeUp(neighborRigidbody.isKinematic, neighborRigidbody.IsSleeping()))
                    neighborRigidbody.WakeUp();
            }
        }

        /// <summary>
        /// The world volume the object actually occupies in the simulation. False when it occupies none,
        /// which is nothing to wake anybody over.
        /// </summary>
        public static bool TryGetSimulatedBounds(GameObject gameObject, out Bounds simulatedBounds)
        {
            gameObject.GetComponentsInChildren(true, _colliderBuffer); //List overload to avoid allocating on every single destroy

            var hasBounds = false;
            simulatedBounds = new Bounds();

            foreach (var collider in _colliderBuffer)
            {
                if (TakesPartInTheSimulation(collider.enabled, collider.isTrigger, collider.gameObject.activeInHierarchy) == false)
                    continue;

                if (hasBounds == false)
                {
                    simulatedBounds = collider.bounds;
                    hasBounds       = true;
                }
                else
                    simulatedBounds.Encapsulate(collider.bounds);
            }

            _colliderBuffer.Clear();

            return hasBounds;
        }

        /// <summary>
        /// Only colliders actually taking part in the simulation say anything about where the object is
        /// being touched. Triggers are especially worth leaving out - a firework's explosion trigger is
        /// many times the size of the firework itself, and searching that whole volume would wake
        /// everything nearby every single time one burns out.
        /// </summary>
        public static bool TakesPartInTheSimulation(bool isEnabled, bool isTrigger, bool isActiveInHierarchy)
        {
            return isEnabled == true && isTrigger == false && isActiveInHierarchy == true;
        }

        /// <summary>
        /// The volume to look for neighbors in, given the object's own. Note <see cref="Bounds.Expand(float)"/>
        /// grows the total size rather than the extents, so the margin has to be doubled to end up with
        /// <see cref="SearchMargin"/> of slack on each side.
        /// </summary>
        public static Bounds GetSearchVolume(Bounds simulatedBounds)
        {
            var searchVolume = simulatedBounds;
            searchVolume.Expand(SearchMargin * 2f);

            return searchVolume;
        }

        /// <summary>
        /// Kinematic bodies don't fall, and an awake body is already being simulated - waking that one
        /// again would only reset its sleep timer for nothing.
        ///
        /// This is also what keeps a client from shoving networked physics around. Every peer plays the
        /// destroy animation and switches its own copy's colliders off, so every peer asks this question -
        /// but on a peer without authority `NetworkRigidbody` has already forced the copy kinematic
        /// (`AutoUpdateKinematicState` is on), so there is nothing there to wake and the client leaves the
        /// simulation to the host. Note it is this rule doing that and not a network check, deliberately:
        /// anything that really is simulated locally - an object this peer has authority over, or modded
        /// content with no `NetworkRigidbody` at all - still gets to fall, which a plain "server only" gate
        /// would silently break.
        /// </summary>
        public static bool ShouldWakeUp(bool isKinematic, bool isSleeping)
        {
            return isKinematic == false && isSleeping == true;
        }
    }
}
