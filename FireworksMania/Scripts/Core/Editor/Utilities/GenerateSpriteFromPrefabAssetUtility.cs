#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;

public class GenerateSpriteFromPrefabAssetUtility : UnityEditor.Editor
{
    private const string PreviewSceneName = "PreviewLightingScene";

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Orthographic/Front View")]
    public static void Prefab2PngOF()
    {
        GameObject Prefab = Selection.activeGameObject;

        if (PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.Model) return;

        CaptureImage(Prefab, true, true);
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Front View Character")]
    public static void PrefabCharacter2PngOF()
    {
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

            GenerateCharacterPreviewImageAsset(selectedGameObjectPrefab);
        }
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Orthographic/Back View")]
    public static void Prefab2PngBF()
    {
        GameObject Prefab = Selection.activeGameObject;

        if (PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.Model) return;

        CaptureImage(Prefab, false, true);
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Perspective/Front View")]
    public static void Prefab2PngPF()
    {
        GameObject Prefab = Selection.activeGameObject;

        if (PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.Model) return;

        CaptureImage(Prefab, true, false);
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Perspective/Back View")]
    public static void Prefab2PngPB()
    {
        GameObject Prefab = Selection.activeGameObject;

        if (PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(Prefab) == PrefabAssetType.Model) return;

        CaptureImage(Prefab, false, false);
    }


    public static void GenerateCharacterPreviewImageAsset(GameObject pref)
    {
        LoadPreviewLightingScene();

        int width                                      = 512;
        int height                                     = 512;

        RuntimePreviewGenerator.PreviewDirection       = new Vector3(0, 0f, -.05f);
        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;

        string folderPath = AssetDatabase.GetAssetPath(pref);
        if (folderPath.Contains("."))
            folderPath = folderPath.Remove(folderPath.LastIndexOf('/'));

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(pref);
        Sprite result   = SetTex(RuntimePreviewGenerator.GenerateModelPreview(temp.transform, width, height, false, true), temp, folderPath);

        DestroyImmediate(temp);
    }

    private static void LoadPreviewLightingScene()
    {
        if (EditorSceneManager.GetActiveScene().name != PreviewSceneName)
        {
            var previewSceneAssetGuid = AssetDatabase.FindAssets(PreviewSceneName).FirstOrDefault();
            var previewScenePath      = AssetDatabase.GUIDToAssetPath(previewSceneAssetGuid);
            EditorSceneManager.OpenScene(previewScenePath);
        }
            
    }

    public static Sprite CaptureImage(GameObject pref, bool front, bool Ortho)
    {
        LoadPreviewLightingScene();

        int width = 512;
        int height = 512;
        RuntimePreviewGenerator.BackgroundColor = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        if (front)
        {
            RuntimePreviewGenerator.PreviewDirection = new Vector3(-0.75f, -1, 1.5f);
        }
        else
        {
            RuntimePreviewGenerator.PreviewDirection = new Vector3(-0.75f, -1, -1.5f);
        }

        RuntimePreviewGenerator.RenderSupersampling = 2;
        if (Ortho)
        {
            RuntimePreviewGenerator.OrthographicMode = true;
        }
        else
        {
            RuntimePreviewGenerator.OrthographicMode = false;
        }

        string folderPath = AssetDatabase.GetAssetPath(pref);
        if (folderPath.Contains("."))
            folderPath = folderPath.Remove(folderPath.LastIndexOf('/'));
        //Debug.Log(folderPath);


        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(pref);
        
        Sprite result = SetTex(RuntimePreviewGenerator.GenerateModelPreview(temp.transform, width, height, false, true), temp, folderPath);

        DestroyImmediate(temp);

        return result;
    }



    public static Sprite SetTex(Texture2D tex, GameObject prefObject, string path)
    {
        if (tex == null)
        {
            Debug.LogWarning("Failed to Produce Texture");
            return null;
        }
        if (!tex.isReadable)
        {
            Debug.Log("Texture Could not be Read");
            return null;
        }

        Sprite Png = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
        Png.name = prefObject.name + " AutoGeneratedImage";

        string name = path + $"/{Png.name}.png";
        Sprite result = SaveSpriteAsAsset(Png, name);
        return result;
    }


    static Sprite SaveSpriteAsAsset(Sprite sprite, string proj_path)
    {
        string dataPath = Application.dataPath;
        int point       = dataPath.LastIndexOf("/");
        dataPath        = dataPath.Substring(0, point);

        var abs_path = Path.Combine(dataPath, proj_path);

        //Directory.CreateDirectory(Path.GetDirectoryName(abs_path));
        File.WriteAllBytes(abs_path, ImageConversion.EncodeToPNG(sprite.texture));

        AssetDatabase.Refresh();

        var ti                 = AssetImporter.GetAtPath(proj_path) as TextureImporter;
        ti.spritePixelsPerUnit = sprite.pixelsPerUnit;
        ti.mipmapEnabled       = false;
        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        ti.textureCompression  = TextureImporterCompression.CompressedHQ;
        ti.maxTextureSize      = 512;

        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();

        Debug.Log($"Saved generated preview '{sprite.name}' at path: {proj_path}");
        Sprite returnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(proj_path);
        EditorGUIUtility.PingObject(returnSprite);
        return returnSprite;
    }
}
#endif