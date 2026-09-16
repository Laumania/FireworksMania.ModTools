using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
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

        /// <summary>
        /// How long a peer waits for the explosion to actually start before giving up and carrying on.
        /// Only non-server peers ever wait here, and normally only for the handful of ticks it takes the
        /// replicated launch state to arrive - the generous limit is there so a lost update can't strand a
        /// firework mid sequence forever.
        /// </summary>
        private const float MaxSecondsToWaitForExplosionToStart = 2f;

        private ExplosionPhysicsForceEffect _explosionForceEffect;
        private CancellationToken _cancellationToken;
        private NetworkVariable<LaunchState> _launchState = new NetworkVariable<LaunchState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _isWaitingOnDelayedSound = false;
        private bool _hasExplosionStarted     = false;

        private void Awake()
        {
            Preconditions.CheckNotNull(_explosionParticleEffect, "Explosion Particle System cannot be null on ExplosionBehavior", this);

            _explosionForceEffect = this.GetComponent<ExplosionPhysicsForceEffect>();
            Preconditions.CheckNotNull(_explosionForceEffect, "ExplosionPhysicsForceEffect cannot be null on ExplosionBehavior", this);

            _explosionParticleEffect.DisableEndlessLooping();
            _explosionParticleEffect.gameObject.SetActive(false);
            _cancellationToken = this.GetCancellationTokenOnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Scene objects are despawned rather than destroyed, so the same component can be spawned again
            _hasExplosionStarted = false;

            //Named handler, paired with the OnNetworkDespawn removal: an anonymous lambda cannot be
            //unsubscribed, so every respawn of a scene object added another one and a single launch then
            //ran that many concurrent ExplodeVisualsAsync loops
            _launchState.OnValueChanged += OnLaunchStateChanged;

            if(IsServer)
            {
                if(_playOnStart)
                    Explode();
            }

            if (_launchState.Value.IsLaunched)
                ExplodeVisualsAsync(_cancellationToken).Forget();
        }

        public override void OnNetworkDespawn()
        {
            _launchState.OnValueChanged -= OnLaunchStateChanged;

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Deliberately not an async lambda: <see cref="NetworkVariable{T}.OnValueChangedDelegate"/> returns
        /// void, so an async subscriber is 'async void' and its OperationCanceledException never reaches
        /// UniTask's handler. Forget() gives the same fire-and-forget behaviour and swallows the cancel.
        /// </summary>
        private void OnLaunchStateChanged(LaunchState previousValue, LaunchState newValue)
        {
            if (newValue.IsLaunched)
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
            //Set before the first await so it is true the instant the launch state is set or arrives
            _hasExplosionStarted = true;

            if (_forceExplosionAlwaysUp)
                _explosionForceEffect.gameObject.transform.rotation = Quaternion.identity;

            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(_explosionSound, this.transform.position, delayBasedOnDistanceToListener: true));

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
            _explosionForceEffect.ApplyExplosionForce();

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject is
            //active, so every peer that activated the effect deactivates it again once it has fully drained
            var wasCancelled = await UniTask.WaitWhile(() => _explosionParticleEffect.IsAlive(true) || _explosionParticleEffect.isPlaying, cancellationToken: token).SuppressCancellationThrow();
            if (wasCancelled)
                return;

            _explosionParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _explosionParticleEffect.gameObject.SetActive(false);
        }

        private async UniTask ExplodeAsync(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) //Note: Attempt to fix https://github.com/Laumania/FireworksMania/issues/1600
                return;

            _launchState.Value = new LaunchState()
            {
                IsLaunched             = true,
                Seed                   = (byte)Random.Range(0, 254),
                ServerStartTimeAsFloat = this.NetworkManager.ServerTime.TimeAsFloat + (_delayInSecondsBetweenSoundAndExplosionEffect * 1000f)
            };            

            await UniTask.WaitWhile(() => _explosionParticleEffect.IsAlive(true) || _explosionParticleEffect.isPlaying || _isWaitingOnDelayedSound, cancellationToken: token);
            
            token.ThrowIfCancellationRequested();

            if (!IsSpawned) //Note: Attempt to fix https://github.com/Laumania/FireworksMania/issues/1600
                return;

            var prevValue = _launchState.Value;
            _launchState.Value = new LaunchState()
            {
                IsLaunched             = false,
                Seed                   = prevValue.Seed,
                ServerStartTimeAsFloat = prevValue.ServerStartTimeAsFloat
            };
        }

        /// <summary>
        /// Waits out the explosion, and is safe to await on the server and on clients alike - always use
        /// this rather than waiting on <see cref="IsExploding"/> yourself.
        ///
        /// <see cref="Explode"/> only does anything on the server, so on a client <see cref="IsExploding"/>
        /// stays false until the replicated launch state arrives a few ticks later. Waiting for the flag to
        /// turn false is therefore an instant no-op on a client, which used to make it play the firework's
        /// destroy animation at the very moment the firework was supposed to explode - the firework vanished
        /// and the explosion was never drawn (#2337). Waiting for the explosion to start first fixes that,
        /// and costs the server nothing since it sets the flag synchronously inside <see cref="Explode"/>.
        /// </summary>
        public async UniTask WaitForExplosionToFinishAsync(CancellationToken token)
        {
            if (await WaitForExplosionToStartAsync(token) == false)
                return;

            await UniTask.WaitWhile(() => IsExploding, cancellationToken: token);
        }


        /// <summary>
        /// False if the explosion never started and this peer gave up waiting for it, in which case the
        /// caller should carry on rather than wait for something that will never happen.
        /// </summary>
        private async UniTask<bool> WaitForExplosionToStartAsync(CancellationToken token)
        {
            //The server has already set the flag by the time it gets here, so if it isn't set it never will be
            if (IsServer == false && _hasExplosionStarted == false)
            {
                var giveUpTime = Time.timeAsDouble + MaxSecondsToWaitForExplosionToStart;

                await UniTask.WaitUntil(() => _hasExplosionStarted || Time.timeAsDouble >= giveUpTime, cancellationToken: token);
                token.ThrowIfCancellationRequested();

                if (_hasExplosionStarted == false)
                {
                    Debug.LogWarning($"Explosion on '{this.gameObject.name}' never started within {MaxSecondsToWaitForExplosionToStart} seconds - carrying on without it", this);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Note that this is replicated from the server, so it is false on a client for the first few ticks
        /// after the explosion was supposed to start. Await <see cref="WaitForExplosionToFinishAsync"/>
        /// instead of building your own wait on top of this.
        /// </summary>
        public bool IsExploding => _launchState.Value.IsLaunched;

        /// <summary>
        /// Only there so the effect can be read off the prefab - the content tests sweep it - a firework never
        /// needs to read it back.
        /// </summary>
        public ParticleSystem ExplosionEffect => _explosionParticleEffect;

        /// <summary>
        /// Seconds from <see cref="Explode"/> until the burst has fired, read from the prefab for the duration
        /// the inventory shows and the spawn limit counts down (#2657): the sound-to-effect delay plus the
        /// burst's firing time - see <see cref="ParticleEffectDuration.MeasureFiringDurationInSeconds"/>. For
        /// a plain burst that is a tenth of a second; what the stars do afterwards is the tail.
        /// </summary>
        public float EstimateDurationInSeconds()
        {
            return _delayInSecondsBetweenSoundAndExplosionEffect + ParticleEffectDuration.MeasureFiringDurationInSeconds(_explosionParticleEffect);
        }
    }
}