using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Client Network Transform")]
    [UMod.Shared.ModDontCompile]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// The interpolation type every <see cref="ClientNetworkTransform"/> in the game runs with,
        /// applied in <see cref="Awake"/> (#2809).
        /// </summary>
        /// <remarks>
        /// !!! This OVERRIDES the three interpolation dropdowns in the inspector. !!! They are
        /// decorative on this component - whatever a prefab serializes is replaced before the base class
        /// ever reads it.
        ///
        /// That is deliberate. Of the 975 ClientNetworkTransform instances in the project, exactly one
        /// was ever set to anything but the default, and by accident - a fly-mode commit that nudged the
        /// dropdown on Player.prefab (b40ac426f, #2809). Setting it in one place also reaches prefabs
        /// authored in the Mod Tools, which writing the value into our own assets would not.
        ///
        /// It ships as LegacyLerp, which is what everything except that one prefab was already running,
        /// so this is not a behaviour change. Whether Lerp is better is a real question: its buffer
        /// depth tracks measured RTT, where LegacyLerp's is a fixed 2 ticks (66.7 ms at TickRate 30)
        /// regardless of ping - so LegacyLerp starves above roughly 67 ms RTT and recovers by draining
        /// its queue up to now in one pass, which is a jump. Flip it with fm-net-interp and watch it
        /// before changing this default, and read that command's remarks first: a loopback A/B reports
        /// the wrong winner.
        ///
        /// NGO's own AssignDefaultInterpolationType / DefaultInterpolationType hooks do exactly this but
        /// are internal static and unreachable from here, which is why this is an Awake override.
        /// </remarks>
        public static InterpolationTypes DefaultInterpolationType = InterpolationTypes.LegacyLerp;

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        protected override void Awake()
        {
            //Before base.Awake(): that is where the interpolators are created, and where NGO would apply
            //a default of its own.
            PositionInterpolationType = DefaultInterpolationType;
            RotationInterpolationType = DefaultInterpolationType;
            ScaleInterpolationType    = DefaultInterpolationType;

            base.Awake();
        }
    }
}
