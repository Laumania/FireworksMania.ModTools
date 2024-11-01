using FireworksMania.Core.Definitions.EntityDefinitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class UnwrappedShellFuse : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defines the position and rotation on the unwrapped shell fuse where the ignite tool should ignite it and the fuse burning effect is shown")]
        private Transform _ignitePosition;

        public Transform IgnitePosition => _ignitePosition;

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if(_ignitePosition != null)
                FireworksMania.Core.Utilities.GizmosUtility.DrawArrow(_ignitePosition.position, _ignitePosition.up, Color.yellow, 0.1f, 0.05f);
        }

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
