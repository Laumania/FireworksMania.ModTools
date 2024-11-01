using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/RocketStrobeBehavior")]
    public class RocketStrobeBehavior : RocketBehavior
    {
        [Header("Rocket Strobe Settings")]
        [SerializeField]
        [GameSound]
        private string _startWhistleSound;
        [SerializeField]
        [GameSound]
        private string _endWhistleSound;
        [SerializeField]
        private float _hangTimeInSecondsAfterThrusterFinish = 1.5f;

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _thruster.TurnOn();
            Messenger.Broadcast(new MessengerEventPlaySound(_startWhistleSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));

            await UniTask.Delay(200, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            await UniTask.WaitWhile(() => _thruster.IsThrusting, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            Messenger.Broadcast(new MessengerEventPlaySound(_endWhistleSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));

            //Hang time
            var randomTimeFactor = _randomTimeDelayAfterThruster ? UnityEngine.Random.Range(0.9f, 1.1f) : 1f;
            //Debug.Log($"randomTimeFactor: {randomTimeFactor}");
            await UniTask.Delay(Mathf.RoundToInt(_hangTimeInSecondsAfterThrusterFinish * 1000f * randomTimeFactor), cancellationToken: token);
            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
            {
                DisableRigidBodyAndColliders();
                _model.SetActive(false);
            }
            
            _explosion.Explode();
            await UniTask.WaitWhile(() => _explosion.IsExploding, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
            {
                await DestroyFireworkAsync(token);
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
