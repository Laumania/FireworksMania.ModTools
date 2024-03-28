using FireworksMania.Core.Definitions.EntityDefinitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class UnwrappedShellFuse : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defines the position on the unwrapped shell fuse where the ignite tool should ignite it")]
        private Transform _ignitePosition;

        public Transform IgnitePosition => _ignitePosition;

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {

                    if (_ignitePosition == null)
                    {
                        Debug.LogError($"Missing {nameof(IgnitePosition)} on {this.gameObject.name}", this.gameObject);
                        return;
                    }
                }
            };
        }
#endif
    }
}
