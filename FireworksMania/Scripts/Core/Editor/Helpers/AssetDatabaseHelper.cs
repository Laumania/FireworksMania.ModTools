using System.Collections.Generic;
using UnityEditor;

namespace FireworksMania.Core.Editor.Helpers
{
    public static class AssetDatabaseHelper
    {
        public static List<T> FindAssetsByType<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}", searchInFolders);

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }
    }
}
