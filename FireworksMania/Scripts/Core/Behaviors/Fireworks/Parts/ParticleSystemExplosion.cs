using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemExplosion")]
    public class ParticleSystemExplosion : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        [Tooltip("Can be left blank if no effect is needed")]
        private ExplosionPhysicsForceEffect _particleSpawnedPhysicsEffect;
        [SerializeField]
        [Tooltip("Can be left blank if no effect is needed")]
        private ExplosionPhysicsForceEffect _particleDestroyedPhysicsEffect;

        private ParticleSystemObserver _particleObserver;

        private void Awake()
        {
            _particleObserver = this.GetComponent<ParticleSystemObserver>();
            if (_particleObserver == null)
            {
                //_particleObserver = this.gameObject.AddComponent<ParticleSystemObserver>();
                Debug.LogError($"'{this.gameObject.name}' was missing {nameof(ParticleSystemObserver)} so it was added automatically", this);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this.GetComponent<ParticleSystemObserver>() == null)
                Debug.LogError($"{nameof(ParticleSystemExplosion)} is missing ParticleSystemObserver on '{this.gameObject.name}' else it will not work", this);
        }

        private void OnEnable()
        {
            if (_particleSpawnedPhysicsEffect != null)
                _particleObserver.OnParticleSpawned += ParticleSpawned;

            if (_particleDestroyedPhysicsEffect != null)
                _particleObserver.OnParticleDestroyed += ParticleDestroyed;
        }

        private void OnDisable()
        {
            _particleObserver.OnParticleSpawned -= ParticleSpawned;
            _particleObserver.OnParticleDestroyed -= ParticleDestroyed;
        }

        private void ParticleSpawned(Vector3 particlePosition)
        {
            _particleSpawnedPhysicsEffect.ApplyExplosionForce(particlePosition);
        }

        private void ParticleDestroyed(Vector3 particlePosition)
        {
            _particleDestroyedPhysicsEffect.ApplyExplosionForce(particlePosition);
        }
    }
}
