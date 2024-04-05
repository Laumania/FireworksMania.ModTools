using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Persistence;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    public abstract class BaseFireworkBehavior : NetworkBehaviour, IAmGameObject, ISaveableComponent, IHaveBaseEntityDefinition, IIgnitable, IHaveFuse, IHaveFuseConnectionPoint
    {
        [Header("General")]
        //[HideInInspector]
        //[ReadOnly]
        [FormerlySerializedAs("_metadata")]
        [Tooltip("This field should never been necessary to setup manually. It will be set automatically when this prefab is assigned to FireworksEntityDefinition")]
        [SerializeField]
        private FireworkEntityDefinition _entityDefinition;

        [SerializeField]
        protected Fuse _fuse;
        protected CancellationToken _cancellationTokentoken;

        private SaveableEntity _saveableEntity;

        public Action<BaseFireworkBehavior> OnDestroyed;

        protected NetworkVariable<LaunchState> _launchState = new NetworkVariable<LaunchState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

            if (this.GetComponent<IErasable>() == null)
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

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _launchState.OnValueChanged += (prevValue, newValue) =>
            {
                //NetworkLog.LogInfo($"[{NetworkManager.Singleton.LocalClientId}] _launchState.OnValueChanged newValue.IsLaunched {newValue.IsLaunched}");
                if (prevValue.IsLaunched == false && newValue.IsLaunched == true)
                    LaunchInternalAsync(_cancellationTokentoken).Forget();
            };

            if(_launchState.Value.IsLaunched)
                LaunchInternalAsync(_cancellationTokentoken).Forget();
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

            ValidateErasableBehavior();
            ValidateSaveableEntity();
        }

        private void ValidateSaveableEntity()
        {
            var saveableComponents = GetComponents<SaveableEntity>();
            if (saveableComponents.Length > 1)
            {
                Debug.LogError($"'{this.EntityDefinition?.Id}' have '{saveableComponents.Length}' '{nameof(SaveableEntity)}'s' - it can have one and only one - please delete so only one is left else it will be saved multiple times in blueprints", this.gameObject);
            }

            _saveableEntity = GetComponent<SaveableEntity>();
            if (_saveableEntity == null)
            {
                _saveableEntity = this.gameObject.AddComponent<SaveableEntity>();
            }

            _saveableEntity.EntityDefinition = _entityDefinition;
        }

        private void ValidateErasableBehavior()
        {
            var erasableComponents = GetComponents<ErasableBehavior>();
            if (erasableComponents.Length == 0)
            {
                this.gameObject.AddComponent<ErasableBehavior>();
                Debug.Log($"Added required '{nameof(ErasableBehavior)}' to this entity can be removed via the Eraser Tool in game", this.gameObject);
            }
            
            if (erasableComponents.Length > 1)
            {
                Debug.LogWarning($"'{this.EntityDefinition?.Id}' have '{erasableComponents.Length}' '{nameof(ErasableBehavior)}'s' it should have one and only one - removing all the extra ones", this.gameObject);
            }
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
            
            Destroy(this.gameObject);
            
        }

        public virtual CustomEntityComponentData CaptureState()
        {
            var customData         = new CustomEntityComponentData();
            var currentRigidbody   = this.GetComponent<Rigidbody>();

            customData.Add<SerializableVector3>(nameof(BaseFireworkBehaviorData.Position), new SerializableVector3
            {
                X = this.transform.position.x,
                Y = this.transform.position.y,
                Z = this.transform.position.z
            });

            customData.Add<SerializableRotation>(nameof(BaseFireworkBehaviorData.Rotation), new SerializableRotation()
            {
                X = this.transform.rotation.x,
                Y = this.transform.rotation.y,
                Z = this.transform.rotation.z,
                W = this.transform.rotation.w
            });

            customData.Add<bool>(nameof(BaseFireworkBehaviorData.IsKinematic), currentRigidbody != null ? currentRigidbody.isKinematic : false);
        
            return customData;
        }

        public virtual void RestoreState(CustomEntityComponentData customComponentData)
        {
            var position    = customComponentData.Get<SerializableVector3>(nameof(BaseFireworkBehaviorData.Position));
            var rotation    = customComponentData.Get<SerializableRotation>(nameof(BaseFireworkBehaviorData.Rotation));
            var isKinematic = customComponentData.Get<bool>(nameof(BaseFireworkBehaviorData.IsKinematic));

            this.transform.position   = new Vector3(position.X, position.Y, position.Z);
            this.transform.rotation   = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        
            var rigidbody = this.GetComponent<Rigidbody>();
            if (rigidbody != null)
                rigidbody.isKinematic = isKinematic;
        }

        public virtual void Ignite(float ignitionForce)
        {
            if (_fuse == null)
            {
                Debug.LogError($"Trying to call Ignite on '{this.gameObject.name}' but Fuse is null... that's a problem - trying to delete firework to avoid further issues");

                if (NetworkManager.IsServer)
                    GameObject.Destroy(this.gameObject);

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
                    GameObject.Destroy(this.gameObject);

                return;
            }

            _fuse.IgniteInstant();
        }

        public virtual Fuse GetFuse()
        {
            return _fuse;
        }

        protected float GetLaunchTimeDifference()
        {
            return this.NetworkManager.ServerTime.TimeAsFloat - _launchState.Value.ServerStartTimeAsFloat;
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
        public virtual bool IsIgnited                         => _launchState.Value.IsLaunched;
    }

    [Serializable]
    public struct BaseFireworkBehaviorData
    {
        public SerializableVector3 Position;
        public SerializableRotation Rotation;
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