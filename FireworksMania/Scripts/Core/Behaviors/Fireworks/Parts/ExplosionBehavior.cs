using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionBehavior")]
    public class ExplosionBehavior : NetworkBehaviour, IExplosion
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
        private NetworkVariable<LaunchState> _launchState = new NetworkVariable<LaunchState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _isWaitingOnDelayedSound = false;

        private void Awake()
        {
            if (_explosionParticleEffect == null)
                Debug.LogError("Missing particlesystem on ExplosionBehavior", this);

            _explosionForceEffect = this.GetComponent<ExplosionPhysicsForceEffect>();
            if (_explosionForceEffect == null)
                Debug.LogError("Missing ExplosionPhysicsForce on Explosion", this);

            _explosionParticleEffect.gameObject.SetActive(false);
            _cancellationToken = this.GetCancellationTokenOnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _launchState.OnValueChanged += async (prevValue, newValue) =>
            {
                if(newValue.IsLaunched == true)
                {
                    await ExplodeVisualsAsync(_cancellationToken);
                }
            };

            if(IsServer)
            {
                if(_playOnStart)
                    Explode();
            }

            if (_launchState.Value.IsLaunched)
                ExplodeVisualsAsync(_cancellationToken).Forget();
        }

        private float GetLaunchTimeDifference()
        {
            return this.NetworkManager.ServerTime.TimeAsFloat - _launchState.Value.ServerStartTimeAsFloat;
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

        private async UniTask ExplodeVisualsAsync(CancellationToken token)
        {
            if (_forceExplosionAlwaysUp)
                _explosionForceEffect.gameObject.transform.rotation = Quaternion.identity;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_explosionSound, this.transform.position, delayBasedOnDistanceToListener: true));

            if (_delayInSecondsBetweenSoundAndExplosionEffect > 0f)
            {
                _isWaitingOnDelayedSound = true;
                await UniTask.Delay(Mathf.RoundToInt((_delayInSecondsBetweenSoundAndExplosionEffect * 1000f)), cancellationToken: token);
                token.ThrowIfCancellationRequested();
                _isWaitingOnDelayedSound = false;
            }

            _explosionParticleEffect.gameObject.SetActive(true);
            _explosionParticleEffect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _explosionParticleEffect.Play(true);
        }

        private async UniTask ExplodeAsync(CancellationToken token)
        {
            if (!IsServer)
                return;

            _launchState.Value = new LaunchState()
            {
                IsLaunched             = true,
                Seed                   = (byte)Random.Range(0, 254),
                ServerStartTimeAsFloat = this.NetworkManager.ServerTime.TimeAsFloat + (_delayInSecondsBetweenSoundAndExplosionEffect * 1000f)
            };

            _explosionForceEffect.ApplyExplosionForce();

            await UniTask.WaitWhile(() => _explosionParticleEffect.IsAlive(true) || _explosionParticleEffect.isPlaying || _isWaitingOnDelayedSound, cancellationToken: token);
            
            token.ThrowIfCancellationRequested();

            var prevValue = _launchState.Value;
            _launchState.Value = new LaunchState()
            {
                IsLaunched             = false,
                Seed                   = prevValue.Seed,
                ServerStartTimeAsFloat = prevValue.ServerStartTimeAsFloat
            };
        }

        public bool IsExploding => _launchState.Value.IsLaunched;
    }
}