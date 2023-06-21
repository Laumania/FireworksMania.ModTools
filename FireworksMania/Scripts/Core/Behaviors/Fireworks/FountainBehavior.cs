using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/FountainBehavior")]
    public class FountainBehavior : BaseFireworkBehavior
    {
        [Header("Fountain Settings")]
        [SerializeField]
        private ParticleSystem _effect;

        [SerializeField]
        [GameSound]
        private string _startSound;

        [SerializeField]
        [GameSound]
        private string _coreSound;

        [SerializeField]
        [GameSound]
        private string _endSound;

        protected override void Awake()
        {
            base.Awake();

            if (_effect == null)
                Debug.LogError($"Missing particle effects in {nameof(FountainBehavior)}!");

            StopEffect();
        }

        private void StopEffect()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _effect.SetRandomSeed(_effectSeed.Value);
            _effect.gameObject.SetActive(true);
            _effect.Play(true);

            Messenger.Broadcast(new MessengerEventPlaySound(_coreSound, this.transform, followTransform: true));

            await UniTask.WaitWhile(() => _effect.isEmitting, cancellationToken: token);

            Messenger.Broadcast(new MessengerEventStopSound(_coreSound, this.transform));
            Messenger.Broadcast(new MessengerEventPlaySound(_endSound, this.transform, followTransform: true));

            await UniTask.WaitWhile(() => _effect.IsAlive(true) || _effect.isPlaying, cancellationToken: token);

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        public override void OnDestroy()
        {
            Messenger.Broadcast(new MessengerEventStopSound(_coreSound, this.transform));
            
            base.OnDestroy();            
        }
    }
}
