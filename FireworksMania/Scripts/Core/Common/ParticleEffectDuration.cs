using System.Collections.Generic;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// Works out how long a firework effect keeps firing, from its emission modules rather than from
    /// anything Unity reports at runtime - the one number that is both shown as the firework's duration in
    /// the inventory and counted down by the host's spawn limit to decide when the firework is spent
    /// (#2651, #2657).
    /// <para>
    /// <see cref="ParticleSystem.isEmitting"/> looks like the answer and is not: it only says the emission
    /// module's window is still open, not that anything is coming out of it. A system whose emission is a
    /// single burst at t=0 but whose duration is 25 seconds reports isEmitting for the full 25 - which is
    /// how the stock mortar shell is authored, so a spent shell once held its owner's spawn slot for twenty
    /// seconds after the last star had gone out.
    /// </para>
    /// <para>
    /// What is measured is emission, not what is left in the air: a burst is over a tenth of a second after
    /// the bang however long its stars and smoke hang, a cake is over when its last shot has burst, a
    /// firecracker fires for no time at all. Four rules on top of the emission arithmetic:
    /// <list type="bullet">
    /// <item>Only systems that are active in the prefab count, sub-emitters included - an authored-off
    /// system never plays, and a parent referencing it as a sub-emitter does not wake it. The stock shell
    /// carries one that would emit for 15 s, which is where its 15 s came from before this rule
    /// existed.</item>
    /// <item>Death sub-emitters are the next stage of the firework, reached when the parent's particles
    /// die: a shell's single particle flies for 1.6 s and then its burst fires, a cake's shot flies for two
    /// seconds and then bangs. Without this a shell measures 0 - its whole show is sub-emitters.</item>
    /// <item>Birth, Collision, Trigger and Manual sub-emitters are what a particle drags behind it - trails,
    /// smoke - and never count.</item>
    /// <item>A system driven by a <see cref="Behaviors.Fireworks.Parts.SingleSystemZipper"/> is measured
    /// from the zipper, not from its emission module: the zipper emits from code and blanks that module
    /// itself, so reading it says "never emits" for a cake that fires hundreds of shots. Four of the 2026
    /// cakes are authored this way and every one of them measured 0 before this rule existed (#2822).</item>
    /// </list>
    /// </para>
    /// <para>
    /// Measured once per firework prefab while the map loads (see <see cref="FireworkDurationCatalog"/>)
    /// and then used as a plain timer, so nothing is measured while the game is running. Where it cannot be
    /// exact it errs LATE - the top of every range, a curve read as running for its whole duration. A
    /// firework that reads a little long is a small annoyance; one that reads too short is a spawn limit
    /// that does not hold.
    /// </para>
    /// </summary>
    public static class ParticleEffectDuration
    {
        private static readonly List<ParticleSystem> _systemsBuffer  = new List<ParticleSystem>();
        private static readonly HashSet<ParticleSystem> _subEmitters = new HashSet<ParticleSystem>();
        private static readonly List<ParticleSystem> _stageChain     = new List<ParticleSystem>();
        private static readonly List<SingleSystemZipper> _zipperBuffer = new List<SingleSystemZipper>();
        private static readonly Dictionary<ParticleSystem, SingleSystemZipper> _zippers = new Dictionary<ParticleSystem, SingleSystemZipper>();

        /// <summary>
        /// Death sub-emitter chains are followed this deep. Real content is two or three stages; the limit
        /// exists alongside the chain check for the odd authoring where a child lists its parent as a
        /// sub-emitter.
        /// </summary>
        private const int MaxStageDepth = 8;

        /// <summary>
        /// Seconds from the moment <paramref name="effect"/> is played until it has stopped firing. Main-thread
        /// only, and the buffers are shared between calls, so don't call it re-entrantly.
        /// </summary>
        public static float MeasureFiringDurationInSeconds(ParticleSystem effect)
        {
            if (effect == null)
                return 0f;

            _systemsBuffer.Clear();
            effect.GetComponentsInChildren(true, _systemsBuffer);

            _subEmitters.Clear();
            for (int i = 0; i < _systemsBuffer.Count; i++)
                CollectSubEmitters(_systemsBuffer[i]);

            CollectZippers(effect);

            var latest = 0f;

            for (int i = 0; i < _systemsBuffer.Count; i++)
            {
                var system = _systemsBuffer[i];

                //The effect itself is always measured - it is the one that gets played - even in the odd
                //authoring where something below it lists it as a sub-emitter. Skipping it there would
                //leave nothing to measure at all and hand back 0, which reads as "already spent"
                if (system != effect && _subEmitters.Contains(system))
                    continue;

                if (IsActiveWithin(system, effect) == false)
                    continue;

                _stageChain.Clear();
                var end = MeasureFiringEnd(system, effect);

                if (end > latest)
                    latest = end;
            }

            return latest;
        }

        /// <summary>
        /// The zippers driving systems in this effect, keyed by the system each one drives.
        ///
        /// By <see cref="SingleSystemZipper.MainSystem"/> rather than by the zipper's own GameObject: that
        /// is a plain field a zipper can aim anywhere, and although it defaults to the system beside it,
        /// keying on the object would quietly miss any that does not.
        /// </summary>
        private static void CollectZippers(ParticleSystem effect)
        {
            _zippers.Clear();
            _zipperBuffer.Clear();

            effect.GetComponentsInChildren(true, _zipperBuffer);

            for (int i = 0; i < _zipperBuffer.Count; i++)
            {
                var zipper = _zipperBuffer[i];
                if (zipper == null || zipper.MainSystem == null)
                    continue;

                _zippers[zipper.MainSystem] = zipper;
            }
        }

        /// <summary>
        /// Whether a zipper drives this system, and for how long it keeps it emitting.
        ///
        /// A zipper that fires no bursts, or runs no cycles, emits nothing at all - it must not hand back a
        /// window, or an inert one would keep the firework alive for its full configured time.
        /// </summary>
        private static bool ZipperFires(ParticleSystem system, out float durationInSeconds)
        {
            durationInSeconds = 0f;

            if (_zippers.TryGetValue(system, out var zipper) == false || zipper == null)
                return false;

            if (zipper.NumberOfBursts <= 0 || zipper.NumberOfCycles <= 0)
                return false;

            durationInSeconds = zipper.FiringDurationInSeconds;
            return true;
        }

        private static void CollectSubEmitters(ParticleSystem system)
        {
            var subEmitters = system.subEmitters;
            if (subEmitters.enabled == false)
                return;

            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                var subEmitter = subEmitters.GetSubEmitterSystem(i);
                if (subEmitter != null)
                    _subEmitters.Add(subEmitter);
            }
        }

        /// <summary>
        /// When this system has fired its last: its own emission window, and then - if it has a Death
        /// sub-emitter that emits - the lifetime of its particles, because that stage fires when they die,
        /// plus whatever that stage fires for. A system with no such stage is the last one, and what its
        /// particles do after they are thrown is the tail, not the firework.
        /// </summary>
        private static float MeasureFiringEnd(ParticleSystem system, ParticleSystem effect)
        {
            if (Emits(system) == false)
                return 0f;

            //A zipper emits from code and blanks the emission module of the system it drives, so its own
            //number IS that system's emission window. It already includes the zipper's StartDelay, which
            //UpdateParticleSystem also copies onto the system's main module whenever the zipper has cycle
            //delays - taking both would push every multi-cycle cake late.
            var end = ZipperFires(system, out var zipperDuration)
                ? zipperDuration
                : MaxOf(system.main.startDelay) + MeasureOwnEmissionWindow(system);

            if (_stageChain.Count >= MaxStageDepth)
                return end;

            var subEmitters = system.subEmitters;
            if (subEmitters.enabled == false)
                return end;

            var nextStage = -1f;
            _stageChain.Add(system);

            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                if (subEmitters.GetSubEmitterType(i) != ParticleSystemSubEmitterType.Death)
                    continue;

                var subEmitter = subEmitters.GetSubEmitterSystem(i);
                if (subEmitter == null || _stageChain.Contains(subEmitter) || IsActiveWithin(subEmitter, effect) == false || Emits(subEmitter) == false)
                    continue;

                var stageEnd = MeasureFiringEnd(subEmitter, effect);
                if (stageEnd > nextStage)
                    nextStage = stageEnd;
            }

            _stageChain.RemoveAt(_stageChain.Count - 1);

            if (nextStage >= 0f)
                end += MaxOf(system.main.startLifetime) + nextStage;

            return end;
        }

        private static bool Emits(ParticleSystem system)
        {
            //Checked before the emission module, not after it: a zipper-driven system is authored with a
            //blank one, so reading that module alone answers "never emits" for a firework that fires
            //hundreds of shots
            if (ZipperFires(system, out _))
                return true;

            var emission = system.emission;
            if (emission.enabled == false)
                return false;

            if (IsEverNonZero(emission.rateOverTime) || IsEverNonZero(emission.rateOverDistance))
                return true;

            for (int i = 0; i < emission.burstCount; i++)
            {
                if (MaxOf(emission.GetBurst(i).count) > 0f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether a system plays at all, judged from the prefab: activeInHierarchy is always false on an
        /// asset, and the effect root itself is left out of the walk because the behaviors switch it on when
        /// they play it. Asked of sub-emitters too - a disabled one stays silent even when its parent
        /// references it.
        /// </summary>
        private static bool IsActiveWithin(ParticleSystem system, ParticleSystem effect)
        {
            var current = system.transform;
            var root    = effect.transform;

            while (current != null && current != root)
            {
                if (current.gameObject.activeSelf == false)
                    return false;

                current = current.parent;
            }

            return true;
        }

        /// <summary>
        /// How long this one system keeps emitting after it starts.
        /// </summary>
        private static float MeasureOwnEmissionWindow(ParticleSystem system)
        {
            var emission = system.emission;
            if (emission.enabled == false)
                return 0f;

            //Looping is read as "runs for its duration" rather than as forever, so that measuring the
            //prefab gives the same answer as measuring the live instance would: a looping non sub-emitter
            //is un-looped by Extensions.DisableEndlessLooping in Awake, which has not run on the prefab.
            //Rare in practice - measured across the base game, only 3 of 58 firework effects have a
            //looping non sub-emitter at all, and all three are Mod Tools dummy templates, which is the
            //mis-authored content that warning exists to catch. Plenty of systems here still read as
            //looping, but those are sub-emitters: they are meant to loop and nothing ever un-loops them.
            var main   = system.main;
            var window = 0f;

            //Emission by time or by distance runs for as long as the system does. A curve is not read key
            //by key on purpose - the full duration is the safe answer and this is the rarer authoring
            if (IsEverNonZero(emission.rateOverTime) || IsEverNonZero(emission.rateOverDistance))
                window = main.duration;

            for (int i = 0; i < emission.burstCount; i++)
            {
                var burst = emission.GetBurst(i);

                //A cycle count of 0 means "repeat for as long as the system runs"
                var lastCycle = burst.cycleCount <= 0
                    ? main.duration
                    : burst.time + ((burst.cycleCount - 1) * burst.repeatInterval);

                if (lastCycle > window)
                    window = lastCycle;
            }

            //Bursts are authored against the system's duration and Unity does not fire one past it
            return Mathf.Min(window, main.duration);
        }

        private static bool IsEverNonZero(ParticleSystem.MinMaxCurve rate)
        {
            switch (rate.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return rate.constant > 0f;

                case ParticleSystemCurveMode.TwoConstants:
                    return rate.constantMax > 0f;

                //A curve could be zero throughout, but reading that off reliably is more trouble than the
                //rare authoring is worth - assume it emits and measure late
                default:
                    return true;
            }
        }

        private static float MaxOf(ParticleSystem.MinMaxCurve value)
        {
            switch (value.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return value.constant;

                case ParticleSystemCurveMode.TwoConstants:
                    return value.constantMax;

                case ParticleSystemCurveMode.Curve:
                    return MaxOf(value.curve) * value.curveMultiplier;

                default:
                    return Mathf.Max(MaxOf(value.curveMin), MaxOf(value.curveMax)) * value.curveMultiplier;
            }
        }

        private static float MaxOf(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            var max = curve[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                if (curve[i].value > max)
                    max = curve[i].value;
            }

            return max;
        }
    }
}
