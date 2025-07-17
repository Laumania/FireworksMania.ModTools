using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const string OtherObjectEnterSound       = "MortarTubeEnter";
        private const string OtherObjectRejectSound      = "MortarTubeReject";
        private const float RejectionForce               = 2f;

        private ShellBehavior _shellBehaviorFromPrefab;
        private ParticleSystem _shellEffect;
        private ParticleSystem _launchEffect;
        private UnwrappedShellFuse _shellUnwrappedFuse;
        private GameObject _loadedShellMesh;
        
        private SaveableEntity _saveableEntity;
                
        private NetworkVariable<MortarTubeState> _tubeState = new NetworkVariable<MortarTubeState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private MortarTubeState? _restoredState;

        private List<Rigidbody> _otherRigidbodiesInsideMortarTube           = new List<Rigidbody>();
        private Dictionary<int, Rigidbody> _rigidbodiesRejectedThisFrame    = new Dictionary<int, Rigidbody>();

        private bool _isShellLoadingInProgress = false;
        private float _allowedBoundMaxSize;

        private void Awake()
        {
            InstantiateMortarTubeFuse();

            if (this.GetComponent<Collider>().OrNull() == null)
                Debug.LogWarning($"MortarTube (on {this.gameObject.name}) requires at least one collider for the player to be able to ignite, erase, fuse etc. properly", this.gameObject);
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
            _saveableEntity = GetComponentInParent<SaveableEntity>(); //Note: Test for now to see if this is a workable approach... can we be sure it always get the right one?
            Preconditions.CheckNotNull(_mortarInternalFuse);
            Preconditions.CheckNotNull(_saveableEntity);

            _mortarInternalFuse.SaveableEntityOwner = _saveableEntity;
            _allowedBoundMaxSize = _mortarTubeTop.DetectionRadius * 3f;
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                if (_mortarInternalFuse != null)
                    _mortarInternalFuse.OnFuseCompleted -= OnFuseCompleted;

                if (_mortarTubeTop != null)
                    _mortarTubeTop.OnTriggerEnterAction -= OnTriggerEnterMortarTube;

                foreach (var rigidbodyInsideMortar in _otherRigidbodiesInsideMortarTube)
                    rigidbodyInsideMortar?.gameObject.DestroyOrDespawn();
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _mortarInternalFuse.MarkAsUsed(); //Hack to make the FuseConnectionPoint not to show up initially on mortar before shell is loaded
                _mortarInternalFuse.OnFuseCompleted += OnFuseCompleted;
                _mortarTubeTop.OnTriggerEnterAction += OnTriggerEnterMortarTube;
            }

            if (_restoredState.HasValue)
                _tubeState.Value = _restoredState.Value;

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

        private void FixedUpdate()
        {
            if(IsServer)
                ApplyForceToRejectedRigidBodies();
        }

        private void ApplyForceToRejectedRigidBodies()
        {
            if(_rigidbodiesRejectedThisFrame.Count == 0)
                return;

            foreach (var rejectedRigidBody in _rigidbodiesRejectedThisFrame.Values)
            {
                var rejectionForce = _mortarTubeTop.transform.up.normalized * RejectionForce * rejectedRigidBody.mass;
                rejectedRigidBody.AddForce(rejectionForce, ForceMode.Impulse);
            }

            _rigidbodiesRejectedThisFrame.Clear();
            PlayOtherObjectRejectSoundClientRpc();
        }

        private void LateUpdate()
        {
            UpdatePositionOfObjectsInsideMortar();
        }

        private void UpdatePositionOfObjectsInsideMortar()
        {
            foreach (var rigidbodyInsideMortar in _otherRigidbodiesInsideMortarTube)
            {
                if (rigidbodyInsideMortar.OrNull() != null)
                    rigidbodyInsideMortar.transform.position = _mortarTubeTop.transform.position;
            }
        }

        private void Setup(string entityDefinitionId)
        {
            if (string.IsNullOrEmpty(entityDefinitionId) || IsShellLoaded)
                return;

            var entityDatabase       = DependencyResolver.Instance.Get<IEntityDefinitionDatabase>();
            var entityDefinition     = entityDatabase.GetEntityDefinition(entityDefinitionId);

            if (entityDefinition == null)
                return;

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

                ShootOutOtherObjectsInTube(_shellBehaviorFromPrefab.Recoil);

                _shellBehaviorFromPrefab = null;
                _launchEffect            = null;
                _shellEffect             = null;
                _loadedShellMesh         = null;
            }
            else
                Debug.LogWarning($"Unable to launch '{this.gameObject.name}' due to missing effects, some of them are null...can't explain it");
        }

        
        private async void OnTriggerEnterMortarTube(Collider other)
        {
            if (!IsServer)
                return;

            if (other.OrNull() == null)
                return;

            if (other.isTrigger)
                return;

            if (other.gameObject.isStatic)
                return;

            if (_isShellLoadingInProgress)
                return;
            
            var otherRigidbody = other.attachedRigidbody;
            if (otherRigidbody.OrNull() == null)
                return;

            if (IsShellLoaded == false)
            {
                var shellBehaviorToLoad = otherRigidbody.GetComponent<ShellBehavior>();
                if (shellBehaviorToLoad != null)
                {
                    if (shellBehaviorToLoad.DiameterDefinition.Diameter <= this.DiameterDefinition.Diameter &&
                        shellBehaviorToLoad.IsIgnited == false)
                    {
                        _isShellLoadingInProgress  = true;
                        otherRigidbody.isKinematic = true;

                        foreach (var collider in shellBehaviorToLoad.gameObject.GetComponentsInChildren<Collider>())
                            collider.enabled = false;

                        PlayShellLoadSoundClientRpc();

                        var sequence = DOTween.Sequence();
                        sequence.Join(shellBehaviorToLoad.gameObject.transform.DORotateQuaternion(_mortarTubeTop.transform.rotation, 0.4f));
                        sequence.Join(shellBehaviorToLoad.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.4f));
                        sequence.Append(
                            DOVirtual.Float(0f, 1f, 2f, (float value) => {
                                var position = Vector3.Lerp(_mortarTubeTop.transform.position, _mortarTubeBottom.transform.position, value);
                                shellBehaviorToLoad.gameObject.transform.position = position;
                            })
                        );
                        await sequence.AsyncWaitForCompletion();

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
            else if (IsAllowedToEnterMortarTube(otherRigidbody))
            {
                //_isShellLoadingInProgress = true; //Removed to remove the cooldown on putting stuff into the mortar tube, as its more fun if it goes fast
                otherRigidbody.isKinematic = true;

                foreach (var collider in otherRigidbody.gameObject.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                // Calculate the scale factor to resize objectSize to (0.2, 0.2, 0.2)
                var targetScaledSize     = _mortarTubeTop.DetectionRadius * 2f;
                Vector3 scaleFactor      = Vector3.one;
                var otherRigidbodyBounds = CalculateBounds(otherRigidbody.gameObject);
                
                if (otherRigidbodyBounds != null)
                {
                    // Avoid division by zero
                    scaleFactor.x = otherRigidbodyBounds.Value.size.x != 0 ? Math.Clamp(targetScaledSize / otherRigidbodyBounds.Value.size.x, 0f, 1f) : 1f;
                    scaleFactor.y = otherRigidbodyBounds.Value.size.y != 0 ? Math.Clamp(targetScaledSize / otherRigidbodyBounds.Value.size.y, 0f, 1f) : 1f;
                    scaleFactor.z = otherRigidbodyBounds.Value.size.z != 0 ? Math.Clamp(targetScaledSize / otherRigidbodyBounds.Value.size.z, 0f, 1f) : 1f;

                    // Use the smallest scale to maintain proportions
                    float uniformScale = Mathf.Min(scaleFactor.x, scaleFactor.y, scaleFactor.z);
                    scaleFactor = new Vector3(uniformScale, uniformScale, uniformScale);
                }

                PlayOtherObjectEnterLoadSoundClientRpc();

                var sequence = DOTween.Sequence();
                sequence.Join(otherRigidbody.gameObject.transform.DOScale(scaleFactor, 0.2f));
                sequence.Join(otherRigidbody.gameObject.transform.DORotateQuaternion(_mortarTubeTop.transform.rotation, 0.2f));
                sequence.Join(otherRigidbody.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.2f));
                sequence.Append(otherRigidbody.gameObject.transform.DOMove(_mortarTubeBottom.transform.position, 0.1f));
                sequence.Join(otherRigidbody.gameObject.transform.DOScale(0f, .1f));
                await sequence.AsyncWaitForCompletion();

                otherRigidbody.gameObject.transform.position = _mortarTubeTop.transform.position; //Move it back to be inside the MortarTubeTop so it's loaded in properly when loaded via blueprints

                otherRigidbody.GetComponent<NetworkObject>()?.Despawn(false);

                _otherRigidbodiesInsideMortarTube.Add(otherRigidbody);

                //_isShellLoadingInProgress = false;
            }
            else if(ShouldBeRejectedWithForce(otherRigidbody))
            {
                _rigidbodiesRejectedThisFrame.Add(otherRigidbody.GetInstanceID(), otherRigidbody);
            }
        }

        private Bounds? CalculateBounds(GameObject gameObject)
        {
            var originalRotation          = gameObject.transform.rotation;
            gameObject.transform.rotation = Quaternion.identity; //Reset rotation to calculate bounds correctly

            var meshRenderers      = gameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length > 0)
            {
                Bounds resultingBounds = meshRenderers[0].bounds;
                for (int i = 1; i < meshRenderers.Length; i++)
                {
                    resultingBounds.Encapsulate(meshRenderers[i].bounds);
                }

                gameObject.transform.rotation = originalRotation; //Restore original rotation
                return resultingBounds;
            }
            return null;
        }

        private bool ShouldBeRejectedWithForce(Rigidbody otherRigidbody)
        {
            if (_rigidbodiesRejectedThisFrame.ContainsKey(otherRigidbody.GetInstanceID()))
                return false;

            if (otherRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
                return false;

            if (otherRigidbody.isKinematic)
                return false;

            return true;
        }

        private bool IsAllowedToEnterMortarTube(Rigidbody otherRigidbody)
        {
            if (otherRigidbody.gameObject.GetComponent<IIgnitable>() != null &&
                otherRigidbody.gameObject.GetComponent<IIgnitable>().IsIgnited)
                return false;

            //Note: For some reasons IsSceneObjects are not being destroyed correctly on clients, why we don't want them into a mortar as it behaves oddly. Don't know why it works like that.
            if (otherRigidbody.GetComponent<NetworkObject>() == null || otherRigidbody.GetComponent<NetworkObject>()?.IsSceneObject == true)
                return false;

            if (otherRigidbody.gameObject.GetComponent<MortarBehavior>() != null)
                return false;

            if (otherRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
                return false;

            
            var otherRigidbodyBounds = CalculateBounds(otherRigidbody.gameObject);
            if (otherRigidbodyBounds.HasValue && 
                otherRigidbodyBounds.Value.size.x > _allowedBoundMaxSize && 
                otherRigidbodyBounds.Value.size.y > _allowedBoundMaxSize &&
                otherRigidbodyBounds.Value.size.z > _allowedBoundMaxSize)
                return false;

            return true;
        }

        private void ShootOutOtherObjectsInTube(float shellRecoil)
        {
            if (!IsServer)
               return;

            var calculatedForce = shellRecoil * 0.5f; //Adjusted force better match how far things are flying

            foreach (var otherObjectRigidbody in _otherRigidbodiesInsideMortarTube)
            {
                if (otherObjectRigidbody == null)
                    continue;

                if (otherObjectRigidbody.GetComponent<IHaveFuse>() != null)
                {
                    var fuse = otherObjectRigidbody.GetComponent<IHaveFuse>().GetFuse();
                    fuse.FuseTime *= Random.Range(0.05f, 0.5f);
                }

                if (otherObjectRigidbody.GetComponent<IIgnitable>() != null)
                    otherObjectRigidbody.GetComponent<IIgnitable>().IgniteInstant();

                otherObjectRigidbody.transform.localScale = Vector3.one;
                otherObjectRigidbody.MovePosition(((Random.insideUnitSphere * _mortarTubeTop.DetectionRadius * 5f) + _mortarTubeTop.transform.position + (_mortarTubeTop.transform.up * 0.5f)));
                otherObjectRigidbody.isKinematic = false;

                foreach (var collider in otherObjectRigidbody.gameObject.GetComponentsInChildren<Collider>())
                    collider.enabled = true;

                otherObjectRigidbody.rotation = _mortarTubeTop.transform.rotation;
                otherObjectRigidbody.AddForce(Random.Range(0.8f, 1.2f) * (_mortarTubeTop.transform.up.normalized * calculatedForce * otherObjectRigidbody.mass), ForceMode.Impulse);
                otherObjectRigidbody.GetComponent<NetworkObject>()?.Spawn(true);
            }

            _otherRigidbodiesInsideMortarTube.Clear();
        }

        [Rpc(SendTo.Everyone)]
        private void PlayShellLoadSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(_loadSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectEnterLoadSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(OtherObjectEnterSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectRejectSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(OtherObjectRejectSound, _mortarTubeTop.transform.position));
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

        internal void RestoreTubeState(string shellEntityId)
        {
            _restoredState = new MortarTubeState()
            {
                IsLaunched             = false,
                Seed                   = 0,
                ServerStartTimeAsFloat = 0,
                ShellEntityId          = shellEntityId
            };
        }

        private bool IsShellLoaded                            => _shellBehaviorFromPrefab != null;
        public Transform IgnitePositionTransform              => IsShellLoaded ? _mortarInternalFuse.transform : null;
        public bool Enabled                                   => IsShellLoaded;
        public bool IsIgnited                                 => _tubeState.Value.IsLaunched || _mortarInternalFuse.IsIgnited;
        public IFuseConnectionPoint ConnectionPoint           => _mortarInternalFuse.ConnectionPoint;
        public EntityDiameterDefinition DiameterDefinition    => _diameter;
        public string Name                                    => GenerateObjectNameWithOptionalShellName();
        public GameObject GameObject                          => this.gameObject;
        public MortarTubeState TubeState                      => _tubeState.Value;

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
