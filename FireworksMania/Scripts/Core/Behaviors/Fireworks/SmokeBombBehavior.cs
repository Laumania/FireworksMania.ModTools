using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/SmokeBombBehavior")]
    public class SmokeBombBehavior : BaseFireworkBehavior
    {
        [Header("SmokeBomb Settings")]
        [SerializeField]
        private ParticleSystem _smokeEffect;
        [SerializeField]
        private ExplosionPhysicsForceEffect _ignitionExplosionEffect;

        [GameSound]
        [SerializeField]
        private string _sound;

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody on rocket", this);

            if (_ignitionExplosionEffect == null)
                Debug.LogError($"Missing '{nameof(ExplosionPhysicsForceEffect)}' on {nameof(SmokeBombBehavior)}", this);

            if (_smokeEffect == null)
                Debug.LogError($"Missing particle effect in {nameof(SmokeBombBehavior)}!");

            StopAllEffects();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (GetComponent<Rigidbody>() == null)
                _rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _smokeEffect.gameObject.SetActive(true);
            _smokeEffect.Play(true);
            _ignitionExplosionEffect.ApplyExplosionForce(false, false, true);

            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform, followTransform: true));
            await UniTask.WaitWhile(() => _smokeEffect.isEmitting, cancellationToken: token);
            Messenger.Broadcast(new MessengerEventStopSound(_sound, this.transform));

            await UniTask.WaitWhile(() => _smokeEffect.IsAlive() || _smokeEffect.isPlaying, cancellationToken: token);

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void StopAllEffects()
        {
            _smokeEffect.Stop();
            _smokeEffect.gameObject.SetActive(false);
        }
    }
}
