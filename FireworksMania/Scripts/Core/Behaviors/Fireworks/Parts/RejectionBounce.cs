using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// How a mortar tube or a rack socket throws away something it will not take (#2937): a low lob that lands
    /// <see cref="ThrowDistance"/> out from the opening and peaks <see cref="ThrowHeight"/> above where it started.
    ///
    /// It used to be 2 m/s added along the opening's axis. On an upright tube that is straight up, so the item hopped,
    /// landed back on the tube and was refused again - three reject sounds and 20-25 cm from the tube was typical - and
    /// a heavy item falling in fast all but cancelled it.
    ///
    /// The velocity is SET rather than added, so how the item arrived no longer matters, and the lob is solved for the
    /// item's own linear damping: that runs from 0 to 1 across the game's items, heavy ones carrying the most, and a
    /// cake at 1 would otherwise land about a third short of a shell at 0. Mass never enters.
    /// </summary>
    internal static class RejectionBounce
    {
        //Measured level with where the throw starts, so on the ground it lands a little further out. The 50 Hz
        //physics step brings every item down about 2% short and 3 cm low, the same for all of them - tune by eye
        public const float ThrowDistance = 1.5f;
        public const float ThrowHeight   = 0.4f;

        //Closer to the middle of the opening than this, an item is dead centre, and the way it goes is decided by
        //the opening's lean instead
        private const float DeadCentreOffset = 0.01f;

        //Less horizontal lean than this (about 6 degrees) and the opening counts as upright
        private const float UprightLean = 0.1f;

        //Below this the damping changes nothing over a throw lasting well under a second, and the damped formulas
        //below start losing precision
        private const double NegligibleDamping = 1e-3;

        public static void ThrowClear(Rigidbody rejectedRigidbody, Pose opening)
        {
            //A kinematic body has no velocity to set, and one that is kinematic now was picked up or seated after
            //it was refused
            if (rejectedRigidbody.OrNull() == null || rejectedRigidbody.isKinematic)
                return;

            var awayDirection = CalculateAwayDirection(rejectedRigidbody.worldCenterOfMass, opening.position, opening.up, Random.insideUnitCircle);

            rejectedRigidbody.linearVelocity = CalculateThrowVelocity(awayDirection, ThrowDistance, ThrowHeight, Physics.gravity.magnitude, rejectedRigidbody.linearDamping);
        }

        /// <summary>
        /// Which way, horizontally, a refused item is thrown: out from the middle of the opening toward wherever the
        /// item is; if it is dead centre, the way the opening leans, which is away from the rack it stands in; and if
        /// the opening is upright too, <paramref name="randomDirection"/>.
        /// </summary>
        public static Vector3 CalculateAwayDirection(Vector3 itemPosition, Vector3 openingPosition, Vector3 openingUp, Vector2 randomDirection)
        {
            var offset = Flatten(itemPosition - openingPosition);
            if (offset.magnitude >= DeadCentreOffset)
                return offset.normalized;

            var lean = Flatten(openingUp);
            if (lean.magnitude >= UprightLean)
                return lean.normalized;

            var random = new Vector3(randomDirection.x, 0f, randomDirection.y);
            return random.sqrMagnitude > 1e-6f ? random.normalized : Vector3.forward;
        }

        /// <summary>
        /// The velocity that lands a body with <paramref name="linearDamping"/> <paramref name="distance"/> away along
        /// <paramref name="awayDirection"/>, level with where it started, peaking <paramref name="apexHeight"/> above
        /// it. Damping is the drag Unity applies, dv/dt = -k v, on top of gravity.
        /// </summary>
        public static Vector3 CalculateThrowVelocity(Vector3 awayDirection, float distance, float apexHeight, float gravity, float linearDamping)
        {
            //Nothing to throw against - never hand the physics a NaN
            if (gravity <= 0f || apexHeight <= 0f)
                return awayDirection * distance;

            //Doubles: with little damping the damped apex is a small difference of two large terms
            double g = gravity;
            double k = linearDamping;

            //Straight up to apexHeight with nothing slowing it down: where the solve below starts, and the whole
            //answer when damping is negligible
            var verticalSpeed = System.Math.Sqrt(2.0 * g * apexHeight);
            var flightTime    = 2.0 * verticalSpeed / g;

            if (k < NegligibleDamping)
                return awayDirection * (float)(distance / flightTime) + Vector3.up * (float)verticalSpeed;

            //The apex a launch speed v reaches is v/k - g/k^2 ln(1 + kv/g). That only ever grows with v and bends
            //upward, so Newton's method from the undamped speed steps past the answer once and then closes in
            for (var i = 0; i < 30; i++)
            {
                var apex  = verticalSpeed / k - g / (k * k) * System.Math.Log(1.0 + k * verticalSpeed / g);
                var slope = verticalSpeed / (g + k * verticalSpeed);
                var step  = (apex - apexHeight) / slope;

                verticalSpeed -= step;

                if (System.Math.Abs(step) < 1e-9)
                    break;
            }

            //Back down to launch height: h(t) = (v + g/k)(1 - e^-kt)/k - gt/k. Damping makes the way down slower than
            //the way up, so twice the time to the apex is still short of it; Newton's method from there steps past
            //the landing once and then closes in
            flightTime = 2.0 * System.Math.Log(1.0 + k * verticalSpeed / g) / k;
            for (var i = 0; i < 30; i++)
            {
                var decay  = System.Math.Exp(-k * flightTime);
                var height = (verticalSpeed + g / k) * (1.0 - decay) / k - g * flightTime / k;
                var slope  = (verticalSpeed + g / k) * decay - g / k;
                var step   = height / slope;

                flightTime -= step;

                if (System.Math.Abs(step) < 1e-9)
                    break;
            }

            //Across, only damping acts: in time t a speed u covers u(1 - e^-kt)/k
            var horizontalSpeed = distance * k / (1.0 - System.Math.Exp(-k * flightTime));

            return awayDirection * (float)horizontalSpeed + Vector3.up * (float)verticalSpeed;
        }

        private static Vector3 Flatten(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
    }
}
