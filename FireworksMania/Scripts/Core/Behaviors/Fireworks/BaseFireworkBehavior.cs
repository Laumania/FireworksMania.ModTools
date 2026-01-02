using Cysharp.Threading.Tasks;
using DG.Tweening;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
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
        IFiringSystemReceiver
    {
        [Header("General")]
        [FormerlySerializedAs("_metadata")]
        [SerializeField]
        private FireworkEntityDefinition _entityDefinition;

        [SerializeField]
        protected Fuse _fuse;
        protected CancellationToken _cancellationTokentoken;

        private SaveableEntity _saveableEntity;

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
        }

        protected virtual void Start()
        {
            _fuse.OnFuseCompleted += OnFuseCompleted;
        }

        public override void OnDestroy()
        {
            if (_fuse != null)
                _fuse.OnFuseCompleted -= OnFuseCompleted;

            if(IsServer)
                Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignal>(OnFiringSystemControllerSendSignal);

            base.OnDestroy();
        }

        public override void OnNetworkDespawn()
        {
            _launchState.OnValueChanged -= OnLaunchStateValueChanged;
            _firingSystemReceiverNetworkData.OnValueChanged -= OnFiringSystemReceiverNetworkDataValueChanged;

            if (IsServer)
                Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignal>(OnFiringSystemControllerSendSignal);

            base.OnNetworkDespawn();
        }

        public override void OnNetworkSpawn()
        {
            _launchState.OnValueChanged += OnLaunchStateValueChanged;
            _firingSystemReceiverNetworkData.OnValueChanged += OnFiringSystemReceiverNetworkDataValueChanged;

            if (_launchState.Value.IsLaunched)
                LaunchInternalAsync(_cancellationTokentoken).Forget();

            if (IsServer)
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignal>(OnFiringSystemControllerSendSignal);

            base.OnNetworkSpawn();
        }

        private void OnFiringSystemReceiverNetworkDataValueChanged(FiringSystemReceiverData previousValue, FiringSystemReceiverData newValue)
        {
            OnFiringSystemReceiverDataUpdated?.Invoke();
        }

        private void OnLaunchStateValueChanged(LaunchState previousValue, LaunchState newValue)
        {
            //NetworkLog.LogInfo($"[{NetworkManager.Singleton.LocalClientId}] _launchState.OnValueChanged newValue.IsLaunched {newValue.IsLaunched}");
            if (previousValue.IsLaunched == false && newValue.IsLaunched == true)
                LaunchInternalAsync(_cancellationTokentoken).Forget();
        }

        private void OnFiringSystemControllerSendSignal(MessengerEventFiringSystemControllerSendSignal arg)
        {
            if(this.FiringSystemReceiverData.HasValue &&
                arg.ModuleIndex == this.FiringSystemReceiverData.ModuleIndex && 
                arg.CueIndex == this.FiringSystemReceiverData.CueIndex && 
                _fuse?.IsIgnited == false && 
                _fuse?.IsUsed == false)
            {
                _fuse.IgniteWithoutFuseTime();
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

        protected virtual void ResetLaunchState()
        {
            if (IsServer)
            {
                _launchState.Value = default;
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
            if (!IsServer)
                return;
#if UNITY_EDITOR
            Debug.LogWarning("Todo: Implement nice destroy animation in DestroyFireworkAnimatedAsync", this);
#endif
            await this.transform.DOShakeScale(.3f, 0.5f, 5, 50f, true).WithCancellation(token);
            token.ThrowIfCancellationRequested();
            await this.transform.DOScale(0f, UnityEngine.Random.Range(.1f, .2f)).WithCancellation(token);
            token.ThrowIfCancellationRequested();

            OnDestroyed?.Invoke(this);
            
            this.gameObject.DestroyOrDespawn();
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

            _fuse.Ignite(ignitionForce);
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

            _fuse.IgniteInstant();
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