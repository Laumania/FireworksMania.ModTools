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


        protected Rigidbody   _rigidbody;
        private Collider[]  _colliders;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();

            Preconditions.CheckNotNull(_model, $"Missing model reference in rocket on '{this.gameObject.name}'");
            Preconditions.CheckNotNull(_thruster, $"Missing Thruster on rocket on '{this.gameObject.name}'");
            Preconditions.CheckNotNull(_fuse, $"Missing Fuse on rocket on '{this.gameObject.name}'");
            Preconditions.CheckNotNull(_explosion, $"Missing Explosion on rocket on '{this.gameObject.name}'");
            Preconditions.CheckNotNull(_rigidbody, $"Missing Rigidbody on rocket on '{this.gameObject.name}'");

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
                await UniTask.Delay(Mathf.RoundToInt(UnityEngine.Random.Range(0f, 0.1f) * 1000f), cancellationToken: token);
                token.ThrowIfCancellationRequested();
            }

            if(CoreSettings.AutoDespawnFireworks)
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
