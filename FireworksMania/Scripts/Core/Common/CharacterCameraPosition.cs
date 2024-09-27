using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    [Tooltip("Position of where the First Person Camera should be located on a character")]
    [AddComponentMenu("Fireworks Mania/Common/CharacterCameraPosition")]
    public class CharacterCameraPosition : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            FireworksMania.Core.Utilities.GizmosUtility.DrawArrow(this.transform.position, this.transform.forward, Color.gray, .5f, 0.25f);
        }
#endif
    }
}
