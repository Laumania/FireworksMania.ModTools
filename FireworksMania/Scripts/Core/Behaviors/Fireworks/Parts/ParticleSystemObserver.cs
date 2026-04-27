using System;
using System.Collections.Generic;
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

        private ParticleSystem          _observedParticleSystem;
        private ParticleSystem.Particle[] _liveParticlesBuffer = Array.Empty<ParticleSystem.Particle>();

        public Action<Vector3> OnParticleSpawned;
        public Action<Vector3> OnParticleDestroyed;

        private bool _hasBeenAliveOnce = false;
        private bool _hasLoggedBufferCapWarning = false;

        private void Start()
        {
            _observedParticleSystem = this.GetComponent<ParticleSystem>();
            if (_observedParticleSystem == null)
            {
                Debug.LogError($"Missing ParticleSystem on {nameof(ParticleSystemObserver)}", this);
                return;
            }

            _liveParticlesBuffer = new ParticleSystem.Particle[GetSafeBufferSize(_observedParticleSystem.main.maxParticles)];
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

            var safeBufferSize = GetSafeBufferSize(_observedParticleSystem.main.maxParticles);
            if (_liveParticlesBuffer.Length < safeBufferSize)
                _liveParticlesBuffer = new ParticleSystem.Particle[safeBufferSize];

            var liveParticleCount = _observedParticleSystem.GetParticles(_liveParticlesBuffer);

            ComputeParticleDelta(liveParticleCount);

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

        private void ComputeParticleDelta(int liveParticleCount)
        {
            _addedParticlePositions.Clear();
            _removedParticlePositions.Clear();
            _currentSeeds.Clear();

            for (int i = 0; i < liveParticleCount; i++)
            {
                var particle = _liveParticlesBuffer[i];
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
                if (!_hasLoggedBufferCapWarning)
                {
                    Debug.LogWarning($"{nameof(ParticleSystemObserver)} on '{this.gameObject.name}' has maxParticles={maxParticles} which exceeds the safe buffer cap of {MaxParticlesBufferSize}. Capping buffer to avoid OutOfMemoryException.", this);
                    _hasLoggedBufferCapWarning = true;
                }
                return MaxParticlesBufferSize;
            }
            return maxParticles;
        }
    }
}
