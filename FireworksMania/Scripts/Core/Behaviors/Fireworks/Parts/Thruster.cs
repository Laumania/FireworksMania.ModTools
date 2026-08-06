using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/Thruster")]
    public class Thruster : NetworkBehaviour
    {
        [Header("General")]
        [SerializeField]
        private float _thrustForcePerSecond = 2500.0f;
        [SerializeField]
        private float _thrustTime = 3f;
        [SerializeField]
        private AnimationCurve _thrustEffectCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField]
        private ForceMode _thrustForceMode = ForceMode.Force;
        [SerializeField]
        private ParticleSystem _effect;

        [SerializeField]
        [Tooltip("If false, force will be applied in the up direction of the truster on the entire rigidbody. If true the force will be applied at the specific position")]
        private bool _thrustAtPosition = false;

        //[Space]
        //[Header("Rotation Alignment")]
        //[SerializeField]
        //[Tooltip("If true, applies torque to align the rigidbody's up direction with its velocity while thrusting.")]
        private bool _alignRotationDuringThrust = true;
        //[SerializeField]
        //[Tooltip("Strength of the torque used to align rotation. Higher values rotate faster.")]
        private float _rotationAlignStrength = 10f;
        //[SerializeField]
        //[Tooltip("Angular damping applied while thrusting to stabilize rotation.")]
        private float _angularDampingStrength = 0.2f;
        //[SerializeField]
        //[Tooltip("Minimum velocity magnitude required before attempting alignment.")]
        private float _minAlignmentVelocity = 0.5f;

        [Space]
        [Header("Sound")]
        [GameSound]
        [SerializeField]
        private string _thrustSound;

        private float _curveDeltaTime = 0.0f;
        private float _remainingThrustTime;
        private Transform _thrusterTransform;
        private Rigidbody _rigidbody;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _effectDrainCancellationTokenSource;

        private NetworkVariable<bool> _isThrusting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            if (_effect == null)
                Debug.LogError("Missing at least one particle system on Thruster", this);

            _thrusterTransform = this.transform;
            _remainingThrustTime = _thrustTime * Random.Range(0.9f, 1.1f);
            _cancellationToken = this.gameObject.GetCancellationTokenOnDestroy();
            SetEmissionOnParticleSystems(false);
        }

        private void Start()
        {
            SetEmissionOnParticleSystems(false);
            this.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isThrusting.OnValueChanged += (prevValue, newValue) =>
            {
                if (newValue == true)
                {
                    Messenger.Broadcast(new MessengerEventPlaySoundStruct(_thrustSound, _thrusterTransform, delayBasedOnDistanceToListener: false, followTransform: true));
                    SetEmissionOnParticleSystems(true);
                }
                else
                {
                    Messenger.Broadcast(new MessengerEventStopSoundStruct(_thrustSound, _thrusterTransform));
                    SetEmissionOnParticleSystems(false);
                }
            };
        }

        public void Setup(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        private void FixedUpdate()
        {
            if (!IsServer)
                return;

            if (_isThrusting.Value)
            {
                _remainingThrustTime -= Time.deltaTime;

                if (_remainingThrustTime <= 0f)
                {
                    TurnOff();
                    return;
                }

                _curveDeltaTime += Time.fixedDeltaTime;

                //ForceMode.Force and ForceMode.Acceleration are already integrated over the fixed
                //timestep by Unity, so scaling by the live Time.fixedDeltaTime here applied it twice
                //and made rockets fly higher whenever PerformanceManager throttled physics (issue #2233)
                var thrustTimestep = PhysicsForceUtility.GetForceTimestep(_thrustForceMode, Time.fixedDeltaTime);

                var thrust = _thrusterTransform.up * _thrustForcePerSecond * _thrustEffectCurve.Evaluate(_curveDeltaTime) * thrustTimestep;

                if (_thrustAtPosition)
                    _rigidbody.AddForceAtPosition(thrust, _thrusterTransform.position, _thrustForceMode);
                else
                    _rigidbody.AddForce(thrust, _thrustForceMode);

                // Apply rotation torque to align up direction with current velocity during thrust
                if (_alignRotationDuringThrust && _rigidbody != null)
                {
                    var velocity = _rigidbody.linearVelocity;
                    var speed = velocity.magnitude;

                    if (speed >= _minAlignmentVelocity)
                    {
                        var desiredUp = velocity.normalized;
                        var currentUp = _thrusterTransform.up;

                        // Axis and magnitude to rotate currentUp towards desiredUp
                        var axis = Vector3.Cross(currentUp, desiredUp);
                        var angle = Vector3.SignedAngle(currentUp, desiredUp, axis == Vector3.zero ? _thrusterTransform.forward : axis);

                        // Torque proportional to angle and strength
                        var torque = axis.normalized * angle * Mathf.Deg2Rad * _rotationAlignStrength;
                        _rigidbody.AddTorque(torque, ForceMode.Acceleration);

                        // Light angular damping to stabilize
                        if (_angularDampingStrength > 0f)
                        {
                            var damping = -_rigidbody.angularVelocity * _angularDampingStrength;
                            _rigidbody.AddTorque(damping, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }

        public void TurnOn()
        {
            if (!IsServer)
                return;

            if (_rigidbody == null)
            {
                Debug.LogError("Missing Rigidbody to apply thrust too! Did you forget to call Setup()?", this);
                return;
            }

            this.enabled = true;
            _isThrusting.Value = true;
        }

        public void TurnOff()
        {
            if (!IsServer)
                return;

            if (_isThrusting.Value)
            {
                _isThrusting.Value = false;
            }

            this.enabled = false;
        }

        private void SetEmissionOnParticleSystems(bool enableEmission)
        {
            CancelPendingEffectDeactivation();

            if (enableEmission)
            {
                if (CanToggleEffectGameObject)
                    _effect.gameObject.SetActive(true);

                _effect.Play();
            }
            else
            {
                _effect.Stop();
                DeactivateEffectWhenDrained();
            }
        }

        //Stop() leaves every particle system in the effect on Unity's update list for the lifetime
        //of the object - only a deactivated GameObject stops ticking (#2283)
        private void DeactivateEffectWhenDrained()
        {
            if (CanToggleEffectGameObject == false)
                return;

            if (_effect.IsAlive(true))
            {
                _effectDrainCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                DeactivateEffectWhenDrainedAsync(_effect, _effectDrainCancellationTokenSource.Token).Forget();
            }
            else
                _effect.gameObject.SetActive(false);
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

        //Deactivating the effect GameObject is only safe when the Thruster itself doesn't sit on or
        //under it - deactivating the Thruster's own GameObject would stop thrust and network sync
        private bool CanToggleEffectGameObject => _effect != null && this.transform.IsChildOf(_effect.transform) == false;

        public override void OnDestroy()
        {
            CancelPendingEffectDeactivation();
            TurnOff();
            base.OnDestroy();
        }

        public bool IsThrusting => _isThrusting.Value;
    }
}
