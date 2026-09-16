using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Behaviors;
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

        //Serialization must stay unconditional: the exported MortarTubeFusePrefab carries this flag
        //into the Mod Tools project, where FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS is not defined -
        //only the inspector visibility is internal
        [SerializeField]
#if !FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
        [HideInInspector]
#endif
        [Tooltip("Internal: the game's mortar tube fuse gets its effect provided by code when a shell is loaded, so its Particle System is deliberately left empty and not flagged as an error")]
        private bool _effectProvidedAtRuntime = false;

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
        private IIgnitionCauserCarrier _causerCarrier;
        private bool                   _causerCarrierResolved;

        private void Awake()
        {
            Preconditions.CheckNotNull(_fuseConnectionPoint, this);
            if (_effectProvidedAtRuntime == false)
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
                
            if(_particleSystem == null && _effectProvidedAtRuntime == false)
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
            IgniteWithoutFuseTime(ExplosionDamageSource.NoCauser);
        }

        public void IgniteWithoutFuseTime(ulong causerClientId)
        {
            //Zeroing the field here only covers this machine, which is why the intent also travels with the
            //request below - see RequestIgniteRpc. It is still done locally because it is the only thing
            //that can cut short a burn that is already running: InternalIgnite returns early once ignited
            _remainingFuseTime = 0f;
            InternalIgnite(0f, instantIgnite: true, skipRemainingFuseTime: true, causerClientId);
        }

        public void IgniteInstant()
        {
            IgniteInstant(ExplosionDamageSource.NoCauser);
        }

        public void IgniteInstant(ulong causerClientId)
        {
            InternalIgnite(0f, instantIgnite: true, skipRemainingFuseTime: false, causerClientId);
        }

        public void Ignite(float ignitionForce)
        {
            Ignite(ignitionForce, ExplosionDamageSource.NoCauser);
        }

        public void Ignite(float ignitionForce, ulong causerClientId)
        {
            InternalIgnite(ignitionForce, instantIgnite: false, skipRemainingFuseTime: false, causerClientId);
        }

        private void InternalIgnite(float ignitionForce, bool instantIgnite, bool skipRemainingFuseTime, ulong causerClientId)
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

            RequestIgniteRpc(skipRemainingFuseTime, causerClientId);
        }

        [Rpc(SendTo.Server)]
        private void RequestIgniteRpc(bool skipRemainingFuseTime, ulong causerClientId)
        {
            if (IsServer == false)
            {
                Debug.LogError("Unable to call RequestIgniteServerOnly if not IsServer");
                return;
            }

            if (_isIgnited.Value == false && _isUsed.Value == false)
            {
                //Attribution: the first ignition names the player whose chain lit this firework. The
                //carrier sits on the behavior above this fuse (BaseFireworkBehavior / MortarTube), so
                //the explosion effects - parented under the same behavior - resolve the same value.
                StampCauserCarrier(causerClientId);

                //_remainingFuseTime is plain local state and the burn in IgniteAsync only runs on the server,
                //so a client zeroing its own copy in IgniteWithoutFuseTime never reached here and the firework
                //sat out its full fuse time instead of going off at once. That is what a fuse connection whose
                //spark happened to arrive on a client first looked like (#2351)
                if (skipRemainingFuseTime)
                    _remainingFuseTime = 0f;

                _isIgnited.Value = true;
                IgniteAsync(_cancellationToken).Forget();
            }
        }

        private void StampCauserCarrier(ulong causerClientId)
        {
            if (causerClientId == ExplosionDamageSource.NoCauser)
                return;

            if (_causerCarrierResolved == false)
            {
                _causerCarrierResolved = true;
                _causerCarrier         = GetComponentInParent<IIgnitionCauserCarrier>();
            }

            _causerCarrier?.TrySetIgnitionCauser(causerClientId);
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
            //Reached from OnDisable, which also runs when a firework is despawned while still burning.
            //Writing a NetworkVariable after despawn is the hazard in Docs/Development/netcode-gotchas.md,
            //so the write is skipped there - but the write was doing two jobs. Locally it fired
            //_isIgnited.OnValueChanged, which is what stops the burning effect and broadcasts the stop for
            //its looping ignited sound. Skipping the whole block would leave that sound playing, so the
            //local half is done directly instead.
            if (IsServer)
            {
                if (IsSpawned)
                    _isIgnited.Value = false;
                else
                    SetEmissionOnParticleSystems(false);
            }

            //Null-guarded like every other use of this field (see ResetFuse): the owner is assigned by
            //BaseFireworkBehavior or MortarTube, so a Fuse authored without either never gets one
            if(IsUsed == false && _remainingFuseTime > 0f)
                SaveableEntityOwner.OrNull()?.SetIsValidForSaving(true);
        }

        private async UniTask IgniteAsync(CancellationToken token)
        {
            if (!IsServer)
            {
                Debug.Log("Fuse IgniteAsync skipped as this is not the server");
                return;
            }

            OnFuseIgnited?.Invoke();
            //Guarded: this runs on the server before the client RPC and before the burn timer, so a
            //throwing listener would leave the firework unfired AND the clients never told.
            _onFuseIgnited.InvokeSafe(this, nameof(_onFuseIgnited));
            OnFuseIgnitedRpc();

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
            //Guarded: a throwing listener would skip the client RPC and Extinguish() below.
            _onFuseCompleted.InvokeSafe(this, nameof(_onFuseCompleted));
            OnFuseCompletedRpc();

            Extinguish();
        }

        //ClientsAndHost, not NotServer: a [ClientRpc] runs on the host too, which is what stops the
        //host's own fuse particles below - the IsServer guard after it is there because the server
        //raises the events itself at the call site.
        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
        private void OnFuseCompletedRpc()
        {
            SetEmissionOnParticleSystems(false);

            if (IsServer)
                return;
            
            OnFuseCompleted?.Invoke();
            _onFuseCompleted.InvokeSafe(this, nameof(_onFuseCompleted));
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
        private void OnFuseIgnitedRpc()
        {
            if (IsServer)
                return;
            
            OnFuseIgnited?.Invoke();
            _onFuseIgnited.InvokeSafe(this, nameof(_onFuseIgnited));
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
            //The effect can be absent until ReplaceEffect provides one - the mortar tube's internal
            //fuse starts without an effect (_effectProvidedAtRuntime) and gets one on shell load
            if (_particleSystem == null)
                return;

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