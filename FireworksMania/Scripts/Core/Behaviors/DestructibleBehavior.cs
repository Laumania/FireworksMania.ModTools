using FireworksMania.Core.Common;
using FireworksMania.Core.Utilities;
using System.Collections;
using Unity.Netcode;
using UnityEditor;
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

        private int _debriLayerInt = -1;

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
            if(NetworkManager.Singleton.IsServer && CoreSettings.EnableDestruction && damage > _ignoreDamageUnder && IsDestroyed == false)
            {
                _currentHitPoints -= damage;

                if(_currentHitPoints <= 0)
                    DestroyInternally();
            }
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

            if (_destroyedPrefab.OrNull() != null)
            {
                var spawnLocationTransform = _destroyedPrefabSpawnLocation != null ? _destroyedPrefabSpawnLocation : this.transform;
                var spawnedNetworkObject = DependencyResolver.Instance.Get<IDestructionObjectPool>().GetNetworkObject(_destroyedPrefab, spawnLocationTransform.position, spawnLocationTransform.rotation);
                spawnedNetworkObject.gameObject.SetLayersRecursively(_debriLayerInt);
                spawnedNetworkObject.Spawn(true);

                // Keep NetworkTransform in sync (no interpolation)
                var nt = spawnedNetworkObject.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (nt.OrNull() != null && nt.HasAuthority)
                    nt.Teleport(spawnLocationTransform.position, spawnLocationTransform.rotation, spawnLocationTransform.localScale);

                DisableCollidersRpc();
            }
            
            StartCoroutine(DestroyDelayed());
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