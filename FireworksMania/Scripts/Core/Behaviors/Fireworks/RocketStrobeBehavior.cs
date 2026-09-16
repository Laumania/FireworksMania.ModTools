using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
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

        //LaunchInternalAsync stretches the hang time by up to this much when the random delay is on; the
        //duration estimate takes all of it (#2657)
        private const float MaxHangTimeFactor = 1.1f;

        /// <summary>Thrust, the hang time, then the burst (#2657).</summary>
        public override float EstimateDurationInSeconds()
        {
            var seconds = 0f;

            if (_thruster.OrNull() != null)
                seconds += _thruster.MaxThrustTimeInSeconds;

            seconds += _hangTimeInSecondsAfterThrusterFinish * (_randomTimeDelayAfterThruster ? MaxHangTimeFactor : 1f);

            if (_explosion.OrNull() != null)
                seconds += _explosion.EstimateDurationInSeconds();

            return seconds;
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _thruster.TurnOn();
            Messenger.Broadcast(new MessengerEventPlaySoundStruct(_startWhistleSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));

            await UniTask.Delay(200, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            await UniTask.WaitWhile(() => _thruster.IsThrusting, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            Messenger.Broadcast(new MessengerEventPlaySoundStruct(_endWhistleSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));

            //Hang time
            var randomTimeFactor = _randomTimeDelayAfterThruster ? UnityEngine.Random.Range(0.9f, MaxHangTimeFactor) : 1f;
            //Debug.Log($"randomTimeFactor: {randomTimeFactor}");
            await UniTask.Delay(Mathf.RoundToInt(_hangTimeInSecondsAfterThrusterFinish * 1000f * randomTimeFactor), cancellationToken: token);
            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
            {
                DisableRigidBodyAndColliders();
                _model.SetActive(false);
            }
            
            _explosion.Explode();

            await _explosion.WaitForExplosionToFinishAsync(token);
            token.ThrowIfCancellationRequested();

            if (CoreSettings.AutoDespawnFireworks)
            {
                await DestroyFireworkAsync(token);
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
