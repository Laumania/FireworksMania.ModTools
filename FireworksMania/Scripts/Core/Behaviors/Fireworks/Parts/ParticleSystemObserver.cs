using System;
using System.Collections.Generic;
using FireworksMania.Core.Common;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemObserver")]
    public class ParticleSystemObserver : MonoBehaviour
    {
        private const int MaxParticlesBufferSize = 10000;

        // Store only the last-known position (Vector3, 12 bytes) rather than the full
        // ParticleSystem.Particle struct (~120+ bytes) to reduce per-frame memory
        // operations when many fireworks are active simultaneously.
        private readonly Dictionary<uint, Vector3> _trackedParticlePositions = new Dictionary<uint, Vector3>();
        private readonly List<Vector3>             _addedParticlePositions   = new List<Vector3>();
        private readonly List<Vector3>             _removedParticlePositions = new List<Vector3>();
        private readonly HashSet<uint>             _currentSeeds             = new HashSet<uint>();
        private readonly List<uint>                _keysToRemove             = new List<uint>();

        private ParticleSystem _observedParticleSystem;

        public Action<Vector3> OnParticleSpawned;
        public Action<Vector3> OnParticleDestroyed;

        private bool _hasBeenAliveOnce           = false;
        private bool _hasCheckedBufferCapWarning = false;

        /// <summary>
        /// The item this observer's effect belongs to, for the log. Only set where the hierarchy can't tell: a
        /// shell effect loaded into a mortar tube hangs under the tube, which would otherwise be blamed for it.
        /// </summary>
        internal string ItemName { get; set; }

        private void Start()
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

        private void Update()
        {
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
                return;

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
    }
}
