using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
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

            _smokeEffect.DisableEndlessLooping();
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
            _smokeEffect.gameObject.SetActive(true);
            _smokeEffect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _smokeEffect.Play(true);
            _ignitionExplosionEffect.ApplyExplosionForce(false, false, true);

            Messenger.Broadcast(new MessengerEventPlaySoundStruct(_sound, this.transform, followTransform: true));
            //The sound follows the pumping and stops when the bomb stops emitting; the spawn limit does not
            //hang off this - IsSpent counts the firework's duration down on its own (#2657)
            await UniTask.WaitWhile(() => _smokeEffect.isEmitting, cancellationToken: token);

            Messenger.Broadcast(new MessengerEventStopSoundStruct(_sound, this.transform));

            await UniTask.WaitWhile(() => _smokeEffect.IsAlive() || _smokeEffect.isPlaying, cancellationToken: token);

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject is active
            StopAllEffects();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        //Exposed only so the duration catalog can measure it on the prefab (#2651)
        public override ParticleSystem PrimaryEffect => _smokeEffect;

        private void StopAllEffects()
        {
            _smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _smokeEffect.gameObject.SetActive(false);
        }
    }
}
