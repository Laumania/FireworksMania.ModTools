using UnityEngine;

namespace FireworksMania.Core.Persistence
{
    [AddComponentMenu("Fireworks Mania/Persistence/SaveableTransformComponent")]
    public class SaveableTransformComponent : MonoBehaviour, ISaveableComponent
    {
        public CustomEntityComponentData CaptureState()
        {
            var customData = new CustomEntityComponentData();

            customData.Add<SerializableVector3>(nameof(transform.position), new SerializableVector3
            {
                X = this.transform.position.x,
                Y = this.transform.position.y,
                Z = this.transform.position.z
            });

            customData.Add<SerializableRotation>(nameof(transform.rotation), new SerializableRotation()
            {
                X = this.transform.rotation.x,
                Y = this.transform.rotation.y,
                Z = this.transform.rotation.z,
                W = this.transform.rotation.w
            });

            customData.Add<SerializableVector3>(nameof(transform.localScale), new SerializableVector3()
            {
                X = this.transform.localScale.x,
                Y = this.transform.localScale.y,
                Z = this.transform.localScale.y
            });

            return customData;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            var position = customComponentData.Get<SerializableVector3>(nameof(transform.position));
            var rotation = customComponentData.Get<SerializableRotation>(nameof(transform.rotation));
            var scale    = customComponentData.Get<SerializableVector3>(nameof(transform.localScale));

            this.transform.position   = new Vector3(position.X, position.Y, position.Z);
            this.transform.rotation   = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            this.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z);
        }

        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
