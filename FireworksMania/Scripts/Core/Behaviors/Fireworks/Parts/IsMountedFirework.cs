using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Server-side marker added to an entity while it is seated in a FireworkMountPoint, so
    /// neighboring sockets don't try to steal it (mirrors how IsPickedUp marks carried objects).
    /// </summary>
    public class IsMountedFirework : MonoBehaviour
    {
    }
}
