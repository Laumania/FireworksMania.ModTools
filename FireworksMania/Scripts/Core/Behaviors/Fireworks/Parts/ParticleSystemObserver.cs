﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemObserver")]
    public class ParticleSystemObserver : MonoBehaviour
    {
        private IDictionary<uint, ParticleSystem.Particle> _trackedParticles = new Dictionary<uint, ParticleSystem.Particle>();
        private ParticleSystem _observedParticleSystem;

        public Action<Vector3> OnParticleSpawned;
        public Action<Vector3> OnParticleDestroyed;

        private bool _hasBeenAliveOnce = false;

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

            var liveParticles = new ParticleSystem.Particle[_observedParticleSystem.particleCount];
            _observedParticleSystem.GetParticles(liveParticles);

            var particleDelta = GetParticleDelta(liveParticles);

            foreach (var particleRemoved in particleDelta.Removed)
            {
                if (OnParticleDestroyed != null)
                    OnParticleDestroyed.Invoke(particleRemoved.position);
            }

            foreach (var particleAdded in particleDelta.Added)
            {
                if (OnParticleSpawned != null)
                    OnParticleSpawned.Invoke(particleAdded.position);
            }
        }

        private ParticleDelta GetParticleDelta(ParticleSystem.Particle[] liveParticles)
        {
            var deltaResult = new ParticleDelta();

            foreach (var activeParticle in liveParticles)
            {
                ParticleSystem.Particle foundParticle;
                if (_trackedParticles.TryGetValue(activeParticle.randomSeed, out foundParticle))
                {
                    _trackedParticles[activeParticle.randomSeed] = activeParticle;
                }
                else
                {
                    deltaResult.Added.Add(activeParticle);
                    _trackedParticles.Add(activeParticle.randomSeed, activeParticle);
                }
            }

            var updatedParticleAsDictionary = liveParticles.ToDictionary(x => x.randomSeed, x => x);
            var dictionaryKeysAsList = _trackedParticles.Keys.ToList();

            foreach (var dictionaryKey in dictionaryKeysAsList)
            {
                if (updatedParticleAsDictionary.ContainsKey(dictionaryKey) == false)
                {
                    deltaResult.Removed.Add(_trackedParticles[dictionaryKey]);
                    _trackedParticles.Remove(dictionaryKey);
                }
            }

            return deltaResult;
        }

        private class ParticleDelta
        {
            public IList<ParticleSystem.Particle> Added { get; set; } = new List<ParticleSystem.Particle>();
            public IList<ParticleSystem.Particle> Removed { get; set; } = new List<ParticleSystem.Particle>();
        }
    }
}
