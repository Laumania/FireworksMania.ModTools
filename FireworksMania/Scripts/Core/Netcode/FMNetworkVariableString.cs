using System;
using Unity.Collections;
using Unity.Netcode;

namespace FireworksMania.Core.Netcode
{
    /// <summary>
    /// TEMP SOLUTION: Test for using NetworkVariables from Mods. Use this with care as it can break any moment as it's not the way it should be done, but we have to try it as NetCode doesn't support this officially yet.
    /// </summary>
    [Obsolete("This should not be used anymore as we now have real Netcode for Gameobjects CodeGen available in the mod build pipeline. You can use regular NGO NetworkVariables in your own code now.", true)]
    public class FMNetworkVariableString : NetworkBehaviour
    {
        private NetworkVariable<FixedString4096Bytes> _networkVariable = new NetworkVariable<FixedString4096Bytes>(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<FixedString4096Bytes> Variable => _networkVariable;
    }
}
