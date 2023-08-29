using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
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
            _OnLaunched?.Invoke();

            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            ApplyRecoilForce();

            await UniTask.WaitWhile(() => _effect.IsAlive() || _effect.isPlaying, cancellationToken: token);

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void ApplyRecoilForce()
        {
            _rigidbody.AddForceAtPosition(-_rigidbody.transform.up * _recoilForce, _effect.transform.position, ForceMode.Impulse);
        }

        private void StopEffect()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }
    }
}
