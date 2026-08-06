using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Decides which registered carriers are worth an <see cref="ImpactCollisionRelay"/>.
    ///
    /// Unity marshals a managed <c>Collision</c> for every contact pair that involves a GameObject
    /// carrying an <c>OnCollisionEnter</c> message, and that marshaling is the whole cost - measured
    /// at ~2.3us per contact-enter, paid before any of our code runs, and NOT avoidable by disabling
    /// the receiving component (https://github.com/Laumania/FireworksMania/issues/2236). The cost is
    /// linear in how many objects carry the message, so the only lever is to have fewer of them.
    ///
    /// Policy: carry the message inside an adaptive radius around the camera, sized so that roughly
    /// <see cref="Budget"/> objects are inside it, with <see cref="Budget"/> ALSO enforced as a hard
    /// cap. The cap is not redundant - the radius bottoms out at <see cref="MinCarryDistance"/>, and a
    /// dense enough pile (a blueprint of 2000 propane tanks) still leaves hundreds inside it, at which
    /// point the radius has nothing left to give. Impact sounds are a close-range detail anyway: a
    /// fragment settling 40m away is inaudible, but costs exactly as much as one at your feet.
    ///
    /// The sweep is AMORTIZED: a slice of the registry is visited per frame rather than all of it on a
    /// timer. The first version did a full two-pass scan every 0.25s and cost 1.0-1.6ms on the frames
    /// it ran (profiled: p99 1.23ms, max 1.63ms - larger than the game's entire average physics frame).
    /// Round-robin turns that spike into a flat sliver, and converges FASTER than the old 0.25s timer
    /// because a newly spawned fragment is reached within a fraction of a cycle rather than waiting out
    /// a fixed interval - which matters, since a fragment's first bounce is its loudest.
    /// </summary>
    public class ImpactCarrierSweep
    {
        /// <summary>How hard the radius may correct in a single cycle, so it eases in rather than oscillating.</summary>
        private const float MinRadiusScalePerCycle = 0.6f;
        private const float MaxRadiusScalePerCycle = 1.6f;

        public int   Budget            { get; set; } = 40;
        public float MinCarryDistance  { get; set; } = 5f;
        public float MaxCarryDistance  { get; set; } = 30f;
        public float ReleaseHysteresis { get; set; } = 1.2f;

        /// <summary>Current radius inside which carriers keep the collision message.</summary>
        public float CarryRadius { get; private set; } = 30f;

        /// <summary>How many carriers held the collision message during the last completed full cycle.</summary>
        public int LastCycleCarryingCount { get; private set; }

        /// <summary>
        /// How many carriers were INSIDE the radius during the last completed cycle, whether or not they
        /// got the message. This is what the radius is sized against; when it stays far above
        /// <see cref="Budget"/> the radius has bottomed out at <see cref="MinCarryDistance"/> and the
        /// hard cap is doing the work instead.
        /// </summary>
        public int LastCycleCandidateCount { get; private set; }

        public int CompletedCycles { get; private set; }

        private int  _cursor;
        private int  _visitsThisCycle;
        private int  _carryingThisCycle;
        private int  _candidatesThisCycle;
        private int  _liveCarryingCount;
        private bool _hasLoggedCarrierException;

        public ImpactCarrierSweep()
        {
            CarryRadius = MaxCarryDistance;
        }

        /// <summary>
        /// Visits up to <paramref name="visitsThisFrame"/> carriers, granting or revoking the collision
        /// message as the current radius dictates. Allocation free.
        /// </summary>
        public void Step(IReadOnlyList<IImpactSoundCarrier> carriers, Vector3 cameraPosition, int visitsThisFrame)
        {
            var count = carriers == null ? 0 : carriers.Count;
            if (count == 0 || visitsThisFrame <= 0)
                return;

            var radiusSqr = CarryRadius * CarryRadius;
            var visits    = Mathf.Min(visitsThisFrame, count);

            for (int i = 0; i < visits; i++)
            {
                if (_cursor >= count)
                    _cursor = 0;

                var carrier = carriers[_cursor];
                _cursor++;
                _visitsThisCycle++;

                if (carrier != null)
                    Visit(carrier, cameraPosition, radiusSqr);

                //A full pass over the registry is what makes the carrying count meaningful, so the
                //radius is only re-sized once the cursor has been all the way round.
                if (_visitsThisCycle >= count)
                {
                    CompleteCycle();
                    radiusSqr = CarryRadius * CarryRadius;
                }
            }
        }

        /// <summary>
        /// Grants or revokes the collision message for one carrier.
        ///
        /// Guarded, because this runs over every registered carrier every cycle - including any
        /// registered by mod code - so a carrier that throws must cost that one carrier and not the
        /// rest of the frame's sweep. That is exactly what #2241 did: an NRE from one carrier escaped
        /// all the way out of Step and abandoned everything behind it in the registry. Logged once,
        /// since a broken carrier is revisited on every single cycle.
        /// </summary>
        private void Visit(IImpactSoundCarrier carrier, Vector3 cameraPosition, float radiusSqr)
        {
            try
            {
                var sqrDistance = (carrier.Position - cameraPosition).sqrMagnitude;
                var carrying    = carrier.IsCarryingCollisionMessage;

                //Candidates drive the radius; the radius alone cannot bound the count, because a
                //dense enough pile (2000 propane tanks) leaves hundreds inside even at the floor.
                if (sqrDistance <= radiusSqr)
                    _candidatesThisCycle++;

                //Budget is a HARD cap: in range is necessary but not sufficient. Existing carriers
                //keep their slot, so the set stays stable instead of churning round the cursor.
                var shouldCarry = ShouldCarry(sqrDistance, radiusSqr, carrying, ReleaseHysteresis)
                               && (carrying || Budget <= 0 || _liveCarryingCount < Budget);

                if (shouldCarry != carrying)
                {
                    var wasCarrying = carrying;

                    carrier.SetCarryingCollisionMessage(shouldCarry);

                    //Read back rather than assume it took: a carrier is allowed to refuse, e.g. when a
                    //relay shared with another behaviour on the same GameObject is still pending
                    //destruction (#2241). Assuming it took would burn a budget slot on nothing.
                    carrying = carrier.IsCarryingCollisionMessage;

                    if (carrying != wasCarrying)
                        _liveCarryingCount = Mathf.Max(0, _liveCarryingCount + (carrying ? 1 : -1));
                }

                if (carrying)
                    _carryingThisCycle++;
            }
            catch (System.Exception exception)
            {
                if (_hasLoggedCarrierException)
                    return;

                _hasLoggedCarrierException = true;
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Hysteresis so a carrier sitting exactly on the radius does not get its relay added and
        /// removed every cycle - re-adding a component costs ~5x what removing one does.
        /// </summary>
        public static bool ShouldCarry(float sqrDistance, float radiusSqr, bool currentlyCarrying, float releaseHysteresis)
        {
            if (radiusSqr <= 0f)
                return false;

            if (currentlyCarrying)
                return sqrDistance <= radiusSqr * Mathf.Max(1f, releaseHysteresis);

            return sqrDistance <= radiusSqr;
        }

        public void Reset()
        {
            _cursor                    = 0;
            _visitsThisCycle           = 0;
            _carryingThisCycle         = 0;
            _candidatesThisCycle       = 0;
            _liveCarryingCount         = 0;
            _hasLoggedCarrierException = false;
            CompletedCycles            = 0;
            LastCycleCarryingCount     = 0;
            LastCycleCandidateCount    = 0;
            CarryRadius                = MaxCarryDistance;
        }

        /// <summary>
        /// Re-sizes the radius toward the one that holds <see cref="Budget"/> CANDIDATES. Sizing it
        /// against the carrying count instead would deadlock against the hard cap: capped at the budget,
        /// the count could never exceed it, so the radius would only ever grow. Carriers are mostly
        /// debris resting on the ground, so count grows with the SQUARE of the radius - hence the square
        /// root. Clamped per cycle so a sudden burst eases the radius in rather than slamming it to a
        /// value it then has to bounce back from.
        /// </summary>
        private void CompleteCycle()
        {
            LastCycleCarryingCount  = _carryingThisCycle;
            LastCycleCandidateCount = _candidatesThisCycle;
            CompletedCycles++;

            //Exact recount, so grants/revokes missed because a carrier unregistered while holding the
            //message (pooled debris, destroyed props) cannot make the live count drift indefinitely.
            _liveCarryingCount = _carryingThisCycle;

            if (Budget > 0)
            {
                var scale = Mathf.Sqrt(Budget / (float)Mathf.Max(1, _candidatesThisCycle));
                scale     = Mathf.Clamp(scale, MinRadiusScalePerCycle, MaxRadiusScalePerCycle);

                CarryRadius = Mathf.Clamp(CarryRadius * scale, MinCarryDistance, MaxCarryDistance);
            }

            _visitsThisCycle     = 0;
            _carryingThisCycle   = 0;
            _candidatesThisCycle = 0;
        }
    }
}
