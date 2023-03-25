using UnityEngine;

namespace FireworksMania.Core.Persistence
{
    [AddComponentMenu("Fireworks Mania/Persistence/SaveableRigidbodyComponent")]
    public class SaveableRigidbodyComponent : MonoBehaviour, ISaveableComponent
    {
        private Rigidbody _rigidbody;

        void Awake()
        {
            _rigidbody = this.GetComponent<Rigidbody>();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this.gameObject.GetComponent<Rigidbody>() == null)
            {
                this.gameObject.AddComponent<Rigidbody>();
                Debug.Log("Added require Rigidbody", this);
            }
        }

        public CustomEntityComponentData CaptureState()
        {
            var customData = new CustomEntityComponentData();

            customData.Add<bool>(nameof(_rigidbody.isKinematic), _rigidbody.isKinematic);

            return customData;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            _rigidbody.isKinematic = customComponentData.Get<bool>(nameof(_rigidbody.isKinematic));
        }

        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
