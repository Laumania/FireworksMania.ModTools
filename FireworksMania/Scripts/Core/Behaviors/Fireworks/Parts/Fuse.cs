using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
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
        public Action OnFuseCompleted;
        public Action OnFuseIgnited;

        private CancellationToken _token;

        private readonly NetworkVariable<bool> _isIgnited              = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isUsed                 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _isIgnitionVisualsShown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _clientRequestForIgnitionSend = false;

        private void Awake()
        {
            if (_fuseConnectionPoint == null || _fuseConnectionPoint.Equals(null))
            {
                Debug.LogError("Missing IFuseConnectionPoint", this);
                return;
            }

            _fuseConnectionPoint.Setup(this);

            _remainingFuseTime = _fuseTime;
            _token             = this.gameObject.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            if (_particleSystem == null)
            {
                Debug.LogError("Missing at ParticleSystem", this);
            }

            SetEmissionOnParticleSystems(false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isIgnitionVisualsShown.OnValueChanged += (prevValue, newValue) =>
            {
                if(newValue == true)
                {
                    Messenger.Broadcast(new MessengerEventPlaySound(_fuseIgnitedSound, this.transform, delayBasedOnDistanceToListener: false, followTransform: true));
                    SetEmissionOnParticleSystems(true);
                }
                else
                {
                    Messenger.Broadcast(new MessengerEventStopSound(_fuseIgnitedSound, this.transform));
                    SetEmissionOnParticleSystems(false);
                }
            };

            SetEmissionOnParticleSystems(false);
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
            RequestIgniteServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestIgniteServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (_isIgnited.Value == false && _isUsed.Value == false)
            {
                _isIgnited.Value = true;
                
                if (_remainingFuseTime > 0f)
                    _isIgnitionVisualsShown.Value = true;

                IgniteAsync(_token).Forget();
            }
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

            OnFuseCompleted?.Invoke();
            _onFuseCompleted?.Invoke();
            OnFuseCompletedClientRpc();

            _isUsed.Value = true;
            Extinguish();
            //this.gameObject.SetActive(false);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void OnFuseCompletedClientRpc()
        {
            if (IsServer)
                return;
            
            OnFuseCompleted?.Invoke();
            _onFuseCompleted?.Invoke();
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
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
                _particleSystem.Play(true);
            else
                _particleSystem.Stop();
        }

        public bool IsIgnited => _isIgnited.Value;
        public bool IsUsed    => _isUsed.Value;
        public SaveableEntity SaveableEntityOwner   { get; set; }

        public Transform IgnitePositionTransform    => _fuseConnectionPoint.Transform;
        public IFuseConnectionPoint ConnectionPoint => _fuseConnectionPoint;

        public bool Enabled => IsUsed == false && this.enabled;
    }
}