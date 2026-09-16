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

            _effect.DisableEndlessLooping();
            StopEffect();
        }

        //Exposed only so the duration catalog can measure it on the prefab (#2651)
        public override ParticleSystem PrimaryEffect => _effect;

        private void StopEffect()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            Messenger.Broadcast(new MessengerEventPlaySoundStruct(_coreSound, this.transform, followTransform: true));

            //The core sound follows the hiss: it stops when the fountain stops firing, which for a
            //continuous emitter like this is exactly what isEmitting reports. The spawn limit does not hang
            //off this - IsSpent counts the firework's duration down on its own (#2657)
            await UniTask.WaitWhile(() => _effect.isEmitting, cancellationToken: token);

            Messenger.Broadcast(new MessengerEventStopSoundStruct(_coreSound, this.transform));
            Messenger.Broadcast(new MessengerEventPlaySoundStruct(_endSound, this.transform, followTransform: true));

            await UniTask.WaitWhile(() => _effect.IsAlive(true) || _effect.isPlaying, cancellationToken: token);

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject is active
            StopEffect();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        public override void OnDestroy()
        {
            Messenger.Broadcast(new MessengerEventStopSoundStruct(_coreSound, this.transform));
            
            base.OnDestroy();            
        }
    }
}
