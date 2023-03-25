using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// Serializable Nullable (SN) Does the same as C# System.Nullable, except it's an ordinary
    /// serializable struct, allowing unity to serialize it and show it in the inspector.
    /// Based on https://answers.unity.com/questions/1654475/why-doesnt-unity-property-editor-show-a-nullable-v.html
    /// </summary>
    [System.Serializable]
    public struct SerializableNullable<T>
    {
        public T Value
        {
            get
            {
                if (!HasValue)
                    throw new System.InvalidOperationException("Serializable nullable object must have a value.");
                return v;
            }
        }

        public bool HasValue { get { return hasValue; } }

        [SerializeField]
        private T v;

        [SerializeField]
        private bool hasValue;

        public SerializableNullable(bool hasValue, T v)
        {
            this.v = v;
            this.hasValue = hasValue;
        }

        private SerializableNullable(T v)
        {
            this.v = v;
            this.hasValue = true;
        }

        public static implicit operator SerializableNullable<T>(T value)
        {
            return new SerializableNullable<T>(value);
        }
    }
}
