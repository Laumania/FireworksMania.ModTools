using System.Collections.Generic;
using FireworksMania.Core.Definitions.EntityDefinitions;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    //Refuses to build a mod with a firework whose diameter, caliber or class is not set (#1497). In game such an item
    //still loads - every mod built before #1497 has no caliber or class, and failing those would stop maps loading -
    //but it is listed under no size in the inventory and Shop. So the check is here, where only a new build meets it.
    //uMod hands this every .asset in the mod in one ProcessAssetBatch call: every problem is logged first - clicking
    //one selects the prefab - and the build fails once (Docs/Development/mod-tools-build-checks.md).
    //uMod finds processors through reflection, so this does not need to be public.
    [UModBuildProcessor(".asset")]
    internal class ModToolsBuildClassificationProcessor : BuildEngineProcessor
    {
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets)
        {
            var definitions = new List<BaseInventoryEntityDefinition>();
            foreach (var asset in assets)
            {
                if (asset.LoadedObject is BaseInventoryEntityDefinition definition)
                    definitions.Add(definition);
            }

            var problems = EntityClassificationValidator.FindMissing(definitions);
            if (problems.Count == 0)
                return;

            for (var i = 0; i < problems.Count; i++)
                Debug.LogError(problems[i].Message, problems[i].Prefab);

            context.FailBuild($"{problems.Count} firework(s) in this mod have no diameter, caliber or class set - see the errors above.");
        }
    }
}
