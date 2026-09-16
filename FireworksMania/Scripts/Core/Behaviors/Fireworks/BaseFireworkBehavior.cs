using Cysharp.Threading.Tasks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using System;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [SelectionBase]
    public abstract class BaseFireworkBehavior : NetworkBehaviour,
        IAmGameObject,
        ISaveableComponent,
        IHaveBaseEntityDefinition,
        IIgnitable,
        IHaveFuse,
        IHaveFuseConnectionPoint,
        IFiringSystemReceiver,
        IIgnitionCauserCarrier
    {
        [Header("General")]
        [FormerlySerializedAs("_metadata")]
        [SerializeField]
        private FireworkEntityDefinition _entityDefinition;

        [SerializeField]
        protected Fuse _fuse;
        protected CancellationToken _cancellationTokentoken;

        private SaveableEntity _saveableEntity;
        private IgnitionCauser _ignitionCauser;
        private bool _isSpent;
        private float _durationInSeconds = -1f;

        public event Action<BaseFireworkBehavior> OnDestroyed;
        public event Action OnFiringSystemReceiverDataUpdated;

        protected NetworkVariable<LaunchState> _launchState = new NetworkVariable<LaunchState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        protected NetworkVariable<FiringSystemReceiverData> _firingSystemReceiverNetworkData = new NetworkVariable<FiringSystemReceiverData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        protected virtual void Awake()
        {
            if (_entityDefinition == null)
            {
                Debug.LogError($"Missing {nameof(FireworkEntityDefinition)} on '{this.gameObject.name}' - everything will go wrong this way!", this);
                return;
            }

            if(_fuse == null)
            {
                Debug.LogError($"Missing {nameof(Fuse)} on '{this.gameObject.name}' - this is not gonna work! Make sure this fireworks have a fuse.", this);
                this.enabled = false;
                return;
            }

            if (this.gameObject.GetComponent<IErasable>() == null)
                this.gameObject.AddComponent<ErasableBehavior>();

            _saveableEntity = GetComponent<SaveableEntity>();
            if (_saveableEntity == null)
            {
                Debug.LogError($"Missing '{nameof(SaveableEntity)}' which is a required component - make sure '{this.name}' have one", this);
                return;
            }

            _fuse.SaveableEntityOwner       = _saveableEntity;
            _cancellationTokentoken         = this.GetCancellationTokenOnDestroy();

            var particleSystems = this.GetComponentsInChildren<ParticleSystem>(true);
            Messenger.Broadcast(new MessengerEventFireworkParticleSystemsRegisteringStruct(this.gameObject, particleSystems));
        }

        protected virtual void Start()
        {
            _fuse.OnFuseCompleted += OnFuseCompleted;
        }

        public override void OnDestroy()
        {
            Messenger.Broadcast(new MessengerEventFireworkParticleSystemsUnregisteringStruct(this.gameObject));

            if (_fuse != null)
                _fuse.OnFuseCompleted -= OnFuseCompleted;

            if(IsServer)
                Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFiringSystemControllerSendSignal);

            base.OnDestroy();
        }

        public override void OnNetworkDespawn()
        {
            _launchState.OnValueChanged -= OnLaunchStateValueChanged;
            _firingSystemReceiverNetworkData.OnValueChanged -= OnFiringSystemReceiverNetworkDataValueChanged;

            if (IsServer)
                Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFiringSystemControllerSendSignal);

            base.OnNetworkDespawn();

            if(this.NetworkObject?.IsSceneObject.HasValue == true && this.NetworkObject.IsSceneObject.Value == true)
                gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if(this.gameObject.activeInHierarchy == false)
                this.gameObject.SetActive(true);

            _launchState.OnValueChanged += OnLaunchStateValueChanged;
            _firingSystemReceiverNetworkData.OnValueChanged += OnFiringSystemReceiverNetworkDataValueChanged;

            if (_launchState.Value.IsLaunched)
                CatchUpLaunchAfterSpawnAsync().Forget();

            if (IsServer)
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFiringSystemControllerSendSignal);

            base.OnNetworkSpawn();
        }

        //A peer can spawn a firework whose replicated state is already "launched" - a late joiner, or
        //ObjectVisibilityManager revealing it mid-effect. The catch-up launch must not start inside NGO's
        //OnNetworkSpawn loop: behaviors like FirecrackerBehavior synchronously deactivate their model, and
        //a Fuse nested under it is then skipped by NGO ("Disabled NetworkBehaviours will be excluded...")
        //so its OnNetworkSpawn never runs on this peer (#2558). Yield once so the spawn loop finishes over
        //an intact hierarchy, then re-check state in case it changed while deferred.
        private async UniTask CatchUpLaunchAfterSpawnAsync()
        {
            await UniTask.Yield(_cancellationTokentoken);

            if (this.IsSpawned == false || _launchState.Value.IsLaunched == false)
                return;

            StartLaunchInternal();
        }

        private void OnFiringSystemReceiverNetworkDataValueChanged(FiringSystemReceiverData previousValue, FiringSystemReceiverData newValue)
        {
            OnFiringSystemReceiverDataUpdated?.Invoke();
        }

        private void OnLaunchStateValueChanged(LaunchState previousValue, LaunchState newValue)
        {
            //NetworkLog.LogInfo($"[{NetworkManager.Singleton.LocalClientId}] _launchState.OnValueChanged newValue.IsLaunched {newValue.IsLaunched}");
            if (previousValue.IsLaunched == false && newValue.IsLaunched == true)
                StartLaunchInternal();
        }

        private void StartLaunchInternal()
        {
            Messenger.Broadcast(new MessengerEventFireworkEffectStartedStruct(this.gameObject));
            LaunchAndMarkFinishedAsync().Forget();
        }

        //No try/catch on purpose: a cancellation means this firework is being destroyed mid-effect, and
        //the exception should reach the same Forget() it always did - it just must not mark a firework
        //that never finished as finished.
        private async UniTask LaunchAndMarkFinishedAsync()
        {
            await LaunchInternalAsync(_cancellationTokentoken);
            HasFinishedEffect = true;

            //Order matters: IsSpent latches on HasFinishedEffect above, so clearing the launch state
            //here cannot cost this firework its place in the host's spawn limit. Auto-despawn runs
            //inside LaunchInternalAsync, so the destroy animation has already read its seed by now.
            ResetLaunchState();
        }

        /// <summary>
        /// The wait a firework built around a single particle effect needs: first until the firework is
        /// spent - the same moment the host's spawn limit stops counting it, a float compare per frame -
        /// and then until the last particle is gone, so nothing is destroyed mid-effect.
        /// </summary>
        protected async UniTask WaitForEffectToFinishAsync(ParticleSystem effect, CancellationToken token)
        {
            await UniTask.WaitUntil(() => IsSpent, cancellationToken: token);

            //A drained ParticleSystem keeps ticking in Unity's particle update as long as its GameObject
            //is active, which is what isPlaying catches here
            await UniTask.WaitWhile(() => effect.IsAlive(true) || effect.isPlaying, cancellationToken: token);
        }

        private void OnFiringSystemControllerSendSignal(MessengerEventFiringSystemControllerSendSignalStruct arg)
        {
            if(this.FiringSystemReceiverData.HasValue &&
                arg.ModuleIndex == this.FiringSystemReceiverData.ModuleIndex && 
                arg.CueIndex == this.FiringSystemReceiverData.CueIndex && 
                _fuse?.IsIgnited == false && 
                _fuse?.IsUsed == false)
            {
                //The firing-system signal is a purely local broadcast and this handler is only
                //subscribed on the server, so the sequence runner is this machine
                _fuse.IgniteWithoutFuseTime(NetworkManager.LocalClientId);
            }
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_entityDefinition == null)
            {
                Debug.LogError($"Missing '{nameof(FireworkEntityDefinition)}' on '{this.gameObject.name}'", this);
                return;
            }

            if (_fuse == null)
            {
                Debug.LogError($"Missing {nameof(Fuse)} on '{this.gameObject.name}' - this is not gonna work! Make sure this fireworks have a fuse.", this);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ValidateEntityDefinitionReference();
                    ValidateErasableBehavior();
                    ValidateSaveableEntity();
                }
            };
#endif
        }


        private void ValidateEntityDefinitionReference()
        {
            //Remove logic for now... can't make it work properly for now

            //if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this.gameObject) == false ||
            //    UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this.gameObject))
            //    return;

            //if (_entityDefinition.PrefabGameObject != null && 
            //    _entityDefinition.PrefabGameObject != this.gameObject && 
            //    GameObject.ReferenceEquals(_entityDefinition.PrefabGameObject, this.gameObject) == false)
            //{
            //    Debug.LogWarning($"'{this.gameObject.name}' got its reference to '{_entityDefinition.name}' (EntityDefinition) removed as '{_entityDefinition.name}' does not reference '{this.gameObject.name}' as its Prefab.", this.gameObject);
            //    _entityDefinition = null;

            //    UnityEditor.EditorUtility.SetDirty(this.gameObject);
            //}
        }

        private void ValidateSaveableEntity()
        {
#if UNITY_EDITOR
            var saveableComponents = GetComponents<SaveableEntity>();
            if (saveableComponents.Length > 1)
            {
                Debug.LogError($"'{this.EntityDefinition?.Id}' have '{saveableComponents.Length}' '{nameof(SaveableEntity)}'s' - it can have one and only one - please delete so only one is left else it will be saved multiple times in blueprints", this.gameObject);
                return;
            }

            _saveableEntity = GetComponent<SaveableEntity>();
            if (_saveableEntity == null)
            {
                _saveableEntity = this.gameObject.AddComponent<SaveableEntity>();
            }

            if (_saveableEntity.EntityDefinition != _entityDefinition)
            {
                Debug.Log("ValidateSaveableEntity marked as dirty", this.gameObject);
                _saveableEntity.EntityDefinition = _entityDefinition;
                UnityEditor.EditorUtility.SetDirty(this.gameObject);
            }
#endif
        }

        private void ValidateErasableBehavior()
        {
#if UNITY_EDITOR
            var erasableComponents = GetComponents<ErasableBehavior>();
            if (erasableComponents.Length == 0)
            {
                this.gameObject.AddComponent<ErasableBehavior>();
                UnityEditor.EditorUtility.SetDirty(this.gameObject);

                Debug.Log($"Added required '{nameof(ErasableBehavior)}' to this entity can be removed via the Eraser Tool in game", this.gameObject);
            }
            
            if (erasableComponents.Length > 1)
            {
                Debug.LogWarning($"'{this.EntityDefinition?.Id}' have '{erasableComponents.Length}' '{nameof(ErasableBehavior)}'s' it should have one and only one - removing all the extra ones", this.gameObject);
                return;
            }
#endif
        }

        private void OnFuseCompleted()
        {
            if(IsServer)
            {
                _launchState.Value = new LaunchState()
                {
                    IsLaunched             = true,
                    ServerStartTimeAsFloat = this.NetworkManager.ServerTime.TimeAsFloat,
                    Seed                   = (byte)Random.Range(0, 254)
                };
            }
        }

        /// <summary>
        /// Puts the firework back to "not launched" once its effect has run through. Until #2746 this was
        /// never called, so <see cref="IsIgnited"/> stayed true for the rest of a fired firework's life -
        /// and with auto-despawn off that life is the rest of the session. ObjectVisibilityManager reads
        /// IsIgnited to decide what a joining client must be shown immediately, so a world of spent
        /// fireworks was streamed to every joiner in a single tick.
        ///
        /// IsSpawned is checked as well as IsServer because a NetworkVariable write on a despawned object
        /// is a Debug.LogError per write rather than an exception (Docs/Development/netcode-gotchas.md),
        /// and with auto-despawn on this runs right after the firework has despawned itself - so without
        /// the guard every firework in a show would log one on its way out.
        /// </summary>
        protected virtual void ResetLaunchState()
        {
            if (IsSpawned && IsServer)
            {
                //Seed survives on purpose. It is what varies the destroy animation's length, and with
                //auto-despawn off a spent firework is destroyed much later - by Clear All or the eraser -
                //so zeroing it here would make every one of them shrink away in perfect lockstep.
                _launchState.Value = new LaunchState()
                {
                    IsLaunched             = false,
                    ServerStartTimeAsFloat = 0f,
                    Seed                   = _launchState.Value.Seed
                };
            }
        }

        protected virtual async UniTask DestroyFireworkAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            await DestroyFireworkAnimatedAsync(token).SuppressCancellationThrow();
        }

        protected abstract UniTask LaunchInternalAsync(CancellationToken token);

        private async UniTask DestroyFireworkAnimatedAsync(CancellationToken token)
        {
            await PlayDestroyAnimationAsync(token);
            token.ThrowIfCancellationRequested();

            if (!IsServer)
                return;

            await DestroyAnimation.WaitForClientsAsync(NetworkManager, token);
            token.ThrowIfCancellationRequested();

            OnDestroyed?.Invoke(this);

            this.gameObject.DestroyOrDespawn();
        }

        /// <summary>
        /// The destroy animation is played on host and clients alike so fireworks doesn't just pop out of
        /// existence on clients. Only the host despawns the firework once the animation is done. The timing
        /// is varied by the replicated launch seed, so all peers plays the exact same animation and it
        /// therefore ends at the same time on all of them.
        /// </summary>
        protected virtual UniTask PlayDestroyAnimationAsync(CancellationToken token)
        {
            return DestroyAnimation.PlayAsync(this.transform, _launchState.Value.Seed / (float)byte.MaxValue, token);
        }

        public virtual CustomEntityComponentData CaptureState()
        {
            var customComponentData = new CustomEntityComponentData();

            if(this.FiringSystemReceiverData.HasValue &&
               this.FiringSystemReceiverData.ModuleIndex > 0 &&
               this.FiringSystemReceiverData.CueIndex > 0)
            {
                customComponentData.Add<byte>(nameof(BaseFireworkBehaviorSaveData.ModuleIndex), this.FiringSystemReceiverData.ModuleIndex);
                customComponentData.Add<byte>(nameof(BaseFireworkBehaviorSaveData.CueIndex), this.FiringSystemReceiverData.CueIndex);
            }

            return customComponentData;
        }

        public virtual void RestoreState(CustomEntityComponentData customComponentData)
        {
            //BEGIN LEGACY: This code is here to be able to support legacy blueprints
            var position    = customComponentData.Get<SerializableVector3>(nameof(BaseFireworkBehaviorSaveData.Position));
            var rotation    = customComponentData.Get<SerializableRotation>(nameof(BaseFireworkBehaviorSaveData.Rotation));
            var isKinematic = customComponentData.Get<bool>(nameof(BaseFireworkBehaviorSaveData.IsKinematic));

            this.transform.position = new Vector3(position.X, position.Y, position.Z);
            this.transform.rotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

            var rigidbody = this.GetComponent<Rigidbody>();
            if (rigidbody != null)
                rigidbody.isKinematic = isKinematic;

            var networkRigidbody = this.GetComponent<NetworkRigidbody>();
            if (networkRigidbody != null)
            {
                networkRigidbody.SetPosition(this.transform.position);
                networkRigidbody.SetRotation(this.transform.rotation);
            }

            //END LEGACY

            var moduleIndex = customComponentData.Get<byte>(nameof(BaseFireworkBehaviorSaveData.ModuleIndex));
            var cueIndex    = customComponentData.Get<byte>(nameof(BaseFireworkBehaviorSaveData.CueIndex));

            if(moduleIndex > 0 && cueIndex > 0)
            {
                this.FiringSystemReceiverData = new FiringSystemReceiverData()
                {
                    CueIndex = cueIndex,
                    ModuleIndex = moduleIndex
                };
            }
        }

        public virtual void Ignite(float ignitionForce)
        {
            if (_fuse == null)
            {
                Debug.LogError($"Trying to call Ignite on '{this.gameObject.name}' but Fuse is null... that's a problem - trying to delete firework to avoid further issues");

                if (NetworkManager.IsServer)
                    this.gameObject.DestroyOrDespawn();

                return;
            }

            _fuse.Ignite(ignitionForce, IgnitionCauserClientId);
        }

        public virtual void Ignite(float ignitionForce, ulong causerClientId)
        {
            TrySetIgnitionCauser(causerClientId);
            Ignite(ignitionForce);
        }

        public virtual void IgniteInstant()
        {
            if (_fuse == null)
            {
                Debug.LogError($"Trying to call Ignite on '{this.gameObject.name}' but Fuse is null... that's a problem - trying to delete firework to avoid further issues");

                if(NetworkManager.IsServer)
                    this.gameObject.DestroyOrDespawn();

                return;
            }

            _fuse.IgniteInstant(IgnitionCauserClientId);
        }

        public virtual void IgniteInstant(ulong causerClientId)
        {
            TrySetIgnitionCauser(causerClientId);
            IgniteInstant();
        }

        public virtual IFuse GetFuse()
        {
            return _fuse;
        }

        protected float GetLaunchTimeDifference()
        {
            return this.NetworkManager.ServerTime.TimeAsFloat - _launchState.Value.ServerStartTimeAsFloat;
        }

        public Vector3 GetFiringSystemReceiverWorldPosition()
        {
            return this.GetFuse().ConnectionPoint.Transform.position;
        }

        public string SaveableComponentTypeId                 => this.GetType().Name;
        public virtual string Name                            => _entityDefinition.ItemName;
        public GameObject GameObject                          => this.gameObject;
        public BaseEntityDefinition EntityDefinition
        {
            get => _entityDefinition;
            set => _entityDefinition = (FireworkEntityDefinition)value;
        }

        /// <summary>
        /// True once this firework has run its effect all the way through - it is done, and is only still
        /// in the world because Auto Despawn is off (with it on, the firework is destroyed at that same
        /// moment). Deliberately not networked: the only thing that asks is the host's spawn limit
        /// (#2455), and the launch runs on every peer anyway. Never goes back to false - nothing re-arms
        /// a spent firework.
        /// </summary>
        public bool HasFinishedEffect                         { get; private set; }

        /// <summary>
        /// True once this firework is spent as far as the host's spawn limit is concerned: its
        /// <see cref="DurationInSeconds"/> - the very number the inventory shows for it - has passed since
        /// launch, or its effect has run all the way through, whichever comes first. So a player gets the
        /// slot back the moment their firework has stopped firing rather than when the last wisp of smoke
        /// has faded (#2651), and the inventory's number is a promise the spawn limit keeps (#2657).
        ///
        /// Counted against the replicated launch time, so every peer agrees and a late joiner is not off by
        /// however long it took to arrive; not networked itself, because the only thing that asks is the
        /// host's spawn limit. <see cref="HasFinishedEffect"/> is the later moment and remains what the
        /// firework is destroyed on - nothing is destroyed any earlier than it was. Never goes back to
        /// false: nothing re-arms a spent firework.
        /// </summary>
        public bool IsSpent
        {
            get
            {
                if (_isSpent == false && (HasFinishedEffect || (IsSpawned && _launchState.Value.IsLaunched && GetLaunchTimeDifference() >= DurationInSeconds)))
                    _isSpent = true;

                return _isSpent;
            }
        }

        /// <summary>
        /// How long this firework keeps firing, from launch until its last stage has gone off - the number
        /// the inventory shows for it and the one <see cref="IsSpent"/> counts down. Read once from the catalog the map load
        /// filled; content the catalog never saw - a firework spawned before the map's entities were
        /// registered, say - is estimated on the spot instead.
        /// </summary>
        public float DurationInSeconds
        {
            get
            {
                if (_durationInSeconds < 0f)
                    _durationInSeconds = FireworkDurationCatalog.TryGetDurationInSeconds(_entityDefinition, out var cached) ? cached : EstimateDurationInSeconds();

                return _durationInSeconds;
            }
        }

        /// <summary>
        /// The effect this firework's own behavior plays, or null for one whose show is an
        /// <see cref="Parts.ExplosionBehavior"/>. Only there so the duration catalog can measure it on the
        /// prefab before anything is spawned - a firework never needs to read its own effect back.
        /// </summary>
        public virtual ParticleSystem PrimaryEffect           => null;

        /// <summary>
        /// Roughly how many seconds this firework keeps firing, from the fuse burning through until its last
        /// stage has gone off - the number the inventory's item details show a player, and the one the host's
        /// spawn limit counts down through <see cref="IsSpent"/>, so the two are one and the same (#2657).
        /// Evaluated on the PREFAB while the map loads, so only serialized fields and other prefab components
        /// may be read: Awake has not run. The default is the firing duration of <see cref="PrimaryEffect"/>;
        /// a behavior with a sequence of its own - thrust, hang time, then a burst - overrides it. What is
        /// left in the air afterwards is the tail and does not count, so a bang measures zero. The fuse is
        /// deliberately not part of it either: how long that burns depends on how the firework was lit.
        /// </summary>
        public virtual float EstimateDurationInSeconds() => ParticleEffectDuration.MeasureFiringDurationInSeconds(PrimaryEffect);

        public virtual Transform IgnitePositionTransform      => _fuse.IgnitePositionTransform;
        public IFuseConnectionPoint ConnectionPoint           => _fuse.ConnectionPoint;
        public virtual bool Enabled                           => _fuse.Enabled;
        public virtual bool IsIgnited                         => _fuse.IsIgnited || _launchState.Value.IsLaunched;


        [Rpc(SendTo.Server)]
        private void SetFiringSystemRecieverDataRpc(FiringSystemReceiverData data)
        {
            _firingSystemReceiverNetworkData.Value = data;
        }

        public FiringSystemReceiverData FiringSystemReceiverData
        {
            get
            {
                return _firingSystemReceiverNetworkData.Value;
            }
            set
            {
                if (NetworkManager.IsServer)
                    _firingSystemReceiverNetworkData.Value = value;
                else
                    SetFiringSystemRecieverDataRpc(value);
            }
        }

        public ulong IgnitionCauserClientId                 => _ignitionCauser.Value;
        public void TrySetIgnitionCauser(ulong causerClientId) => _ignitionCauser.TrySet(causerClientId);
        public void ResetIgnitionCauser()                      => _ignitionCauser.Reset();
    }

    [Serializable]
    public struct BaseFireworkBehaviorSaveData
    {
        public SerializableVector3 Position;
        public SerializableRotation Rotation;
        public int ModuleIndex;
        public int CueIndex;
        public bool IsKinematic;
    }    

    [Serializable]
    public struct LaunchState : INetworkSerializable, System.IEquatable<LaunchState>
    {
        public bool IsLaunched;
        public float ServerStartTimeAsFloat;
        public byte Seed;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out IsLaunched);
                reader.ReadValueSafe(out ServerStartTimeAsFloat);
                reader.ReadValueSafe(out Seed);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(IsLaunched);
                writer.WriteValueSafe(ServerStartTimeAsFloat);
                writer.WriteValueSafe(Seed);
            }
        }

        public bool Equals(LaunchState other)
        {
            return IsLaunched == other.IsLaunched && ServerStartTimeAsFloat == other.ServerStartTimeAsFloat && Seed == other.Seed;
        }
    }

}