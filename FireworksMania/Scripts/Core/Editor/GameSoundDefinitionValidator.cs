using System;
using System.Collections.Generic;
using FireworksMania.Core.Definitions;

namespace FireworksMania.Core.Editor
{
    /// <summary>
    /// What the Mod Tools build refuses in a mod's GameSoundDefinitions (#1073): no clips, an empty or missing clip
    /// slot, or a name another definition in the mod already has. None of those can work in game, and none of them
    /// said so clearly there. Kept apart from ModToolsBuildSoundDefinitionsProcessor so nothing in here needs uMod,
    /// and the EditMode tests can call it.
    /// </summary>
    internal static class GameSoundDefinitionValidator
    {
        internal readonly struct Problem
        {
            public readonly string              Message;
            public readonly GameSoundDefinition Definition;

            public Problem(string message, GameSoundDefinition definition)
            {
                Message    = message;
                Definition = definition;
            }
        }

        /// <summary>
        /// Every problem with the given definitions: the clip problems first, in the order the definitions came in,
        /// then one duplicate-name problem per copy. Null entries are skipped.
        /// </summary>
        public static List<Problem> FindProblems(IReadOnlyList<(GameSoundDefinition Definition, string AssetPath)> soundDefinitions)
        {
            var problems = new List<Problem>();

            for (var i = 0; i < soundDefinitions.Count; i++)
            {
                var definition = soundDefinitions[i].Definition;
                if (definition != null)
                    AddClipProblem(definition, problems);
            }

            AddDuplicateNameProblems(soundDefinitions, problems);

            return problems;
        }

        private static void AddClipProblem(GameSoundDefinition definition, List<Problem> problems)
        {
            var clips = definition.AudioVariationClips;
            if (clips == null || clips.Length == 0)
            {
                problems.Add(new Problem($"'{definition.name}' (GameSoundDefinition) has no audio clips assigned, so it can never play. Add at least one clip to 'Audio Variation Clips'.", definition));
                return;
            }

            var emptyElements = new List<int>();
            for (var i = 0; i < clips.Length; i++)
            {
                //Unity's == rather than ReferenceEquals: a Missing clip (its asset was deleted) is not a null reference
                if (clips[i] == null)
                    emptyElements.Add(i);
            }

            if (emptyElements.Count == 1)
                problems.Add(new Problem($"'{definition.name}' (GameSoundDefinition) has an empty or missing clip at Element {emptyElements[0]} of 'Audio Variation Clips'. Assign a clip there or remove the element.", definition));
            else if (emptyElements.Count > 1)
                problems.Add(new Problem($"'{definition.name}' (GameSoundDefinition) has empty or missing clips at Elements {string.Join(", ", emptyElements)} of 'Audio Variation Clips'. Assign clips there or remove the elements.", definition));
        }

        private static void AddDuplicateNameProblems(IReadOnlyList<(GameSoundDefinition Definition, string AssetPath)> soundDefinitions, List<Problem> problems)
        {
            //Ignoring case, because MasterAudio looks sound groups up that way: 'Boom' and 'boom' share one group in game
            var indicesByName = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            var namesInOrder  = new List<string>();

            for (var i = 0; i < soundDefinitions.Count; i++)
            {
                var definition = soundDefinitions[i].Definition;
                if (definition == null)
                    continue;

                if (indicesByName.TryGetValue(definition.name, out var indices) == false)
                {
                    indices = new List<int>();
                    indicesByName.Add(definition.name, indices);
                    namesInOrder.Add(definition.name);
                }

                indices.Add(i);
            }

            for (var n = 0; n < namesInOrder.Count; n++)
            {
                var indices = indicesByName[namesInOrder[n]];
                if (indices.Count < 2)
                    continue;

                for (var i = 0; i < indices.Count; i++)
                {
                    var otherPaths = new List<string>();
                    for (var j = 0; j < indices.Count; j++)
                    {
                        if (j != i)
                            otherPaths.Add(soundDefinitions[indices[j]].AssetPath);
                    }

                    var definition = soundDefinitions[indices[i]].Definition;
                    problems.Add(new Problem($"'{definition.name}' (GameSoundDefinition) has the same name as {string.Join(", ", otherPaths)}. Sounds are looked up by name, so only one of them will ever be heard. Rename all but one.", definition));
                }
            }
        }
    }
}
