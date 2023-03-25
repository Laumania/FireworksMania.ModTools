using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;
using ExplosionBehavior = FireworksMania.Core.Behaviors.Fireworks.Parts.ExplosionBehavior;
using Thruster = FireworksMania.Core.Behaviors.Fireworks.Parts.Thruster;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/WhistlerBehavior")]
    public class WhistlerBehavior : BaseFireworkBehavior
    {
        [Header("Whistler Settings")]
        [SerializeField]
        private GameObject _model;
        [SerializeField]
        private Thruster _thruster;
        [SerializeField]
        [Tooltip("If a whistler is stepped by the player before ignition it will explode after thruster burns out")]
        private ExplosionBehavior _explosion;


        [Header("Sound")]
        [SerializeField]
        [GameSound]
        private string _whistlingSound;

        private Rigidbody _rigidbody;
        private bool _isSteppedOn = false;

        protected override void Awake()
        {
            base.Awake();

            if (_model == null)
                Debug.LogError("Missing model reference in rocket", this);

            if (_fuse == null)
                Debug.LogError("Missing Fuse on rocket", this);

            if (_thruster == null)
                Debug.LogError("Missing Thruster on rocket - this is not gonna fly!", this);

            _rigidbody = this.GetComponent<Rigidbody>();

            if (_rigidbody == null)
                Debug.LogError("Missing Rigidbody on rocket", this);
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
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            _thruster.TurnOn();

            Messenger.Broadcast(new MessengerEventPlaySound(_whistlingSound, this.transform, delayBasedOnDistanceToListener: true, followTransform: true));
            await UniTask.WaitWhile(() => _thruster.IsThrusting, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            if (_isSteppedOn)
            {
                _rigidbody.isKinematic = true; //To avoid it falling through the ground and be Destroyed
                _model.SetActive(false);
                _explosion.Explode();

                await UniTask.WaitWhile(() => _explosion.IsExploding, cancellationToken: token);
                token.ThrowIfCancellationRequested();
            }

            if (CoreSettings.AutoDespawnFireworks)
            {
                await UniTask.Delay(5000);
                token.ThrowIfCancellationRequested();

                await DestroyFireworkAsync(token);
                token.ThrowIfCancellationRequested();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isSteppedOn == false && other.gameObject.CompareTag("Player"))
            {
                _isSteppedOn = true;
                //Todo: Some sound or visual indication that it's stepped on
                //this.transform.DOScaleX(.7f, .1f);
            }
        }
    }
}
