using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Client Network Transform")]
    [UMod.Shared.ModDontCompile]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
