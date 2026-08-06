using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FireworksMania.Core.Common;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/FireworkMountPoint")]
    public class FireworkMountPoint : NetworkBehaviour
    {
        [Header("Socket")]
        [SerializeField]
        [Tooltip("The pose a seated firework is snapped and held to. Its up-axis is the firing direction.")]
        private Transform _mountPoseTransform;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Inner diameter of the tube in meters. Fireworks whose renderer bounds footprint is larger than this (plus a small tolerance) bounce off instead of seating.")]
        private float _allowedDiameter = 0.08f;

        private const string MountSound        = "MortarTubeEnter";
        private const string RejectSound      = "MortarTubeReject";
        private const float  RejectionForce   = 2f;
        private const float  MountTweenSeconds = 0.4f;

        //0 = empty socket. Written by the server, read by everyone so every peer can pin locally.
        private NetworkVariable<ulong> _mountedEntityNetworkObjectId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        //Registry of live sockets, so tools can enumerate placement targets without physics
        //queries (overlap buffers silently truncate in busy scenes - #1105 ghost previews)
        private static readonly List<FireworkMountPoint> _activeMountPoints = new List<FireworkMountPoint>();
        public static IReadOnlyList<FireworkMountPoint> ActiveMountPoints => _activeMountPoints;

        private BaseFireworkBehavior _mountedFirework;
        private Rigidbody            _mountedRigidbody;
        private float                _mountedBottomOffset;
        private bool                 _isMountingInProgress;
        private int                  _reSeatBlockedRigidbodyId;
        private int                  _playerLayer;
        private Collider[]           _rackColliders;
        private Collider             _triggerCollider;

        private readonly List<(Collider RackCollider, Collider SeatedCollider)> _ignoredCollisionPairs = new List<(Collider, Collider)>();
        private readonly Dictionary<int, Rigidbody> _rigidbodiesRejectedThisFrame                      = new Dictionary<int, Rigidbody>();

        private struct MountingCandidate
        {
            public Rigidbody            Rigidbody;
            public BaseFireworkBehavior Firework;
            public NetworkObject        NetworkObject;
        }

        private void Awake()
        {
            Preconditions.CheckNotNull(_mountPoseTransform, this);
            _playerLayer = LayerMask.NameToLayer("Player");
        }

        private void Start()
        {
            var mountRoot = GetComponentInParent<FireworkMountBehavior>();
            Preconditions.CheckNotNull(mountRoot, this);
            _rackColliders = mountRoot.GetComponentsInChildren<Collider>();

            //Update methods only have work while something is seated or queued for rejection, so the
            //component sleeps until then - same pattern as MortarTube. Trigger callbacks still fire
            //while disabled. Must happen after Start: Start never runs on a disabled component.
            //Sleep only when there is nothing to do. OnNetworkSpawn may already have woken this
            //tube for a restored/late-joined seated entity - and on dynamically spawned objects
            //it runs BEFORE Start, so an unconditional disable here would undo that wake.
            this.enabled = _mountedEntityNetworkObjectId.Value != 0;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _activeMountPoints.Add(this);
            _mountedEntityNetworkObjectId.OnValueChanged += OnSeatedEntityChanged;

            if (_mountedEntityNetworkObjectId.Value != 0)
                this.enabled = true; //Late joiner or restored state - resolved lazily in LateUpdate
        }

        public override void OnNetworkDespawn()
        {
            //The tube is going away (rack erased/despawned). Free the seated firework here rather
            //than relying on the rack's OnDestroy - Unity doesn't guarantee parent-before-child
            //destroy order, and NGO invokes OnNetworkDespawn deterministically on every peer.
            if (IsServer)
            {
                if (_mountedFirework.OrNull() == null)
                    TryResolveMountedFirework();

                if (_mountedRigidbody.OrNull() != null && _mountedRigidbody.TryGetComponent<IsPickedUp>(out _) == false)
                    _mountedRigidbody.isKinematic = false;
            }

            ClearLocalMountState();

            _activeMountPoints.Remove(this);
            _mountedEntityNetworkObjectId.OnValueChanged -= OnSeatedEntityChanged;
            base.OnNetworkDespawn();
        }

        private void OnSeatedEntityChanged(ulong previousId, ulong newId)
        {
            if (newId == 0)
            {
                ClearLocalMountState();
                return;
            }

            //A blueprint restore can overwrite an occupied socket - never let a cached entity from
            //a previous id keep its pin, marker or collision pairs
            if (_mountedFirework.OrNull() != null && _mountedFirework.NetworkObject.NetworkObjectId != newId)
                ClearLocalMountState();

            this.enabled = true;
        }

        private void ClearLocalMountState()
        {
            foreach (var (rackCollider, seatedCollider) in _ignoredCollisionPairs)
            {
                if (rackCollider != null && seatedCollider != null)
                    Physics.IgnoreCollision(rackCollider, seatedCollider, false);
            }
            _ignoredCollisionPairs.Clear();

            if (IsServer && _mountedFirework.OrNull() != null)
            {
                if (_mountedFirework.TryGetComponent<IsMountedFirework>(out var seatedMarker))
                    Destroy(seatedMarker);
            }

            _mountedFirework     = null;
            _mountedRigidbody    = null;
            _mountedBottomOffset = 0f;
        }

        private bool TryResolveMountedFirework()
        {
            var seatedId = _mountedEntityNetworkObjectId.Value;
            if (seatedId == 0)
                return false;

            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(seatedId, out var seatedNetworkObject) == false)
                return false;

            _mountedFirework  = seatedNetworkObject.GetComponent<BaseFireworkBehavior>();
            _mountedRigidbody = seatedNetworkObject.GetComponent<Rigidbody>();

            if (_mountedFirework == null || _mountedRigidbody == null)
            {
                _mountedFirework  = null;
                _mountedRigidbody = null;
                return false;
            }

            _mountedBottomOffset = CalculateBottomOffset(seatedNetworkObject.gameObject);
            IgnoreCollisionsWithRack(seatedNetworkObject.gameObject);
            return true;
        }

        private float CalculateBottomOffset(GameObject seatedGameObject)
        {
            var seatedBounds = FireworkMountRules.CalculateUprightRendererBounds(seatedGameObject);
            if (seatedBounds.HasValue == false)
                return 0f;

            //Distance from the entity's pivot to the bottom of its visuals when upright. Fireworks
            //pivot at their center (or higher), so pinning the pivot straight to the tube floor
            //would sink or float them - this offset puts their BASE on the tube floor instead.
            //Every peer computes the same value from the same renderers, so pins stay consistent.
            return seatedGameObject.transform.position.y - seatedBounds.Value.min.y;
        }

        private Vector3 GetMountWorldPosition()
        {
            return _mountPoseTransform.position + _mountPoseTransform.up * _mountedBottomOffset;
        }

        private void IgnoreCollisionsWithRack(GameObject seatedGameObject)
        {
            if (_ignoredCollisionPairs.Count > 0)
                return; //Already set up for this seat cycle (pairs are cleared on release)

            //A seated entity keeps its colliders enabled (so it stays ignitable/grabbable/erasable),
            //and those permanently overlap the rack's own colliders. Without this, the kinematic
            //seated body would shove the dynamic rack around every physics step.
            foreach (var seatedCollider in seatedGameObject.GetComponentsInChildren<Collider>())
            {
                if (seatedCollider.isTrigger)
                    continue;

                foreach (var rackCollider in _rackColliders)
                {
                    if (rackCollider == null || rackCollider.isTrigger)
                        continue;

                    Physics.IgnoreCollision(rackCollider, seatedCollider, true);
                    _ignoredCollisionPairs.Add((rackCollider, seatedCollider));
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
                ApplyForceToRejectedRigidbodies();
        }

        private void ApplyForceToRejectedRigidbodies()
        {
            if (_rigidbodiesRejectedThisFrame.Count == 0)
                return;

            foreach (var rejectedRigidbody in _rigidbodiesRejectedThisFrame.Values)
            {
                if (rejectedRigidbody.OrNull() == null)
                    continue;

                var rejectionForce = _mountPoseTransform.up.normalized * RejectionForce * rejectedRigidbody.mass;
                rejectedRigidbody.AddForce(rejectionForce, ForceMode.Impulse);
            }

            _rigidbodiesRejectedThisFrame.Clear();
            PlayRejectSoundRpc();
        }

        private void LateUpdate()
        {
            if (_mountedEntityNetworkObjectId.Value == 0)
            {
                if (_rigidbodiesRejectedThisFrame.Count == 0 && _isMountingInProgress == false)
                    this.enabled = false;
                return;
            }

            if (_mountedFirework.OrNull() == null && TryResolveMountedFirework() == false)
            {
                //Server: the seated entity is gone (fired + despawned, erased) -> free the socket.
                //Clients: the entity may simply not have spawned locally yet (late join) - keep waiting.
                if (IsServer)
                    _mountedEntityNetworkObjectId.Value = 0;
                return;
            }

            var isPickedUp = _mountedRigidbody.TryGetComponent<IsPickedUp>(out _);

            if (IsServer && FireworkMountRules.ShouldRelease(_mountedRigidbody.OrNull() != null, _mountedRigidbody.isKinematic, isPickedUp))
            {
                //Block instant re-seating until the object has left the trigger, else a grab-out
                //would snap it straight back into the socket
                _reSeatBlockedRigidbodyId          = _mountedRigidbody.GetInstanceID();
                _mountedEntityNetworkObjectId.Value = 0; //OnValueChanged does the local cleanup on every peer
                return;
            }

            //Pin on every peer while seated - the rack can move and its tubes follow with zero lag
            if (_mountedRigidbody.isKinematic && isPickedUp == false)
                _mountedRigidbody.transform.SetPositionAndRotation(GetMountWorldPosition(), _mountPoseTransform.rotation);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsServer == false || IsSpawned == false)
                return;

            var candidate = GetMountingCandidate(other);
            if (candidate.Rigidbody == null)
                return;

            //The just-released body is on its way out (grab-out, launch) - never bounce it
            if (candidate.Rigidbody.GetInstanceID() == _reSeatBlockedRigidbodyId)
                return;

            var canMountIgnoringFit = CanMountCandidate(candidate);
            var fitsMountPoint      = FitsMountPoint(candidate.Rigidbody.gameObject);
            var isIgnited           = candidate.Firework != null && candidate.Firework.IsIgnited;

            if (FireworkMountRules.ShouldRejectWithForce(canMountIgnoringFit, fitsMountPoint, candidate.Rigidbody.isKinematic, isIgnited) &&
                _rigidbodiesRejectedThisFrame.ContainsKey(candidate.Rigidbody.GetInstanceID()) == false)
            {
                _rigidbodiesRejectedThisFrame.Add(candidate.Rigidbody.GetInstanceID(), candidate.Rigidbody);
                this.enabled = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (IsServer == false || IsSpawned == false)
                return;

            if (_mountedEntityNetworkObjectId.Value != 0 || _isMountingInProgress)
                return;

            var candidate = GetMountingCandidate(other);
            if (candidate.Rigidbody == null)
                return;

            if (candidate.Rigidbody.GetInstanceID() == _reSeatBlockedRigidbodyId)
                return;

            if (CanMountCandidate(candidate) == false)
                return;

            if (FitsMountPoint(candidate.Rigidbody.gameObject) == false)
                return;

            MountAsync(candidate.Firework, candidate.Rigidbody);
        }

        private void OnTriggerExit(Collider other)
        {
            var exitingRigidbody = other.attachedRigidbody;
            if (exitingRigidbody != null && exitingRigidbody.GetInstanceID() == _reSeatBlockedRigidbodyId)
                _reSeatBlockedRigidbodyId = 0;
        }

        private MountingCandidate GetMountingCandidate(Collider other)
        {
            var candidate = new MountingCandidate();

            if (other.OrNull() == null || other.isTrigger || other.gameObject.isStatic)
                return candidate;

            var otherRigidbody = other.attachedRigidbody;
            if (otherRigidbody.OrNull() == null || otherRigidbody.gameObject.layer == _playerLayer)
                return candidate;

            //Holders never go into holders - a mount rack can not be mounted onto another rack,
            //the same way mortars and racks can not go into mortar tubes
            if (otherRigidbody.TryGetComponent<IFireworkEntityHolder>(out _))
                return candidate;

            candidate.Rigidbody     = otherRigidbody;
            candidate.Firework      = otherRigidbody.GetComponent<BaseFireworkBehavior>();
            candidate.NetworkObject = otherRigidbody.GetComponent<NetworkObject>();
            return candidate;
        }

        private bool CanMountCandidate(MountingCandidate candidate)
        {
            //Note: For some reason IsSceneObjects are not being destroyed correctly on clients (see
            //MortarTube) - keep them out of sockets the same way they are kept out of mortars
            var isMissingSpawnedNetworkObject = candidate.NetworkObject == null ||
                                                candidate.NetworkObject.IsSpawned == false ||
                                                candidate.NetworkObject.IsSceneObject == true;

            return FireworkMountRules.CanMount(
                isSocketOccupied:              _mountedEntityNetworkObjectId.Value != 0,
                isFirework:                    candidate.Firework != null,
                isIgnited:                     candidate.Firework != null && candidate.Firework.IsIgnited,
                isPickedUp:                    candidate.Rigidbody.TryGetComponent<IsPickedUp>(out _),
                isAlreadySeated:               candidate.Rigidbody.TryGetComponent<IsMountedFirework>(out _),
                isMissingSpawnedNetworkObject: isMissingSpawnedNetworkObject,
                isKinematic:                   candidate.Rigidbody.isKinematic);
        }

        private bool FitsMountPoint(GameObject candidateGameObject)
        {
            var candidateBounds = FireworkMountRules.CalculateUprightRendererBounds(candidateGameObject);
            if (candidateBounds.HasValue == false)
                return false;

            return FireworkMountRules.FitsDiameter(candidateBounds.Value.size, _allowedDiameter);
        }

        private async void MountAsync(BaseFireworkBehavior firework, Rigidbody fireworkRigidbody)
        {
            _isMountingInProgress = true;
            this.enabled         = true;

            try
            {
                //Marker + kinematic must be set before the first await so a neighboring socket's
                //OnTriggerStay in the same physics step can't seat the same entity twice
                fireworkRigidbody.gameObject.GetOrAdd<IsMountedFirework>();
                fireworkRigidbody.isKinematic = true;

                _mountedBottomOffset = CalculateBottomOffset(fireworkRigidbody.gameObject);

                //Collision-ignore must cover the insert tween too, not just the seated state -
                //otherwise the kinematic firework grinds against the rack colliders on its way in
                IgnoreCollisionsWithRack(fireworkRigidbody.gameObject);

                PlayMountSoundRpc();

                var sequence = DOTween.Sequence();
                await sequence.Join(fireworkRigidbody.transform.DORotateQuaternion(_mountPoseTransform.rotation, MountTweenSeconds))
                              .Join(fireworkRigidbody.transform.DOMove(GetMountWorldPosition(), MountTweenSeconds))
                              .SetLink(fireworkRigidbody.gameObject);

                var tubeAlive = this.OrNull() != null && IsSpawned;

                if (fireworkRigidbody.OrNull() == null || firework.OrNull() == null)
                {
                    //The firework died mid-tween - drop the collision pairs set up at seat start
                    if (tubeAlive)
                        ClearLocalMountState();
                    return;
                }

                if (tubeAlive == false)
                {
                    //The rack (or this tube) was destroyed while the firework was still tweening in -
                    //undo the half-done seat so it isn't left frozen mid-air and marked seated forever
                    if (fireworkRigidbody.TryGetComponent<IsMountedFirework>(out var strandedMarker))
                        Destroy(strandedMarker);
                    if (fireworkRigidbody.TryGetComponent<IsPickedUp>(out _) == false)
                        fireworkRigidbody.isKinematic = false;
                    if (this.OrNull() != null)
                        ClearLocalMountState(); //despawned but not destroyed - drop the pairs from seat start
                    return;
                }

                if (fireworkRigidbody.TryGetComponent<IsPickedUp>(out _))
                {
                    //A player grabbed it out of the air mid-seat - let them keep it
                    if (fireworkRigidbody.TryGetComponent<IsMountedFirework>(out var grabbedMarker))
                        Destroy(grabbedMarker);
                    ClearLocalMountState();
                    return;
                }

                //Replicated only once the entity is in place, so peers never see the tween fight the pin
                _mountedEntityNetworkObjectId.Value = firework.NetworkObject.NetworkObjectId;
            }
            finally
            {
                _isMountingInProgress = false;
            }
        }

        internal FireworkMountPointSaveData CaptureMountPointSaveData()
        {
            var saveData = new FireworkMountPointSaveData();

            if (_mountedFirework.OrNull() == null)
                TryResolveMountedFirework();

            if (_mountedFirework.OrNull() != null)
            {
                var saveableEntity = _mountedFirework.GetComponent<SaveableEntity>();
                if (saveableEntity != null)
                    saveData.MountedEntityInstanceId = saveableEntity.EntityInstanceId;
            }

            return saveData;
        }

        internal void RestoreMountedEntity(SaveableEntity savedEntity)
        {
            if (IsServer == false)
                return;

            var fireworkRigidbody = savedEntity.GetComponent<Rigidbody>();
            var firework          = savedEntity.GetComponent<BaseFireworkBehavior>();
            var networkObject     = savedEntity.GetComponent<NetworkObject>();

            if (firework == null || fireworkRigidbody == null || networkObject == null || networkObject.IsSpawned == false)
            {
                Debug.LogWarning($"Unable to restore seated firework in '{this.gameObject.name}' - the saved entity is missing a required component or is not spawned", this);
                return;
            }

            fireworkRigidbody.gameObject.GetOrAdd<IsMountedFirework>();
            fireworkRigidbody.isKinematic = true;

            _mountedBottomOffset = CalculateBottomOffset(fireworkRigidbody.gameObject);
            fireworkRigidbody.transform.SetPositionAndRotation(GetMountWorldPosition(), _mountPoseTransform.rotation);

            _mountedEntityNetworkObjectId.Value = networkObject.NetworkObjectId;

            //Resolve right away so the collision-ignore pairs exist before the next physics step -
            //a spawned-into-socket entity must never get even one frame of contact with the rack
            TryResolveMountedFirework();
        }

        internal void ReleaseMountedEntity()
        {
            if (IsServer == false || _mountedEntityNetworkObjectId.Value == 0)
                return;

            if (_mountedFirework.OrNull() == null)
                TryResolveMountedFirework();

            //Let it drop when the rack disappears under it - unless a player is carrying it right now
            if (_mountedRigidbody.OrNull() != null && _mountedRigidbody.TryGetComponent<IsPickedUp>(out _) == false)
                _mountedRigidbody.isKinematic = false;

            if (IsSpawned)
                _mountedEntityNetworkObjectId.Value = 0;
            else
                ClearLocalMountState();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayMountSoundRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(MountSound, _mountPoseTransform.position));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayRejectSoundRpc()
        {
            Messenger.Broadcast(new MessengerEventPlaySoundAtVector3Struct(RejectSound, _mountPoseTransform.position));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (_mountPoseTransform == null)
                        Debug.LogError($"Missing MountPoseTransform on '{this.gameObject.name}' - a mounted firework has nowhere to snap to", this);

                    var foundTriggerCollider = GetComponents<Collider>().FirstOrDefault(x => x.isTrigger);
                    if (foundTriggerCollider == null)
                        Debug.LogError($"{nameof(FireworkMountPoint)} (on '{this.gameObject.name}') requires a collider marked as trigger to detect fireworks being inserted", this.gameObject);
                }
            };
        }

        private void OnDrawGizmos()
        {
            //Two things an author needs to SEE, not read as numbers: the firing direction (orange
            //arrow) and the allowed diameter (cyan wire cylinder along the bore - anything slimmer
            //than the cylinder fits). The cylinder extends well past the sleeve so it stays
            //visible outside the rack model, unlike a single disc at the buried mount pose.
            var poseTransform = _mountPoseTransform != null ? _mountPoseTransform : this.transform;
            var origin        = poseTransform.position;
            var direction     = poseTransform.up;
            var radius        = _allowedDiameter * 0.5f;
            var boreLength    = Mathf.Max(0.15f, _allowedDiameter * 3f);
            var boreTop       = origin + direction * boreLength;

            var right   = poseTransform.right   * radius;
            var forward = poseTransform.forward * radius;

            UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(origin, direction, radius);
            UnityEditor.Handles.DrawWireDisc(boreTop, direction, radius);
            UnityEditor.Handles.DrawLine(origin + right,   boreTop + right);
            UnityEditor.Handles.DrawLine(origin - right,   boreTop - right);
            UnityEditor.Handles.DrawLine(origin + forward, boreTop + forward);
            UnityEditor.Handles.DrawLine(origin - forward, boreTop - forward);

            UnityEditor.Handles.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            UnityEditor.Handles.DrawLine(origin, boreTop);
            UnityEditor.Handles.ConeHandleCap(0, boreTop + direction * (boreLength * 0.1f), Quaternion.LookRotation(direction), boreLength * 0.2f, EventType.Repaint);
        }
#endif

        internal BaseFireworkBehavior MountedFirework
        {
            get
            {
                if (_mountedFirework.OrNull() == null && _mountedEntityNetworkObjectId.Value != 0)
                    TryResolveMountedFirework();

                return _mountedFirework.OrNull();
            }
        }

        public bool HasMountedFirework => _mountedEntityNetworkObjectId.Value != 0;

        /// <summary>
        /// Whether an item with the given upright renderer-bounds size would pass this tube's
        /// seating fit check - lets tools show the fit before the player commits (#1105).
        /// </summary>
        public bool FitsFootprint(Vector3 uprightBoundsSize)
        {
            return FireworkMountRules.FitsDiameter(uprightBoundsSize, _allowedDiameter);
        }

        /// <summary>
        /// The exact pose an item would be held at when seated in this tube, given the distance
        /// from the item's pivot to the bottom of its visuals measured upright. Lets tools preview
        /// and spawn items already in place (#1105).
        /// </summary>
        public Pose GetMountedPose(float uprightPivotToBottomOffset)
        {
            return new Pose(_mountPoseTransform.position + _mountPoseTransform.up * uprightPivotToBottomOffset, _mountPoseTransform.rotation);
        }

        /// <summary>
        /// Server-only: instantly seats a freshly spawned firework in this tube (no tween), so
        /// spawn-into-socket never has a free-physics frame colliding with the rack. Returns false
        /// when the socket is occupied or the entity is not seatable - caller decides the fallback.
        /// </summary>
        public bool TryMountSpawnedFirework(GameObject spawnedFirework)
        {
            if (IsServer == false || _mountedEntityNetworkObjectId.Value != 0 || _isMountingInProgress)
                return false;

            var saveableEntity = spawnedFirework.OrNull()?.GetComponent<SaveableEntity>();
            if (saveableEntity == null)
                return false;

            RestoreMountedEntity(saveableEntity);
            return _mountedEntityNetworkObjectId.Value != 0;
        }

        /// <summary>
        /// World position of the tube mouth (the seating trigger) - lets tools snap a held firework
        /// onto an empty tube, the same way the SpawnTool snaps shells to a MortarTubeTop.
        /// </summary>
        public Vector3 SnapPointWorldPosition
        {
            get
            {
                if (_triggerCollider == null)
                    _triggerCollider = GetComponents<Collider>().FirstOrDefault(x => x.isTrigger);

                return _triggerCollider != null ? _triggerCollider.bounds.center : _mountPoseTransform.position;
            }
        }
    }

    [Serializable]
    public struct FireworkMountPointSaveData
    {
        public string MountedEntityInstanceId;
    }
}
