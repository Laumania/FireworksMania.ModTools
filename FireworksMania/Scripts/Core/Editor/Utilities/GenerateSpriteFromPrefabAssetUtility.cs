#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor;
using System.Collections.Generic;
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
    private const string PostProcessProfileName             = "FM Default";
    // Must match RuntimePreviewGenerator.PREVIEW_LAYER - the layer the preview camera culls to
    private const int PreviewLayer                          = 22;
    // Fixed flat ambient for the preview scene, lifting the shadow sides that the directional
    // rig can't reach (previously icons borrowed this from whatever scene was open)
    private static readonly Color PreviewAmbientColor       = new Color(0.25f, 0.25f, 0.25f);
    // TAA accumulates over multiple frames - the preview warms up its history with this many renders
    // before the final captured frame, so a single-frame capture still gets the temporal smoothing
    private const int TemporalAAWarmupRenders               = 8;
    private static GameObject PreviewLightingPrefab         = null;
    private static GameObject PreviewLightingPrefabInstance = null;
    private static GameObject PreviewCameraInstance         = null;
    private static Camera     PreviewAlphaCamera            = null;
    private static PostProcessLayer   PreviewPostProcessLayer   = null;
    private static PostProcessProfile PreviewPostProcessProfile = null;

    // Icons must always render identically: lit only by the PreviewLightingPrefab, never by the
    // open scene's lights, ambient, fog, probes or reflections. So all preview rendering happens
    // in a throwaway empty scene; the user's scene setup is restored afterwards.
    private static SceneSetup[] PreviousSceneSetup          = null;


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
        // Capture the selection BEFORE swapping scenes - creating the preview scene clears Selection
        var selectedGameObjects = Selection.gameObjects;

        if (!InstansiatePreviewLightingPrefab())
            return;

        try
        {
            foreach (var selectedGameObjectPrefab in selectedGameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

                CaptureImage(selectedGameObjectPrefab, true, true);
            }
        }
        finally
        {
            DestroyPreviewLightingPrefabInstance();
        }
    }

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Character (Mug shot-style)")]
    public static void PrefabCharacter2PngOF()
    {
        // Capture the selection BEFORE swapping scenes - creating the preview scene clears Selection
        var selectedGameObjects = Selection.gameObjects;

        if (!InstansiatePreviewLightingPrefab())
            return;

        try
        {
            foreach (var selectedGameObjectPrefab in selectedGameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

                GenerateCharacterPreviewImageAsset(selectedGameObjectPrefab);
            }
        }
        finally
        {
            DestroyPreviewLightingPrefabInstance();
        }
    }

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Orthographic/Back View")]
    public static void Prefab2PngBF()
    {
        // Capture the selection BEFORE swapping scenes - creating the preview scene clears Selection
        var selectedGameObjects = Selection.gameObjects;

        if (!InstansiatePreviewLightingPrefab())
            return;

        try
        {
            foreach (var selectedGameObjectPrefab in selectedGameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

                CaptureImage(selectedGameObjectPrefab, false, true);
            }
        }
        finally
        {
            DestroyPreviewLightingPrefabInstance();
        }
    }

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Perspective/Front View")]
    public static void Prefab2PngPF()
    {
        // Capture the selection BEFORE swapping scenes - creating the preview scene clears Selection
        var selectedGameObjects = Selection.gameObjects;

        if (!InstansiatePreviewLightingPrefab())
            return;

        try
        {
            foreach (var selectedGameObjectPrefab in selectedGameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

                CaptureImage(selectedGameObjectPrefab, true, false);
            }
        }
        finally
        {
            DestroyPreviewLightingPrefabInstance();
        }
    }

    [MenuItem("Assets/Fireworks Mania/Generate Icon(s)/Perspective/Back View")]
    public static void Prefab2PngPB()
    {
        // Capture the selection BEFORE swapping scenes - creating the preview scene clears Selection
        var selectedGameObjects = Selection.gameObjects;

        if (!InstansiatePreviewLightingPrefab())
            return;

        try
        {
            foreach (var selectedGameObjectPrefab in selectedGameObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(selectedGameObjectPrefab) == PrefabAssetType.Model) return;

                CaptureImage(selectedGameObjectPrefab, false, false);
            }
        }
        finally
        {
            DestroyPreviewLightingPrefabInstance();
        }
    }

    public static Sprite GenerateCharacterPreviewImageAsset(GameObject pref)
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

        // Characters are photographed from +Z looking towards -Z
        OrientPreviewLighting(Vector3.back);

        Sprite result   = null;

        try
        {
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
                    RenderWithAlphaFix(() => RuntimePreviewGenerator.GenerateModelPreviewStrict(temp.transform, width, height, false, false, worldOffset, cameraRot, fov)),
                    temp, folderPath);
            }

            if (result == null)
            {
                // Fallback: full-body front view
                RuntimePreviewGenerator.PreviewDirection = new Vector3(0f, 0f, -1f);
                result = SetTex(RenderWithAlphaFix(() => RuntimePreviewGenerator.GenerateModelPreview(temp.transform, width, height, false, true)), temp, folderPath);
            }
        }
        finally
        {
            DestroyImmediate(temp);
        }

        return result;
    }

    private static Transform FindBoneContaining(Transform[] bones, string namePart)
    {
        if (bones == null)
            return null;
        return bones.FirstOrDefault(b => b != null && b.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // The lighting rig is authored for a camera at +Z looking towards -Z (character mug shots).
    // Rotate the rig to match the actual view direction so every icon type is lit the same way
    // relative to its camera - key light from behind the camera, fills from the sides.
    private static void OrientPreviewLighting(Vector3 viewDirection)
    {
        if (PreviewLightingPrefabInstance.OrNull() == null)
            return;

        PreviewLightingPrefabInstance.transform.rotation = Quaternion.LookRotation(-viewDirection.normalized, Vector3.up);
    }

    // Post-processing destroys the alpha channel of the render, so when the post-processing preview
    // camera is active the model is rendered a second time without it, purely to restore the alpha
    private static Texture2D RenderWithAlphaFix(Func<Texture2D> render)
    {
        // TAA needs several frames of history before the captured frame is anti-aliased
        if (RuntimePreviewGenerator.PreviewRenderCamera != null &&
            PreviewPostProcessLayer.OrNull() != null &&
            PreviewPostProcessLayer.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing)
        {
            PreviewPostProcessLayer.ResetHistory();
            for (int i = 0; i < TemporalAAWarmupRenders; i++)
            {
                var warmupTexture = render();
                if (warmupTexture != null)
                    DestroyImmediate(warmupTexture);
            }
        }

        var texture = render();

        if (RuntimePreviewGenerator.PreviewRenderCamera == null || PreviewAlphaCamera.OrNull() == null || texture == null)
            return texture;

        var previewCamera = RuntimePreviewGenerator.PreviewRenderCamera;

        // Both passes must frame identically, so the alpha camera mirrors the post camera's fov
        PreviewAlphaCamera.fieldOfView              = previewCamera.fieldOfView;
        RuntimePreviewGenerator.PreviewRenderCamera = PreviewAlphaCamera;
        try
        {
            var alphaTexture = render();
            if (alphaTexture == null)
                return texture;

            var pixels      = texture.GetPixels32();
            var alphaPixels = alphaTexture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i].a = alphaPixels[i].a;

            texture.SetPixels32(pixels);
            texture.Apply(false);

            DestroyImmediate(alphaTexture);
        }
        finally
        {
            RuntimePreviewGenerator.PreviewRenderCamera = previewCamera;
        }

        return texture;
    }

    /// <summary>
    /// Swaps to a throwaway empty scene with deterministic render settings, the PreviewLightingPrefab
    /// and a post-processing preview camera, so icons always render identically no matter which scene
    /// was open. Returns false if preview setup could not start (play mode, or the user cancelled
    /// saving modified scenes). DestroyPreviewLightingPrefabInstance restores the previous scene setup.
    /// </summary>
    internal static bool InstansiatePreviewLightingPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Icon generation is not supported in play mode");
            return false;
        }

        if (PreviewLightingPrefab.OrNull() == null)
        {
            var previewLightingPrefabAssetGuid = AssetDatabase.FindAssets(PreviewLightingPrefabName).FirstOrDefault();
            var previewLightingPrefabPath      = AssetDatabase.GUIDToAssetPath(previewLightingPrefabAssetGuid);
            var filePathWithOutExtension       = Path.GetFileNameWithoutExtension(previewLightingPrefabPath);
            PreviewLightingPrefab              = Resources.Load<GameObject>(filePathWithOutExtension);
        }

        DestroyPreviewLightingPrefabInstance();

        // Give the user the chance to save unsaved scene changes before we swap scenes
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Icon generation cancelled");
            return false;
        }

        PreviousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Deterministic render settings for the throwaway scene - directional lighting comes solely
        // from the PreviewLightingPrefab, plus a fixed neutral ambient that lifts shadow sides the
        // same way scene ambient used to. No restore needed, the scene is discarded afterwards.
        RenderSettings.ambientMode         = AmbientMode.Flat;
        RenderSettings.ambientLight        = PreviewAmbientColor;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog                 = false;

        // The ambient probe shaders sample from is updated lazily - without this the render
        // would still use the previous scene's ambient
        DynamicGI.UpdateEnvironment();

        PreviewLightingPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(PreviewLightingPrefab);

        DestroyLeakedInternalPreviewCameras();
        CreatePreviewPostProcessingCamera();

        return true;
    }

    // RuntimePreviewGenerator's internal camera is HideAndDontSave, so it both leaks one instance per
    // domain reload and keeps the fieldOfView of previous renders (its setup save/restore doesn't
    // cover fov). Destroying stale instances makes it recreate a fresh camera with default settings.
    private static void DestroyLeakedInternalPreviewCameras()
    {
        foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (camera.gameObject.name == "ModelPreviewGeneratorCamera" && !EditorUtility.IsPersistent(camera.gameObject))
                DestroyImmediate(camera.gameObject);
        }
    }

    internal static void DestroyPreviewLightingPrefabInstance()
    {
        RuntimePreviewGenerator.PreviewRenderCamera = null;
        PreviewAlphaCamera                          = null;
        PreviewPostProcessLayer                     = null;

        if (PreviewPostProcessProfile.OrNull() != null)
        {
            DestroyImmediate(PreviewPostProcessProfile);
            PreviewPostProcessProfile = null;
        }

        if (PreviewCameraInstance.OrNull() != null)
            DestroyImmediate(PreviewCameraInstance);

        if (PreviewLightingPrefabInstance.OrNull() != null)
            DestroyImmediate(PreviewLightingPrefabInstance);

        if (PreviousSceneSetup != null)
        {
            if (PreviousSceneSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(PreviousSceneSetup);
            else
                // All previously open scenes were untitled and couldn't be restored by path
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            PreviousSceneSetup = null;
        }
    }

    // Renders previews through a camera with the game's "FM Default" post-processing profile, so
    // icons match the in-game look. RuntimePreviewGenerator uses this camera instead of its own.
    private static void CreatePreviewPostProcessingCamera()
    {
        var profileGuid = AssetDatabase.FindAssets($"{PostProcessProfileName} t:{nameof(PostProcessProfile)}").FirstOrDefault();
        var profile     = string.IsNullOrEmpty(profileGuid) ? null : AssetDatabase.LoadAssetAtPath<PostProcessProfile>(AssetDatabase.GUIDToAssetPath(profileGuid));
        if (profile == null)
        {
            Debug.LogWarning($"Post-processing profile '{PostProcessProfileName}' not found - generating icons without post-processing");
            return;
        }

        var resourcesGuid         = AssetDatabase.FindAssets($"t:{nameof(PostProcessResources)}").FirstOrDefault();
        var postProcessResources  = string.IsNullOrEmpty(resourcesGuid) ? null : AssetDatabase.LoadAssetAtPath<PostProcessResources>(AssetDatabase.GUIDToAssetPath(resourcesGuid));
        if (postProcessResources == null)
        {
            Debug.LogWarning("PostProcessResources not found - generating icons without post-processing");
            return;
        }

        PreviewCameraInstance = new GameObject("PreviewPostProcessingCamera");

        var previewCamera      = PreviewCameraInstance.AddComponent<Camera>();
        previewCamera.enabled  = false; // Rendered manually by RuntimePreviewGenerator
        previewCamera.allowHDR = true;

        var postProcessLayer           = PreviewCameraInstance.AddComponent<PostProcessLayer>();
        postProcessLayer.Init(postProcessResources);
        postProcessLayer.volumeTrigger = PreviewCameraInstance.transform;
        postProcessLayer.volumeLayer   = 1 << PreviewLayer;

        // Same anti-aliasing as the Post-process Layer on the MainCamera in the MapEssentials scene,
        // so icons get the in-game look
        postProcessLayer.antialiasingMode                      = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        postProcessLayer.temporalAntialiasing.jitterSpread     = 0.75f;
        postProcessLayer.temporalAntialiasing.stationaryBlending = 0.95f;
        postProcessLayer.temporalAntialiasing.motionBlending   = 0.85f;
        postProcessLayer.temporalAntialiasing.sharpness        = 0.25f;
        PreviewPostProcessLayer                                = postProcessLayer;

        var volumeGameObject   = new GameObject("PreviewPostProcessingVolume");
        volumeGameObject.transform.SetParent(PreviewCameraInstance.transform, false);
        volumeGameObject.layer = PreviewLayer;

        // Use a stripped copy of the profile: these effects can't work in a single-frame icon render.
        // AutoExposure adapts over many frames, MotionBlur needs motion history, Grain is time-seeded
        // noise (icons would differ every run), DepthOfField is tuned for the gameplay camera and
        // AmbientOcclusion is scale-dependent - on a small object filling the frame it renders as
        // dirty halos/outlines around geometry.
        PreviewPostProcessProfile           = Instantiate(profile);
        PreviewPostProcessProfile.hideFlags = HideFlags.HideAndDontSave;
        RemoveProfileSettings<AutoExposure>(PreviewPostProcessProfile);
        RemoveProfileSettings<MotionBlur>(PreviewPostProcessProfile);
        RemoveProfileSettings<Grain>(PreviewPostProcessProfile);
        RemoveProfileSettings<DepthOfField>(PreviewPostProcessProfile);
        RemoveProfileSettings<AmbientOcclusion>(PreviewPostProcessProfile);

        var volume           = volumeGameObject.AddComponent<PostProcessVolume>();
        volume.isGlobal      = true;
        volume.sharedProfile = PreviewPostProcessProfile;

        // Second camera without post-processing for the alpha pass. It must be a fresh camera with
        // the same settings as the post camera: RuntimePreviewGenerator's internal fallback camera
        // keeps the field of view of previous renders (its setup save/restore doesn't cover fov),
        // which frames the two passes differently and makes the icon appear shifted/off-center.
        var alphaCameraGameObject = new GameObject("PreviewAlphaCamera");
        alphaCameraGameObject.transform.SetParent(PreviewCameraInstance.transform, false);

        PreviewAlphaCamera          = alphaCameraGameObject.AddComponent<Camera>();
        PreviewAlphaCamera.enabled  = false;
        PreviewAlphaCamera.allowHDR = true;

        RuntimePreviewGenerator.PreviewRenderCamera = previewCamera;
    }

    private static void RemoveProfileSettings<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
    {
        if (profile.HasSettings<T>())
            profile.RemoveSettings<T>();
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

        OrientPreviewLighting(RuntimePreviewGenerator.PreviewDirection);

        Sprite result = null;
        try
        {
            var texture = RenderWithAlphaFix(() => RuntimePreviewGenerator.GenerateModelPreview(temp.transform, Width, Height, false, true));

            // The generator's auto-fit can leave the model off-center in the frame
            if (texture != null)
                CenterContentByAlpha(texture);

            result = SetTex(texture, temp, folderPath);
        }
        finally
        {
            DestroyImmediate(temp);
        }

        return result;
    }

    // Shifts the rendered content so the bounding box of its opaque pixels sits in the middle of
    // the image. Only used for the auto-framed firework views - character mug shots and the
    // scene-view captures are composed deliberately and must not be recentered.
    private static void CenterContentByAlpha(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        int width  = texture.width;
        int height = texture.height;

        int minX = width, maxX = -1, minY = height, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a > 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < 0)
            return; // Fully transparent

        int shiftX = (width  - 1 - maxX - minX) / 2;
        int shiftY = (height - 1 - maxY - minY) / 2;
        if (shiftX == 0 && shiftY == 0)
            return;

        var shifted = new Color32[pixels.Length]; // Defaults to fully transparent
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                shifted[(y + shiftY) * width + (x + shiftX)] = pixels[y * width + x];
            }
        }

        texture.SetPixels32(shifted);
        texture.Apply(false);
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