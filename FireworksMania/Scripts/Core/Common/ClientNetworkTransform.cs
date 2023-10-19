using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Client Network Transform")]
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        //Note: Temp fix for ClientNetworkTransform to make pickup/throw work again https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2563#issuecomment-1551414707
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (IsServer && OwnerClientId != NetworkManager.LocalClientId)
            {
                OnLostOwnership();
            }
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
            if (IsServer && OwnerClientId == NetworkManager.LocalClientId)
            {
                OnGainedOwnership();
            }
        }
    }
}
