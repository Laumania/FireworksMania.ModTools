using System.Linq;
using FireworksMania.Core.Common;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class MultiplayerEnablingUtility
    {
        [MenuItem("GameObject/Fireworks Mania/Add Network Components", false, priority = 10)]
        [MenuItem("Assets/Fireworks Mania/Add Network Components", false, priority = 10)]
        private static void AddNetworkComponents()
        {
            foreach (var selectedGameObject in Selection.gameObjects)
            {
                TryAddNetworkComponents(selectedGameObject);
            }
        }

        private static void TryAddNetworkComponents(GameObject target)
        {
            if (target.OrNull() == null)
                return;
            
            if (target.GetComponent<NetworkObject>().OrNull() == null)
                target.AddComponent<NetworkObject>();

            if (target.GetComponent<ClientNetworkTransform>().OrNull() == null)
                target.AddComponent<ClientNetworkTransform>();

            if (target.GetComponent<Rigidbody>().OrNull() != null && target.GetComponent<ClientNetworkRigidbody>().OrNull() == null)
                target.AddComponent<ClientNetworkRigidbody>();

            Debug.Log($"Added network components to '{target.name}'", target);
        }


        [MenuItem("Mod Tools/Utilities/Multiplayer/Revert All NetworkObject Overrides In Current Scene")]
        private static void RevertAllNetworkObjectOverridesInCurrentScene()
        {
            var networkObjectsInScene = GameObject.FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            Debug.Log($"Found {networkObjectsInScene.Length} NetworkObjects in current scene");

            foreach (var networkObject in networkObjectsInScene)
            {
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(networkObject.gameObject, false))
                {
                    PrefabUtility.RevertObjectOverride(networkObject, InteractionMode.AutomatedAction);
                    EditorUtility.SetDirty(networkObject);
                    Debug.Log($"Reverted Overrides on NetworkObject '{networkObject.gameObject.name}'", networkObject.gameObject);
                }
            }
        }

        [MenuItem("Mod Tools/Utilities/Multiplayer/Mark all NetworkObjects as dirty in current scene")]
        private static void MarkAllNetworkObjectsAsDirtyInCurrentScene()
        {
            var networkObjectsInScene = GameObject.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"Found {networkObjectsInScene.Length} NetworkObjects in current scene");

            foreach (var networkObject in networkObjectsInScene)
            {
                EditorUtility.SetDirty(networkObject);
                Debug.Log($"Marked NetworkObject '{networkObject.gameObject.name}' as dirty (force update)", networkObject.gameObject);
            }
        }
    }
}
