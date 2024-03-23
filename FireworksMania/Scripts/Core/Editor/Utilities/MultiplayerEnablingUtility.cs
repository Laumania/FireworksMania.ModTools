using FireworksMania.Core.Common;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class MultiplayerEnablingUtility
    {
        [MenuItem("GameObject/Fireworks Mania/Utilities/Add Network Components", false, priority = 10)]
        private static void AddNetworkComponents()
        {
            TryAddNetworkComponents(Selection.activeGameObject);

            foreach (var selectedGameObject in Selection.gameObjects)
            {
                TryAddNetworkComponents(selectedGameObject);
            }
        }

        private static void TryAddNetworkComponents(GameObject target)
        {
            if (target == null)
                return;

            if (target.GetComponent<NetworkObject>() == null)
                target.AddComponent<NetworkObject>();

            if (target.GetComponent<ClientNetworkTransform>() == null)
            {
                var clientTransform = target.AddComponent<ClientNetworkTransform>();
                clientTransform.SyncScaleX = false;
                clientTransform.SyncScaleY = false;
                clientTransform.SyncScaleZ = false;

                ComponentUtility.MoveComponentUp(clientTransform);
            }

            if (target.GetComponent<Rigidbody>() != null && target.GetComponent<NetworkRigidbody>() == null)
            {
                var networkRigidbody = target.AddComponent<NetworkRigidbody>();
                ComponentUtility.MoveComponentUp(networkRigidbody);
            }
        }
    }
}
