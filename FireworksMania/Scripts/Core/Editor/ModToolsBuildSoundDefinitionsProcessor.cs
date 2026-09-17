using System.Collections.Generic;
using FireworksMania.Core.Definitions;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    //Refuses to build a mod whose GameSoundDefinitions can't work in game (#1073). uMod hands this every definition in
    //the mod in one ProcessAssetBatch call, and that matters: context.FailBuild throws and ends the build on the spot,
    //so failing per asset would show a modder one broken definition per build. Every problem is logged first -
    //clicking one selects its asset - and the build fails once.
    //uMod finds processors through reflection, so this does not need to be public.
    [UModBuildProcessor(typeof(GameSoundDefinition))]
    internal class ModToolsBuildSoundDefinitionsProcessor : BuildEngineProcessor
    {
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets)
        {
            var soundDefinitions = new List<(GameSoundDefinition Definition, string AssetPath)>();
            foreach (var asset in assets)
            {
                if (asset.LoadedObject is GameSoundDefinition soundDefinition)
                    soundDefinitions.Add((soundDefinition, AssetDatabase.GetAssetPath(soundDefinition)));
            }

            var problems = GameSoundDefinitionValidator.FindProblems(soundDefinitions);
            if (problems.Count == 0)
                return;

            for (var i = 0; i < problems.Count; i++)
                Debug.LogError(problems[i].Message, problems[i].Definition);

            context.FailBuild($"{problems.Count} problem(s) with sound definitions in this mod - see the errors above.");
        }
    }
}
