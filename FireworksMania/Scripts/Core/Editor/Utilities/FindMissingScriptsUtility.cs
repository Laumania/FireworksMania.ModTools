using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class FindMissingScriptsUtility
    {
        //[ContextMenu("Fireworks Mania/Utilities/Find missing scripts", true)]
        //[MenuItem("CONTEXT/Fireworks Mania/Utilities/Find missing scripts")]
        //public static void FindInSelected()
        //{
        //    int goCount = 0, componentsCount = 0, missingCount = 0;

        //    GameObject[] go = Selection.gameObjects;
        //    goCount         = 0;
        //    componentsCount = 0;
        //    missingCount    = 0;

        //    foreach (GameObject g in go)
        //    {
        //        FindInGO(g);
        //    }

        //    Debug.Log($"Searched {goCount} GameObjects, {componentsCount} components, found {missingCount} missing");
        //}

        //[MenuItem("CONTEXT/Fireworks Mania/Utilities/Find missing scripts",  isValidateFunction: true)]
        //public static bool ValidateFindInSelected()
        //{
        //    return Selection.gameObjects?.Length > 0;
        //}

        [MenuItem("Mod Tools/Utilities/Find all Prefabs with missing scripts")]
        private static void FindPrefabsWithMissingScriptInProject()
        {
            Debug.Log("Looking for Prefabs with missing scripts...");

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

                if ((fileType == typeof(GameObject) && fileExtension.ToLower() == ".prefab"))
                {
                    var gameObject = AssetDatabase.LoadAssetAtPath(assetFilePath, typeof(GameObject)) as GameObject;
                    FindInGO(gameObject);
                }
            }

            Debug.Log("Done looking for Prefabs with missing scripts...");
        }

        private static void FindInGO(GameObject gameObject)
        {
            int goCount = 0, componentsCount = 0, missingCount = 0;
            goCount++;
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                componentsCount++;
                if (components[i] == null)
                {
                    missingCount++;
                    string s = gameObject.name;
                    Transform t = gameObject.transform;
                    while (t.parent != null)
                    {
                        s = t.parent.name + "/" + s;
                        t = t.parent;
                    }
                    Debug.Log($"{s} has an empty script attached in position: {i}", gameObject);
                }
            }

            // Now recurse through each child GO (if there are any):
            foreach (Transform childT in gameObject.transform)
            {
                //Debug.Log("Searching " + childT.name  + " " );
                FindInGO(childT.gameObject);
            }
        }
    }
}
