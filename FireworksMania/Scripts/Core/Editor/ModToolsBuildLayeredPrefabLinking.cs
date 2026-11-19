using System;
using System.Collections.Generic;
using System.Linq;
using UMod.BuildEngine;
using UMod.BuildPipeline;
using UMod.BuildPipeline.Build;
using UMod.Shared;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor
{
    //Works around a uMod 2.9.9 build bug that loses Mod Tools scripts in a mod (#3454).
    //
    //uMod's own PrefabLinkProcessor (priority 110) edits every prefab of the mod in memory - an identity component, link
    //components, the scripts stripped - and then writes them all with one AssetDatabase.SaveAssets(). That call picks its
    //own order. When it writes a prefab before a prefab built from it (a variant of it, or one that nests it), Unity
    //reimports the dependent and throws away the dependent's unsaved edits:
    //- the dependent ships with its scripts unstripped and unlinked;
    //- throwing away its ModObjectIdentity overwrites the build's mod id with zeros, because uMod gives every identity
    //  component the same ModIdentifier object. modinfo.dat then gets a zero id while the scene, linked earlier, has the
    //  real one, so at load uMod adds back none of the scene's Mod Tools scripts. That is how Alpine lost its player
    //  spawns and its cave sound.
    //Which order SaveAssets picks follows the GUIDs, so whether a mod breaks looks random.
    //
    //The fix links in layers. Before uMod's pass, every prefab built from another prefab of the mod is taken out of the
    //build, and uMod's pass links the rest. After it, each layer goes back in and gets uMod's own pass, once the prefabs
    //it is built from are written. Nothing is then reimported over unsaved edits.
    //
    //Pausing imports around uMod's pass instead (AssetDatabase.StartAssetEditing) saves the id too, but it leaves a variant
    //of a prefab with a Mod Tools script linking that script twice, so it would run twice in game. Linking in layers
    //links it once (measured in #3454).
    //
    //UModAlone_LosesTheDependentsWork_InOneWriteOrder fails once uMod stops losing the work, and this workaround can go
    //then. It is in ModToolsBuildLayeredPrefabLinkingBuildTests, which is [Explicit]: run it after every uMod upgrade.
    internal static class LayeredPrefabLinking
    {
        //uMod's own PrefabLinkProcessor
        internal const string UModPrefabExtension    = ".prefab";
        internal const int    UModPrefabLinkPriority = 110;

        private static          BuildContext       _deferredFor;
        private static readonly List<List<string>> _deferredLayers = new List<List<string>>();
        private static          byte[]             _idBefore;

        //Groups prefab assets so that each one comes after every prefab in the set it is built from: its variant base,
        //or a prefab it nests. Layer 0 is built from nothing in the set.
        internal static List<List<string>> Layers(IEnumerable<string> prefabPaths) => Layers(prefabPaths, DirectSources);

        //The same, with what each prefab is directly built from given - the EditMode tests use this to stay off the
        //AssetDatabase, where every operation costs seconds once a long test run has loaded half the project
        internal static List<List<string>> Layers(IEnumerable<string> prefabPaths, Func<string, IEnumerable<string>> directSources)
        {
            var paths   = prefabPaths.Distinct().ToList();
            var inSet   = new HashSet<string>(paths);
            var sources = paths.ToDictionary(p => p, p => directSources(p).Where(s => s != p && inSet.Contains(s)).Distinct().ToList());
            var layerOf = new Dictionary<string, int>();

            int LayerOf(string path)
            {
                if (layerOf.TryGetValue(path, out var known))
                    return known;

                layerOf[path] = 0; //Prefab sources can't form a cycle - this only stops a malformed one recursing forever
                var layer = sources[path].Count == 0 ? 0 : 1 + sources[path].Max(LayerOf);
                return layerOf[path] = layer;
            }

            foreach (var path in paths)
                LayerOf(path);

            var count = paths.Count == 0 ? 0 : layerOf.Values.Max() + 1;
            return Enumerable.Range(0, count).Select(i => paths.Where(p => layerOf[p] == i).ToList()).ToList();
        }

        //The prefab assets prefabPath is directly built from: its variant base and the prefabs it nests
        private static IEnumerable<string> DirectSources(string prefabPath)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (root == null)
                yield break;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (source != null)
                    yield return AssetDatabase.GetAssetPath(source);
            }
        }

        //uMod's PrefabLinkProcessor is internal. It is fetched the way uMod finds every processor, this one included:
        //BuildEngineService.LoadProcessors over uMod's build engine assembly. That is public API, so a uMod update that
        //changes it stops this compiling rather than breaking it silently. Null when uMod no longer has that processor.
        internal static BuildPipelineProcessor<BuildContext, BuildPipelineAsset> FindUModPrefabLinkProcessor()
        {
            var service = new BuildEngineService();
            service.LoadProcessors(typeof(BuildEngineProcessor).Assembly);

            var processors = service.LoadedProcessors;
            for (var i = 0; i < processors.Count; i++)
            {
                var attribute = processors[i].Attribute;
                if (attribute.AssetExtension == UModPrefabExtension && attribute.Priority == UModPrefabLinkPriority)
                    return processors[i];
            }

            return null;
        }

        //Before uMod's prefab pass: takes every prefab above layer 0 out of the build's assets, and remembers the mod id
        internal static void DeferDependents(BuildContext context, IEnumerable<BuildPipelineAsset> prefabs)
        {
            _deferredFor = context;
            _deferredLayers.Clear();
            _idBefore = context.ModIdentity == null ? null : (byte[])((byte[])context.ModIdentity.ModIdentifier).Clone();

            var assets = prefabs.ToList();
            var layers = Layers(assets.Select(a => a.RelativePath));
            foreach (var layer in layers.Skip(1))
            {
                var layerAssets = layer.Select(path => assets.First(a => a.RelativePath == path)).ToList();
                foreach (var asset in layerAssets)
                    context.BuildAssets.ExcludeAsset(asset);

                _deferredLayers.Add(layerAssets.Select(a => a.FullPath).ToList());
            }

            context.Log.Message("Layered prefab linking: {0} prefab(s) built from another prefab of the mod are linked after uMod's pass, in {1} layer(s)",
                _deferredLayers.Sum(l => l.Count), _deferredLayers.Count);
        }

        //After uMod's prefab pass: puts the deferred prefabs back and runs uMod's own prefab pass on them, a layer at a time
        internal static void LinkDeferred(BuildContext context)
        {
            if (_deferredFor != context)
                return;

            if (_deferredLayers.Count > 0)
            {
                var uModPass = FindUModPrefabLinkProcessor();
                if (uModPass == null)
                    throw new InvalidOperationException($"uMod has no '{UModPrefabExtension}' processor at priority {UModPrefabLinkPriority} any more, so the deferred prefabs can't be linked. Check whether uMod still loses a prefab's link work (#3454).");

                for (var i = 0; i < _deferredLayers.Count; i++)
                {
                    var assets = _deferredLayers[i].Select(fullPath => context.BuildAssets.CreateAsset(fullPath)).ToList();
                    if (assets.Any(a => a == null))
                        throw new InvalidOperationException("A deferred prefab is gone: " + string.Join(", ", _deferredLayers[i]));

                    context.Log.Message("Layered prefab linking: layer {0}, {1} prefab(s)", i + 1, assets.Count);
                    uModPass.ProcessAssetBatch(context, assets);
                }
            }

            _deferredLayers.Clear();
            _deferredFor = null;
        }

        //At the end of the processors: why the build can't go on, or null
        internal static string ProblemAtEnd(BuildContext context)
        {
            if (_deferredLayers.Count > 0)
                return $"Layered prefab linking took {_deferredLayers.Sum(l => l.Count)} prefab(s) out of the build and never put them back.";

            return IdProblem(_idBefore, context.ModIdentity == null ? null : (byte[])context.ModIdentity.ModIdentifier);
        }

        //Why the build's mod id can't be shipped, or null when it can
        internal static string IdProblem(byte[] idBefore, byte[] idNow)
        {
            if (idNow == null || idNow.All(b => b == 0))
                return "The mod id is zero. The game would add back none of this mod's Mod Tools scripts, because uMod matches each object's id against modinfo.dat.";

            if (idBefore != null && !idNow.SequenceEqual(idBefore))
                return "The mod id changed during the build, so the scene and the prefabs may carry different ids.";

            return null;
        }

        //uMod only logs a warning when a processor throws, and carries on, so any exception here fails the build instead
        internal static void FailOnError(BuildContext context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e) when (!(e is ModBuildException))
            {
                context.FailBuild("Layered prefab linking failed: " + e);
            }
        }
    }

    //Just before uMod's PrefabLinkProcessor
    [UModBuildProcessor(LayeredPrefabLinking.UModPrefabExtension, LayeredPrefabLinking.UModPrefabLinkPriority - 1)]
    internal sealed class ModToolsBuildDeferDependentPrefabsProcessor : BuildEngineProcessor
    {
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets) =>
            LayeredPrefabLinking.FailOnError(context, () => LayeredPrefabLinking.DeferDependents(context, assets));
    }

    //Just after uMod's PrefabLinkProcessor
    [UModBuildProcessor(LayeredPrefabLinking.UModPrefabExtension, LayeredPrefabLinking.UModPrefabLinkPriority + 1)]
    internal sealed class ModToolsBuildLinkDeferredPrefabsProcessor : BuildEngineProcessor
    {
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets) =>
            LayeredPrefabLinking.FailOnError(context, () => LayeredPrefabLinking.LinkDeferred(context));
    }

    //After every other processor: refuses to write a mod whose id was lost
    [UModBuildProcessor(LayeredPrefabLinking.UModPrefabExtension, 100000)]
    internal sealed class ModToolsBuildModIdentityCheckProcessor : BuildEngineProcessor
    {
        public override void ProcessAssetBatch(BuildContext context, IEnumerable<BuildPipelineAsset> assets)
        {
            var problem = LayeredPrefabLinking.ProblemAtEnd(context);
            if (problem != null)
                context.FailBuild(problem);

            context.Log.Message("Layered prefab linking: mod id {0} kept", BitConverter.ToString((byte[])context.ModIdentity.ModIdentifier));
        }
    }
}
