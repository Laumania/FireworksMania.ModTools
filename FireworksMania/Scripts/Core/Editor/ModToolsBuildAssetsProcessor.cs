using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    [UModBuildProcessor(".asset")]
    public class ModToolsBuildAssetsProcessor : BuildEngineProcessor
    {
        public override void ProcessAsset(BuildContext context, BuildPipelineAsset asset)
        {
            if (asset.LoadedObject == null)
                return;

            if (asset.LoadedObject is BaseEntityDefinition entityDefinition)
            {
                ProcessBaseEntityDefinition(context, entityDefinition);
            }
        }

        private void ProcessBaseEntityDefinition(BuildContext context, BaseEntityDefinition baseEntityDefinition)
        {
            if (baseEntityDefinition.PrefabGameObject == null)
            {
                var errorMessage = $"'{baseEntityDefinition.name}' (BaseEntityDefinition) is not referencing any prefab. This is required.";
                Debug.LogError(errorMessage, baseEntityDefinition);
                context.FailBuild(errorMessage);
                return;
            }


            var prefabComponentsWithEntityDefinition = baseEntityDefinition.PrefabGameObject.GetComponents<IHaveBaseEntityDefinition>();
            foreach (var haveBaseEntityDefinition in prefabComponentsWithEntityDefinition)
            {
                if (haveBaseEntityDefinition.EntityDefinition == null)
                {
                    var errorMessage = $"'{baseEntityDefinition.PrefabGameObject.name}' (Prefab) is missing reference to an EntityDefinition";
                    Debug.LogError(errorMessage, baseEntityDefinition.PrefabGameObject);
                    context.FailBuild(errorMessage);
                    return;
                }

                if (haveBaseEntityDefinition.EntityDefinition != baseEntityDefinition)
                {
                    var errorMessage = $"'{baseEntityDefinition.PrefabGameObject.name}' (Prefab) is not referencing '{baseEntityDefinition.name}' (EntityDefinition) which is should, as '{baseEntityDefinition.name}' is referencing '{baseEntityDefinition.PrefabGameObject.name}'";
                    Debug.LogError(errorMessage + " (Click to select Prefab)", baseEntityDefinition.PrefabGameObject);
                    Debug.LogError(errorMessage + " (Click to select EntityDefinition)", baseEntityDefinition);
                    context.FailBuild(errorMessage);
                    return;
                }
            }

            if (baseEntityDefinition is BaseInventoryEntityDefinition baseInventoryEntityDefinition)
            {
                if (baseInventoryEntityDefinition.EntityDefinitionType == null)
                {
                    var errorMessage = $"'{baseInventoryEntityDefinition.name}' (BaseInventoryEntityDefinition) is missing an EntityDefinitionType. This is required.";
                    Debug.LogError(errorMessage, baseEntityDefinition);
                    context.FailBuild(errorMessage);
                    return;
                }
            }
        }
    }
}
