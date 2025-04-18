﻿using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/CakeBehavior")]
    public class CakeBehavior : BaseFireworkBehavior
    {
        [Header("Cake Settings")]
        [SerializeField]
        private ParticleSystem _effect;

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            Preconditions.CheckNotNull(_rigidbody, $"Missing Rigidbody on '{this.gameObject.name}'");
            Preconditions.CheckNotNull(_effect, $"Missing particle effects in {nameof(CakeBehavior)} - '{this.gameObject.name}'!");

            AvoidLoopingEffect();
            StopAllEffects();
        }

        private void AvoidLoopingEffect()
        {
            var mainParticleSystem = _effect.main;
            mainParticleSystem.loop = false;
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying)
                return;

            base.OnValidate();

            if (_effect == null)
            {
                Debug.LogError($"Missing particle effect on gameobject '{this.gameObject.name}' on component '{nameof(CakeBehavior)}'!", this.gameObject);
            }
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            await UniTask.WaitWhile(() => _effect.IsAlive() || _effect.isPlaying, cancellationToken: token);

            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void StopAllEffects()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }
    }
}
