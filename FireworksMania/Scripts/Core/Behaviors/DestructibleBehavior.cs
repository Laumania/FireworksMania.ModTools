using Cysharp.Threading.Tasks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public class DestructibleBehavior : NetworkBehaviour, IDestructible
    {
        [Header("General")]
        [SerializeField]
        private float _totalHitPoints = 0;
        [SerializeField]
        private float _currentHitPoints = 0;

        [SerializeField]
        [Tooltip("Only damage bigger than this value are actually applied.")]
        private float _ignoreDamageUnder = 0f;
        
        [SerializeField]
        [Tooltip("This prefab will be spawned and replace this gameobject when the CurrentHitPoints reach 0. If no prefab is specified, this gameobject will just be destroyed.")]
        private GameObject _destroyedPrefab;

        [Header("Optional")]
        [SerializeField]
        [Tooltip("This optional transform will be used to position/rotate the destroyed prefab instance.")]
        private Transform _destroyedPrefabSpawnLocation;
        [SerializeField]
        [Tooltip("This optional delays when the original/current (this gameobject this component is one) is destroyed. This can be useful to perfectly time with an effect and sound.")]
        private float _delayInSecondsUntilOriginalGameObjectIsDetroyed = 0f;

        //After this many waited frames the slow-frame gate is ignored, so wreckage cannot be postponed
        //forever on a machine that stays above the slow-frame threshold for a long stretch
        private const int MaxSlowFramePostponeFrames = 30;
        //Bounds how many explosions hitting this object while its debris swap is pending are remembered
        private const int MaxPendingExplosionSources = 5;

        private int _debriLayerInt = -1;
        private List<ExplosionDamageSource> _pendingExplosionSources;
        private bool _hasSpawnedDebris = false;

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private ParticleSystem[] _particleSystems;

        private void Awake()
        {
            _debriLayerInt    = LayerMask.NameToLayer("DestroyItDebris");
            _currentHitPoints = _totalHitPoints;

            // Optional: auto-fill if not set
            if (_renderers == null || _renderers.Length == 0) _renderers = GetComponentsInChildren<Renderer>(true);
            if (_colliders == null || _colliders.Length == 0) _colliders = GetComponentsInChildren<Collider>(true);
            if (_particleSystems == null || _particleSystems.Length == 0) _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void OnNetworkDespawn()
        {
            // This runs on server + clients when the object despawns
            foreach (var r in _renderers) if (r) r.enabled = false;
            foreach (var c in _colliders) if (c) c.enabled = false;
            foreach (var p in _particleSystems) if (p) p.Stop();

            // If you want it fully gone from the hierarchy interactions:
            gameObject.SetActive(false);
        }

        public void ApplyDamage(float damage)
        {
            ApplyDamageInternal(damage, null);
        }

        public void ApplyDamage(float damage, in ExplosionDamageSource explosionSource)
        {
            ApplyDamageInternal(damage, explosionSource);
        }

        private void ApplyDamageInternal(float damage, ExplosionDamageSource? explosionSource)
        {
            if(NetworkManager.Singleton.IsServer && CoreSettings.EnableDestruction && damage > _ignoreDamageUnder)
            {
                if (IsDestroyed)
                {
                    //The debris swap is staggered over frames, so explosions hitting this object while the swap
                    //is still pending are remembered and applied to the debris when it spawns - before the
                    //staggering, the debris already existed at this point and later blasts pushed it directly
                    if (explosionSource.HasValue && _hasSpawnedDebris == false)
                        RememberPendingExplosionSource(explosionSource.Value);
                    return;
                }

                _currentHitPoints -= damage;

                if(_currentHitPoints <= 0)
                {
                    if (explosionSource.HasValue)
                        RememberPendingExplosionSource(explosionSource.Value);
                    DestroyInternally();
                }
            }
        }

        private void RememberPendingExplosionSource(ExplosionDamageSource explosionSource)
        {
            if (_destroyedPrefab.OrNull() == null)
                return;

            if (_pendingExplosionSources == null)
                _pendingExplosionSources = new List<ExplosionDamageSource>(1);

            if (_pendingExplosionSources.Count < MaxPendingExplosionSources)
                _pendingExplosionSources.Add(explosionSource);
        }

#if UNITY_EDITOR

        //private void OnValidate()
        //{
        //    UnityEditor.EditorApplication.delayCall += () =>
        //    {
        //        if (this != null)
        //        {
        //            if (PrefabUtility.GetPrefabAssetType(this.gameObject) == PrefabAssetType.Regular && PrefabUtility.IsOutermostPrefabInstanceRoot(this.gameObject))
        //            {
        //                var sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject) as GameObject;
        //                if (sourcePrefab.OrNull() != null)
        //                {
        //                    var sourcePrefabDestructibleComponent = sourcePrefab.GetComponent<DestructibleBehavior>();
        //                    var destroyIdDestructibleComponent    = sourcePrefab.GetComponent<DestroyIt.Destructible>();

        //                    if (destroyIdDestructibleComponent.OrNull() != null)
        //                    {
        //                        Debug.Log($"[{sourcePrefab.name}] Found prefab and will try and make multiplayer and add DestructibleBehavior", sourcePrefab);
        //                        sourcePrefabDestructibleComponent.TotalHitPoints   = destroyIdDestructibleComponent.TotalHitPoints;
        //                        sourcePrefabDestructibleComponent.CurrentHitPoints = destroyIdDestructibleComponent.CurrentHitPoints;
        //                        sourcePrefabDestructibleComponent.Prefab           = destroyIdDestructibleComponent.destroyedPrefab;

        //                        DestroyImmediate(destroyIdDestructibleComponent, true);

        //                        var networkObject = sourcePrefab.GetComponent<NetworkObject>();
        //                        if (networkObject.OrNull() == null)
        //                        {
        //                            networkObject = sourcePrefab.AddComponent<NetworkObject>();

        //                            var clientNetworkTransform = sourcePrefab.AddComponent<ClientNetworkTransform>();
        //                            clientNetworkTransform.SyncScaleX = false;
        //                            clientNetworkTransform.SyncScaleY = false;
        //                            clientNetworkTransform.SyncScaleZ = false;

        //                            var rigidBody = sourcePrefab.GetComponent<Rigidbody>();
        //                            if (rigidBody.OrNull() != null)
        //                            {
        //                                sourcePrefab.AddComponent<ClientNetworkRigidbody>();
        //                            }
        //                        }

        //                        EditorUtility.SetDirty(sourcePrefab);
        //                    }
        //                }
        //                else
        //                    Debug.LogWarning($"Unable to find Prefab for {this.gameObject.name}");
        //            }
        //        }
        //    };
        //}

#endif

        private void DestroyInternally()
        {
            IsDestroyed = true;

            if (_destroyedPrefab.OrNull() == null)
            {
                //Nothing expensive to spawn - despawn at end of frame exactly as before the staggering
                //was added, without competing for the debris spawn budget
                StartCoroutine(DestroyDelayed());
                return;
            }

            DestroyStaggeredAsync().Forget();
        }

        private async UniTaskVoid DestroyStaggeredAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();

            //Never pay the debris swap cost in the frame that caused the destruction, and only allow a
            //few swaps globally per frame, so a big explosion destroying many objects at once cannot
            //spawn every debris prefab in a single frame (https://github.com/Laumania/FireworksMania/issues/2220).
            //The slow-frame gate mirrors the ignition queue in FireworksManager, but is bounded so wreckage
            //cannot be postponed forever on a machine having a long stretch of slow frames.
            var waitedFrames = 0;
            do
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                waitedFrames++;
            }
            while ((waitedFrames < MaxSlowFramePostponeFrames && DestructionSpawnBudget.IsCurrentFrameSlow)
                   || DestructionSpawnBudget.TryConsume(Time.frameCount) == false);

            if (this.IsSpawned == false)
                return;

            //Re-checked here as the host can turn destruction off while the swap was pending -
            //in that case the destroyed original still despawns, it just spawns no wreckage
            if (CoreSettings.EnableDestruction)
            {
                var spawnLocationTransform = _destroyedPrefabSpawnLocation != null ? _destroyedPrefabSpawnLocation : this.transform;
                var spawnedNetworkObject = DependencyResolver.Instance.Get<IDestructionObjectPool>().GetNetworkObject(_destroyedPrefab, spawnLocationTransform.position, spawnLocationTransform.rotation);
                spawnedNetworkObject.gameObject.SetLayersRecursively(_debriLayerInt);
                spawnedNetworkObject.Spawn(true);
                _hasSpawnedDebris = true;

                // Keep NetworkTransform in sync (no interpolation)
                var nt = spawnedNetworkObject.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (nt.OrNull() != null && nt.HasAuthority)
                    nt.Teleport(spawnLocationTransform.position, spawnLocationTransform.rotation, spawnLocationTransform.localScale);

                ApplyPendingExplosionForcesToDebris(spawnedNetworkObject);

                DisableCollidersRpc();
            }

            StartCoroutine(DestroyDelayed());
        }

        private void ApplyPendingExplosionForcesToDebris(NetworkObject debrisNetworkObject)
        {
            if (_pendingExplosionSources == null || _pendingExplosionSources.Count == 0)
                return;

            //The explosions that hit this object could not fling the debris themselves, as the debris
            //did not exist yet in the frames they happened - so their forces are applied here instead,
            //going through the same queued explosion force path in FireworksManager as everything else
            foreach (var debrisRigidbody in debrisNetworkObject.GetComponentsInChildren<Rigidbody>())
            {
                foreach (var explosion in _pendingExplosionSources)
                {
                    var rangeMultiplier = ExplosionPhysicsForceEffect.CalculateRangeMultiplier(explosion.Position, debrisRigidbody.ClosestPointOnBounds(explosion.Position), explosion.Range);
                    var massMultiplier  = ExplosionPhysicsForceEffect.CalculateMassMultiplier(debrisRigidbody.mass, explosion.ExplosionForce, explosion.ApplyForceRelativeToMass);

                    Messenger.Broadcast(new MessengerEventApplyExplosionForceStruct(debrisRigidbody, (explosion.ExplosionForce * massMultiplier) * rangeMultiplier, explosion.Position, explosion.Range, explosion.UpwardsModifier, explosion.ForceMode));
                }
            }
        }

        private IEnumerator DestroyDelayed()
        {
            yield return new WaitForEndOfFrame();
            
            if( _delayInSecondsUntilOriginalGameObjectIsDetroyed > 0f)
                yield return new WaitForSeconds(_delayInSecondsUntilOriginalGameObjectIsDetroyed);

            //this.gameObject.DestroyOrDespawn();
            this.NetworkObject.Despawn(false);
        }

        [Rpc(SendTo.Everyone)]
        private void DisableCollidersRpc()
        {
            foreach (var c in _colliders) if (c) c.enabled = false;
        }

        public float TotalHitPoints
        {
            get { return _totalHitPoints; }
            set { _totalHitPoints = value; }
        }

        public float CurrentHitPoints
        {
            get { return _currentHitPoints; }
            set { _currentHitPoints = value; }
        }

        public GameObject Prefab
        {
            get { return _destroyedPrefab; }
            set { _destroyedPrefab = value; }
        }

        public GameObject DestroyedPrefab => _destroyedPrefab;
        public bool IsDestroyed { get; private set; }
    }
}