using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IDestructionObjectPool
    {
        NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}
