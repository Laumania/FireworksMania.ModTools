using System.Collections;
using System.Collections.Generic;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UMod.Shared.Resources;
using UnityEngine;
using NetcodePatcher;
using UnityEditor.Sprites;

// Add a build processor for script assets since we are only interested in scripts that will be compiled by uMod.
// High priority means that it will run after script compilation so build assemblies collection will already be filled out with compiled assemblies.
[UModBuildProcessor(".asset", 1000, true)]
public class ModToolsBuildNGOPatcher : BuildEngineProcessor
{
    // Methods
    // Called by the build engine just after compilation if the mod includes any script content, passing all script assets as a parameter (Not interested in those).
    public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets)
    {
        // get assemblies
        ModAssemblyEntry[] entries = context.BuildAssemblies.Assemblies;

        // Patch all
        foreach (ModAssemblyEntry entry in entries)
        {
            PatchAssembly(context, entry);
        }
    }

    private void PatchAssembly(BuildContext context, ModAssemblyEntry entry)
    {
        // Name info
        string asmName = entry.AssemblyName;
        Debug.Log($"Patching Assembly: '{asmName}'");


        // Get assembly image - loadable by 'Assembly.Load'
        byte[] asmImage = entry.AssemblyImage;

        // TODO - Run custom patcher code
        Debug.Log("TODO - Run custom patcher code");

        //Patcher.Patch(string inputPath, string outputPath, string[] dependencyPaths);

        //// Get the patched result
        //byte[] asmImagePatched = ...;

        //// Check for patched
        //if (asmImagePatched != null)
        //{
        //    // Remove original assembly
        //    context.BuildAssemblies.RemoveFromBuild(entry);

        //    // Register new assembly
        //    if (context.BuildAssemblies.RegisterAssemblyForBuild(asmImagePatched, true) == false)
        //        Debug.LogWarning("Failed to register patches assembly for build! : " + asmName);
        //}
    }
}
