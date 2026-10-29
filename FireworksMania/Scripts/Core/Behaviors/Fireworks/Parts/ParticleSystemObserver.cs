using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Reports every particle its ParticleSystem spawns and every one that dies, by comparing what is alive from one
    /// frame to the next.
    /// <para>
    /// A comparison only sees what is alive when it looks, and Unity moves a whole frame's worth of particle time in
    /// one go after the scripts have run. A particle that is born and dies inside that step is never alive when the
    /// observer looks, so it got no spawn and no destroy event - which is no sound and no explosion force. That is a
    /// real particle only once the frame gets longer than its life: the Lady Finger's bang lives 20-40 ms, and at 20
    /// FPS only one in five of them was heard (#3094).
    /// </para>
    /// <para>
    /// So a system whose particles can live shorter than <see cref="ShortLivedParticleLifetimeInSeconds"/> also gets a
    /// Death "probe": a hidden sub-emitter that Unity hands one particle, at the spot, for every particle of the system
    /// that dies - mid-step or not. Whatever died that the comparison never saw is reported from there, as a spawn and
    /// a destroy at the position it died. The probe only ever adds what was missed, so at a frame rate where nothing is
    /// missed the events are exactly what they always were.
    /// </para>
    /// </summary>
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemObserver")]
    public class ParticleSystemObserver : MonoBehaviour, IReadsParticleSystems
    {
        private const int MaxParticlesBufferSize = 10000;

        /// <summary>
        /// A system whose particles can live shorter than this, in real seconds, gets a death probe. The longest step
        /// Unity takes is <c>Time.maximumDeltaTime</c> - 0.05 s, or 0.08 s while the PerformanceManager throttles
        /// physics - so anything longer-lived than three of those is always alive for at least one frame. In the base
        /// game it splits cleanly: the shortest-lived observed systems live 100 ms at most, and the next ones up 200 ms.
        /// </summary>
        private const float ShortLivedParticleLifetimeInSeconds = 0.25f;

        internal const string DeathProbeName = "ParticleSystemObserver Death Probe";

        /// <summary>
        /// Not tied to the observed system's own cap: more of its particles can die in one long frame than it can hold
        /// at once. Unity does not allocate up front for this, it is only a ceiling.
        /// </summary>
        private const int   DeathProbeMaxParticles      = 100000;
        private const float DeathProbeParticleLifetime  = 1f;

        /// <summary>
        /// A death read in the frame it happened has an age of 0. An older one is history - a Simulate that ran
        /// through an already armed probe, such as a late joiner catching an effect up - and is not a particle anybody
        /// missed just now.
        /// </summary>
        private const float MaxRecoveredDeathAgeInSeconds = 0.25f;

        /// <summary>
        /// How many vanished-particle-to-death comparisons a frame may spend placing the missed particles. Past this the
        /// missed ones are taken in the order the probe holds them, which counts them right but may place them on a
        /// sibling's death instead of their own.
        /// </summary>
        private const int MaxDeathMatchingComparisons = 4096;

        private static readonly ParticleSystem.Burst[] DeathProbeBursts = { new ParticleSystem.Burst(0f, 1) };

        // Store only the last-known position (Vector3, 12 bytes) rather than the full
        // ParticleSystem.Particle struct (~120+ bytes) to reduce per-frame memory
        // operations when many fireworks are active simultaneously.
        private readonly Dictionary<uint, Vector3> _trackedParticlePositions = new Dictionary<uint, Vector3>();
        private readonly List<Vector3>             _addedParticlePositions   = new List<Vector3>();
        private readonly List<Vector3>             _removedParticlePositions = new List<Vector3>();
        private readonly HashSet<uint>             _currentSeeds             = new HashSet<uint>();
        private readonly List<uint>                _keysToRemove             = new List<uint>();

        //Only a probed observer ever reads deaths, so only it pays for these - created when it is first armed
        private List<Vector3> _deathPositions;
        private List<Vector3> _missedParticlePositions;
        private List<bool>    _claimedDeaths;

        private ParticleSystem _observedParticleSystem;
        private ParticleSystem _deathProbe;
        private Action         _armDeathProbeAction;

        public Action<Vector3> OnParticleSpawned;
        public Action<Vector3> OnParticleDestroyed;

        private bool _hasBeenAliveOnce           = false;
        private bool _hasCheckedBufferCapWarning = false;
        private bool _hasCheckedDeathProbeNeed   = false;
        private bool _needsDeathProbe            = false;
        private bool _isDeathProbeArmPending     = false;
        private bool _isDeathProbeArmed          = false;
        private bool _armingEnabledSubEmitters   = false;

        /// <summary>
        /// The item this observer's effect belongs to, for the log. Only set where the hierarchy can't tell: a
        /// shell effect loaded into a mortar tube hangs under the tube, which would otherwise be blamed for it.
        /// </summary>
        internal string ItemName { get; set; }

        //Awake rather than Start: OnEnable needs the system, and on the first life it runs before Start
        private void Awake()
        {
            _observedParticleSystem = this.GetComponent<ParticleSystem>();
            if (_observedParticleSystem == null)
                Debug.LogError($"Missing ParticleSystem on {nameof(ParticleSystemObserver)}", this);
        }

        private void OnValidate()
        {
            if(this.GetComponent<ParticleSystem>() == null)
                Debug.LogWarning($"{nameof(ParticleSystemObserver)} is missing ParticleSystem on '{this.gameObject.name}' else it will not work", this);
        }

        private void OnEnable()
        {
            if (_observedParticleSystem == null)
                return;

            if (_hasCheckedDeathProbeNeed == false)
            {
                _hasCheckedDeathProbeNeed = true;

                //Two observers on one system would share the probe, and whichever read it first would empty it
                _needsDeathProbe = NeedsDeathProbe(_observedParticleSystem) && this.GetComponent<ParticleSystemObserver>() == this;
            }

            if (_needsDeathProbe == false)
                return;

            //Armed at the start of PreLateUpdate rather than here. An effect is launched by activating it, then
            //SetRandomSeed - which on a late joiner Simulates it forward to catch up - and then Play. Armed here, the
            //probe would collect every death of that catch-up and they would all be reported as missed. UniTask runs
            //this queue first in PreLateUpdate, ahead of Unity's ParticleSystemBeginUpdateAll, so the probe still
            //exists before the first real step - which matters for a burst that dies inside that very step, like the
            //rockets' 40 ms crackle. ParticleSystemObserverDeathProbeTests.UniTaskPreLateUpdateContinuations_RunBeforeTheParticleUpdate
            //pins that order.
            _isDeathProbeArmPending = true;
            _armDeathProbeAction ??= ArmDeathProbeIfStillWanted;
            PlayerLoopHelper.AddContinuation(PlayerLoopTiming.PreLateUpdate, _armDeathProbeAction);
        }

        private void OnDisable()
        {
            DisarmDeathProbe();
        }

        private void Update()
        {
            //A probed system cannot stop here with deaths still unread: the probe's particles are the system's own
            //sub-emitter particles, which IsAlive counts, so the frame they arrive in is still a live one
            if(_hasBeenAliveOnce && _observedParticleSystem.IsAlive(false) == false)
            {
                this.enabled = false;
                //Debug.Log($"{nameof(ParticleSystemObserver)} on {this.gameObject.name} has been emitting particle once and stopped again, so this {nameof(ParticleSystemObserver)} will now be disabled for performance reasons.", this);
            }

            if (_hasBeenAliveOnce == false && _observedParticleSystem.IsAlive(false))
                _hasBeenAliveOnce = true;

            if (_observedParticleSystem.IsAlive(false) == false)
                return;

            if (OnParticleSpawned == null && OnParticleDestroyed == null)
            {
                DrainDeathProbe();
                return;
            }

            // Shared across all observers - only valid for the duration of this call, see ParticleBufferPool
            var liveParticlesBuffer = ParticleBufferPool.Rent(GetSafeBufferSize(_observedParticleSystem.main.maxParticles));
            var liveParticleCount   = _observedParticleSystem.GetParticles(liveParticlesBuffer);

            ComputeParticleDelta(liveParticlesBuffer, liveParticleCount);

            foreach (var removedPosition in _removedParticlePositions)
            {
                if (OnParticleDestroyed != null)
                    OnParticleDestroyed.Invoke(removedPosition);
            }

            foreach (var addedPosition in _addedParticlePositions)
            {
                if (OnParticleSpawned != null)
                    OnParticleSpawned.Invoke(addedPosition);
            }

            ReportParticlesMissedBetweenFrames();
        }

        private void ComputeParticleDelta(ParticleSystem.Particle[] liveParticlesBuffer, int liveParticleCount)
        {
            _addedParticlePositions.Clear();
            _removedParticlePositions.Clear();
            _currentSeeds.Clear();

            for (int i = 0; i < liveParticleCount; i++)
            {
                var particle = liveParticlesBuffer[i];
                var seed     = particle.randomSeed;
                var position = particle.position;

                _currentSeeds.Add(seed);

                if (!_trackedParticlePositions.TryGetValue(seed, out _))
                {
                    _addedParticlePositions.Add(position);
                    _trackedParticlePositions.Add(seed, position);
                }
                else
                {
                    _trackedParticlePositions[seed] = position;
                }
            }

            _keysToRemove.Clear();
            foreach (var key in _trackedParticlePositions.Keys)
            {
                if (!_currentSeeds.Contains(key))
                    _keysToRemove.Add(key);
            }

            foreach (var key in _keysToRemove)
            {
                _removedParticlePositions.Add(_trackedParticlePositions[key]);
                _trackedParticlePositions.Remove(key);
            }
        }

        /// <summary>
        /// Every particle that died this frame left one particle in the probe. Those the comparison saw vanish are
        /// already reported; any deaths beyond that are particles that lived and died between two frames, and get
        /// their spawn and their destroy now. Fewer deaths than vanished particles means some were removed without
        /// dying - a Clear, say - and then nothing is made up.
        /// </summary>
        private void ReportParticlesMissedBetweenFrames()
        {
            if (_isDeathProbeArmed == false)
                return;

            if (_deathProbe == null)
            {
                //Destroyed from under the observer: hand the system back, and the next arm builds a new one
                DisarmDeathProbe();
                return;
            }

            //Most frames nothing died, and then there is nothing to read or clear
            var deathCount = _deathProbe.particleCount;
            if (deathCount == 0)
                return;

            if (deathCount > _removedParticlePositions.Count)
            {
                var deathsBuffer = ParticleBufferPool.Rent(Mathf.Min(deathCount, MaxParticlesBufferSize));
                var readCount    = _deathProbe.GetParticles(deathsBuffer, Mathf.Min(deathCount, deathsBuffer.Length));

                _deathPositions.Clear();
                for (int i = 0; i < readCount; i++)
                {
                    var death = deathsBuffer[i];
                    if (death.startLifetime - death.remainingLifetime <= MaxRecoveredDeathAgeInSeconds)
                        _deathPositions.Add(death.position);
                }

                SelectUnmatchedDeaths(_deathPositions, _removedParticlePositions, _claimedDeaths, _missedParticlePositions);

                for (int i = 0; i < _missedParticlePositions.Count; i++)
                {
                    var missedPosition = _missedParticlePositions[i];
                    OnParticleSpawned?.Invoke(missedPosition);
                    OnParticleDestroyed?.Invoke(missedPosition);
                }
            }

            _deathProbe.Clear(false);
        }

        private void DrainDeathProbe()
        {
            if (_isDeathProbeArmed && _deathProbe != null && _deathProbe.particleCount > 0)
                _deathProbe.Clear(false);
        }

        /// <summary>
        /// The deaths no vanished particle accounts for. Every particle the frames saw vanish died too, so each one
        /// first claims the death nearest to where it was last seen; what is left over are the particles nobody saw.
        /// </summary>
        private static void SelectUnmatchedDeaths(List<Vector3> deathPositions, List<Vector3> vanishedPositions, List<bool> claimed, List<Vector3> unmatched)
        {
            unmatched.Clear();

            var missedCount = deathPositions.Count - vanishedPositions.Count;
            if (missedCount <= 0)
                return;

            if (vanishedPositions.Count == 0 || vanishedPositions.Count * deathPositions.Count > MaxDeathMatchingComparisons)
            {
                for (int i = deathPositions.Count - missedCount; i < deathPositions.Count; i++)
                    unmatched.Add(deathPositions[i]);
                return;
            }

            claimed.Clear();
            for (int i = 0; i < deathPositions.Count; i++)
                claimed.Add(false);

            for (int v = 0; v < vanishedPositions.Count; v++)
            {
                var nearest         = -1;
                var nearestDistance = float.MaxValue;

                for (int d = 0; d < deathPositions.Count; d++)
                {
                    if (claimed[d])
                        continue;

                    var distance = (deathPositions[d] - vanishedPositions[v]).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearest         = d;
                        nearestDistance = distance;
                    }
                }

                //Only when every distance is NaN - a degenerate particle - is nothing nearest
                if (nearest >= 0)
                    claimed[nearest] = true;
            }

            for (int d = 0; d < deathPositions.Count; d++)
            {
                if (claimed[d] == false)
                    unmatched.Add(deathPositions[d]);
            }
        }

        /// <summary>
        /// Whether particles of this system can live and die between two frames, and a probe can catch them.
        /// </summary>
        private static bool NeedsDeathProbe(ParticleSystem system)
        {
            var main = system.main;

            //The probe records where a particle died in world space, which is what such a system reports anyway
            if (main.simulationSpace != ParticleSystemSimulationSpace.World)
                return false;

            if (main.simulationSpeed <= 0f)
                return false;

            //A ring buffer recycles particles instead of letting them die, so there would be no deaths to read
            if (main.ringBufferMode != ParticleSystemRingBufferMode.Disabled)
                return false;

            //Switching the module on for the probe would also bring back the sub-emitters the author switched off
            var subEmitters = system.subEmitters;
            if (subEmitters.enabled == false && HasAnySubEmitter(subEmitters, besides: null))
                return false;

            return ShortestValue(main.startLifetime) / main.simulationSpeed < ShortLivedParticleLifetimeInSeconds;
        }

        //Unity serializes an unused module with one empty entry, so the count alone says nothing
        private static bool HasAnySubEmitter(ParticleSystem.SubEmittersModule subEmitters, ParticleSystem besides)
        {
            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                var subEmitter = subEmitters.GetSubEmitterSystem(i);
                if (subEmitter != null && subEmitter != besides)
                    return true;
            }

            return false;
        }

        private static float ShortestValue(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return Mathf.Min(curve.constantMin, curve.constantMax);
                case ParticleSystemCurveMode.Curve:
                    return ShortestKey(curve.curve) * curve.curveMultiplier;
                default:
                    return Mathf.Min(ShortestKey(curve.curveMin), ShortestKey(curve.curveMax)) * curve.curveMultiplier;
            }
        }

        private static float ShortestKey(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            var shortest = float.MaxValue;
            for (int i = 0; i < curve.length; i++)
                shortest = Mathf.Min(shortest, curve[i].value);

            return shortest;
        }

        private void ArmDeathProbeIfStillWanted()
        {
            //Pending is cleared by OnDisable, so a deactivation before PreLateUpdate cancels the arm. UniTask also runs
            //whatever is still queued when play mode ends, by which time this can be destroyed.
            if (this == null || _isDeathProbeArmPending == false)
                return;

            ArmDeathProbe();
        }

        private void ArmDeathProbe()
        {
            _isDeathProbeArmPending = false;

            if (_isDeathProbeArmed)
                return;

            if (_deathProbe == null)
                _deathProbe = FindOrCreateDeathProbe();

            //A probe from an earlier life can have been played along with the effect, and a playing probe would
            //emit its own burst the moment its emission comes on
            _deathProbe.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var probeEmission     = _deathProbe.emission;
            probeEmission.enabled = true;

            //A copy of a live instance arrives with the module already on and only the probe in it - the author had it off
            var subEmitters           = _observedParticleSystem.subEmitters;
            _armingEnabledSubEmitters = subEmitters.enabled == false || HasAnySubEmitter(subEmitters, besides: _deathProbe) == false;
            subEmitters.enabled       = true;

            if (ContainsSubEmitter(subEmitters, _deathProbe) == false)
                subEmitters.AddSubEmitter(_deathProbe, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

            _deathPositions          ??= new List<Vector3>();
            _missedParticlePositions ??= new List<Vector3>();
            _claimedDeaths           ??= new List<bool>();

            _isDeathProbeArmed = true;
        }

        /// <summary>
        /// Hands the system back exactly as it was authored, so a pooled effect or one that is played again starts
        /// from the same place. The probe itself stays, emptied and silent, for the next arm.
        /// </summary>
        private void DisarmDeathProbe()
        {
            _isDeathProbeArmPending = false;

            if (_isDeathProbeArmed == false)
                return;

            _isDeathProbeArmed = false;

            if (_observedParticleSystem != null)
            {
                var subEmitters = _observedParticleSystem.subEmitters;
                if (_deathProbe != null)
                    subEmitters.RemoveSubEmitter(_deathProbe);

                if (_armingEnabledSubEmitters)
                    subEmitters.enabled = false;
            }

            _armingEnabledSubEmitters = false;

            if (_deathProbe != null)
            {
                var probeEmission     = _deathProbe.emission;
                probeEmission.enabled = false;
                _deathProbe.Clear(false);
            }
        }

        /// <summary>
        /// Whether <paramref name="subEmitter"/> is the death probe an observer on <paramref name="system"/> added, which
        /// is not a stage of the firework - see <see cref="ParticleEffectDuration"/>.
        /// </summary>
        internal static bool IsDeathProbe(ParticleSystem system, ParticleSystem subEmitter)
        {
            return subEmitter != null &&
                   system.TryGetComponent(out ParticleSystemObserver observer) &&
                   observer._deathProbe == subEmitter;
        }

        private static bool ContainsSubEmitter(ParticleSystem.SubEmittersModule subEmitters, ParticleSystem subEmitter)
        {
            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                if (subEmitters.GetSubEmitterSystem(i) == subEmitter)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Takes over the probe a copy of a live instance brought along, or builds one: a system that never draws,
        /// never moves and emits only when the observed system hands it a death.
        /// </summary>
        private ParticleSystem FindOrCreateDeathProbe()
        {
            var existing = this.transform.Find(DeathProbeName);
            if (existing != null && existing.TryGetComponent(out ParticleSystem existingProbe))
                return existingProbe;

            //Built inactive: a ParticleSystem added to an active object starts playing its default emission at once
            var probeObject   = new GameObject(DeathProbeName);
            probeObject.layer = this.gameObject.layer;
            probeObject.SetActive(false);
            probeObject.transform.SetParent(this.transform, false);

            var probe          = probeObject.AddComponent<ParticleSystem>();
            var observedMain   = _observedParticleSystem.main;

            var main             = probe.main;
            main.duration        = DeathProbeParticleLifetime;
            main.loop            = false;
            main.prewarm         = false;
            main.playOnAwake     = false;
            main.startLifetime   = DeathProbeParticleLifetime;
            main.startSpeed      = 0f;
            main.startSize       = 0f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = DeathProbeMaxParticles;
            main.useUnscaledTime = observedMain.useUnscaledTime;
            main.cullingMode     = ParticleSystemCullingMode.AlwaysSimulate;   //it draws nothing, so it is never "visible"

            var emission          = probe.emission;
            emission.enabled      = false;
            emission.rateOverTime = 0f;
            emission.SetBursts(DeathProbeBursts);

            var shape     = probe.shape;
            shape.enabled = false;

            probeObject.GetComponent<ParticleSystemRenderer>().enabled = false;

            probeObject.SetActive(true);
            return probe;
        }

        private int GetSafeBufferSize(int maxParticles)
        {
            if (maxParticles > MaxParticlesBufferSize)
            {
                //Asked every frame the effect plays, so each instance only looks once - resolving the item and
                //building the key both allocate
                if (!_hasCheckedBufferCapWarning)
                {
                    _hasCheckedBufferCapWarning = true;
                    WarnAboutBufferCap(maxParticles);
                }
                return MaxParticlesBufferSize;
            }
            return maxParticles;
        }

        /// <summary>
        /// Logged once per item per session, not per instance: every copy of the same firework has the same
        /// maxParticles, so a client's log used to get the same line again for every one launched (#2856).
        /// </summary>
        private void WarnAboutBufferCap(int maxParticles)
        {
            if (ContentWarnings.AreLogged == false)
                return;

            var itemName = string.IsNullOrEmpty(ItemName) ? ContentWarnings.ResolveItemName(this) : ItemName;
            if (ContentWarnings.ShouldLog(nameof(ParticleSystemObserver), itemName, this.gameObject.name) == false)
                return;

            //Hardcoded English on purpose - it tells whoever built the content which particle system to fix
            Debug.LogWarning($"{nameof(ParticleSystemObserver)} on '{ContentWarnings.WithoutCloneSuffix(this.gameObject.name)}' in '{itemName}' has maxParticles={maxParticles} which exceeds the safe buffer cap of {MaxParticlesBufferSize}. Capping buffer to avoid OutOfMemoryException. This is only logged once - the buffer is still capped on every instance you place.", this);
        }

        /// <summary>The observed system and its death probe (#3214). Switching the probe off would lose the deaths it catches.</summary>
        public void CollectReadParticleSystems(List<ParticleSystem> results)
        {
            if (_observedParticleSystem != null)
                results.Add(_observedParticleSystem);
            if (_deathProbe != null)
                results.Add(_deathProbe);
        }
    }
}
