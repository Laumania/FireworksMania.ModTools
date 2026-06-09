#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Threading;
using FireworksMania.Core.Utilities;
using FireworksMania.Core.Common;
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


    [MenuItem("GameObject/Fireworks Mania/Generate Icon(s)/Perspective/From Scene View (Keep object in focus)")]
    public static void PrefabToPngSceneViewKeepObjectInFrame()
    {
        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;
        RuntimePreviewGenerator.PreviewDirection       = SceneView.lastActiveSceneView.camera.transform.forward;

        var prefabpath     = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.activeGameObject);
        var prefabFileName = Path.GetFileName(prefabpath);
        var path           = prefabpath.Replace(prefabFileName, string.Empty);

        SetTex(RuntimePreviewGenerator.GenerateModelPreview(Selection.activeGameObject.transform, Width, Height, false, true), Selection.activeGameObject, path);
    }

    [MenuItem("GameObject/Fireworks Mania/Generate Icon(s)/Perspective/From Scene View (Exactly as Scene View)")]
    public static void PrefabToPngSceneViewPrecise()
    {
        var sceneCam   = SceneView.lastActiveSceneView.camera;
        var selectedObj = Selection.activeGameObject;

        // Compute exact world-space offset from the object root to the scene camera.
        // RuntimePreviewGenerator will recreate this relative positioning in the preview.
        Vector3   worldOffset  = sceneCam.transform.position - selectedObj.transform.position;
        Quaternion cameraRot   = sceneCam.transform.rotation;
        float      fov         = sceneCam.fieldOfView;

        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;

        var prefabpath     = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObj);
        var prefabFileName = Path.GetFileName(prefabpath);
        var path           = prefabpath.Replace(prefabFileName, string.Empty);

        SetTex(RuntimePreviewGenerator.GenerateModelPreviewStrict(selectedObj.transform, Width, Height, false, true, worldOffset, cameraRot, fov), selectedObj, path);
    }

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Orthographic/Front View")]
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

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Character (Mug shot-style)")]
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

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Orthographic/Back View")]
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

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Perspective/Front View")]
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

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Perspective/Back View")]
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
        int width  = 512;
        int height = 512;

        RuntimePreviewGenerator.BackgroundColor        = Color.clear;
        RuntimePreviewGenerator.MarkTextureNonReadable = false;
        RuntimePreviewGenerator.RenderSupersampling    = 2;
        RuntimePreviewGenerator.OrthographicMode       = false;

        string folderPath = AssetDatabase.GetAssetPath(pref);
        if (folderPath.Contains("."))
            folderPath = folderPath.Remove(folderPath.LastIndexOf('/'));

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(pref);

        Sprite result   = null;

        // Prefer CharacterCameraPosition for accurate eye-level framing — it is placed
        // at true eye height on almost all character prefabs.
        var charCamPos  = temp.GetComponentInChildren<CharacterCameraPosition>();
        var skinnedMesh = temp.GetComponentInChildren<SkinnedMeshRenderer>();

        float eyeRelY    = float.MinValue;
        // Default fallback height (metres) used when no SkinnedMeshRenderer is present.
        const float defaultCharacterHeight = 1.8f;
        float charHeight = skinnedMesh != null ? skinnedMesh.bounds.size.y : defaultCharacterHeight;

        if (charCamPos != null)
        {
            eyeRelY = charCamPos.transform.position.y - temp.transform.position.y;
        }
        else if (skinnedMesh != null)
        {
            // Secondary fallback: derive eye level from the head / neck bone
            var headBone = FindBoneContaining(skinnedMesh.bones, "head")
                        ?? FindBoneContaining(skinnedMesh.bones, "neck");

            if (headBone != null)
            {
                float headRelY = headBone.position.y - temp.transform.position.y;
                // Head bone typically sits at the base of the skull; this offset
                // approximates the remaining distance to eye level (~6 % of body height).
                const float eyeLevelOffsetRatio = 0.06f;
                eyeRelY = headRelY + charHeight * eyeLevelOffsetRatio;
            }
        }

        if (eyeRelY > float.MinValue)
        {
            // Frame height: head + neck + hint of upper chest (~30 % of body height)
            const float frameSizeRatio = 0.30f;
            float frameHeight  = charHeight * frameSizeRatio;
            // Place eye level at 10 % above the image centre so the portrait looks
            // like the target: top of skull near the top edge, chin below centre.
            float frameCenterY = eyeRelY - frameHeight * 0.10f;

            // Narrow field-of-view gives a telephoto / portrait feel
            const float fov   = 30f;
            float halfFovRad  = fov * 0.5f * Mathf.Deg2Rad;
            float camDistance = (frameHeight * 0.5f) / Mathf.Tan(halfFovRad);

            // Camera is placed in front of the character (+Z) at eye height.
            // Characters face +Z in their rest pose, so the camera looks in –Z.
            Vector3    worldOffset = new Vector3(0f, frameCenterY, camDistance);
            Quaternion cameraRot   = Quaternion.LookRotation(Vector3.back, Vector3.up);

            result = SetTex(
                RuntimePreviewGenerator.GenerateModelPreviewStrict(temp.transform, width, height, false, false, worldOffset, cameraRot, fov),
                temp, folderPath);
        }

        if (result == null)
        {
            // Fallback: full-body front view
            RuntimePreviewGenerator.PreviewDirection = new Vector3(0f, 0f, -1f);
            result = SetTex(RuntimePreviewGenerator.GenerateModelPreview(temp.transform, width, height, false, true), temp, folderPath);
        }

        DestroyImmediate(temp);
    }

    private static Transform FindBoneContaining(Transform[] bones, string namePart)
    {
        if (bones == null)
            return null;
        return bones.FirstOrDefault(b => b != null && b.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
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
        Png.name = $"Icon_{prefObject.name}"; 

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