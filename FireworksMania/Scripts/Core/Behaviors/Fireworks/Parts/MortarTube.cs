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
    public class MortarTube : NetworkBehaviour, IIgnitable, IHaveFuse, IHaveFuseConnectionPoint, IAmGameObject, IFiringSystemReceiver, IIgnitionCauserCarrier
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
        private IgnitionCauser _ignitionCauser;

        [Header("Sound")]
        [SerializeField]
        [Tooltip("Sound played when a shell enters the tube")]
        [GameSound]
        private string _loadSound;
        private const string OtherObjectEnterSound       = "MortarTubeEnter";
        private const string OtherObjectRejectSound      = "MortarTubeReject";
        private const float RejectionForce               = 2f;
        private const float InchesToMeters               = 0.0254f;

        private ShellBehavior _shellBehaviorFromPrefab;
        private float _shellDurationInSeconds;
        private ParticleSystem _shellEffect;
        private ParticleSystem _launchEffect;
        private UnwrappedShellFuse _shellUnwrappedFuse;
        private GameObject _loadedShellMesh;
        
        private SaveableEntity _saveableEntity;
                
        private NetworkVariable<MortarTubeState> _tubeState = new NetworkVariable<MortarTubeState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        protected NetworkVariable<FiringSystemReceiverData> _firingSystemReceiverNetworkData = new NetworkVariable<FiringSystemReceiverData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private MortarTubeState? _restoredState;

        /// <summary>
        /// Whether the shell this tube fired has finished. Deliberately NOT part of
        /// <see cref="MortarTubeState"/>: the replicated state hangs on to IsLaunched, the shell id and
        /// the launch time on purpose so a late joiner can still replay the launch, and writing to it
        /// again would re-run Setup() for the spent shell on every peer and fire the tube a second time
        /// (see <see cref="SetShellLoadingInProgress"/>). A local flag leaves all of that alone. Every
        /// peer runs <see cref="DestroyWhenFinishedPlayingCoroutine"/> and it already discounts the time
        /// since launch, so a joiner arriving after the shell is over flips its own flag almost at once.
        /// </summary>
        private bool _isShellSpent;

        private List<Rigidbody> _otherRigidbodiesInsideMortarTube           = new List<Rigidbody>();
        private Dictionary<int, Rigidbody> _rigidbodiesRejectedThisFrame    = new Dictionary<int, Rigidbody>();

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

                //Unity's == rather than ?., because ?. is a plain reference check: a DESTROYED Rigidbody
                //sails through it and reading .gameObject off it throws, which would abort the rest of this
                //teardown. Reachable straight from the bug this method is involved in - Clear All destroys
                //the stuffed objects and the tube in the same frame, in whichever order they were placed
                foreach (var rigidbodyInsideMortar in _otherRigidbodiesInsideMortarTube)
                {
                    if (rigidbodyInsideMortar.OrNull() != null)
                        rigidbodyInsideMortar.gameObject.DestroyOrDespawn();
                }

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
                //The firing-system signal is a purely local broadcast and this handler is only
                //subscribed on the server, so the sequence runner is this machine
                _mortarInternalFuse.IgniteWithoutFuseTime(NetworkManager.LocalClientId);
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
            PlayOtherObjectRejectSoundRpc();
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
            MarkEffectAsInMortarTube(_shellEffect, entityDefinition.Id);
            _shellEffect.gameObject.SetActive(false);

            //The effect is cloned straight off the shell prefab, so ShellBehavior.Awake never runs for it and
            //DestroyWhenFinishedPlayingCoroutine below is the only thing that ever cleans it up. It now hangs
            //under the tube, so the shell has to be named explicitly or the log would blame the tube for it
            _shellEffect.DisableEndlessLooping(entityDefinition.Id);

            Messenger.Broadcast(new MessengerEventFireworkParticleSystemsRegisteringStruct(this.gameObject, _shellEffect.GetComponentsInChildren<ParticleSystem>(true)));

            //Read here rather than at launch: the tube is standing still with a shell in it, and the shell
            //it holds cannot change without coming back through here. It is the shell's own duration - the
            //number the inventory shows for that shell - so a shell fired from a tube frees its owner's slot
            //at the same moment one lit on the ground would (#2657). Content the catalog never saw is
            //estimated off the shell prefab on the spot
            _shellDurationInSeconds = FireworkDurationCatalog.TryGetDurationInSeconds(entityDefinition, out var shellDurationInSeconds)
                ? shellDurationInSeconds
                : _shellBehaviorFromPrefab.EstimateDurationInSeconds();

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
                _mortarInternalFuse.FuseTime = actualShellFuse.FuseTime;

            if (actualShellFuse != null && actualShellFuse.Effect != null)
            {
                var fuseEffect = Instantiate(actualShellFuse.Effect, _shellUnwrappedFuse.transform);
                fuseEffect.transform.position = _shellUnwrappedFuse.IgnitePosition.position;
                fuseEffect.transform.rotation = _shellUnwrappedFuse.IgnitePosition.rotation;
                _mortarInternalFuse.ReplaceEffect(fuseEffect, actualShellFuse.IgniteSound);
            }
            else
            {
                //The internal fuse prefab no longer carries its own effect (#2258), so a shell without
                //a fuse of its own (or with a fuse missing its effect) gets the standard mortar fuse
                //effect instantiated for it here
                var mortarTubeFuseEffectPrefabPath     = "Prefabs/Fireworks/Parts/MortarTubeFuseEffectPrefab";
                var mortarTubeFuseEffectPrefabResource = Resources.Load<GameObject>(mortarTubeFuseEffectPrefabPath);
                Preconditions.CheckNotNull(mortarTubeFuseEffectPrefabResource, this);

                var fallbackFuseEffect = Instantiate(mortarTubeFuseEffectPrefabResource, _mortarInternalFuse.transform).GetComponent<ParticleSystem>();
                Preconditions.CheckNotNull(fallbackFuseEffect, this);
                _mortarInternalFuse.ReplaceEffect(fallbackFuseEffect);
            }

            _mortarInternalFuse.ResetFuse();
            //A reloaded tube is a fresh shot: the next igniter earns the next shell's destruction
            ResetIgnitionCauser();
        }

        private void MarkEffectAsInMortarTube(ParticleSystem effect, string shellEntityDefinitionId)
        {
            foreach (var shellSound in effect.GetComponentsInChildren<ParticleSystemShellSound>())
                shellSound.IsInMortarTube = true;

            //Same reason the looping warning is handed the shell's id in Setup: under the tube, an observer that
            //looked its item up in the hierarchy would blame the tube for the shell's content (#2856)
            foreach (var observer in effect.GetComponentsInChildren<ParticleSystemObserver>(true))
                observer.ItemName = shellEntityDefinitionId;
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
                StartCoroutine(DestroyWhenFinishedPlayingCoroutine(_shellEffect, _launchEffect, _shellDurationInSeconds));

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

            if (_tubeState.Value.IsShellLoading)
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
                        //Flagged the moment the shell starts its way down the bore, not when it lands at
                        //the bottom two seconds later - and replicated, so every peer's placement tools
                        //stop offering this tube right away instead of showing a ghost for a tube that
                        //is already taking a shell (#2349)
                        SetShellLoadingInProgress(true);
                        otherRigidbody.isKinematic = true;

                        try
                        {
                            foreach (var collider in shellBehaviorToLoad.gameObject.GetComponentsInChildren<Collider>())
                                collider.enabled = false;

                            PlayShellLoadSoundRpc();

                            var boreAlignedRotation = CalculateBoreAlignedRotation(shellBehaviorToLoad.gameObject.transform.rotation, _mortarTubeTop.transform.rotation);

                            var sequence = DOTween.Sequence();
                            await sequence.Join(shellBehaviorToLoad.gameObject.transform.DORotateQuaternion(boreAlignedRotation, 0.4f))
                                .Join(shellBehaviorToLoad.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.4f))
                                .Append(
                                DOVirtual.Float(0f, 1f, 2f, (float value) => {
                                    var position = Vector3.Lerp(_mortarTubeTop.transform.position, _mortarTubeBottom.transform.position, value);
                                    shellBehaviorToLoad.gameObject.transform.position = position;
                                })
                            ).SetLink(shellBehaviorToLoad.gameObject);

                            //Either side of the load can be erased while the shell is still travelling
                            //down the bore - the finally below is what puts the tube back up for grabs
                            if (shellBehaviorToLoad.OrNull() == null || this.OrNull() == null || IsSpawned == false)
                                return;

                            _tubeState.Value = new MortarTubeState()
                            {
                                IsLaunched             = false,
                                Seed                   = 0,
                                ServerStartTimeAsFloat = 0,
                                ShellEntityId          = shellBehaviorToLoad.EntityDefinition.Id
                            };

                            //The shell stops existing here and the tube takes over its effects, so
                            //anything keeping books on the shell has to be told before it goes (#2630)
                            Messenger.Broadcast(new MessengerEventShellSwallowedByMortarTubeStruct(shellBehaviorToLoad.gameObject, this));

                            shellBehaviorToLoad.gameObject.DestroyOrDespawn();
                        }
                        finally
                        {
                            if (this.OrNull() != null && IsSpawned)
                                SetShellLoadingInProgress(false);
                        }
                    }
                }
            }
            else if (IsAllowedToEnterMortarTube(otherRigidbody, out var cachedBounds))
            {
                //Deliberately does NOT set the shell-loading flag - that would put a cooldown on
                //stuffing things into a mortar tube, and it is more fun when it goes fast
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

                PlayOtherObjectEnterLoadSoundRpc();

                //Bore-aligned for the same reason the shell path above is (#2358): a shell dropped into
                //a tube that is ALREADY loaded comes through here instead, and tweening to the tube's own
                //rotation corkscrewed it in exactly the way that fix removed (#2470). Safe here too -
                //ShootOutOtherObjectsInTube snaps the rotation to the tube's before firing anything out,
                //so this tween's end orientation is thrown away either way.
                var boreAlignedRotation = CalculateBoreAlignedRotation(otherRigidbody.gameObject.transform.rotation, _mortarTubeTop.transform.rotation);

                var sequence = DOTween.Sequence();
                await sequence.Join(otherRigidbody.gameObject.transform.DOScale(scaleFactor, 0.2f))
                    .Join(otherRigidbody.gameObject.transform.DORotateQuaternion(boreAlignedRotation, 0.2f))
                    .Join(otherRigidbody.gameObject.transform.DOMove(_mortarTubeTop.transform.position, 0.2f))
                    .Append(otherRigidbody.gameObject.transform.DOMove(_mortarTubeBottom.transform.position, 0.1f))
                    .Join(otherRigidbody.gameObject.transform.DOScale(0f, .1f))
                    .SetLink(otherRigidbody.gameObject);

                otherRigidbody.gameObject.transform.position = _mortarTubeTop.transform.position; //Move it back to be inside the MortarTubeTop so it's loaded in properly when loaded via blueprints

                if (otherRigidbody.TryGetComponent<NetworkObject>(out var netObj))
                    netObj.Despawn(false);

                _otherRigidbodiesInsideMortarTube.Add(otherRigidbody);
                this.enabled = true;
            }
            else if(ShouldBeRejectedWithForce(otherRigidbody))
            {
                _rigidbodiesRejectedThisFrame.Add(otherRigidbody.GetInstanceID(), otherRigidbody);
                this.enabled = true;
            }
        }

        /// <summary>
        /// Smallest rotation that stands an item up along the bore. All it has to do on its way in is
        /// line its own up axis up with the tube's - which way it faces around that axis makes no
        /// difference, since a shell is destroyed at the bottom and replaced by a mesh clone snapped to
        /// the tube bottom, and anything else stuffed in has its rotation snapped to the tube's again on
        /// the way out. Tweening all the way to the tube's own rotation instead adds a twist around the
        /// bore, so an item inserted yawed spins on its way down and one lying on its side corkscrews -
        /// very obvious on long shells (#2358, and #2470 for the already-loaded tube).
        /// </summary>
        private static Quaternion CalculateBoreAlignedRotation(Quaternion currentRotation, Quaternion boreRotation) =>
            Quaternion.FromToRotation(currentRotation * Vector3.up, boreRotation * Vector3.up) * currentRotation;

        private void SetShellLoadingInProgress(bool isLoading)
        {
            var state = _tubeState.Value;

            //A launched tube hangs on to its shell id and launch time so late joiners can replay the
            //launch - but the tube itself is empty again, so a shell going in has to wipe that stale
            //state. Without the wipe, replicating the loading flag alone would re-run Setup() for the
            //old shell on every peer and fire the tube a second time.
            if (isLoading)
            {
                state.IsLaunched             = false;
                state.Seed                   = 0;
                state.ServerStartTimeAsFloat = 0;
                state.ShellEntityId          = default;

                //Same wipe, for the local half of it - a freshly loaded tube has not fired yet.
                _isShellSpent                = false;
            }

            state.IsShellLoading = isLoading;
            _tubeState.Value     = state;
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
                {
                    spawnNetObj.Spawn(true);

                    //Netcode forgot the recorded parent when the object was despawned on its way into the
                    //tube, so that Spawn just applied "no parent" and left the object at the scene root
                    //instead of back under FireworksManager - where Clear All can never reach it again
                    //(#2452). Every other Spawn of an entity re-parents on the very next line; this one
                    //cannot do it directly, because FireworksManager is in an assembly Core does not
                    //reference, so it asks for it instead
                    Messenger.Broadcast(new MessengerEventEntityRespawnedOutsideSpawnPathStruct(otherObjectRigidbody.gameObject));
                }

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
                    ignitable.IgniteInstant(IgnitionCauserClientId);

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
        private void PlayShellLoadSoundRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(_loadSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectEnterLoadSoundRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(OtherObjectEnterSound, _mortarTubeTop.transform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayOtherObjectRejectSoundRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(OtherObjectRejectSound, _mortarTubeTop.transform.position));
        }

        private IEnumerator DestroyWhenFinishedPlayingCoroutine(ParticleSystem shellEffect, ParticleSystem launchEffect, float shellDurationInSeconds)
        {
            //The shell is spent once its duration has passed since launch - the same number the inventory
            //shows for it - which is where it stops costing its owner a spawn slot (#2651, #2657). Measured
            //against the replicated launch time once, up front: a late joiner has less of it left to wait,
            //and a reload while this shell is still in the air resets the tube's launch time, which must not
            //shorten this shell's wait. The IsAlive wait below is what the destroy still hangs off
            var remainingSeconds = shellDurationInSeconds - GetLaunchTimeDifference();
            if (remainingSeconds > 0f)
                yield return new WaitForSeconds(remainingSeconds);

            Messenger.Broadcast(new MessengerEventMortarTubeShellSpentStruct(this));

            //The tube stays "launched" for the replay, but it is no longer a firework going off, so it
            //must stop counting as one - ObjectVisibilityManager shows every ignited object to a joining
            //client immediately, outside the streaming budget (#2746).
            _isShellSpent = true;

            yield return new WaitWhile(() => shellEffect.IsAlive(true) || shellEffect.isPlaying);

            Destroy(shellEffect.gameObject);
            Destroy(launchEffect.gameObject);
        }

        public void Ignite(float ignitionForce)
        {
            if (IsShellLoaded)
                _mortarInternalFuse.Ignite(ignitionForce, IgnitionCauserClientId);
        }

        public void Ignite(float ignitionForce, ulong causerClientId)
        {
            TrySetIgnitionCauser(causerClientId);
            Ignite(ignitionForce);
        }

        public void IgniteInstant()
        {
            if (IsShellLoaded)
                _mortarInternalFuse.IgniteWithoutFuseTime(IgnitionCauserClientId);
        }

        public void IgniteInstant(ulong causerClientId)
        {
            TrySetIgnitionCauser(causerClientId);
            IgniteInstant();
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

        //The green ring the tools show on this tube in game, drawn from the same trigger sphere (#2911)
        private void OnDrawGizmos()
        {
            if (_mortarTubeTop != null)
                PlacementRing.DrawGizmo(GetOpeningPose(), OpeningRadius);
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
        //"Is going off right now", not "has ever fired" - a tube keeps IsLaunched after firing so a late
        //joiner can replay the launch, and reading that as ignited made every fired tube a permanent
        //instant reveal for joining clients (#2746).
        public bool IsIgnited                                 => (_tubeState.Value.IsLaunched && _isShellSpent == false) || _mortarInternalFuse.IsIgnited;
        public IFuseConnectionPoint ConnectionPoint           => _mortarInternalFuse.ConnectionPoint;
        public EntityDiameterDefinition DiameterDefinition    => _diameter;
        public string Name                                    => GenerateObjectNameWithOptionalShellName();
        public GameObject GameObject                          => this.gameObject;
        public MortarTubeState TubeState                      => _tubeState.Value;

        /// <summary>
        /// Whether the tube would take a shell right now - the same rule the tube itself applies when
        /// a shell hits its trigger, so placement tools can offer exactly the tubes that will accept
        /// one. Note that this is NOT "<see cref="TubeState"/> has no shell id": a fired tube keeps
        /// its shell id for late joiners while being perfectly empty and reloadable (#2349).
        /// </summary>
        public bool IsReadyToLoadShell                        => IsShellLoaded == false && _tubeState.Value.IsShellLoading == false;

        /// <summary>
        /// Where the green placement ring goes on this tube: centered on the trigger sphere of its
        /// <see cref="MortarTubeTop"/>, facing the way that object faces - the same rule as a rack socket's
        /// (#2911). Moving the sphere is how a creator moves the ring, and the gizmo shows it in the editor. A
        /// tube top without a trigger sphere gets its ring on the tube top itself.
        ///
        /// It used to sit on the rim, worked out from where a loaded shell's fuse hangs over the edge. The
        /// sphere is where a shell is caught instead, which on the stock mortars is 1 to 5 cm above the rim.
        /// </summary>
        public Pose GetOpeningPose()
        {
            var triggerSphere = TriggerSphere;
            if (triggerSphere != null)
                return PlacementRing.GetPose(triggerSphere);

            var topTransform = _mortarTubeTop.transform;
            return new Pose(topTransform.position, topTransform.rotation);
        }

        /// <summary>
        /// How big the green placement ring on this tube is: its tube top's trigger sphere's radius, in world
        /// units (#2911). A tube top without a trigger sphere gets a ring the size of the tube's bore instead.
        /// </summary>
        public float OpeningRadius
        {
            get
            {
                var triggerSphere = TriggerSphere;
                if (triggerSphere != null)
                    return PlacementRing.GetRadius(triggerSphere);

                if (_diameter == null)
                    return 0f;

                //The diameter is a shell CLASS in inches and a shared asset, so it cannot know a rack has
                //scaled the tube it sits in
                var tubeScale = _mortarTubeTop.transform.lossyScale;
                return _diameter.Diameter * InchesToMeters * 0.5f * Mathf.Max(Mathf.Abs(tubeScale.x), Mathf.Abs(tubeScale.z));
            }
        }

        //TryGetComponent rather than a cached lookup: it allocates nothing, and the gizmo needs a sphere a
        //creator has only just added or removed to show up straight away
        private SphereCollider TriggerSphere =>
            _mortarTubeTop != null && _mortarTubeTop.TryGetComponent<SphereCollider>(out var sphere) && sphere.isTrigger ? sphere : null;

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

        public ulong IgnitionCauserClientId                 => _ignitionCauser.Value;
        public void TrySetIgnitionCauser(ulong causerClientId) => _ignitionCauser.TrySet(causerClientId);
        public void ResetIgnitionCauser()                      => _ignitionCauser.Reset();
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

        /// <summary>
        /// True from the moment a shell is inserted until it reaches the bottom of the tube. Replicated
        /// so every peer knows the tube is spoken for during those couple of seconds, rather than each
        /// peer having to wait for <see cref="ShellEntityId"/> to arrive at the end of the load (#2349).
        /// </summary>
        public bool IsShellLoading;
        public float ServerStartTimeAsFloat;
        public byte Seed;
        public FixedString128Bytes ShellEntityId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out IsLaunched);
                reader.ReadValueSafe(out IsShellLoading);
                reader.ReadValueSafe(out ServerStartTimeAsFloat);
                reader.ReadValueSafe(out Seed);
                reader.ReadValueSafe(out ShellEntityId);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(IsLaunched);
                writer.WriteValueSafe(IsShellLoading);
                writer.WriteValueSafe(ServerStartTimeAsFloat);
                writer.WriteValueSafe(Seed);
                writer.WriteValueSafe(ShellEntityId);
            }
        }

        public bool Equals(MortarTubeState other)
        {
            return IsLaunched == other.IsLaunched &&
                   IsShellLoading == other.IsShellLoading &&
                   ServerStartTimeAsFloat == other.ServerStartTimeAsFloat &&
                   Seed == other.Seed &&
                   ShellEntityId == other.ShellEntityId;
        }
    }
}
