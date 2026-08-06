using System.Collections.Generic;
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

        // Cached collections to avoid per-explosion heap allocations in FindDistinctRigidbodies
        private readonly Dictionary<int, Rigidbody> _rigidbodySearchDict  = new Dictionary<int, Rigidbody>();
        private readonly List<Rigidbody>             _distinctRigidbodies  = new List<Rigidbody>();

        void Awake()
        {
            if (_originTransform == null)
                _originTransform = this.transform;

            _debrisLayer            = 1 << LayerMask.NameToLayer("DestroyItDebris");

            if (_debrisLayer == -1)
                Debug.LogError("Debris layer name not found!", this);

            _affectedLayers = LayerMask.GetMask("Default") | LayerMask.GetMask("Interactable") | LayerMask.GetMask("Player") | LayerMask.GetMask("DestroyItDebris");
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

                List<Rigidbody> foundDistinctRigidbodies = FindDistinctRigidbodies(_nonAllocColliderArray, foundCount);

                HandleFlammableObjects(_nonAllocColliderArray, foundCount, applyIgnition);

                //Destruction runs before the physics force pass so freshly destroyed objects are already
                //marked IsDestroyed and skipped by it - their debris receives the explosion force when it
                //spawns (staggered over frames) instead of the doomed original being launched
                var wasAnyDestructiblesDestroyed = false;
                if (applyPhysicsForce && CoreSettings.EnableExplosionPhysicsForces && CoreSettings.EnableDestruction)
                    wasAnyDestructiblesDestroyed = HandleDestructibles(_nonAllocColliderArray, foundCount, position);

                HandleIgnitionAndPhysicsForces(foundDistinctRigidbodies, position, applyIgnition, applyPhysicsForce);

                if (wasAnyDestructiblesDestroyed)
                {
                    HandleDebris(position);
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

                if (collider.TryGetComponent<IFlammable>(out var flammable))
                {
                    if(CoreSettings.EnableIgnitionForces)
                        flammable.ApplyFireForce(_explosionForce);
                }
            }
        }

        private void HandleShakeEffect(Vector3 position)
        {
            var shakeRange  = CalculateShakeRange();
            Messenger.Broadcast(new MessengerEventApplyShakeEffectStruct(shakeRange, position));
        }

        private void HandleDebris(Vector3 position)
        {
            var foundCount              = UnityEngine.Physics.OverlapSphereNonAlloc(position, _range, _nonAllocColliderArray, _debrisLayer);
            var destructibleRigidbodies = FindDistinctRigidbodies(_nonAllocColliderArray, foundCount);

            foreach (var destructibleRigidbody in destructibleRigidbodies)
            {
                var rangeMultiplier = CalculateRangeMultiplier(position, destructibleRigidbody.ClosestPointOnBounds(position));
                var massMultiplier  = CalculateMassMultiplier(destructibleRigidbody);
                
                Messenger.Broadcast(new MessengerEventApplyExplosionForceStruct(destructibleRigidbody, (_explosionForce * massMultiplier) * rangeMultiplier, position, _range, _upwardsmodifier, _forceMode));
            }
        }

        private bool HandleDestructibles(Collider[] surroundingColliders, int count, Vector3 position)
        {
            var wasAnyDestructiblesDestroyed = false;
            var explosionSource              = new ExplosionDamageSource(position, _explosionForce, _range, _upwardsmodifier, _forceMode, _applyForceRelativeToMass);
            for (int i = 0; i < count; i++)
            {
                var collider = surroundingColliders[i];
                //If we hit a collider that's null we break out of the loop as we assume no more colliders are left
                if (collider == null)
                    break;

                if (collider.TryGetComponent<IDestructible>(out var destructible))
                {
                    var wasAlreadyDestroyed = destructible.IsDestroyed;
                    var rangeMultiplier     = CalculateRangeMultiplier(position, collider.ClosestPointOnBounds(position));
                    destructible.ApplyDamage(_explosionForce * rangeMultiplier, explosionSource);

                    //Only destructions caused by THIS explosion trigger the debris pass - an object still
                    //waiting for its staggered debris swap was already counted by the explosion that killed it
                    if (wasAlreadyDestroyed == false && destructible.IsDestroyed)
                        wasAnyDestructiblesDestroyed = true;
                }
            }
            return wasAnyDestructiblesDestroyed;
        }

        private void HandleIgnitionAndPhysicsForces(List<Rigidbody> foundDistinctRigidbodies, Vector3 position, bool applyIgnition, bool applyPhysicsForce)
        {
            foreach (var rigidBody in foundDistinctRigidbodies)
            {
                var rangeMultiplier      = CalculateRangeMultiplier(position, rigidBody.ClosestPointOnBounds(position));

                if (_igniteSurroundingIgnitables && CoreSettings.EnableIgnitionForces && applyIgnition)
                {
                    if (rigidBody.TryGetComponent<IIgnitable>(out var ignitable))
                    {
                        var ignitionForce = _explosionForce;// * CalculateRangeMultiplier(position, ignitable.IgnitePositionTransform.position); - This was removed as it applied too little ignition force and wasn't funny
                        
                        Messenger.Broadcast(new MessengerEventApplyIgnitableForceStruct(ignitable, ignitionForce));
                    }
                }

                if (applyPhysicsForce && CoreSettings.EnableExplosionPhysicsForces && ShouldApplyPhysicsForcesToRigidbody(rigidBody))
                {
                    if (_ignoreKinematic && rigidBody.CompareTag("Player") == false)
                        rigidBody.isKinematic = false;

                    var actualExplosionForce = _explosionForce * rangeMultiplier * CalculateMassMultiplier(rigidBody);

                    Messenger.Broadcast(new MessengerEventApplyExplosionForceStruct(rigidBody, actualExplosionForce, position, _range, _upwardsmodifier, _forceMode));
                }
            }
        }

        private bool ShouldApplyPhysicsForcesToRigidbody(Rigidbody targetRigidBody)
        {
            if (targetRigidBody.isKinematic && targetRigidBody.gameObject.TryGetComponent<BaseFireworkBehavior>(out _))
            {
                //Debug.Log($"'{targetRigidBody.gameObject.name}' should not have physics forces applied as it is kinematic and is a firework");
                return false;
            }

            if (targetRigidBody.gameObject.TryGetComponent<IIgnoreExplosionPhysicsForcesBehavior>(out _))
            {
                //Debug.Log($"'{targetRigidBody.gameObject.name}' should not have physics forces applied as it have the IIgnoreExplosionPhysicsForcesBehavior component");
                return false;
            }

            if (targetRigidBody.TryGetComponent<IDestructible>(out var destructible) && destructible.IsDestroyed)
            {
                //A destroyed destructible is only still around because its debris swap is staggered over
                //frames - launching the doomed original would apply the explosion twice, as the debris
                //receives the explosion force when it spawns
                return false;
            }

            //Debug.Log($"'{targetRigidBody.gameObject.name}' should have physics forces applied");
            return true;
        }
        
        private float CalculateMassMultiplier(Rigidbody rigidBody)
        {
            return CalculateMassMultiplier(rigidBody.mass, _explosionForce, _applyForceRelativeToMass);
        }

        public static float CalculateMassMultiplier(float mass, float explosionForce, bool applyForceRelativeToMass)
        {
            if (applyForceRelativeToMass == false)
                return 1f;

            return Mathf.Clamp(mass / explosionForce, .05f, 1f);
        }

        private float CalculateRangeMultiplier(Vector3 explosionPosition, Vector3 targetPosition)
        {
            return CalculateRangeMultiplier(explosionPosition, targetPosition, _range);
        }

        public static float CalculateRangeMultiplier(Vector3 explosionPosition, Vector3 targetPosition, float range)
        {
            var distance        = Vector3.Distance(explosionPosition, targetPosition);
            var relativeToRange = Mathf.Clamp(distance / range, 0f, 1f);
            var rangeMultiplier = Mathf.Clamp(1f - relativeToRange, 0f, 1f);

            return rangeMultiplier;
        }

        private List<Rigidbody> FindDistinctRigidbodies(Collider[] colliders, int count)
        {
            _rigidbodySearchDict.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider collider = colliders[i];
                //If we hit a collider that's null we break out of the loop as we assume no more colliders are left
                if (collider == null)
                    break;

                var r = collider.attachedRigidbody;
                if (r != null)
                {
                    if(_rigidbodySearchDict.ContainsKey(r.gameObject.GetInstanceID()) == false)
                    {
                        _rigidbodySearchDict.Add(r.gameObject.GetInstanceID(), r);
                    }
                }
            }

            if(_ignoreRigidbodies != null)
            {
                foreach (var rigidBodyToIgnore in _ignoreRigidbodies)
                {
                    if (rigidBodyToIgnore == null)
                        continue;

                    if (_rigidbodySearchDict.ContainsKey(rigidBodyToIgnore.gameObject.GetInstanceID()))
                        _rigidbodySearchDict.Remove(rigidBodyToIgnore.gameObject.GetInstanceID());
                }
            }

            _distinctRigidbodies.Clear();
            foreach (var kvp in _rigidbodySearchDict)
                _distinctRigidbodies.Add(kvp.Value);

            return _distinctRigidbodies;
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
