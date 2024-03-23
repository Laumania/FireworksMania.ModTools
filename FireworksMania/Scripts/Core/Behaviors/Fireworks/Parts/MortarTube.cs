using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/MortarTube")]
    public class MortarTube : NetworkBehaviour, IIgnitable, IHaveFuse, IHaveFuseConnectionPoint
    {
        [SerializeField]
        [Tooltip("The diameter of the mortar tube. This is used to calculate if a shell will fit")]
        private EntityDiameterDefinition _diameter;
        
        [SerializeField]
        [Tooltip("This defines where the shell is put into the tube and where it is shot out")]
        private MortarTubeEntryAndLaunchPosition _entryAndLaunchPosition;
        
        [SerializeField]
        [Tooltip("Position at the edge of the tube where the shell fuse should be placed")]
        private ShellFusePivotPosition _shellFusePivotPosition;
        
        [SerializeField]
        [Tooltip("The position of this fuse is not important as it will be moved dynamically to the ignite position of the ShellFuse provided by the Shell put into the MortarTube. Hint: You typically want to use the 'Fuse Invisible' for this, as its only there for the functionality and not the visual")]
        private Fuse _fuse;

        [SerializeField]
        [Tooltip("Sound played when a shell enters the tube")]
        [GameSound]
        private string _loadSound;

        private ShellBehavior _shellBehaviorFromPrefab;
        private ParticleSystem _shellEffect;
        private ParticleSystem _launchEffect;
        private ShellFuse _shellFuse;

        private SaveableEntity _saveableEntity;
                
        private NetworkVariable<MortarTubeState> _tubeState = new NetworkVariable<MortarTubeState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        protected void Start()
        {
            if (_fuse == null)
            {
                Debug.LogError($"Missing {nameof(Fuse)} on '{this.gameObject.name}' - this is not gonna work! Make sure this fireworks have a fuse.", this);
                this.enabled = false;
                return;
            }

            _saveableEntity = GetComponentInParent<SaveableEntity>(); //Note: Test for now to see if this is a workable approach... can we be sure it always get the right one?
            if (_saveableEntity == null)
            {
                Debug.LogError($"Missing '{nameof(SaveableEntity)}' which is a required component - make sure '{this.name}' have one", this);
                return;
            }

            _fuse.SaveableEntityOwner = _saveableEntity;

            if (IsServer)
                _fuse.IgniteWithoutFuseTime(); //Hack to make the FuseConnectionPoint not to show up initially on mortar before shell is loaded

            _fuse.OnFuseCompleted += OnFuseCompleted;
            _entryAndLaunchPosition.OnTriggerEnterAction += OnTriggerEnterMortarTube;
        }

        public override void OnDestroy()
        {
            if (_fuse != null)
                _fuse.OnFuseCompleted -= OnFuseCompleted;

            if(_entryAndLaunchPosition != null)
                _entryAndLaunchPosition.OnTriggerEnterAction -= OnTriggerEnterMortarTube;

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _tubeState.OnValueChanged += OnMortarTubeStateChanged;

            Setup(_tubeState.Value.ShellEntityId.ToString());

            if (_tubeState.Value.IsLaunched)
                LaunchInternally();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _tubeState.OnValueChanged -= OnMortarTubeStateChanged;
        }

        private void OnMortarTubeStateChanged(MortarTubeState prevState, MortarTubeState newState)
        {
            Setup(newState.ShellEntityId.ToString());
            
            if (prevState.IsLaunched == false && newState.IsLaunched == true)
                LaunchInternally();
        }

        private void OnFuseCompleted()
        {
            if (IsServer)
            {
                _tubeState.Value = new MortarTubeState()
                {
                    IsLaunched             = true,
                    ServerStartTimeAsFloat = this.NetworkManager.ServerTime.TimeAsFloat,
                    Seed                   = (byte)UnityEngine.Random.Range(0, 254),
                    ShellEntityId          = _tubeState.Value.ShellEntityId,
                };
            }
        }

        private void Setup(string entityDefinitionId)
        {
            if (string.IsNullOrEmpty(entityDefinitionId) || IsShellLoaded)
                return;

            var entityDatabase       = DependencyResolver.Instance.Get<IEntityDefinitionDatabase>();
            var entityDefinition     = entityDatabase.GetEntityDefinition(entityDefinitionId);
            _shellBehaviorFromPrefab = entityDefinition.PrefabGameObject.GetComponent<ShellBehavior>();

            _launchEffect                    = Instantiate(_shellBehaviorFromPrefab.LaunchEffectPrefab, this.transform);
            _launchEffect.transform.position = _entryAndLaunchPosition.transform.position;
            _launchEffect.transform.rotation = _entryAndLaunchPosition.transform.rotation;
            _launchEffect.gameObject.SetActive(false);

            _shellEffect                    = Instantiate(_shellBehaviorFromPrefab.Effect, this.transform);
            _shellEffect.transform.position = _entryAndLaunchPosition.transform.position;
            _shellEffect.transform.rotation = _entryAndLaunchPosition.transform.rotation;
            _shellEffect.gameObject.SetActive(false);

            _shellFuse                    = Instantiate(_shellBehaviorFromPrefab.ShellFuse, this.transform);
            _shellFuse.transform.position = _shellFusePivotPosition.transform.position;
            _shellFuse.transform.rotation = _shellFusePivotPosition.transform.rotation;
            _shellFuse.gameObject.SetActive(true);

            _fuse.transform.position = _shellFuse.IgnitePosition.position;
            _fuse.transform.rotation = _shellFuse.IgnitePosition.rotation;

            _fuse.ResetFuse();

            if(!IsServer)
                PlayShellLoadSound();
        }

        private void LaunchInternally()
        {
            if (IsShellLoaded)
            {
                _launchEffect.gameObject.SetActive(true);
                _launchEffect.SetRandomSeed(_tubeState.Value.Seed, GetLaunchTimeDifference());
                _launchEffect.Play(true);

                _shellEffect.gameObject.SetActive(true);
                _shellEffect.SetRandomSeed(_tubeState.Value.Seed, GetLaunchTimeDifference());
                _shellEffect.Play(true);

                Destroy(_shellFuse.gameObject);
                StartCoroutine(DestroyWhenFinishedPlayingCoroutine(_shellEffect, _launchEffect));

                _shellBehaviorFromPrefab = null;
                _launchEffect = null;
                _shellEffect = null;
            }
            else
                Debug.LogWarning($"Unable to launch '{this.gameObject.name}' due to missing effects, some of them are null...can't explain it");
        }

        private bool _isShellLoadingInProgress = false;
        private async void OnTriggerEnterMortarTube(Collider other)
        {
            if (_isShellLoadingInProgress)
                return;

            if (!IsServer)
                return;

            if (IsShellLoaded)
                return;

            var shellBehaviorToLoad = other.gameObject.GetComponent<ShellBehavior>();
            if (shellBehaviorToLoad != null && other.gameObject.GetComponent<Rigidbody>()?.isKinematic == false)
            {
                if(shellBehaviorToLoad.DiameterDefinition.Diameter <= this.DiameterDefinition.Diameter &&
                   shellBehaviorToLoad.IsIgnited == false)
                {
                    _isShellLoadingInProgress = true;

                    shellBehaviorToLoad.GetComponent<Rigidbody>().isKinematic = true;

                    foreach (var collider in shellBehaviorToLoad.gameObject.GetComponentsInChildren<Collider>())
                    {
                        Destroy(collider);
                    }

                    PlayShellLoadSound();

                    await shellBehaviorToLoad.gameObject.transform.DORotateQuaternion(_entryAndLaunchPosition.transform.rotation, 0.2f);
                    await shellBehaviorToLoad.gameObject.transform.DOMove(_entryAndLaunchPosition.transform.position, 0.2f);

                    await shellBehaviorToLoad.gameObject.transform.DOMove(_entryAndLaunchPosition.transform.position - (_entryAndLaunchPosition.transform.up * 0.7f), 2f);

                    _tubeState.Value = new MortarTubeState()
                    {
                        IsLaunched             = false,
                        Seed                   = 0,
                        ServerStartTimeAsFloat = 0,
                        ShellEntityId          = shellBehaviorToLoad.EntityDefinition.Id
                    };

                    Destroy(shellBehaviorToLoad.gameObject);

                    _isShellLoadingInProgress = false;
                }
            }
        }

        private void PlayShellLoadSound()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_loadSound, _entryAndLaunchPosition.transform.position));
        }

        private IEnumerator DestroyWhenFinishedPlayingCoroutine(ParticleSystem shellEffect, ParticleSystem launchEffect)
        {
            yield return new WaitWhile(() => shellEffect.IsAlive(true) || shellEffect.isPlaying);

            Destroy(shellEffect.gameObject);
            Destroy(launchEffect.gameObject);
        }

        public void Ignite(float ignitionForce)
        {
            if (IsShellLoaded)
                _fuse.Ignite(ignitionForce);
        }

        public void IgniteInstant()
        {
            if (IsShellLoaded)
                _fuse.IgniteInstant();
        }

        public Fuse GetFuse()
        {
            if (IsShellLoaded)
                return _fuse;
            
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (_diameter == null)
                        Debug.LogError($"Missing {nameof(EntityDiameterDefinition)} on {this.gameObject.name}", this);
                }
            };
        }
#endif
        private float GetLaunchTimeDifference()
        {
            return this.NetworkManager.ServerTime.TimeAsFloat - _tubeState.Value.ServerStartTimeAsFloat;
        }

        private bool IsShellLoaded                            => _shellBehaviorFromPrefab != null;
        public Transform IgnitePositionTransform              => IsShellLoaded ? _fuse.transform : null;
        public bool Enabled                                   => IsShellLoaded;
        public bool IsIgnited                                 => _tubeState.Value.IsLaunched; //_fuse.IsIgnited || _launchState.Value.IsLaunched;
        public IFuseConnectionPoint ConnectionPoint           => _fuse.ConnectionPoint;
        public EntityDiameterDefinition DiameterDefinition    => _diameter;
    }

    [Serializable]
    public struct MortarTubeState : INetworkSerializable, System.IEquatable<MortarTubeState>
    {
        public bool IsLaunched;
        public float ServerStartTimeAsFloat;
        public byte Seed;
        public FixedString128Bytes ShellEntityId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out IsLaunched);
                reader.ReadValueSafe(out ServerStartTimeAsFloat);
                reader.ReadValueSafe(out Seed);
                reader.ReadValueSafe(out ShellEntityId);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(IsLaunched);
                writer.WriteValueSafe(ServerStartTimeAsFloat);
                writer.WriteValueSafe(Seed);
                writer.WriteValueSafe(ShellEntityId);
            }
        }

        public bool Equals(MortarTubeState other)
        {
            return IsLaunched == other.IsLaunched &&
                   ServerStartTimeAsFloat == other.ServerStartTimeAsFloat &&
                   Seed == other.Seed &&
                   ShellEntityId == other.ShellEntityId;
        }
    }
}
