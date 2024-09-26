using FireworksMania.Core.Common;
using FireworksMania.Core.Utilities;
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

        private int _debriLayerInt = -1;

        private void Awake()
        {
            _debriLayerInt    = LayerMask.NameToLayer("DestroyItDebris");
            _currentHitPoints = _totalHitPoints;
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
                var spawnedNetworkObject = DependencyResolver.Instance.Get<IDestructionObjectPool>().GetNetworkObject(_destroyedPrefab, this.transform.position, this.transform.rotation);
                spawnedNetworkObject.gameObject.SetLayersRecursively(_debriLayerInt);
                spawnedNetworkObject.Spawn(true);
            }
            
            this.gameObject.DestroyOrDespawn();

            //if(this.NetworkObject.OrNull() != null && this.NetworkObject.IsSpawned)
            //    this.NetworkObject.Despawn();
            //else
            //    Destroy(this.gameObject);
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