using FireworksMania.Core.Persistence;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    [UModBuildProcessor(".prefab")]
    public class ModToolsBuildPrefabsProcessor : BuildEngineProcessor
    {
        public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
        {
            if (asset.LoadedObject == null || asset.LoadedObject is not GameObject)
                return;

            var prefabGameObject     = (GameObject)asset.LoadedObject;
            var baseFireworkBehavior = prefabGameObject.GetComponent<IHaveBaseEntityDefinition>();

            if (baseFireworkBehavior != null)
            {
                if (baseFireworkBehavior.EntityDefinition == null)
                {
                    var errorMessage = $"Prefab '{prefabGameObject.name}' is missing reference to an EntityDefinition";
                    Debug.LogError(errorMessage, prefabGameObject);
                    context.FailBuild(errorMessage);
                    return;
                }

                if (baseFireworkBehavior.EntityDefinition.PrefabGameObject != prefabGameObject)
                {
                    var errorMessage = $"'{prefabGameObject.name}' (Prefab) is referencing '{baseFireworkBehavior.EntityDefinition.name}' (EntityDefinition). However '{baseFireworkBehavior.EntityDefinition.name}' is not referencing '{prefabGameObject.name}' back. Try dragging in the prefab into the EntityDefinitions Prefab field.";
                    Debug.LogError(errorMessage, prefabGameObject);
                    context.FailBuild(errorMessage);
                    return;
                }
            }
        }
    }
}
