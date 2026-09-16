using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/RocketBehavior")]
    public class RocketBehavior : BaseFireworkBehavior
    {
        [Header("Rocket Settings")]
        [SerializeField]
        protected GameObject _model;
        [SerializeField]
        protected Thruster _thruster;
        [Tooltip("If enabled, a small random delay is added between the thruster finishing and the explosion happening. You should only disable this is you have a very specific reason.")]
        [SerializeField]
        protected bool _randomTimeDelayAfterThruster = true;
        [SerializeField]
        protected ExplosionBehavior _explosion;

        //The longest random pause between the thruster burning out and the burst (see LaunchInternalAsync);
        //the duration estimate takes all of it (#2657)
        private const float MaxRandomDelayAfterThrusterInSeconds = 0.1f;

        protected Rigidbody   _rigidbody;
        private Collider[]  _colliders;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            Preconditions.CheckNotNull(_model, $"Missing model reference in rocket on '{this.gameObject.name}'", this);
            Preconditions.CheckNotNull(_thruster, $"Missing Thruster on rocket on '{this.gameObject.name}'", this);
            Preconditions.CheckNotNull(_fuse, $"Missing Fuse on rocket on '{this.gameObject.name}'", this);
            Preconditions.CheckNotNull(_explosion, $"Missing Explosion on rocket on '{this.gameObject.name}'", this);
            Preconditions.CheckNotNull(_rigidbody, $"Missing Rigidbody on rocket on '{this.gameObject.name}'", this);

            _colliders = _rigidbody.GetComponents<Collider>();
        }

        protected override void Start()
        {
            base.Start();

            _thruster.Setup(_rigidbody);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (GetComponent<Rigidbody>() == null)
                _rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            _thruster.TurnOn();

            await UniTask.Delay(200);
            token.ThrowIfCancellationRequested();

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity  = true;

            await UniTask.WaitWhile(() => _thruster.IsThrusting, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            if (_randomTimeDelayAfterThruster)
            {
                await UniTask.Delay(Mathf.RoundToInt(UnityEngine.Random.Range(0f, MaxRandomDelayAfterThrusterInSeconds) * 1000f), cancellationToken: token);
                token.ThrowIfCancellationRequested();
            }

            if(CoreSettings.AutoDespawnFireworks)
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

        /// <summary>Thrust, the random pause, then the burst (#2657).</summary>
        public override float EstimateDurationInSeconds()
        {
            var seconds = 0f;

            if (_thruster.OrNull() != null)
                seconds += _thruster.MaxThrustTimeInSeconds;

            if (_randomTimeDelayAfterThruster)
                seconds += MaxRandomDelayAfterThrusterInSeconds;

            if (_explosion.OrNull() != null)
                seconds += _explosion.EstimateDurationInSeconds();

            return seconds;
        }

        protected void DisableRigidBodyAndColliders()
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.isKinematic            = true;
            _rigidbody.useGravity             = false;
            foreach (var collider in _colliders)
            {
                collider.enabled = false;
            }
        }
    }
}
