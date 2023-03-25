using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class LegacyScriptRemappingUtility
    {
        private struct ScriptReplacementData
        {
            public string ReplacementText  { get; set; }
            public string LegacyScriptName { get; set; }
        }

        private static Dictionary<string, ScriptReplacementData> _scriptMapping = new Dictionary<string, ScriptReplacementData>()
        {
            //From --> To 
            { "m_Script: {fileID: 1252656377, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "DayNightCycleTriggerBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: e303587e279e22d43a1044fe62571ce2, type: 3}" } },
            { "m_Script: {fileID: 426763944, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "UseableBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 96b243a40ecec394883d672938a33f9e, type: 3}" } },
            { "m_Script: {fileID: -391650715, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ToggleBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 9d52b01b294764b498940922fc8b7fba, type: 3}" } },
            { "m_Script: {fileID: -432571082, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "PlaySoundOnImpactBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 30f422483e2581344b2260f179db3afd, type: 3}" } },
            { "m_Script: {fileID: 633354770, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "PlaySoundBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 8b13cc7fad8f7de4dac73dd75c24736a, type: 3}" } },
            { "m_Script: {fileID: 1702441458, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "CakeBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: c78bf06a529b64e4baf6089271fd6040, type: 3}" } },
            { "m_Script: {fileID: -199537526, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "FirecrackerBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 2ed8819384f76d249b911103b95ef11f, type: 3}" } },
            { "m_Script: {fileID: 224410169, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "FountainBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: e7aad13b047766e478c1e80e0efa1d1a, type: 3}" } },
            { "m_Script: {fileID: 1661691935, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "PreloadedTubeBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 54c045ed85db53c48ab8437f89b8ac6d, type: 3}" } },
            { "m_Script: {fileID: -1430930928, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "RocketBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: d193d5c1187a2b940bedf04735c12f1c, type: 3}" } },
            { "m_Script: {fileID: -1095426966, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "RocketStrobeBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 684a1c125467db54f8a7a12547c55f1a, type: 3}" } },
            { "m_Script: {fileID: 1369398888, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "RomanCandleBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: a76ca553465b3824c952951897ccc8ff, type: 3}" } },
            { "m_Script: {fileID: 126486832, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "SmokeBombBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 532213d6fc4d9624993f8dfd0afa399c, type: 3}" } },
            { "m_Script: {fileID: 1320360184, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "WhistlerBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 8020246f8d7165946ab299eb0e22fc48, type: 3}" } },
            { "m_Script: {fileID: -935664819, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ZipperBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 7dbd394b4d6741043978315834234fd5, type: 3}" } },
            { "m_Script: {fileID: 208378941, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ExplosionBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: 78c8fa5c31e5ac24c993b8c18e553a7c, type: 3}" } },
            { "m_Script: {fileID: 649956930, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "Thruster", ReplacementText = "m_Script: {fileID: 11500000, guid: ea69b54ce3e858b44b5ad8db78912a48, type: 3}" } },
            { "m_Script: {fileID: -740855493, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ParticleSystemExplosion", ReplacementText = "m_Script: {fileID: 11500000, guid: a1ff519f9ccf6a24b8901a77cec81fa8, type: 3}" } },
            { "m_Script: {fileID: 841284212, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ParticleSystemSound", ReplacementText = "m_Script: {fileID: 11500000, guid: 8ef7c1ac7b563ad449f6e03abec85dd4, type: 3}" } },
            { "m_Script: {fileID: -412979324, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ParticleSystemObserver", ReplacementText = "m_Script: {fileID: 11500000, guid: e20a3d12f75858845a23338c4929c13a, type: 3}" } },
            { "m_Script: {fileID: 2030764095, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "ExplosionPhysicsForceEffect", ReplacementText = "m_Script: {fileID: 11500000, guid: 8ddc39d05f82b0c4f988350229d1b834, type: 3}" } },
            { "m_Script: {fileID: -1390082805, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "SaveableRigidbodyComponent", ReplacementText = "m_Script: {fileID: 11500000, guid: 236b5613f2baaf24abaa1f451f63a27c, type: 3}" } },
            { "m_Script: {fileID: 224251883, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "SaveableTransformComponent", ReplacementText = "m_Script: {fileID: 11500000, guid: 8cfe99405ba86ef44b1f70626f5b3376, type: 3}" } },
            { "m_Script: {fileID: 14210082, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "MapDefinition", ReplacementText = "m_Script: {fileID: 11500000, guid: 9d0809a2fb2157147854d9f9b36cd3a3, type: 3}" } },
            { "m_Script: {fileID: 1615081461, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "GameSoundDefinition", ReplacementText = "m_Script: {fileID: 11500000, guid: b054aaa396590d14b8175a51baac8fab, type: 3}" } },
            { "m_Script: {fileID: -1410589922, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "SoundCollection", ReplacementText = "m_Script: {fileID: 11500000, guid: edf3232d67e67b14f9f636fcce7e734b, type: 3}" } },
            { "m_Script: {fileID: -235357843, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "StartupPrefabDefinition", ReplacementText = "m_Script: {fileID: 11500000, guid: 93fe3dbe08fec96478f7c675fc240354, type: 3}" } },
            { "m_Script: {fileID: -1058617317, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "PropEntityDefinition", ReplacementText = "m_Script: {fileID: 11500000, guid: 68423c8bf17e02342993f38ddab557e0, type: 3}" } },
            { "m_Script: {fileID: -1966469926, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "PlayerSpawnLocation", ReplacementText = "m_Script: {fileID: 11500000, guid: d3dc28ddbcfc52e499c7005c52e2366c, type: 3}" } },
            { "m_Script: {fileID: 1130798590, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "Fuse", ReplacementText = "m_Script: {fileID: 11500000, guid: b9d074da9def36549959293b0b05e6d4, type: 3}" } },
            { "m_Script: {fileID: -1879996859, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "FuseConnectionPoint", ReplacementText = "m_Script: {fileID: 11500000, guid: 3d15df1c433248a41a665204fb340592, type: 3}" } },
            { "m_Script: {fileID: 1184703327, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "FireworkEntityDefinition", ReplacementText = "m_Script: {fileID: 11500000, guid: 0b7029976d5733448893cd5be7a3662a, type: 3}" } },
            { "m_Script: {fileID: -1590680979, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "EntityDefinitionType", ReplacementText = "m_Script: {fileID: 11500000, guid: 4d6ebdc2be3639c47ba30fe16929d012, type: 3}" } },
            { "m_Script: {fileID: 249385142, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "SaveableEntity", ReplacementText = "m_Script: {fileID: 11500000, guid: 863a37014c3aafc4b94d2bab656e8c69, type: 3}" } },
        };

        [MenuItem("Mod Tools/Utilities/Remap legacy Core script to new Core")]
        private static void RemapLegacyCoreScriptsToNewCore()
        {
            Debug.Log("Begin remapping legacy FireworksMania.Core scripts to new FireworksMania.Core scripts");

            if (CheckProjectSerializationAndSourceControlModes() == false)
            {
                Debug.LogError("Remapping not possible. SerializationMode needs to be set to 'ForceText' and VersionControlMode to 'Visible Meta Files'");
                return;
            }

            string searchFolder = "Assets/";
            string[] guids      = AssetDatabase.FindAssets("t:Object", new string[] { searchFolder }).Distinct().ToArray();
            var projectPath     = Path.GetFullPath("Assets/..");

            foreach (var guid in guids)
            {
                string assetFilePath = AssetDatabase.GUIDToAssetPath(guid);
                string fileExtension = Path.GetExtension(assetFilePath);
                Type fileType        = AssetDatabase.GetMainAssetTypeAtPath(assetFilePath);

                // Ignore all files other than Scenes and Prefabs.
                if ((fileType == typeof(SceneAsset) || 
                    (fileType == typeof(GameObject) && fileExtension.ToLower() == ".prefab") ||
                    (fileType == null && fileExtension.ToLower() == ".asset")) == false)
                    continue;

                string assetMetaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetFilePath);
                try
                {
                    var assetFileContent = File.ReadAllText(assetFilePath);
                    var fileHasChanges   = false;

                    foreach (var scriptTextToReplace in _scriptMapping.Keys)
                    {
                        if (assetFileContent.Contains(scriptTextToReplace))
                        {
                            var replacementData  = _scriptMapping[scriptTextToReplace];
                            var gameObject       = AssetDatabase.LoadAssetAtPath(assetFilePath, typeof(GameObject)) as GameObject;
                            Debug.Log($"Replacing missing script '{replacementData.LegacyScriptName}' in '{assetFilePath}'", gameObject);

                            assetFileContent = assetFileContent.Replace(scriptTextToReplace, replacementData.ReplacementText);
                            fileHasChanges   = true;
                        }
                    }
                    
                    if(fileHasChanges)
                        File.WriteAllText(assetFilePath, assetFileContent);
                }
                catch
                {
                    // Continue to the next asset if we can't read the current one.
                    continue;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log("Done remapping legacy FireworksMania.Core scripts to new FireworksMania.Core scripts");
        }

        private static bool CheckProjectSerializationAndSourceControlModes()
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText || VersionControlSettings.mode != "Visible Meta Files")
            {
                return false;
            }

            return true;
        }
    }
}
