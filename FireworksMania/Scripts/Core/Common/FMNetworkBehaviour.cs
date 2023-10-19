using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core
{
    public class FMNetworkBehaviour : NetworkBehaviour
    {
        protected NetworkVariable<int> _internalCounter = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }
}
