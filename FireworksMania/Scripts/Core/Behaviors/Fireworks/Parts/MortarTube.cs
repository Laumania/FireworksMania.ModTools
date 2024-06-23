using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DG.Tweening;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Utilities;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/MortarTube")]
    public class MortarTube : NetworkBehaviour, IIgnitable, IHaveFuse, IHaveFuseConnectionPoint, IAmGameObject
    {
        internal event Action<Transform, ShellBehavior> OnShellLaunched;

        [Header("Size")]
        [SerializeField]
        [Tooltip("The diameter of the mortar tube. This is used to calculate if a shell will fit")]
        private EntityDiameterDefinition _diameter;

        [Header("Parts")]
        [SerializeField]
        [Tooltip("Defines where the shell is put into the tube and where it is shot out")]
        private MortarTubeTop _mortarTubeTop;

        [SerializeField]
        [Tooltip("Defines the position of the shell when it is fully loaded into the tube. Aka at the bottom of the tube")]
        private MortarTubeBottom _mortarTubeBottom;

        [Header("Unwrapped Shell Fuse")]
        [SerializeField]
        [Tooltip("Defines the position on the tube where the unwrapped shell fuse pivots/hang over the edge of the tube")]
        private UnwrappedShellFusePivotPosition _unwrappedShellFusePivotPosition;

        private Fuse _mortarInternalFuse;

        [Header("Sound")]
        [SerializeField]
        [Tooltip("Sound played when a shell enters the tube")]
        [GameSound]
        private string _loadSound;

        private ShellBehavior _shellBehaviorFromPrefab;
        private ParticleSystem _shellEffect;
        private ParticleSystem _launchEffect;
        private UnwrappedShellFuse _shellUnwrappedFuse;
        private GameObject _loadedShellMesh;

        private SaveableEntity _saveableEntity;
                
        private NetworkVariable<MortarTubeState> _tubeState = new NetworkVariable<MortarTubeState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private Dictionary<int, Rigidbody> _otherObjectsInTube = new Dictionary<int, Rigidbody>();

        private void Awake()
        {
            InstantiateMortarTubeFuse();
        }

        private void InstantiateMortarTubeFuse()
        {
            var mortarTubeFusePrefabPath     = "Prefabs/Fireworks/Parts/MortarTubeFusePrefab";
            var mortarTubeFusePrefabResource = Resources.Load<GameObject>(mortarTubeFusePrefabPath);

            Preconditions.CheckNotNull(mortarTubeFusePrefabResource);

            _mortarInternalFuse = Instantiate(mortarTubeFusePrefabResource, this.transform).GetComponent<Fuse>();
        }

        private void Start()
        {
            if (_mortarInternalFuse == null)
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

            _mortarInternalFuse.SaveableEntityOwner = _saveableEntity;

            if (IsServer)
                _mortarInternalFuse.IgniteWithoutFuseTime(); //Hack to make the FuseConnectionPoint not to show up initially on mortar before shell is loaded

            _mortarInternalFuse.OnFuseCompleted += OnFuseCompleted;
            _mortarTubeTop.OnTriggerEnterAction += OnTriggerEnterMortarTube;
        }

        public override void OnDestroy()
        {
            if (_mortarInternalFuse != null)
                _mortarInternalFuse.OnFuseCompleted -= OnFuseCompleted;

            if(_mortarTubeTop != null)
                _mortarTubeTop.OnTriggerEnterAction -= OnTriggerEnterMortarTube;

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

            if (_tubeState.Value.IsLaunched)
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
            _launchEffect.transform.position = _mortarTubeTop.transform.position;
            _launchEffect.transform.rotation = _mortarTubeTop.transform.rotation;
            _launchEffect.gameObject.SetActive(false);

            _shellEffect                    = Instantiate(_shellBehaviorFromPrefab.Effect, this.transform);
            _shellEffect.transform.position = _mortarTubeTop.transform.position;
            _shellEffect.transform.rotation = _mortarTubeTop.transform.rotation;
            MarkEffectAsInMortarTube(_shellEffect);
            _shellEffect.gameObject.SetActive(false);

            var mainEffect           = _shellEffect.main;
            var calculatedStartSpeed = mainEffect.startSpeed.Evaluate(0) * CalculateStartSpeedForceMultiplier(this.DiameterDefinition.Diameter, _shellBehaviorFromPrefab.DiameterDefinition.Diameter);
            mainEffect.startSpeed    = calculatedStartSpeed;

            _shellUnwrappedFuse                    = Instantiate(_shellBehaviorFromPrefab.UnwrappedShellFusePrefab, this.transform);
            _shellUnwrappedFuse.transform.position = _unwrappedShellFusePivotPosition.transform.position;
            _shellUnwrappedFuse.transform.rotation = _unwrappedShellFusePivotPosition.transform.rotation;
            _shellUnwrappedFuse.gameObject.SetActive(true);

            _mortarInternalFuse.transform.position = _shellUnwrappedFuse.IgnitePosition.position;
            _mortarInternalFuse.transform.rotation = _shellUnwrappedFuse.IgnitePosition.rotation;

            if (_shellBehaviorFromPrefab.ModelMeshRenderer != null)
            {
                _loadedShellMesh = Instantiate(_shellBehaviorFromPrefab.ModelMeshRenderer.gameObject, this.transform);

                foreach (var componentsInChild in _loadedShellMesh.GetComponentsInChildren<Collider>())
                    componentsInChild.enabled = false;

                _loadedShellMesh.transform.position = _mortarTubeBottom.transform.position;
                _loadedShellMesh.transform.rotation = _mortarTubeBottom.transform.rotation;
            }

            var actualShellFuse = _shellBehaviorFromPrefab.GetFuse();
            if (actualShellFuse != null)
            {
                _mortarInternalFuse.FuseTime = actualShellFuse.FuseTime;
                
                var fuseEffect = Instantiate(actualShellFuse.Effect, _shellUnwrappedFuse.transform);
                fuseEffect.transform.position = _shellUnwrappedFuse.IgnitePosition.position;
                fuseEffect.transform.rotation = _shellUnwrappedFuse.IgnitePosition.rotation;
                _mortarInternalFuse.ReplaceEffect(fuseEffect, actualShellFuse.IgniteSound);
            }
            
            _mortarInternalFuse.ResetFuse();
        }

        private void MarkEffectAsInMortarTube(ParticleSystem effect)
        {
            foreach (var shellSound in effect.GetComponentsInChildren<ParticleSystemShellSound>())
                shellSound.IsInMortarTube = true;
        }

        private float CalculateStartSpeedForceMultiplier(float mortarTubeDiameter, float shellDiameter)
        {
            var rawResult = shellDiameter / mortarTubeDiameter;

            if (rawResult < 1f) //If not perfect fit, we decrease the startspeed multiplayer even more
                rawResult *= 0.75f;

            return Mathf.Clamp(rawResult, 0.1f, 1f);
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

                Destroy(_shellUnwrappedFuse.gameObject);
                Destroy(_loadedShellMesh.gameObject);
                StartCoroutine(DestroyWhenFinishedPlayingCoroutine(_shellEffect, _launchEffect));

                OnShellLaunched?.Invoke(this.transform, _shellBehaviorFromPrefab);

                ShootOutOtherObjectsInTube();

                _shellBehaviorFromPrefab = null;
                _launchEffect            = null;
                _shellEffect             = null;
                _loadedShellMesh         = null;
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

                    PlayShellLoadSoundClientRpc();

                    await shellBehaviorToLoad.gameObject.transform.DORotateQuaternion(_mortarTubeTop.transform.rotation, 0.2f);
                    await shellBehaviorToLoad.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.2f);

                    await shellBehaviorToLoad.gameObject.transform.DOMove(_mortarTubeBottom.transform.position, 2f);

                    _tubeState.Value = new MortarTubeState()
                    {
                        IsLaunched             = false,
                        Seed                   = 0,
                        ServerStartTimeAsFloat = 0,
                        ShellEntityId          = shellBehaviorToLoad.EntityDefinition.Id
                    };

                    shellBehaviorToLoad.gameObject.DestroyOrDespawn();

                    _isShellLoadingInProgress = false;
                }
            }
        }

        private void ShootOutOtherObjectsInTube()
        {
            if (!IsServer)
                return;
            
            var shellBehavior        = _shellBehaviorFromPrefab;
            var originExplosionForce = shellBehavior.Recoil * 30f;
            var explosionRadius      = Vector3.Distance(_mortarTubeBottom.transform.position, _mortarTubeTop.transform.position) * 2.5f;
            foreach (var otherObjectRigidbody in _otherObjectsInTube.Values)
            {
                if(otherObjectRigidbody == null)
                    continue;

                if (otherObjectRigidbody.GetComponent<IHaveFuse>() != null)
                {
                    var fuse      = otherObjectRigidbody.GetComponent<IHaveFuse>().GetFuse();
                    fuse.FuseTime *= Random.Range(0.05f, 0.5f);
                }

                if (otherObjectRigidbody.GetComponent<IIgnitable>() != null)
                    otherObjectRigidbody.GetComponent<IIgnitable>().IgniteInstant();

                otherObjectRigidbody.isKinematic = false;
                otherObjectRigidbody.MovePosition(((Random.insideUnitSphere * _mortarTubeTop.DetectionRadius * 5f) + _mortarTubeTop.transform.position + (_mortarTubeTop.transform.up * 0.5f)));

                var relativeToMassExplosionForce = originExplosionForce * Mathf.Clamp(otherObjectRigidbody.mass / originExplosionForce, .05f, 1f);
                otherObjectRigidbody.AddExplosionForce(relativeToMassExplosionForce, _mortarTubeBottom.transform.position, explosionRadius, 0.45f, ForceMode.Impulse);
            }
            _otherObjectsInTube.Clear();
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;
            var rigidBody = other.GetComponent<Rigidbody>();
            if (rigidBody != null && rigidBody.GetComponent<MortarBehavior>() == null)
            {
                _otherObjectsInTube.TryAdd(other.gameObject.GetInstanceID(), other.GetComponent<Rigidbody>());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer)
                return;

            if (_otherObjectsInTube.ContainsKey(other.gameObject.GetInstanceID()))
            {
                _otherObjectsInTube.Remove(other.gameObject.GetInstanceID());
            }
        }

        [ClientRpc]
        private void PlayShellLoadSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_loadSound, _mortarTubeTop.transform.position));
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
                _mortarInternalFuse.Ignite(ignitionForce);
        }

        public void IgniteInstant()
        {
            if (IsShellLoaded)
                _mortarInternalFuse.IgniteInstant();
        }

        public Fuse GetFuse()
        {
            //if (IsShellLoaded)
            //    return _mortarInternalFuse;
            
            return _mortarInternalFuse;
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

        private string GenerateObjectNameWithOptionalShellName()
        {
            if(IsShellLoaded && _shellBehaviorFromPrefab.EntityDefinition is FireworkEntityDefinition fireworkEntityDefinition)
                return $"{this.ParentEntityDefinition.ItemName}{Environment.NewLine}({fireworkEntityDefinition.ItemName})";

            return this.ParentEntityDefinition.ItemName;
        }

        private bool IsShellLoaded                            => _shellBehaviorFromPrefab != null;
        public Transform IgnitePositionTransform              => IsShellLoaded ? _mortarInternalFuse.transform : null;
        public bool Enabled                                   => IsShellLoaded;
        public bool IsIgnited                                 => _tubeState.Value.IsLaunched || _mortarInternalFuse.IsIgnited;
        public IFuseConnectionPoint ConnectionPoint           => _mortarInternalFuse.ConnectionPoint;
        public EntityDiameterDefinition DiameterDefinition    => _diameter;
        public string Name                                    => GenerateObjectNameWithOptionalShellName();
        public GameObject GameObject                          => this.gameObject;

        internal FireworkEntityDefinition ParentEntityDefinition
        {
            get;
            set;
        }
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
