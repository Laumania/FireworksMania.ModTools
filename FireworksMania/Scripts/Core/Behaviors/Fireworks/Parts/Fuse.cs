using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/Fuse")]
    public class Fuse : NetworkBehaviour, IIgnitable, IHaveFuseConnectionPoint
    {
        [Header("General")]
        [Range(0, 50)]
        [SerializeField]
        private float _fuseTime            = 4f;
        private float _remainingFuseTime;
        [Tooltip("Amount of IgnitionForce that is needed before the fuse ignites")]
        [SerializeField]
        private float _ignitionThreshold = 50f;
        private float _initialIgnitionThredhold;

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

        private CancellationToken _token;
        private MeshRenderer[] _meshRenderer;
        private Collider[] _colliders;

        private readonly NetworkVariable<bool> _isIgnited              = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isUsed                 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isIgnitionVisualsShown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _clientRequestForIgnitionSend = false;

        private void Awake()
        {
            Preconditions.CheckNotNull(_fuseConnectionPoint);
            Preconditions.CheckNotNull(_particleSystem);

            _fuseConnectionPoint.Setup(this);
            _meshRenderer             = this.GetComponentsInChildren<MeshRenderer>();
            _colliders                = this.GetComponentsInChildren<Collider>();
            _remainingFuseTime        = _fuseTime;
            _token                    = this.gameObject.GetCancellationTokenOnDestroy();
            _initialIgnitionThredhold = _ignitionThreshold;

            SetEmissionOnParticleSystems(false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isIgnitionVisualsShown.OnValueChanged += (prevValue, newValue) =>
            {
                SetEmissionOnParticleSystems(newValue);
            };

            _isUsed.OnValueChanged += (prevValue, newValue) =>
            {
                _fuseConnectionPoint.ForceRefresh();
            };

            SetEmissionOnParticleSystems(_isIgnitionVisualsShown.Value);
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

            if(SaveableEntityOwner != null)
                SaveableEntityOwner.SetIsValidForSaving(false);
            
            if (_token.IsCancellationRequested)
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

            if (IsServer)
                RequestIgniteServerOnly();
            else
                RequestIgniteServerRpc();   
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestIgniteServerRpc()
        {
            RequestIgniteServerOnly();
        }

        private void RequestIgniteServerOnly()
        {
            if (IsServer == false)
            {
                Debug.LogError("Unable to call RequestIgniteServerOnly if not IsServer");
                return;
            }

            if (_isIgnited.Value == false && _isUsed.Value == false)
            {
                _isIgnited.Value = true;

                if (_remainingFuseTime > 0f)
                    _isIgnitionVisualsShown.Value = true;

                IgniteAsync(_token).Forget();
            }
        }

        internal void MarkAsUsed()
        {
            _isUsed.Value = true;
        }

        public void ResetFuse()
        {
            _remainingFuseTime            = _fuseTime;
            _ignitionThreshold            = _initialIgnitionThredhold;
            _clientRequestForIgnitionSend = false;
            SetEmissionOnParticleSystems(false);

            if(SaveableEntityOwner.OrNull() != null)
                SaveableEntityOwner.SetIsValidForSaving(true);

            if (!IsServer)
                return;

            _isIgnited.Value             = false;
            _isIgnitionVisualsShown.Value= false;
            _isUsed.Value                = false;
            _fuseConnectionPoint.ForceRefresh();
        }

        private void CalculateRemainingFuseTime(float ignitionForce)
        {
            _remainingFuseTime = Mathf.Clamp(_remainingFuseTime - ignitionForce * Time.deltaTime, 0f, _fuseTime); 
        }

        public void Extinguish()
        {
            if (IsServer)
            {
                _isIgnitionVisualsShown.Value = false;
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

            if(_meshRenderer != null)
                foreach (var renderer in _meshRenderer)
                    renderer.enabled = false;

            if(_colliders != null)
                foreach (var collider in _colliders)    
                    collider.enabled = false;

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

        private void SetEmissionOnParticleSystems(bool enableEmission)
        {
            if (enableEmission)
            {
                _particleSystem.Play(true);
                Messenger.Broadcast(new MessengerEventPlaySound(_fuseIgnitedSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));
            }
            else
            {
                _particleSystem.Stop();
                Messenger.Broadcast(new MessengerEventStopSound(_fuseIgnitedSound, this.transform));
            }
        }

        internal void ReplaceEffect(ParticleSystem newEffect, string igniteSound = null)
        {
            if (_particleSystem != newEffect)
            {
                if(_particleSystem != null)
                    GameObject.Destroy(_particleSystem);

                _particleSystem = newEffect;
            }

            if (igniteSound != null)
                _fuseIgnitedSound = igniteSound;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            FireworksMania.Core.Utilities.GizmosUtility.DrawArrow(this.transform.position, this.transform.up, Color.yellow, 0.1f, 0.05f);
        }
#endif

        public bool IsIgnited => _isIgnited.Value;
        public bool IsUsed    => _isUsed.Value;
        public SaveableEntity SaveableEntityOwner   { get; set; }

        public Transform IgnitePositionTransform    => _fuseConnectionPoint.Transform;
        public IFuseConnectionPoint ConnectionPoint => _fuseConnectionPoint;
        public bool Enabled                         => IsUsed == false && this.enabled;
        /// <summary>
        /// Index of this particular fuse if its on an SaveableEntity that contains multiple fuses. Defaults to 0.
        /// </summary>
        public int Index { get; internal set; }              = 0;

        public float FuseTime
        {
            get => _fuseTime;
            set => _fuseTime = value;
        }

        internal ParticleSystem Effect => _particleSystem;
        internal string IgniteSound => _fuseIgnitedSound;
    }
}