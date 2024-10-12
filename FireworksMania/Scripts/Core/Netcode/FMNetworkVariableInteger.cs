using Unity.Collections;
using Unity.Netcode;

namespace FireworksMania.Core.Netcode
{
    /// <summary>
    /// TEMP SOLUTION: Test for using NetworkVariables from Mods. Use this with care as it can break any moment as it's not the way it should be done, but we have to try it as NetCode doesn't support this officially yet.
    /// </summary>
    public class FMNetworkVariableInteger : NetworkBehaviour
    {
        private NetworkVariable<int> _networkVariable = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<int> Variable => _networkVariable;
    }
}
