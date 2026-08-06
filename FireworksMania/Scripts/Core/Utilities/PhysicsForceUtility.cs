using UnityEngine;

namespace FireworksMania.Core.Utilities
{
    //Helpers to keep physics forces independent of the current fixed timestep (issue #2233).
    //
    //Unity integrates ForceMode.Force and ForceMode.Acceleration over Time.fixedDeltaTime, while
    //ForceMode.Impulse and ForceMode.VelocityChange are applied instantly. Because the game changes
    //Time.fixedDeltaTime at runtime (PerformanceManager throttles physics when quality drops), any
    //force that is scaled by the live timestep on top of that ends up depending on how fast the
    //machine currently runs - rockets flew twice as high with throttled physics, and explosions
    //pushed twice as hard.
    public static class PhysicsForceUtility
    {
        //The fixed timestep all firework content is authored and balanced against. Matches
        //'Fixed Timestep' in ProjectSettings/TimeManager.asset. Deliberately a constant and not the
        //live Time.fixedDeltaTime, so forces keep their tuned strength no matter what physics runs at.
        public const float ReferenceFixedTimestep = 0.02f;

        //True for the force modes Unity itself multiplies by the fixed timestep.
        public static bool IsIntegratedOverTimestep(ForceMode forceMode) =>
            forceMode == ForceMode.Force || forceMode == ForceMode.Acceleration;

        //Timestep to use when converting a per-second force into the amount applied this fixed step.
        //Modes that Unity already integrates get the authored reference timestep (using the live one
        //would apply the timestep twice), the instant modes get the real timestep as they should.
        public static float GetForceTimestep(ForceMode forceMode, float currentFixedTimestep) =>
            IsIntegratedOverTimestep(forceMode) ? ReferenceFixedTimestep : currentFixedTimestep;

        //Converts a one-shot force application into its instant equivalent, so a single AddForce or
        //AddExplosionForce call delivers the same velocity change no matter the current timestep.
        public static ForceMode ToInstantForceMode(ForceMode forceMode)
        {
            switch (forceMode)
            {
                case ForceMode.Force:        return ForceMode.Impulse;
                case ForceMode.Acceleration: return ForceMode.VelocityChange;
                default:                     return forceMode;
            }
        }

        //Force magnitude matching ToInstantForceMode - the reference timestep Unity would otherwise
        //have multiplied the force by.
        public static float ToInstantForce(float force, ForceMode forceMode) =>
            IsIntegratedOverTimestep(forceMode) ? force * ReferenceFixedTimestep : force;
    }
}
