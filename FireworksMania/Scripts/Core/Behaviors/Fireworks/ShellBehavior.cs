using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Definitions.EntityDefinitions;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/ShellBehavior")]
    public class ShellBehavior : BaseFireworkBehavior, IHaveEntityDiameterDefinition
    {
        [SerializeField]
        private EntityDiameterDefinition _diameter;

        [SerializeField]
        private GameObject _model;
        [SerializeField]
        [Tooltip("Reference to the MeshRenderer of the actual shell model. Used to render shell loaded in the mortar.")]
        private MeshRenderer _modelMeshRenderer;

        [Header("Effect Settings")]
        [SerializeField]
        private float _groundLaunchForce = 10f;
        [SerializeField]        
        private ParticleSystem _effect;
        private ParticleSystem.MainModule _mainModule;

        [Header("Prefabs for launch from Mortar")]
        [Tooltip("Prefab of the launch effect used when shell is loaded into a mortartube")]
        [SerializeField]
        private ParticleSystem _launchEffectPrefab;

        [SerializeField]
        [Tooltip("Prefab of the unwrapped shell fuse used when shell is loaded into a mortartube")]
        //[SearchContext("t=UnwrappedShellFuse t=prefab", "asset", SearchViewFlags.GridView)] //Note: This sadly doesn't allow me to search in Packages which is needed in modders Unity as the Mod Tools is installed as a package - bummer!
        private GameObject _unwrappedShellFusePrefab;

        private Rigidbody _rigidbody;
        private Collider[] _colliders; //Todo: Could this be populated in Editor maybe - as it won't really change?

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = this.GetComponent<Rigidbody>();
            _colliders = _rigidbody.GetComponents<Collider>();

            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);

            _mainModule            = _effect.main;
            _mainModule.loop       = false;
            _mainModule.startSpeed = _groundLaunchForce;
        }

        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;

            DisableRigidBodyAndColliders();
            _model.SetActive(false);

            _effect.gameObject.SetActive(true);
            _effect.Play(true);
            
            await UniTask.WaitWhile(() => _effect.IsAlive(true) || _effect.isPlaying, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            await DestroyFireworkAsync(token);
            token.ThrowIfCancellationRequested();
        }
        
        private void DisableRigidBodyAndColliders()
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            foreach (var collider in _colliders)
            {
                collider.enabled = false;
            }
        }

        private MeshRenderer TryGetModelMeshRenderer()
        {
            if(_modelMeshRenderer != null)
                return _modelMeshRenderer;

            foreach (Transform modelChildTransform in _model.transform)
            {
                if(modelChildTransform == null)
                    continue;

                if (modelChildTransform.GetComponent<Fuse>() == null) //We don't want to find the fuse's MeshRenderer
                {
                    _modelMeshRenderer = modelChildTransform.GetComponentInChildren<MeshRenderer>();
                    if (_modelMeshRenderer != null)
                        return _modelMeshRenderer;
                }
            }

            return _modelMeshRenderer;
        }

        private UnwrappedShellFuse TryGetUnwrappedShellFuse()
        {
            return _unwrappedShellFusePrefab.GetComponent<UnwrappedShellFuse>();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    base.OnValidate();

                    if (_diameter == null)
                    {
                        Debug.LogError($"Missing {nameof(EntityDiameterDefinition)} on {this.gameObject.name}", this.gameObject);
                        return;
                    }

                    if (_unwrappedShellFusePrefab == null)
                    {
                        Debug.LogError($"Missing {nameof(UnwrappedShellFusePrefab)} on {this.gameObject.name}", this.gameObject);
                        return;
                    }

                    if (_unwrappedShellFusePrefab.GetComponent<UnwrappedShellFuse>() == null)
                    {
                        Debug.LogError($"Prefab referenced in {nameof(UnwrappedShellFusePrefab)} on {this.gameObject.name} does not seem to have the '{nameof(UnwrappedShellFuse)}' component on it - which is required", this.gameObject);
                        return;
                    }

                    if (_modelMeshRenderer == null)
                    {
                        _modelMeshRenderer = TryGetModelMeshRenderer();

                        if (_modelMeshRenderer != null)
                        {
                            Debug.Log($"Attempted to set {nameof(ModelMeshRenderer)} on {this.gameObject.name} automatically as none was specified. Set it to the GameObject '{_modelMeshRenderer.gameObject.name}'", this.gameObject);
                            UnityEditor.EditorUtility.SetDirty(this.gameObject);
                        }
                    }
                }
            };
        }
#endif

        public ParticleSystem LaunchEffectPrefab                              => _launchEffectPrefab;
        public ParticleSystem Effect                                          => _effect;
        public UnwrappedShellFuse UnwrappedShellFusePrefab                    => TryGetUnwrappedShellFuse();
        public EntityDiameterDefinition DiameterDefinition                    => _diameter;
        public MeshRenderer ModelMeshRenderer                                 => TryGetModelMeshRenderer();
        public override bool IsIgnited                                        => _fuse.IsIgnited;
        public float Recoil                                                   => _effect.main.startSpeed.Evaluate(0);
    }
}
