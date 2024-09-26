using UnityEngine;

namespace FireworksMania.Core.Common
{
    public class PlayerSpawnLocation : MonoBehaviour
    {

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            FireworksMania.Core.Utilities.GizmosUtility.DrawArrow(this.transform.position, this.transform.forward, Color.gray, 1f, 0.25f);
        }
#endif

    }
}
