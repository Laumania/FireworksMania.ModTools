using FireworksMania.Core.Common;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireworksMania.Core.Editor
{
    [UModBuildProcessor(".unity")]
    public class BuildAssetSceneProcessor : BuildEngineProcessor
    {
        public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
        {
            Scene scene = EditorSceneManager.OpenScene(asset.FullPath);
            GameObject[] rootObjects = scene.GetRootGameObjects();

            //CheckForPlayerSpawnLocation(scene, rootObjects); //Have to comment this one out for now as it just wont work and I don't get why. It doesn't find the PlayerSpawnLocation even though it's right there in the scene...
            CheckForDirectionalLights(scene, rootObjects);
            CheckForCamera(scene, rootObjects);
            
            //if (rootObject.GetComponentInChildren<Light>() != null)
            //{
            //    context.FailBuild(string.Format("Scene: {0} contains one or more lights. Mod scenes must not contains any light componenets!", asset.RelativePath));
            //}
        }

        private void CheckForCamera(Scene scene, GameObject[] rootObjects)
        {
            foreach (GameObject rootObject in rootObjects)
            {
                if(rootObject.GetComponentInChildren<Camera>() != null)
                    Debug.LogWarning($"Found '{nameof(Camera)}' in scene '{scene.name}'. Scenes should not contains a '{nameof(Camera)}' when used in a mod as it will most likely break the game when mod is loaded, unless you know what you are doing.");
            }
        }

        private void CheckForPlayerSpawnLocation(Scene scene, GameObject[] rootObjects)
        {
            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject.GetComponentInChildren<PlayerSpawnLocation>() != null)
                    return;
            }

            Debug.LogWarning($"Missing '{nameof(PlayerSpawnLocation)}' in scene '{scene.name}'. If no '{nameof(PlayerSpawnLocation)}' is present the player will be spawned at 0,0,0.");
        }

        private void CheckForDirectionalLights(Scene scene, GameObject[] rootObjects)
        {
            foreach (GameObject rootObject in rootObjects)
            {
                var lights = rootObject.GetComponentsInChildren<Light>();

                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                        Debug.LogWarning($"Seems like you have a Directional Light in your scene '{scene.name}'. This will most likely make the day/night cycle in the game look odd, so consider removing it", light.gameObject);
                }
            }
        }
    }
}
