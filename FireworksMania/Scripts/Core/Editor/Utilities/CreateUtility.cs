using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class CreateModTemplatesUtility
    {
        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Mortar 3 Inch Template", priority = 1)]
        public static void CreateMortarTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Mortar_3inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Mortar Rack 6 Inch Template", priority = 1)]
        public static void CreateMortarRackTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Mortar_6inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Cake Template", priority = 1)]
        public static void CreateCakeTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Cake_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Firecracker Template", priority = 1)]
        public static void CreateFirecrackerTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Firecracker_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Fountains Template", priority = 1)]
        public static void CreateFountainsTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Fountain_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/PreloadedTube Template", priority = 1)]
        public static void CreatePreloadedTubeTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/PreloadedTube_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Rocket Template", priority = 1)]
        public static void CreateRocketTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Rocket_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Roman Candle Template", priority = 1)]
        public static void CreateRomanCandleTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/RomanCandle_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Single Shot Rack Template", priority = 1)]
        public static void CreateSingleShotRackTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/SingleShotRack_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Smoke Bomb Template", priority = 1)]
        public static void CreateSmokeBombTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/SmokeBomb_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Whistler Template", priority = 1)]
        public static void CreateWhistlerTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Whistler_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Zipper Template", priority = 1)]
        public static void CreateZipperTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Zipper_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Shell 3 Inch Template", priority = 1)]
        public static void CreateShell3InchTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Shell_3inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fireworks/Shell 6 Inch Template", priority = 1)]
        public static void CreateShell6InchTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/Shell_6inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Parts/Unwrapped Shell Fuse Template", priority = 1)]
        public static void CreateUnwrappedShellFuseTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("Editor/ModSamples/Prefabs/UnwrappedShellFuse_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        private static void ConvertToTemplate(GameObject prefabInstance)
        {
            PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        }

        // Eyes sit behind the camera position, so the camera doesn't clip into the face/eye meshes
        private const float EyeForwardOffset           = 0.07f;
        // Head bone typically sits at the base of the skull; eye level is ~6% of body height above it
        private const float EyeLevelAboveHeadBoneRatio = 0.06f;
        // Approximate distance from the head bone to the face surface when no eye transforms exist
        private const float EstimatedFaceDepthOffset   = 0.10f;
        // Eye candidates further away from the head bone than this are considered bogus
        private const float MaxEyeDistanceFromHeadBone = 0.5f;
        private const float DefaultCharacterHeight     = 1.8f;

        private static readonly string[] KnownCharacterPrefabPrefixes = { "Character_", "SM_Chr_", "Chr_" };

        [MenuItem("Assets/Fireworks Mania/Generate CharacterDefinition from Prefab", true, priority = 10)]
        private static bool ValidateGenerateCharacterDefinitionFromPrefab()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;

            foreach (var selectedGameObject in Selection.gameObjects)
            {
                if (!IsEditablePrefabAsset(selectedGameObject))
                    return false;
            }

            return true;
        }

        [MenuItem("Assets/Fireworks Mania/Generate CharacterDefinition from Prefab", false, priority = 10)]
        private static void GenerateCharacterDefinitionFromPrefabMenu()
        {
            GenerateCharacterDefinitionFromPrefabs(Selection.gameObjects);
        }

        public static void GenerateCharacterDefinitionFromPrefabs(GameObject[] selectedGameObjects)
        {
            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                Debug.LogWarning("No prefabs selected");
                return;
            }

            if (!GenerateSpriteFromPrefabAssetUtility.InstansiatePreviewLightingPrefab())
                return;

            try
            {
                foreach (var selectedGameObject in selectedGameObjects)
                {
                    try
                    {
                        GenerateCharacterDefinitionFromPrefab(selectedGameObject);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to generate CharacterDefinition for '{selectedGameObject.name}': {e.Message}");
                    }
                }
            }
            finally
            {
                GenerateSpriteFromPrefabAssetUtility.DestroyPreviewLightingPrefabInstance();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateCharacterDefinitionFromPrefab(GameObject prefabAsset)
        {
            if (!IsEditablePrefabAsset(prefabAsset))
            {
                Debug.LogWarning($"Skipping '{prefabAsset.name}' as it's not an editable prefab asset (scene instances are not supported, select the prefab in the Project window)");
                return;
            }

            var animator = prefabAsset.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"Skipping '{prefabAsset.name}' as it doesn't contain an Animator");
                return;
            }

            var avatar = animator.avatar;
            if (avatar == null)
            {
                Debug.LogWarning($"Skipping '{prefabAsset.name}' as its Animator doesn't have an Avatar");
                return;
            }

            var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

            if (!TryAddCharacterCameraPositionToPrefab(prefabPath))
                return;

            var icon = GenerateSpriteFromPrefabAssetUtility.GenerateCharacterPreviewImageAsset(prefabAsset);
            if (icon == null)
                Debug.LogWarning($"Failed to generate icon for '{prefabAsset.name}'");

            CreateOrUpdateCharacterDefinition(prefabAsset, avatar, icon, prefabPath);
        }

        private static bool TryAddCharacterCameraPositionToPrefab(string prefabPath)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var animator = prefabRoot.GetComponentInChildren<Animator>(true);
                var headBone = FindHeadBone(animator, prefabRoot);
                if (headBone == null)
                {
                    Debug.LogWarning($"Skipping '{prefabRoot.name}' as no head could be found via its Avatar");
                    return false;
                }

                var cameraPosition = prefabRoot.GetComponentInChildren<CharacterCameraPosition>(true);
                if (cameraPosition == null)
                {
                    var cameraPositionPrefab = Resources.Load<GameObject>(CreatePartsUtility.CharacterCameraPositionPrefabResourcePath);
                    if (cameraPositionPrefab == null)
                    {
                        Debug.LogError($"Unable to load '{CreatePartsUtility.CharacterCameraPositionPrefabResourcePath}' from Resources");
                        return false;
                    }

                    // Instantiate under the prefab root so the instance faces the same forward direction as the character
                    var cameraPositionInstance = (GameObject)PrefabUtility.InstantiatePrefab(cameraPositionPrefab, prefabRoot.transform);
                    cameraPositionInstance.transform.localPosition = Vector3.zero;
                    cameraPositionInstance.transform.localRotation = Quaternion.identity;
                    cameraPosition = cameraPositionInstance.GetComponent<CharacterCameraPosition>();
                }

                var eyePosition = CalculateEyePosition(animator, prefabRoot, headBone);

                // Keep world orientation while moving under the head bone, so the camera keeps facing the character's forward direction
                if (cameraPosition.transform.parent != headBone)
                    cameraPosition.transform.SetParent(headBone, worldPositionStays: true);
                cameraPosition.transform.position = eyePosition;
                cameraPosition.transform.rotation = prefabRoot.transform.rotation;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool IsEditablePrefabAsset(GameObject gameObject)
        {
            if (gameObject == null || !EditorUtility.IsPersistent(gameObject))
                return false;

            var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
            return prefabAssetType != PrefabAssetType.NotAPrefab && prefabAssetType != PrefabAssetType.Model;
        }

        private static Transform FindHeadBone(Animator animator, GameObject prefabRoot)
        {
            Transform headBone = null;

            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
                headBone = animator.GetBoneTransform(HumanBodyBones.Head);

            if (headBone == null)
            {
                var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true);
                headBone = allTransforms.FirstOrDefault(t => t.name.Equals("head", StringComparison.OrdinalIgnoreCase))
                        ?? allTransforms.FirstOrDefault(t => t.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return headBone;
        }

        private static Vector3 CalculateEyePosition(Animator animator, GameObject prefabRoot, Transform headBone)
        {
            var forward = prefabRoot.transform.forward;

            Transform leftEyeBone  = null;
            Transform rightEyeBone = null;

            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                leftEyeBone  = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                rightEyeBone = animator.GetBoneTransform(HumanBodyBones.RightEye);
            }

            if (leftEyeBone != null && rightEyeBone != null)
                return Vector3.Lerp(leftEyeBone.position, rightEyeBone.position, 0.5f) + forward * EyeForwardOffset;

            // Fallback: transforms under the head with "eye" in their name (e.g. an 'Eyes' mesh) mark the eye area
            var eyeTransforms = headBone.GetComponentsInChildren<Transform>(true)
                                        .Where(t => t.name.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0)
                                        .ToArray();
            if (eyeTransforms.Length > 0)
            {
                var eyeCenter = Vector3.zero;
                foreach (var eyeTransform in eyeTransforms)
                    eyeCenter += eyeTransform.position;
                eyeCenter /= eyeTransforms.Length;

                if (Vector3.Distance(eyeCenter, headBone.position) <= MaxEyeDistanceFromHeadBone)
                    return eyeCenter + forward * EyeForwardOffset;
            }

            // Last resort: estimate eye level from the head bone and the character's height
            var skinnedMesh     = prefabRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var characterHeight = skinnedMesh != null ? skinnedMesh.bounds.size.y : DefaultCharacterHeight;
            return headBone.position
                 + prefabRoot.transform.up * (characterHeight * EyeLevelAboveHeadBoneRatio)
                 + forward * EstimatedFaceDepthOffset;
        }

        private static void CreateOrUpdateCharacterDefinition(GameObject prefabAsset, Avatar avatar, Sprite icon, string prefabPath)
        {
            var prefabFileName = Path.GetFileNameWithoutExtension(prefabPath);
            var definitionPath = $"{Path.GetDirectoryName(prefabPath)}/{prefabFileName}.asset".Replace('\\', '/');

            var characterDefinition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(definitionPath);
            if (characterDefinition == null)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(definitionPath) != null)
                {
                    Debug.LogWarning($"Skipping CharacterDefinition for '{prefabFileName}' as '{definitionPath}' already contains an asset of another type");
                    return;
                }

                characterDefinition = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(characterDefinition, definitionPath);
            }

            var serializedDefinition = new SerializedObject(characterDefinition);
            serializedDefinition.FindProperty("_id").stringValue                       = prefabFileName;
            serializedDefinition.FindProperty("_name").stringValue                     = RemoveKnownCharacterPrefabPrefix(prefabFileName);
            serializedDefinition.FindProperty("_characterPrefab").objectReferenceValue = prefabAsset;
            serializedDefinition.FindProperty("_characterAvatar").objectReferenceValue = avatar;

            // Keep an already assigned icon if generation failed, so updating never wipes a working reference
            if (icon != null)
                serializedDefinition.FindProperty("_icon").objectReferenceValue = icon;

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(characterDefinition);

            Debug.Log($"Created/updated CharacterDefinition at: {definitionPath}");
        }

        private static string RemoveKnownCharacterPrefabPrefix(string prefabFileName)
        {
            foreach (var prefix in KnownCharacterPrefabPrefixes)
            {
                if (prefabFileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return prefabFileName.Substring(prefix.Length);
            }

            // Dash-separated category prefixes (e.g. 'Civil-Alex', 'Police-Bonnie') - strip the first segment
            var dashIndex = prefabFileName.IndexOf('-');
            if (dashIndex > 0 && dashIndex < prefabFileName.Length - 1)
                return prefabFileName.Substring(dashIndex + 1);

            return prefabFileName;
        }
    }

    public static class CreatePartsUtility
    {
        internal const string CharacterCameraPositionPrefabResourcePath = "Prefabs/CharacterCameraPositionPrefab";

        [MenuItem("GameObject/Fireworks Mania/Parts/Common/Standard Fuse Prefab", priority = 1)]
        public static void CreateStandardFuse(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild("Prefabs/Fireworks/Parts/FuseStandardPrefab", menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Parts/Mortar/Unwrapped Shell Fuse Pivot Position Prefab", priority = 1)]
        public static void CreateUnwrappedShellFusePivotPositionPrefab(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild("Prefabs/Fireworks/Parts/UnwrappedShellFusePivotPositionPrefab", menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Parts/Mortar/Mortar Top Prefab", priority = 1)]
        public static void CreateMortarTopPrefab(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild("Prefabs/Fireworks/Parts/MortarTubeTopPrefab", menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Parts/Mortar/Mortar Bottom Prefab", priority = 1)]
        public static void CreateMortarBottomPrefab(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild("Prefabs/Fireworks/Parts/MortarTubeBottomPrefab", menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Maps/Player Spawn Location Prefab", priority = 1)]
        public static void CreatePlayerSpawnLocationPrefab(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild("Prefabs/PlayerSpawnLocationPrefab", menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Parts/Character/Character Camera Position Prefab", priority = 1)]
        public static void CreateCharacterCameraPositionPrefab(MenuCommand menuCommand)
        {
            CreateUtility.CreatePrefabAsChild(CharacterCameraPositionPrefabResourcePath, menuCommand.context as GameObject);
        }
    }

    public static class CreateUtility
    {
        internal static GameObject CreatePrefabAsChild(string prefabPath, GameObject parent)
        {
            var gameObject = CreatePrefab(prefabPath);

            if (parent != null)
                GameObjectUtility.SetParentAndAlign(gameObject, parent);

            // Make sure we place the object in the proper scene, with a relevant name
            StageUtility.PlaceGameObjectInCurrentStage(gameObject);
            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);

            // Record undo, and select
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create Object: {gameObject.name}");
            Selection.activeGameObject = gameObject;

            // For prefabs, let's mark the scene as dirty for saving
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return gameObject;
        }

        internal static void CreatePrefabAndPlace(string path)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(Resources.Load(path)) as GameObject;
            Place(newObject);
        }

        internal static void CreateObjectAndPlace(string name, params Type[] types)
        {
            GameObject newObject = ObjectFactory.CreateGameObject(name, types);
            Place(newObject);
        }

        private static GameObject CreatePrefab(string path)
        {
            var resource = Resources.Load(path);

            if (resource == null)
                throw new UnityException($"Unable to load requested resource '{path}'");

            return PrefabUtility.InstantiatePrefab(resource) as GameObject;
        }

        private static void Place(GameObject gameObject)
        {
            // Find location
            SceneView lastView = SceneView.lastActiveSceneView;
            gameObject.transform.position = lastView ? lastView.pivot : Vector3.zero;

            // Make sure we place the object in the proper scene, with a relevant name
            StageUtility.PlaceGameObjectInCurrentStage(gameObject);
            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);

            // Record undo, and select
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create Object: {gameObject.name}");
            Selection.activeGameObject = gameObject;

            // For prefabs, let's mark the scene as dirty for saving
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Assets/Fireworks Mania/Create GameSoundDefinition from AudioClip", true, priority = 10)]
        private static bool ValidateCreateGameSoundDefinitionFromAudioClip()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
                return false;

            foreach (var obj in Selection.objects)
            {
                if (!(obj is AudioClip))
                    return false;
            }

            return true;
        }

        [MenuItem("Assets/Fireworks Mania/Create GameSoundDefinition from AudioClip", false, priority = 10)]
        private static void CreateGameSoundDefinitionFromAudioClipMenu()
        {
            CreateGameSoundDefinitionFromAudioClip(Selection.objects);
        }

        private static void CreateGameSoundDefinitionFromAudioClip(UnityEngine.Object[] selectedObjects)
        {
            // Collect all valid AudioClips
            var audioClips = new System.Collections.Generic.List<AudioClip>();
            foreach (var obj in selectedObjects)
            {
                var audioClip = obj as AudioClip;
                if (audioClip != null)
                {
                    audioClips.Add(audioClip);
                }
            }

            if (audioClips.Count == 0)
            {
                Debug.LogWarning("No valid AudioClips selected");
                return;
            }

            // Create the GameSoundDefinition asset
            var soundDefinition = ScriptableObject.CreateInstance<GameSoundDefinition>();
            soundDefinition.AudioVariationClips = audioClips.ToArray();
            
            // Use the first AudioClip's name for the definition name
            soundDefinition.name = audioClips[0].name;

            // Get the path based on the first selected AudioClip
            string audioClipPath = AssetDatabase.GetAssetPath(audioClips[0]);
            string directory = System.IO.Path.GetDirectoryName(audioClipPath);
            string fileName = soundDefinition.name;
            string newAssetPath = $"{directory}/{fileName}.asset";

            // Ensure unique path
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            // Create the asset
            AssetDatabase.CreateAsset(soundDefinition, newAssetPath);
            
            Debug.Log($"Created GameSoundDefinition with {audioClips.Count} variation(s) at: {newAssetPath}");

            // Save and refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
