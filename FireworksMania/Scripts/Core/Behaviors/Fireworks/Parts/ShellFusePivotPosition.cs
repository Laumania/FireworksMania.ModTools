﻿using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class ShellFusePivotPosition : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private Mesh _gizmoRendermesh;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_gizmoRendermesh != null)
            {
                Gizmos.color = Color.green;
                _gizmoRendermesh.RecalculateNormals();
                Gizmos.DrawWireMesh(_gizmoRendermesh, this.transform.position, this.transform.rotation);
            }
        }
#endif

    }
}