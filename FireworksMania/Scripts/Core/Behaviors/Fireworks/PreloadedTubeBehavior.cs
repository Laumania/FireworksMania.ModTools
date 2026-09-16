using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/PreloadedTubeBehavior")]
    public class PreloadedTubeBehavior : BaseFireworkBehavior
    {
        [Header("Preloaded Tube Settings")]
        [SerializeField]
        private ParticleSystem _effect;
        [SerializeField]
        private float _recoilForce = 100f;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _OnLaunched;

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody", this);

            if (_effect == null)
                Debug.LogError($"Missing particle effect", this);

            _effect.DisableEndlessLooping();
            StopEffect();
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();

            if (GetComponent<Rigidbody>() == null)
                _rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            //Guarded: a throwing listener here would skip the effect, the recoil force and the await
            //below, so the tube would never launch at all.
            _OnLaunched.InvokeSafe(this, nameof(_OnLaunched));

            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            ApplyRecoilForce();

            await WaitForEffectToFinishAsync(_effect, token);

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject is active
            StopEffect();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void ApplyRecoilForce()
        {
            _rigidbody.AddForceAtPosition(-_rigidbody.transform.up * _recoilForce, _effect.transform.position, ForceMode.Impulse);
        }

        //Exposed only so the duration catalog can measure it on the prefab (#2651)
        public override ParticleSystem PrimaryEffect => _effect;

        private void StopEffect()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }
    }
}
