using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/RomanCandleBehavior")]
    public class RomanCandleBehavior : BaseFireworkBehavior
    {
        [Header("Roman Candle Settings")]
        [SerializeField]
        private ParticleSystem _effect;

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody", this);

            if (_effect == null)
                Debug.LogError($"Missing particle effects", this);

            _effect.DisableEndlessLooping();
            StopAllEffects();
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();

            if (GetComponent<Rigidbody>() == null)
                _rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            await WaitForEffectToFinishAsync(_effect, token);

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject is active
            StopAllEffects();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        //Exposed only so the duration catalog can measure it on the prefab (#2651)
        public override ParticleSystem PrimaryEffect => _effect;

        private void StopAllEffects()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }
    }
}
