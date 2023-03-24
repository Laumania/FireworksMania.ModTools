using System;
using System.Collections;
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
            { "m_Script: {fileID: 1252656377, guid: 15f2946a651cfd84c804c26926af6433, type: 3}", new ScriptReplacementData() { LegacyScriptName = "DayNightCycleTriggerBehavior", ReplacementText = "m_Script: {fileID: 11500000, guid: e303587e279e22d43a1044fe62571ce2, type: 3}" } }
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
                if ((fileType == typeof(SceneAsset) || (fileType == typeof(GameObject) && fileExtension.ToLower() == ".prefab")) == false)
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
                            var replacementData = _scriptMapping[scriptTextToReplace];
                            var gameObject      = AssetDatabase.LoadAssetAtPath(assetFilePath, typeof(GameObject)) as GameObject;
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
