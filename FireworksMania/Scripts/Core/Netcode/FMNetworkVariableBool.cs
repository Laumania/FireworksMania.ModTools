using Unity.Netcode;

namespace FireworksMania.Core.Netcode
{
    /// <summary>
    /// TEMP SOLUTION: Test for using NetworkVariables from Mods. Use this with care as it can break any moment as it's not the way it should be done, but we have to try it as NetCode doesn't support this officially yet.
    /// </summary>
    public class FMNetworkVariableBool : NetworkBehaviour
    {
        private NetworkVariable<bool> _networkVariable = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<bool> Variable => _networkVariable;
    }
}
