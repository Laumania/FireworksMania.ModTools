using UnityEngine;

namespace FireworksMania.Core.Utilities
{
#if UNITY_EDITOR
    public static class GizmosUtility
    {
        public static void DrawArrow(Vector3 pos, Vector3 direction, Color color, float arrowLength, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
        {
            Gizmos.color = color;
            var directionLength = direction * arrowLength;
            Gizmos.DrawRay(pos, directionLength);

            Vector3 right = Quaternion.LookRotation(directionLength) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(directionLength) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(pos + directionLength, right * arrowHeadLength);
            Gizmos.DrawRay(pos + directionLength, left * arrowHeadLength);
        }
    }
#endif
}
