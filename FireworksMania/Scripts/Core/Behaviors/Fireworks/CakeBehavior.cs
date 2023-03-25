using System.Threading;
using Cysharp.Threading.Tasks;
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

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody", this);

            if (_effect == null)
                Debug.LogError($"Missing particle effects in {nameof(CakeBehavior)}!");

            StopAllEffects();
        }

        protected override void Start()
        {
            base.Start();
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
            _effect.Play(true);

            await UniTask.WaitWhile(() => _effect.IsAlive() || _effect.isPlaying, cancellationToken: token);

            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void StopAllEffects()
        {
            _effect.Stop();
            _effect.gameObject.SetActive(false);
        }
    }
}
