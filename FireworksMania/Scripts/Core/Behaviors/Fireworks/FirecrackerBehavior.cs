using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/FirecrackerBehavior")]
    public class FirecrackerBehavior : BaseFireworkBehavior
    {
        [Header("Firecracker Settings")]
        [SerializeField]
        private GameObject _model;
        [SerializeField]
        private ExplosionBehavior _explosion;

        private Rigidbody _rigidbody;
        private Collider[] _colliders;

        protected override void Awake()
        {
            base.Awake();

            if (_model == null)
                Debug.LogError("Missing model reference in firecracker", this);

            if (_fuse == null)
                Debug.LogError("Missing Fuse on firecracker", this);

            if (_explosion == null)
                Debug.LogError("Missing IExplosion on firecracker", this);

            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody on firecracker", this);

            _colliders = _rigidbody.GetComponents<Collider>();
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
            {
                return;
            }

            DisableRigidBodyAndColliders();
            _model.SetActive(false);

            _explosion.Explode();
            await UniTask.WaitWhile(() => _explosion.IsExploding, cancellationToken: token);

            if(IsServer)
                this.gameObject.DestroyOrDespawn();
        }

        private void DisableRigidBodyAndColliders()
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            foreach (var collider in _colliders)
            {
                collider.enabled = false;
            }
        }
    }
}
