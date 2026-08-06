using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Netcode;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/Fuse")]
    public class Fuse : NetworkBehaviour, IFuse, IIgnitable, IHaveFuseConnectionPoint
    {
        [Header("General")]
        [Range(0, 50)]
        [SerializeField]
        private float _fuseTime            = 4f;
        private float _remainingFuseTime;
        [Tooltip("Amount of IgnitionForce that is needed before the fuse ignites")]
        [SerializeField]
        private float _ignitionThreshold = 50f;
        private float _initialIgnitionThreshold;

        [SerializeField]
        [FormerlySerializedAs("_ignitePosition")]
        private FuseConnectionPoint _fuseConnectionPoint;

        [SerializeField]
        private ParticleSystem _particleSystem;

        [Header("Sound")]
        [GameSound]
        [SerializeField]
        [FormerlySerializedAs("FuseIgnitedSound")]
        private string _fuseIgnitedSound;


        [Header("Events")]
        [SerializeField]
        private UnityEvent _onFuseIgnited;
        [SerializeField]
        private UnityEvent _onFuseCompleted;
        public event Action OnFuseCompleted;
        public event Action OnFuseIgnited;

        private CancellationToken _cancellationToken;
        private CancellationTokenSource _effectDrainCancellationTokenSource;
        private MeshRenderer[] _enabledMeshRenderers;
        private Collider[] _enabledColliders;

        private readonly NetworkVariable<bool> _isIgnited              = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isUsed                 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _clientRequestForIgnitionSend = false;

        private void Awake()
        {
            Preconditions.CheckNotNull(_fuseConnectionPoint, this);
            Preconditions.CheckNotNull(_particleSystem, this);

            _fuseConnectionPoint.Setup(this);
            _enabledMeshRenderers                         = this.GetComponentsInChildren<MeshRenderer>(false);
            _enabledColliders                             = this.GetComponentsInChildren<Collider>(false);
            _remainingFuseTime                            = this._fuseTime;
            _cancellationToken                            = this.gameObject.GetCancellationTokenOnDestroy();
            _initialIgnitionThreshold                     = this._ignitionThreshold;

            //Fuse models never cast shadows: the thin rope's shadow is invisible, but every caster
            //is still drawn into all shadow cascades. Enforced in code so modded fuses are covered too.
            foreach (var renderer in _enabledMeshRenderers)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            SetEmissionOnParticleSystems(false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isIgnited.OnValueChanged += (prevValue, newValue) =>
            {
                SetEmissionOnParticleSystems(newValue);
            };

            _isUsed.OnValueChanged += (prevValue, newValue) =>
            {
                SetMeshAndColliders(!newValue);
                _fuseConnectionPoint.ForceRefresh();
            };

            SetEmissionOnParticleSystems(_isIgnited.Value);
            SetMeshAndColliders(!_isUsed.Value);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_fuseConnectionPoint == null || _fuseConnectionPoint.Equals(null))
            {
                Debug.LogError($"Missing Fuse Connection Point on '{typeof(Fuse)}' on gameobject '{this.gameObject.name}'", this);
                return;
            }
                
            if(_particleSystem == null)
            {
                Debug.LogError($"Missing ParticleSystem on '{typeof(Fuse)}' on gameobject '{this.gameObject.name}'", this);
                return;
            }

            if(String.IsNullOrEmpty(_fuseIgnitedSound))
            {
                Debug.LogError($"Missing Fuse Ignited Sound on '{typeof(Fuse)}' on gameobject '{this.gameObject.name}'", this);
            }
        }

        private void OnDisable()
        {
            if (IsIgnited && IsUsed == false)
                Extinguish();
        }

        public override void OnDestroy()
        {
            CancelPendingEffectDeactivation();
            base.OnDestroy();
        }

        public void IgniteWithoutFuseTime()
        {
            _remainingFuseTime = 0f;
            IgniteInstant();
        }

        public void IgniteInstant()
        {
            InternalIgnite(0f, true);
        }

        public void Ignite(float ignitionForce)
        {
            InternalIgnite(ignitionForce, false);
        }

        private void InternalIgnite(float ignitionForce, bool instantIgnite)
        {
            if (_clientRequestForIgnitionSend || _isIgnited.Value)
                return;

            //Note: The RPC below requires the NetworkObject to be spawned, which it isn't for the physics
            //frame a blueprint load leaves between Instantiate and NetworkObject.Spawn. Fire or a torch can
            //reach the firework in that window. Nothing is consumed here, so a later ignition still works (#2245)
            if (IsSpawned == false)
                return;

            if(SaveableEntityOwner != null)
                SaveableEntityOwner.SetIsValidForSaving(false);
            
            if (_cancellationToken.IsCancellationRequested)
                return;

            if (instantIgnite)
            {
                _ignitionThreshold = 0f;
            }
            else
            {
                _ignitionThreshold -= ignitionForce;
            }

            if (_ignitionThreshold > 0f)
                return;

            if (_remainingFuseTime > 0f)
                CalculateRemainingFuseTime(ignitionForce);

            _clientRequestForIgnitionSend = true;

            IgniteOnServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void IgniteOnServerRpc()
        {
            if (IsServer == false)
            {
                Debug.LogError("Unable to call RequestIgniteServerOnly if not IsServer");
                return;
            }

            if (_isIgnited.Value == false && _isUsed.Value == false)
            {
                _isIgnited.Value = true;
                IgniteAsync(_cancellationToken).Forget();
            }
        }

        //Todo: Could we maybe have a different method only about visuals or something as this seems to only be used for mortarfuses
        internal void MarkAsUsed()
        {
            _isUsed.Value = true;
        }

        internal void ResetFuse()
        {
            _remainingFuseTime            = _fuseTime;
            _ignitionThreshold            = _initialIgnitionThreshold;
            _clientRequestForIgnitionSend = false;
            SetEmissionOnParticleSystems(false);

            if(SaveableEntityOwner.OrNull() != null)
                SaveableEntityOwner.SetIsValidForSaving(true);

            if (!IsServer)
                return;

            _isIgnited.Value             = false;
            _isUsed.Value                = false;
            _fuseConnectionPoint.ForceRefresh();
        }

        private void CalculateRemainingFuseTime(float ignitionForce)
        {
            _remainingFuseTime = Mathf.Clamp(_remainingFuseTime - ignitionForce * Time.deltaTime, 0f, _fuseTime); 
        }

        private void Extinguish()
        {
            if (IsServer)
            {
                _isIgnited.Value = false;
            }

            if(IsUsed == false && _remainingFuseTime > 0f)
                SaveableEntityOwner.SetIsValidForSaving(true);
        }

        private async UniTask IgniteAsync(CancellationToken token)
        {
            if (!IsServer)
            {
                Debug.Log("Fuse IgniteAsync skipped as this is not the server");
                return;
            }

            OnFuseIgnited?.Invoke();
            _onFuseIgnited?.Invoke();
            OnFuseIgnitedClientRpc();

            if(_remainingFuseTime > 0f)
            {
                await UniTask.WaitWhile(() => 
                {
                    _remainingFuseTime -= Time.deltaTime;
                    return _remainingFuseTime > 0f;
                }, cancellationToken: token);
            }

            _isUsed.Value = true;

            OnFuseCompleted?.Invoke();
            _onFuseCompleted?.Invoke();
            OnFuseCompletedClientRpc();
            
            Extinguish();            
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void OnFuseCompletedClientRpc()
        {
            SetEmissionOnParticleSystems(false);

            if (IsServer)
                return;
            
            OnFuseCompleted?.Invoke();
            _onFuseCompleted?.Invoke();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void OnFuseIgnitedClientRpc()
        {
            if (IsServer)
                return;
            
            OnFuseIgnited?.Invoke();
            _onFuseIgnited?.Invoke();
        }

        private void SetMeshAndColliders(bool enable)
        {
            if (_enabledMeshRenderers != null)
                foreach (var renderer in _enabledMeshRenderers)
                    if (renderer.OrNull() != null)
                        renderer.enabled = enable;
            if (_enabledColliders != null)
                foreach (var collider in _enabledColliders)
                    if (collider.OrNull() != null)
                        collider.enabled = enable;
        }

        private void SetEmissionOnParticleSystems(bool enableEmission)
        {
            CancelPendingEffectDeactivation();

            if (enableEmission)
            {
                if (CanToggleEffectGameObject)
                    _particleSystem.gameObject.SetActive(true);

                _particleSystem.Play(true);
                Messenger.Broadcast(new MessengerEventPlaySoundStruct(_fuseIgnitedSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));
            }
            else
            {
                _particleSystem.Stop();
                Messenger.Broadcast(new MessengerEventStopSoundStruct(_fuseIgnitedSound, this.transform));
                DeactivateEffectWhenDrained();
            }
        }

        //Stop() leaves every particle system in the effect on Unity's update list for the lifetime
        //of the object - only a deactivated GameObject stops ticking (#2277)
        private void DeactivateEffectWhenDrained()
        {
            if (CanToggleEffectGameObject == false)
                return;

            if (_particleSystem.IsAlive(true))
            {
                _effectDrainCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                DeactivateEffectWhenDrainedAsync(_particleSystem, _effectDrainCancellationTokenSource.Token).Forget();
            }
            else
                _particleSystem.gameObject.SetActive(false);
        }

        private async UniTask DeactivateEffectWhenDrainedAsync(ParticleSystem effect, CancellationToken token)
        {
            //Deactivating while particles are alive would make them visibly pop out of existence
            await UniTask.WaitWhile(() => effect != null && (effect.IsAlive(true) || effect.isPlaying), cancellationToken: token);

            if (effect != null)
                effect.gameObject.SetActive(false);
        }

        private void CancelPendingEffectDeactivation()
        {
            if (_effectDrainCancellationTokenSource == null)
                return;

            _effectDrainCancellationTokenSource.Cancel();
            _effectDrainCancellationTokenSource.Dispose();
            _effectDrainCancellationTokenSource = null;
        }

        internal void ReplaceEffect(ParticleSystem newEffect, string igniteSound = null)
        {
            if (_particleSystem != newEffect)
            {
                CancelPendingEffectDeactivation();

                if (_particleSystem != null)
                {
                    //Destroying only the ParticleSystem component would orphan its GameObject and
                    //children as forever-ticking particle systems nothing references anymore (#2279)
                    if (CanToggleEffectGameObject)
                        GameObject.Destroy(_particleSystem.gameObject);
                    else
                        GameObject.Destroy(_particleSystem);
                }

                _particleSystem = newEffect;
            }

            if (igniteSound != null)
                _fuseIgnitedSound = igniteSound;
        }

        //Deactivating or destroying the effect GameObject is only safe when the fuse itself doesn't
        //sit on or under it - deactivating the fuse's own GameObject would extinguish it via OnDisable
        private bool CanToggleEffectGameObject => _particleSystem != null && this.transform.IsChildOf(_particleSystem.transform) == false;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            FireworksMania.Core.Utilities.GizmosUtility.DrawArrow(this.transform.position, this.transform.up, Color.yellow, 0.1f, 0.05f);
        }
#endif

        public bool IsIgnited => _isIgnited.Value;

        public FuseNetworkIdentifier FuseNetworkIdentifier => new()
        {
            FuseNetworkObjectId   = this.NetworkObjectId,
            FuseNetworkBehaviorId = this.NetworkBehaviourId,
            FuseIndex             = this.Index
        };

        public bool IsUsed    => _isUsed.Value;
        public SaveableEntity SaveableEntityOwner   { get; set; }
        public Transform Transform => this.transform;

        public Transform IgnitePositionTransform    => _fuseConnectionPoint.Transform;
        public IFuseConnectionPoint ConnectionPoint => _fuseConnectionPoint;
        public bool Enabled                         => IsUsed == false && this.enabled;
        /// <summary>
        /// Index of this particular fuse if its on an SaveableEntity that contains multiple fuses. Defaults to 0.
        /// </summary>
        public int Index { get; set; }              = 0;

        public float FuseTime
        {
            get => _fuseTime;
            set => _fuseTime = value;
        }

        public ParticleSystem Effect => _particleSystem;
        public string IgniteSound => _fuseIgnitedSound;
    }
}