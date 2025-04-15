#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Threading;
using FireworksMania.Core.Utilities;
using UnityEditor.SceneManagement;
using FireworksMania.Core.Definitions;
using System.Drawing.Drawing2D;
using System;

public class GenerateSpriteFromPrefabAssetUtility : UnityEditor.Editor
{
    private const int Width                                 = 512;
    private const int Height                                = 512;
    private const string PreviewLightingPrefabName          = "PreviewLightingPrefab";
    private static GameObject PreviewLightingPrefab         = null;
    private static GameObject PreviewLightingPrefabInstance = null;


    [MenuItem("GameObject/Fireworks Mania/Generate Preview/Perspective/Current Veiw In Scene")]
    public static void PrefabToPngSceneView()
    {
        Vector3 camerapos = SceneView.lastActiveSceneView.camera.transform.position;
        Vector3 relative  = Selection.activeGameObject.transform.InverseTransformPoint(camerapos);

        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;

        var prefabpath          = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.activeGameObject);
        var prefabFileName      = Path.GetFileName(prefabpath);
        var path                = prefabpath.Replace(prefabFileName, string.Empty);

        SetTex(RuntimePreviewGenerator.GenerateModelPreview(Selection.activeGameObject.transform, Width, Height, false, true, true, relative), Selection.activeGameObject, path);
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Orthographic/Front View")]
    public static void Prefab2PngOF()
    {
        InstansiatePreviewLightingPrefab();
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;
            
            CaptureImage(selectedGameObjectPrefab, true, true);
        }
        DestroyPreviewLightingPrefabInstance();
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Front View Character")]
    public static void PrefabCharacter2PngOF()
    {
        InstansiatePreviewLightingPrefab();
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

            GenerateCharacterPreviewImageAsset(selectedGameObjectPrefab);
        }
        DestroyPreviewLightingPrefabInstance();
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Orthographic/Back View")]
    public static void Prefab2PngBF()
    {
        InstansiatePreviewLightingPrefab();
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

            CaptureImage(selectedGameObjectPrefab, false, true);
        }
        DestroyPreviewLightingPrefabInstance();
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Perspective/Front View")]
    public static void Prefab2PngPF()
    {
        InstansiatePreviewLightingPrefab();
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

            CaptureImage(selectedGameObjectPrefab, true, false);
        }
        DestroyPreviewLightingPrefabInstance();
    }

    [MenuItem("Assets/Fireworks Mania/Generate Preview/Perspective/Back View")]
    public static void Prefab2PngPB()
    {
        InstansiatePreviewLightingPrefab();
        foreach (var selectedGameObjectPrefab in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

            CaptureImage(selectedGameObjectPrefab, false, false);
        }
        DestroyPreviewLightingPrefabInstance();
    }

    public static void GenerateCharacterPreviewImageAsset(GameObject pref)
    {
        int width                                      = 512;
        int height                                     = 512;

        RuntimePreviewGenerator.PreviewDirection       = new Vector3(0, 0f, -.05f);
        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;
        //RuntimePreviewGenerator.Padding                = 0.05f;

        string folderPath = AssetDatabase.GetAssetPath(pref);
        if (folderPath.Contains("."))
            folderPath = folderPath.Remove(folderPath.LastIndexOf('/'));

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(pref);
        Sprite result   = SetTex(RuntimePreviewGenerator.GenerateModelPreview(temp.transform, width, height, false, true), temp, folderPath);

        DestroyImmediate(temp);
    }

    private static void InstansiatePreviewLightingPrefab()
    {
        if (PreviewLightingPrefab.OrNull() == null)
        {
            var previewLightingPrefabAssetGuid = AssetDatabase.FindAssets(PreviewLightingPrefabName).FirstOrDefault();
            var previewLightingPrefabPath      = AssetDatabase.GUIDToAssetPath(previewLightingPrefabAssetGuid);
            var filePathWithOutExtension       = Path.GetFileNameWithoutExtension(previewLightingPrefabPath);
            PreviewLightingPrefab              = Resources.Load<GameObject>(filePathWithOutExtension);
        }

        DestroyPreviewLightingPrefabInstance();
        
        PreviewLightingPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(PreviewLightingPrefab);
    }

    private static void DestroyPreviewLightingPrefabInstance()
    {
        if (PreviewLightingPrefabInstance.OrNull() != null)
            DestroyImmediate(PreviewLightingPrefabInstance);
    }

    public static Sprite CaptureImage(GameObject pref, bool front, bool Ortho)
    {
        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        //RuntimePreviewGenerator.Padding                = 0.05f;

        if (front)
        {
            RuntimePreviewGenerator.PreviewDirection = new Vector3(-0.75f, -1, -1f);
        }
        else
        {
            RuntimePreviewGenerator.PreviewDirection = new Vector3(-0.75f, -1, 1f);
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
        
        Sprite result = SetTex(RuntimePreviewGenerator.GenerateModelPreview(temp.transform, Width, Height, false, true), temp, folderPath);

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