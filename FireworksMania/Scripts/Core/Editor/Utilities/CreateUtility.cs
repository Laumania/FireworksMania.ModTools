using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    public static class CreateModTemplatesUtility
    {
        [MenuItem("GameObject/Fireworks Mania/Templates/Mortar 3 Inch Template", priority = 1)]
        public static void CreateMortarTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Mortar_3inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Mortar Rack 6 Inch Template", priority = 1)]
        public static void CreateMortarRackTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Mortar_6inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Cake Template", priority = 1)]
        public static void CreateCakeTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Cake_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Firecracker Template", priority = 1)]
        public static void CreateFirecrackerTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Firecracker_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Fountains Template", priority = 1)]
        public static void CreateFountainsTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Fountain_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/PreloadedTube Template", priority = 1)]
        public static void CreatePreloadedTubeTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/PreloadedTube_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Rocket Template", priority = 1)]
        public static void CreateRocketTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Rocket_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Roman Candle Template", priority = 1)]
        public static void CreateRomanCandleTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/RomanCandle_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Smoke Bomb Template", priority = 1)]
        public static void CreateSmokeBombTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/SmokeBomb_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Whistler Template", priority = 1)]
        public static void CreateWhistlerTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Whistler_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Zipper Template", priority = 1)]
        public static void CreateZipperTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Zipper_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Shell 3 Inch Template", priority = 1)]
        public static void CreateShell3InchTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Shell_3inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        [MenuItem("GameObject/Fireworks Mania/Templates/Shell 6 Inch Template", priority = 1)]
        public static void CreateShell6InchTemplate(MenuCommand menuCommand)
        {
            var gameObject = CreateUtility.CreatePrefabAsChild("ModSamples/Prefabs/Shell_6inch_Template_Prefab", menuCommand.context as GameObject);
            ConvertToTemplate(gameObject);
        }

        private static void ConvertToTemplate(GameObject prefabInstance)
        {
            PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        }
    }

    public static class CreatePartsUtility
    {
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
    }
}
