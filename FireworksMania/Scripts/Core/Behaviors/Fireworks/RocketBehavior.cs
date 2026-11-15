using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/RocketBehavior")]
    public class RocketBehavior : BaseFireworkBehavior, IHaveEntityClassificationDefinition
    {
        [SerializeField]
        [Tooltip("How the Inventory and Shop list this firework: F2, F3 or F4")]
        private EntityClassDefinition _class;

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

        //Every rocket burns this long before its thrust pushes, frozen or not (#3287). The rockets were tuned with this
        //hold from v2021.6.1; v2026.10.1 (#3047) dropped it and frozen and rack rockets flew up to 1.66x as high.
        //It used to pin only a kinematic rocket, so a rocket standing free flew far higher than a frozen one. Now the
        //thruster itself holds the force, so every rocket starts its climb from rest at the same point of the burn.
        internal const float LaunchHoldSeconds = 0.2f;

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

            _thruster.TurnOn(LaunchHoldSeconds);

            await LetGoWhenThrustStartsAsync(token);
            token.ThrowIfCancellationRequested();

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

        //A frozen or seated rocket is kinematic: let it go in the step its thrust starts to push (#3287). The server
        //waits on the thruster's own burn clock, never an Update timer: one frame can hold any number of physics steps,
        //so a timer let go of rockets later the lower the frame rate. A client's thruster doesn't run, so it counts the
        //same stretch of physics time.
        internal async UniTask LetGoWhenThrustStartsAsync(CancellationToken token)
        {
            if (IsServer)
                await UniTask.WaitWhile(() => _thruster.IsHoldingForce, PlayerLoopTiming.FixedUpdate, token);
            else
                await UniTask.Delay(System.TimeSpan.FromSeconds(LaunchHoldSeconds), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, token);

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity  = true;
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

        public EntityClassificationDefinition ClassificationDefinition => _class;
    }
}
