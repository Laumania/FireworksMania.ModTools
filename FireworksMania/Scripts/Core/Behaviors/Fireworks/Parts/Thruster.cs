using FireworksMania.Core.Attributes;
using UnityEngine;
using Messenger = FireworksMania.Core.Messaging.Messenger;
using MessengerEventPlaySound = FireworksMania.Core.Messaging.MessengerEventPlaySound;
using MessengerEventStopSound = FireworksMania.Core.Messaging.MessengerEventStopSound;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/Thruster")]
    public class Thruster : MonoBehaviour
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

        [Space]
        [Header("Sound")]
        [GameSound]
        [SerializeField]
        private string _thrustSound;

    
        private float _curveDeltaTime = 0.0f;
        private float _remainingThrustTime;
        private bool _isThrusting = false;
        private Transform _thrusterTransform;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            if (_effect == null)
                Debug.LogError("Missing at least one particle system on Thruster", this);

            _thrusterTransform          = this.transform;
            _remainingThrustTime        = _thrustTime * Random.Range(0.9f, 1.1f);
            _isThrusting                = false;
            SetEmissionOnParticleSystems(false);
        }

        private void Start()
        {
            SetEmissionOnParticleSystems(false);
            this.enabled = false;
        }

        public void Setup(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        private void FixedUpdate()
        {
            if(_isThrusting)
            {
                _remainingThrustTime -= Time.deltaTime;

                if(_remainingThrustTime <= 0f)
                {
                    TurnOff();
                    return;
                }

                _curveDeltaTime += Time.fixedDeltaTime;
            
                if(_thrustAtPosition)
                    _rigidbody.AddForceAtPosition(_thrusterTransform.up * _thrustForcePerSecond * _thrustEffectCurve.Evaluate(_curveDeltaTime) * Time.fixedDeltaTime, _thrusterTransform.position, _thrustForceMode);
                else
                    _rigidbody.AddForce(_thrusterTransform.up * _thrustForcePerSecond * _thrustEffectCurve.Evaluate(_curveDeltaTime) * Time.fixedDeltaTime, _thrustForceMode);
            }
        }

        public void TurnOn()
        {
            if(_rigidbody == null)
            {
                Debug.LogError("Missing Rigidbody to apply thrust too! Did you forget to call Setup()?", this);
                return;
            }

            this.enabled = true;
            _isThrusting = true;
            Messenger.Broadcast(new MessengerEventPlaySound(_thrustSound, _thrusterTransform, delayBasedOnDistanceToListener: false, followTransform: true));
            SetEmissionOnParticleSystems(true);
        }

        public void TurnOff()
        {
            if(_isThrusting)
            {
                _isThrusting = false;
                Messenger.Broadcast(new MessengerEventStopSound(_thrustSound, _thrusterTransform));
                SetEmissionOnParticleSystems(false);
            }

            this.enabled = false;
        }

        private void SetEmissionOnParticleSystems(bool enableEmission)
        {
            if (enableEmission)
                _effect.Play();
            else
                _effect.Stop();
        }

        private void OnDestroy()
        {
            TurnOff();
        }

        public bool IsThrusting => _isThrusting;
    }
}
