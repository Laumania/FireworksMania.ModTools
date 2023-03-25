using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using UnityEngine;
using Messenger = FireworksMania.Core.Messaging.Messenger;
using MessengerEventPlaySoundAtVector3 = FireworksMania.Core.Messaging.MessengerEventPlaySoundAtVector3;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionBehavior")]
    public class ExplosionBehavior : MonoBehaviour, IExplosion
    {
        [Header("General")]
        [SerializeField]
        private ParticleSystem _explosionParticleEffect;
        [SerializeField]
        private bool _playOnStart = false;
        [SerializeField]
        private bool _forceExplosionAlwaysUp = false;
        [SerializeField]
        private float _delayInSecondsBetweenSoundAndExplosionEffect = 0f;

        [Header("Sound")]
        [GameSound]
        [SerializeField]
        public string _explosionSound;
    
        private ExplosionPhysicsForceEffect _explosionForceEffect;

        private CancellationToken _cancellationToken;

        void Awake()
        {
            if (_explosionParticleEffect == null)
                Debug.LogError("Missing particlesystem on ExplosionBehavior", this);

            _explosionForceEffect = this.GetComponent<ExplosionPhysicsForceEffect>();
            if (_explosionForceEffect == null)
                Debug.LogError("Missing ExplosionPhysicsForce on Explosion", this);

            _explosionParticleEffect.gameObject.SetActive(false);
            _cancellationToken = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            if (_playOnStart)
                Explode();
        }

        public async void Explode()
        {
            await ExplodeAsync(_cancellationToken).SuppressCancellationThrow();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this.gameObject.GetComponent<ExplosionPhysicsForceEffect>() == null)
            {
                this.gameObject.AddComponent<ExplosionPhysicsForceEffect>();
                Debug.Log("Added require ExplosionPhysicsForceEffect", this);
            }
        }

        private async UniTask ExplodeAsync(CancellationToken token)
        {
            IsExploding = true;

            if (_forceExplosionAlwaysUp)
                _explosionForceEffect.gameObject.transform.rotation = Quaternion.identity;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_explosionSound, this.transform.position, delayBasedOnDistanceToListener: true));

            if (_delayInSecondsBetweenSoundAndExplosionEffect > 0f)
                await UniTask.Delay(Mathf.RoundToInt((_delayInSecondsBetweenSoundAndExplosionEffect * 1000f)), cancellationToken: token);

            token.ThrowIfCancellationRequested();

            _explosionParticleEffect.gameObject.SetActive(true);
            _explosionParticleEffect.Play();
            _explosionForceEffect.ApplyExplosionForce();

            await UniTask.WaitWhile(() => _explosionParticleEffect.IsAlive(true) || _explosionParticleEffect.isPlaying, cancellationToken: token);
            
            token.ThrowIfCancellationRequested();

            IsExploding = false;
        }

        public bool IsExploding { get; private set; } = false;
    }
}