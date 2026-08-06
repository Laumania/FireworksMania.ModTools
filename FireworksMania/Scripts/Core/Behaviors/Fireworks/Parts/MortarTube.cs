using Cysharp.Threading.Tasks;
using DG.Tweening;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Random = UnityEngine.Random;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/MortarTube")]
    public class MortarTube : NetworkBehaviour, IIgnitable, IHaveFuse, IHaveFuseConnectionPoint, IAmGameObject, IFiringSystemReceiver
    {
        internal event Action<Transform, ShellBehavior> OnShellLaunched;
        public event Action OnFiringSystemReceiverDataUpdated;

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
        protected NetworkVariable<FiringSystemReceiverData> _firingSystemReceiverNetworkData = new NetworkVariable<FiringSystemReceiverData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private MortarTubeState? _restoredState;

        private List<Rigidbody> _otherRigidbodiesInsideMortarTube           = new List<Rigidbody>();
        private Dictionary<int, Rigidbody> _rigidbodiesRejectedThisFrame    = new Dictionary<int, Rigidbody>();

        private bool _isShellLoadingInProgress = false;
        private float _allowedBoundMaxSize;
        private int   _playerLayer;

        private void Awake()
        {
            InstantiateMortarTubeFuse();

            _playerLayer = LayerMask.NameToLayer("Player");

            if (this.GetComponent<Collider>().OrNull() == null)
                Debug.LogWarning($"MortarTube (on {this.gameObject.name}) requires at least one collider for the player to be able to ignite, erase, fuse etc. properly", this.gameObject);
        }

        private void InstantiateMortarTubeFuse()
        {
            var mortarTubeFusePrefabPath     = "Prefabs/Fireworks/Parts/MortarTubeFusePrefab";
            var mortarTubeFusePrefabResource = Resources.Load<GameObject>(mortarTubeFusePrefabPath);

            Preconditions.CheckNotNull(mortarTubeFusePrefabResource, this);

            _mortarInternalFuse = Instantiate(mortarTubeFusePrefabResource, this.transform).GetComponent<Fuse>();
        }

        private void Start()
        {
            _saveableEntity = GetComponentInParent<SaveableEntity>(); //Note: Test for now to see if this is a workable approach... can we be sure it always get the right one?
            Preconditions.CheckNotNull(_mortarInternalFuse, this);
            Preconditions.CheckNotNull(_saveableEntity, this);

            _mortarInternalFuse.SaveableEntityOwner = _saveableEntity;
            _allowedBoundMaxSize = _mortarTubeTop.DetectionRadius * 3f;

            //FixedUpdate/LateUpdate only have work while rigidbodies are inside the tube or queued
            //for rejection, and with thousands of tubes the empty per-frame calls alone cost
            //milliseconds - so the component sleeps until the trigger fills either collection.
            //Must happen after Start's initialization: Start never runs on a disabled component.
            this.enabled = false;
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

                Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFiringSystemControllerSendSignal);
            }

            Messenger.Broadcast(new MessengerEventFireworkParticleSystemsUnregisteringStruct(this.gameObject));
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _mortarInternalFuse.OnFuseCompleted += OnFuseCompleted;
                _mortarTubeTop.OnTriggerEnterAction += OnTriggerEnterMortarTube;
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFiringSystemControllerSendSignal);
            }

            if (_restoredState.HasValue)
                _tubeState.Value = _restoredState.Value;

            _tubeState.OnValueChanged += OnMortarTubeStateChanged;
            _firingSystemReceiverNetworkData.OnValueChanged += (prevData, newData) =>
            {
                OnFiringSystemReceiverDataUpdated?.Invoke();
            };

            Setup(_tubeState.Value.ShellEntityId.ToString());

            if (_tubeState.Value.IsLaunched)
                LaunchInternally();
        }

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();

            if (IsServer && IsShellLoaded == false)
            {
                _mortarInternalFuse.MarkAsUsed(); //Hack to make the FuseConnectionPoint not to show up initially on mortar before shell is loaded
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _tubeState.OnValueChanged -= OnMortarTubeStateChanged;            
        }

        private void OnFiringSystemControllerSendSignal(MessengerEventFiringSystemControllerSendSignalStruct arg)
        {
            if (this.FiringSystemReceiverData.HasValue &&
                arg.ModuleIndex == this.FiringSystemReceiverData.ModuleIndex &&
                arg.CueIndex == this.FiringSystemReceiverData.CueIndex &&
                _mortarInternalFuse?.IsIgnited == false &&
                _mortarInternalFuse?.IsUsed == false)
            {
                _mortarInternalFuse.IgniteWithoutFuseTime();
            }
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

            if (_otherRigidbodiesInsideMortarTube.Count == 0 && _rigidbodiesRejectedThisFrame.Count == 0)
                this.enabled = false;
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

            Messenger.Broadcast(new MessengerEventFireworkParticleSystemsRegisteringStruct(this.gameObject, _shellEffect.GetComponentsInChildren<ParticleSystem>(true)));

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
                Messenger.Broadcast(new MessengerEventFireworkEffectStartedStruct(this.gameObject));

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
                        await sequence.Join(shellBehaviorToLoad.gameObject.transform.DORotateQuaternion(_mortarTubeTop.transform.rotation, 0.4f))
                            .Join(shellBehaviorToLoad.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.4f))
                            .Append(
                            DOVirtual.Float(0f, 1f, 2f, (float value) => {
                                var position = Vector3.Lerp(_mortarTubeTop.transform.position, _mortarTubeBottom.transform.position, value);
                                shellBehaviorToLoad.gameObject.transform.position = position;
                            })
                        ).SetLink(shellBehaviorToLoad.gameObject);

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
            else if (IsAllowedToEnterMortarTube(otherRigidbody, out var cachedBounds))
            {
                //_isShellLoadingInProgress = true; //Removed to remove the cooldown on putting stuff into the mortar tube, as its more fun if it goes fast
                otherRigidbody.isKinematic = true;

                foreach (var collider in otherRigidbody.gameObject.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                // Calculate the scale factor to resize objectSize to (0.2, 0.2, 0.2)
                var targetScaledSize     = _mortarTubeTop.DetectionRadius * 2f;
                Vector3 scaleFactor      = Vector3.one;
                
                if (cachedBounds != null)
                {
                    // Avoid division by zero
                    scaleFactor.x = cachedBounds.Value.size.x != 0 ? Math.Clamp(targetScaledSize / cachedBounds.Value.size.x, 0f, 1f) : 1f;
                    scaleFactor.y = cachedBounds.Value.size.y != 0 ? Math.Clamp(targetScaledSize / cachedBounds.Value.size.y, 0f, 1f) : 1f;
                    scaleFactor.z = cachedBounds.Value.size.z != 0 ? Math.Clamp(targetScaledSize / cachedBounds.Value.size.z, 0f, 1f) : 1f;

                    // Use the smallest scale to maintain proportions
                    float uniformScale = Mathf.Min(scaleFactor.x, scaleFactor.y, scaleFactor.z);
                    scaleFactor = new Vector3(uniformScale, uniformScale, uniformScale);
                }

                PlayOtherObjectEnterLoadSoundClientRpc();

                var sequence = DOTween.Sequence();
                await sequence.Join(otherRigidbody.gameObject.transform.DOScale(scaleFactor, 0.2f))
                    .Join(otherRigidbody.gameObject.transform.DORotateQuaternion(_mortarTubeTop.transform.rotation, 0.2f))
                    .Join(otherRigidbody.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.2f))
                    .Append(otherRigidbody.gameObject.transform.DOMove(_mortarTubeBottom.transform.position, 0.1f))
                    .Join(otherRigidbody.gameObject.transform.DOScale(0f, .1f))
                    .SetLink(otherRigidbody.gameObject);

                otherRigidbody.gameObject.transform.position = _mortarTubeTop.transform.position; //Move it back to be inside the MortarTubeTop so it's loaded in properly when loaded via blueprints

                if (otherRigidbody.TryGetComponent<NetworkObject>(out var netObj))
                    netObj.Despawn(false);

                _otherRigidbodiesInsideMortarTube.Add(otherRigidbody);
                this.enabled = true;

                //_isShellLoadingInProgress = false;
            }
            else if(ShouldBeRejectedWithForce(otherRigidbody))
            {
                _rigidbodiesRejectedThisFrame.Add(otherRigidbody.GetInstanceID(), otherRigidbody);
                this.enabled = true;
            }
        }

        private bool ShouldBeRejectedWithForce(Rigidbody otherRigidbody)
        {
            if (_rigidbodiesRejectedThisFrame.ContainsKey(otherRigidbody.GetInstanceID()))
                return false;

            if (otherRigidbody.gameObject.layer == _playerLayer)
                return false;

            if (otherRigidbody.isKinematic)
                return false;

            return true;
        }

        private bool IsAllowedToEnterMortarTube(Rigidbody otherRigidbody, out Bounds? calculatedBounds)
        {
            calculatedBounds = null;

            if (otherRigidbody.gameObject.TryGetComponent<IIgnitable>(out var ignitable) && ignitable.IsIgnited)
                return false;

            //Note: For some reasons IsSceneObjects are not being destroyed correctly on clients, why we don't want them into a mortar as it behaves oddly. Don't know why it works like that.
            if (!otherRigidbody.TryGetComponent<NetworkObject>(out var networkObject) || networkObject.IsSceneObject == true)
                return false;

            //Holders never go into holders - no mortars inside mortars, and no firework mount
            //racks swallowed as "extra items" either (#2288)
            if (otherRigidbody.TryGetComponent<IFireworkEntityHolder>(out _))
                return false;

            if (otherRigidbody.gameObject.layer == _playerLayer)
                return false;


            calculatedBounds = FireworkMountRules.CalculateUprightRendererBounds(otherRigidbody.gameObject);
            if (calculatedBounds.HasValue && 
                calculatedBounds.Value.size.x > _allowedBoundMaxSize && 
                calculatedBounds.Value.size.y > _allowedBoundMaxSize &&
                calculatedBounds.Value.size.z > _allowedBoundMaxSize)
                return false;

            return true;
        }

        private void ShootOutOtherObjectsInTube(float shellRecoil)
        {
            if (!IsServer)
               return;

            var calculatedForce = shellRecoil * 0.7f; //Adjusted force better match how far things are flying
            foreach (var otherObjectRigidbody in _otherRigidbodiesInsideMortarTube)
            {
                if (otherObjectRigidbody == null)
                    continue;

                //Note: We have to spawn it before igniting etc. else events are not hooked up on Server and therefore won't actually ignite
                otherObjectRigidbody.transform.localScale = Vector3.one; //Scale have to be set before spawning else scale is wrong on clients
                if (otherObjectRigidbody.TryGetComponent<NetworkObject>(out var spawnNetObj))
                    spawnNetObj.Spawn(true);

                var foundFuse = otherObjectRigidbody.GetComponent<IHaveFuse>()?.GetFuse();
                if (foundFuse != null)
                {
                    if (otherObjectRigidbody.GetComponent<ShellBehavior>())
                    {
                        foundFuse.FuseTime = Random.Range(0.9f, 2.5f);
                        //Skip main shoot out effect and just explode as it looks better when shooting out of a mortar as shells fly out fast
                        //var mainModule = otherObjectRigidbody.GetComponent<ShellBehavior>().Effect.main;
                        //mainModule.startDelay = 0f;
                        //mainModule.startSpeed = 0f;
                        //mainModule.startLifetime = 0.1f;
                    }
                    else
                        //fuse.FuseTime *= Random.Range(0.05f, 0.5f);
                        foundFuse.FuseTime = Random.Range(0.2f, 0.6f);
                }

                if (otherObjectRigidbody.TryGetComponent<IIgnitable>(out var ignitable))
                    ignitable.IgniteInstant();

                var calculatedRadius                    = _mortarTubeTop.DetectionRadius * 5f;
                var randomPositionInMortarTopRadius     = _mortarTubeTop.transform.position + (_mortarTubeTop.transform.up * calculatedRadius * 0.5f) + (Random.insideUnitSphere * calculatedRadius);
                otherObjectRigidbody.transform.position = randomPositionInMortarTopRadius;
                otherObjectRigidbody.isKinematic        = false;
                otherObjectRigidbody.linearDamping      = 0f;
                otherObjectRigidbody.angularDamping     = 0f;

                foreach (var collider in otherObjectRigidbody.gameObject.GetComponentsInChildren<Collider>())
                    collider.enabled = true;

                otherObjectRigidbody.rotation = _mortarTubeTop.transform.rotation;
                otherObjectRigidbody.AddForce(Random.Range(0.7f, 1.3f) * (_mortarTubeTop.transform.up.normalized * calculatedForce * otherObjectRigidbody.mass), ForceMode.Impulse);
            }

            _otherRigidbodiesInsideMortarTube.Clear();
        }

        [Rpc(SendTo.Everyone)]
        private void PlayShellLoadSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(_loadSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectEnterLoadSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(OtherObjectEnterSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectRejectSoundClientRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(OtherObjectRejectSound, _mortarTubeTop.transform.position));
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
                _mortarInternalFuse.IgniteWithoutFuseTime();
        }

        public IFuse GetFuse()
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

            return this?.ParentEntityDefinition?.ItemName;
        }

        internal MortarTubeSaveData CaptureTubeState()
        {
            return new MortarTubeSaveData()
            {
                ShellEntityId            = _tubeState.Value.ShellEntityId.ToString(),
                FiringSystemReceiverData = this.FiringSystemReceiverData
            };
        }

        internal void RestoreTubeState(MortarTubeSaveData mortarTubeSaveData)
        {
            this.FiringSystemReceiverData = mortarTubeSaveData.FiringSystemReceiverData;

            _restoredState = new MortarTubeState()
            {
                IsLaunched             = false,
                Seed                   = 0,
                ServerStartTimeAsFloat = 0,
                ShellEntityId          = mortarTubeSaveData.ShellEntityId
            };
        }

        public Vector3 GetFiringSystemReceiverWorldPosition()
        {
            //return this.GetFuse().ConnectionPoint.Transform.position;
            return _mortarInternalFuse.transform.position;
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
    public struct MortarTubeSaveData
    {
        public string ShellEntityId;
        public FiringSystemReceiverData FiringSystemReceiverData;
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
