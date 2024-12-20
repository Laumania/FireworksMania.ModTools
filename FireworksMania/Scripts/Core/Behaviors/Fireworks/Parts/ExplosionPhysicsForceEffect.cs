using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using FireworksMania.Core.Messaging;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionPhysicsForceEffect")]
    public class ExplosionPhysicsForceEffect : MonoBehaviour
    {
        private Transform _originTransform      = null;

        [Header("Explosion Effect")]
        [Tooltip("The range of where the explosion should have effect. Only gameobjects inside will be affected.")]
        [SerializeField]
        private float _range                    = 0.2f;

        [Tooltip("Adjustment to the apparent position of the explosion to make it seem to lift objects.")]
        [SerializeField]
        private float _upwardsmodifier          = 0.25f;

        [Tooltip("The method used to apply the force to its targets.")]
        [SerializeField]
        private ForceMode _forceMode            = ForceMode.Impulse;

        [Tooltip("The amount of explosion force applied to surrounding Rigidbodies")]
        [SerializeField]
        private float _explosionForce           = 100f;

        [Tooltip("If set to true, applies the force relative to the mass of the exploding rigidbodies.")]
        [SerializeField]
        private bool _applyForceRelativeToMass = true;

        [Tooltip("If set to true, also Rigidbodies with Kinematic = true, will be affected by the explosion physics force, and their Kinematic will be set to false.")]
        [SerializeField]
        private bool _ignoreKinematic           = true;

        [Tooltip("Rigidbodies to ignore when applying physics forces")]
        [SerializeField]
        private Rigidbody[] _ignoreRigidbodies;

        [Tooltip("Layers that should be affected by the explosion physics force effect.")]
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("_layers")]
        private LayerMask _affectedLayers               = 0;
        private LayerMask _debrisLayer;

        [Header("Ignitable Effect")]
        [Tooltip("Indicates if game objects with a 'Ignitable' component should be ignited.")]
        [SerializeField]
        private bool _igniteSurroundingIgnitables = true;

        [Header("Shake Effect")]
        [Tooltip("Indicates if player should be affected with camera shake if close enough to the explosion.")]
        [SerializeField]
        private bool _enableShakeEffect         = true;

        [Tooltip("Range multipler for shake effect, for game objects with the ShakeableTransform attached.")]
        [Range(0f, 100f)]
        [SerializeField]
        private float _shakeRangeMultipler      = 1f;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onApplyExplosionForce;


        private static Collider[] _nonAllocColliderArray = new Collider[2500];

        void Awake()
        {
            if (_originTransform == null)
                _originTransform = this.transform;

            _debrisLayer            = 1 << LayerMask.NameToLayer("DestroyItDebris");

            if (_debrisLayer == -1)
                Debug.LogError("Debris layer name not found!", this);

            _affectedLayers = LayerMask.GetMask("Default") | LayerMask.GetMask("Interactable") | LayerMask.GetMask("Player");
        }

        private void OnValidate()
        {
            if (_ignoreRigidbodies != null)
            {
                foreach (var rigidBodyToIgnore in _ignoreRigidbodies)
                {
                    if (rigidBodyToIgnore == null)
                        Debug.LogWarning($"'IgnoreRigidbodies' on component '{nameof(ExplosionPhysicsForceEffect)}' on gameobject '{this.gameObject.name}' have an entry that is null, please remove the item or drag in an actual rigidbody", this);
                }
            }
        }

        public void ApplyExplosionForce(bool applyPhysicsForce = true, bool applyShakeEffect = true, bool applyIgnition = true)
        {
            ApplyExplosionForce(transform.position, applyPhysicsForce, applyShakeEffect, applyIgnition);
        }

        public void ApplyExplosionForce(Vector3 position, bool applyPhysicsForce = true, bool applyShakeEffect = true, bool applyIgnition = true)
        {
            _onApplyExplosionForce?.Invoke();

            if (applyPhysicsForce || applyIgnition)
            {
                var foundCount = UnityEngine.Physics.OverlapSphereNonAlloc(position, _range, _nonAllocColliderArray, _affectedLayers);
                
                Rigidbody[] foundDistinctRigidbodies = FindDistinctRigidbodies(_nonAllocColliderArray, foundCount);

                HandleFlammableObjects(_nonAllocColliderArray, foundCount, applyIgnition);
                HandleIgnitionAndPhysicsForces(foundDistinctRigidbodies, position, applyIgnition, applyPhysicsForce);

                if (applyPhysicsForce && CoreSettings.EnableExplosionPhysicsForces)
                {
                    if (CoreSettings.EnableDestruction && HandleDestructibles(_nonAllocColliderArray, foundCount, position))
                    {
                        HandleDebris(position);
                    }
                }
            }

            if (_enableShakeEffect && applyShakeEffect && CoreSettings.EnableCameraShake)
            {
                HandleShakeEffect(position);
            }
        }

        private void HandleFlammableObjects(Collider[] surroundingColliders, int count, bool applyIgnition)
        {
            for (int i = 0; i < count; i++)
            {
                var collider = surroundingColliders[i];
                if (collider == null)
                    break;

                var flammable = collider.GetComponent<IFlammable>();
                if (flammable != null)
                {
                    if(CoreSettings.EnableIgnitionForces)
                        flammable.ApplyFireForce(_explosionForce);
                }
            }
        }

        private void HandleShakeEffect(Vector3 position)
        {
            var shakeRange  = CalculateShakeRange();
            Messenger.Broadcast(new MessengerEventApplyShakeEffect(shakeRange, position));
        }

        private void HandleDebris(Vector3 position)
        {
            var foundCount              = UnityEngine.Physics.OverlapSphereNonAlloc(position, _range, _nonAllocColliderArray, _debrisLayer);
            var destructibleRigidbodies = FindDistinctRigidbodies(_nonAllocColliderArray, foundCount);

            foreach (var destructibleRigidbody in destructibleRigidbodies)
            {
                var rangeMultiplier = CalculateRangeMultiplier(position, destructibleRigidbody.ClosestPointOnBounds(position));
                var massMultiplier  = CalculateMassMultiplier(destructibleRigidbody);
                
                Messenger.Broadcast(new MessengerEventApplyExplosionForce(destructibleRigidbody, (_explosionForce * massMultiplier) * rangeMultiplier, position, _range, _upwardsmodifier, _forceMode));
            }
        }

        private bool HandleDestructibles(Collider[] surroundingColliders, int count, Vector3 position)
        {
            var wasAnyDestructiblesDestroyed = false;
            for (int i = 0; i < count; i++)
            {
                var collider = surroundingColliders[i];
                //If we hit a collider that's null we break out of the loop as we assume no more colliders are left
                if (collider == null)
                    break;

                var rangeMultiplier = CalculateRangeMultiplier(position, collider.ClosestPointOnBounds(position));
                var destructible    = collider.GetComponent<IDestructible>();
                if (destructible != null)
                {
                    destructible.ApplyDamage(_explosionForce * rangeMultiplier);

                    if (destructible.IsDestroyed)
                        wasAnyDestructiblesDestroyed = true;
                }
            }
            return wasAnyDestructiblesDestroyed;
        }

        private void HandleIgnitionAndPhysicsForces(Rigidbody[] foundDistinctRigidbodies, Vector3 position, bool applyIgnition, bool applyPhysicsForce)
        {
            foreach (var rigidBody in foundDistinctRigidbodies)
            {
                var rangeMultiplier      = CalculateRangeMultiplier(position, rigidBody.ClosestPointOnBounds(position));

                if (_igniteSurroundingIgnitables && CoreSettings.EnableIgnitionForces && applyIgnition)
                {
                    var ignitable = rigidBody.GetComponent<IIgnitable>();
                    if (ignitable != null)
                    {
                        var ignitionForce = _explosionForce;// * CalculateRangeMultiplier(position, ignitable.IgnitePositionTransform.position); - This was removed as it applied too little ignition force and wasn't funny
                        
                        Messenger.Broadcast(new MessengerEventApplyIgnitableForce(ignitable, ignitionForce));
                    }
                }

                if (applyPhysicsForce && CoreSettings.EnableExplosionPhysicsForces && ShouldApplyPhysicsForcesToRigidbody(rigidBody))
                {
                    if (_ignoreKinematic && rigidBody.CompareTag("Player") == false)
                        rigidBody.isKinematic = false;

                    var actualExplosionForce = _explosionForce * rangeMultiplier;

                    if (_applyForceRelativeToMass)
                        actualExplosionForce = _explosionForce * CalculateMassMultiplier(rigidBody);

                    Messenger.Broadcast(new MessengerEventApplyExplosionForce(rigidBody, actualExplosionForce, position, _range, _upwardsmodifier, _forceMode));
                }
            }
        }

        private bool ShouldApplyPhysicsForcesToRigidbody(Rigidbody targetRigidBody)
        {
            if (targetRigidBody.isKinematic && targetRigidBody.gameObject.GetComponent<BaseFireworkBehavior>() != null)
            {
                //Debug.Log($"'{targetRigidBody.gameObject.name}' should not have physics forces applied as it is kinematic and is a firework");
                return false;
            }

            if (targetRigidBody.gameObject.GetComponent<IIgnoreExplosionPhysicsForcesBehavior>() != null)
            {
                //Debug.Log($"'{targetRigidBody.gameObject.name}' should not have physics forces applied as it have the IIgnoreExplosionPhysicsForcesBehavior component");
                return false;
            }

            //Debug.Log($"'{targetRigidBody.gameObject.name}' should have physics forces applied");
            return true;
        }
        
        private float CalculateMassMultiplier(Rigidbody rigidBody)
        {
            return Mathf.Clamp(rigidBody.mass / _explosionForce, .05f, 1f);
        }

        private float CalculateRangeMultiplier(Vector3 explosionPosition, Vector3 targetPosition)
        {
            var distance        = Vector3.Distance(explosionPosition, targetPosition);
            var relativeToRange = Mathf.Clamp(distance / _range, 0f, 1f);
            var rangeMultiplier = Mathf.Clamp(1f - relativeToRange, 0f, 1f);

            //return Mathf.Clamp(1f - (Mathf.Clamp(Vector3.Distance(explosionPosition, targetPosition) / _range, 0f, 1f)), 0f, 1f);
            return rangeMultiplier;
        }

        private Rigidbody[] FindDistinctRigidbodies(Collider[] colliders, int count)
        {
            var foundRigidbodies                = new Dictionary<int, Rigidbody>();
            for (int i = 0; i < count; i++)
            {
                Collider collider = colliders[i];
                //If we hit a collider that's null we break out of the loop as we assume no more colliders are left
                if (collider == null)
                    break;

                var r = collider.GetComponent<Rigidbody>();
                if (r != null)
                {
                    if(foundRigidbodies.ContainsKey(r.gameObject.GetInstanceID()) == false)
                    {
                        foundRigidbodies.Add(r.gameObject.GetInstanceID(), r);
                    }
                }
            }

            if(_ignoreRigidbodies != null)
            {
                foreach (var rigidBodyToIgnore in _ignoreRigidbodies)
                {
                    if (rigidBodyToIgnore == null)
                        continue;

                    if (foundRigidbodies.ContainsKey(rigidBodyToIgnore.gameObject.GetInstanceID()))
                        foundRigidbodies.Remove(rigidBodyToIgnore.gameObject.GetInstanceID());
                }
            }

            return foundRigidbodies.Values.ToArray();
        }

        private float CalculateShakeRange()
        {
            return _range * _shakeRangeMultipler;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(this.transform.position, _range);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(this.transform.position, CalculateShakeRange());
        }    
    }
}
