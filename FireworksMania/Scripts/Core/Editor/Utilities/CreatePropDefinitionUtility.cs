using System;
using System.IO;
using System.Text;
using FireworksMania.Core.Behaviors;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.Utilities
{
    /// <summary>
    /// Turns a plain art prefab into a usable in-game PROP and gives it a
    /// <see cref="PropEntityDefinition"/>, the way
    /// <c>Generate CharacterDefinition from Prefab</c> does for characters.
    ///
    /// It does the boring, identical-every-time half of prop authoring: the physics and netcode
    /// components every prop carries, an icon rendered with the project's own preview rig, and the
    /// definition asset wired to the prefab. What it CANNOT know — a sensible mass for a hollow
    /// thing, the right impact sound, whether the collider should really be a box — is left at a
    /// derived default for a human to correct, and it never overwrites a value that is already set.
    ///
    /// Re-runnable on purpose: run it again after changing the mesh and it refreshes the icon and
    /// definition without duplicating components or resetting what you tuned by hand.
    ///
    /// Lives in Core.Editor so it ships in the Mod Tools package — a modder authoring a prop gets
    /// the same one-click setup. It deliberately does NOT touch
    /// <c>BaseGameEntityDefinitionCollection</c>: that asset is base-game content and a mod must
    /// never be registered into it.
    /// </summary>
    public static class CreatePropDefinitionUtility
    {
        private const string MenuPath = "Assets/Fireworks Mania/Generate PropDefinition from Prefab";

        private const string PropEntityDefinitionTypePath =
            "Assets/FireworksMania/Configuration/Definitions/EntityDefinitionTypes/Prop.asset";

        //The game ships exactly two general impact sounds, so the tool picks by size and the author
        //swaps in something specific by hand if the prop deserves it.
        private const string ImpactSoundMedium = "SFX_Impact_GeneralMedium";
        private const string ImpactSoundLarge  = "SFX_Impact_GeneralLarge";

        /// <summary>Longest bounds edge, in metres, at or above which a prop sounds "large".</summary>
        private const float LargePropSizeThreshold = 1.5f;

        //Bounding-box volume badly overestimates a sparse shape (a light-up tree is mostly air), so
        //the derived mass is clamped hard at both ends rather than trusted.
        private const float MassPerCubicMetre = 10f;
        private const float MinMass           = 1f;
        private const float MaxMass           = 30f;

        private static readonly string[] KnownPropPrefabPrefixes = { "SM_Prop_", "SM_Bld_", "SM_", "Prop_" };

        [MenuItem(MenuPath, true, priority = 10)]
        private static bool ValidateGeneratePropDefinitionFromPrefab()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return false;

            foreach (var selected in Selection.gameObjects)
            {
                if (IsEditablePrefabAsset(selected) == false)
                    return false;
            }

            return true;
        }

        [MenuItem(MenuPath, false, priority = 10)]
        private static void GeneratePropDefinitionFromPrefabMenu()
        {
            GeneratePropDefinitionFromPrefabs(Selection.gameObjects);
        }

        public static void GeneratePropDefinitionFromPrefabs(GameObject[] selectedGameObjects)
        {
            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                Debug.LogWarning("No prefabs selected");
                return;
            }

            //The preview rig is expensive to stand up, so it is created once for the whole batch -
            //the character generator does the same.
            if (GenerateSpriteFromPrefabAssetUtility.InstansiatePreviewLightingPrefab() == false)
                return;

            var report = new StringBuilder();
            var done   = 0;

            try
            {
                foreach (var selected in selectedGameObjects)
                {
                    try
                    {
                        if (GeneratePropDefinitionFromPrefab(selected, report))
                            done++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to generate PropDefinition for '{selected.name}': {e.Message}");
                    }
                }
            }
            finally
            {
                GenerateSpriteFromPrefabAssetUtility.DestroyPreviewLightingPrefabInstance();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Props] Generated {done}/{selectedGameObjects.Length} prop(s):\n{report}");
        }

        private static bool GeneratePropDefinitionFromPrefab(GameObject prefabAsset, StringBuilder report)
        {
            if (IsEditablePrefabAsset(prefabAsset) == false)
            {
                Debug.LogWarning($"Skipping '{prefabAsset.name}' as it's not an editable prefab asset (select the prefab in the Project window, not a scene instance)");
                return false;
            }

            var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

            var added = ConvertPrefabToProp(prefabPath);

            //Reload: ConvertPrefabToProp saved a new version, so the passed reference is stale.
            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            var icon = GenerateSpriteFromPrefabAssetUtility.CaptureImage(prefabAsset, true, true);
            if (icon == null)
                Debug.LogWarning($"Failed to generate icon for '{prefabAsset.name}' - the definition keeps whatever icon it already had");

            var definition = CreateOrUpdatePropDefinition(prefabAsset, icon, prefabPath);

            //The prefab and the definition reference EACH OTHER, and this is the half that is easy
            //to miss: SaveableEntity needs to know which entity it is, or a blueprint cannot
            //restore the prop. It runs last because the definition has to exist first.
            if (definition != null)
                LinkSaveableEntity(prefabPath, definition);

            report.Append("  ").Append(prefabAsset.name)
                  .Append("  added:[").Append(added).Append(']')
                  .Append(icon != null ? "  icon:ok" : "  icon:FAILED")
                  .AppendLine();

            return true;
        }

        // ── Prefab conversion ─────────────────────────────────────────────

        /// <summary>
        /// Adds the components every prop carries. Idempotent: an existing component is left exactly
        /// as it is, so re-running never resets a mass or a sound someone tuned.
        /// </summary>
        private static string ConvertPrefabToProp(string prefabPath)
        {
            var root  = PrefabUtility.LoadPrefabContents(prefabPath);
            var added = new StringBuilder();

            try
            {
                EnsureCollider(root, added);

                //Order matters for Unity's RequireComponent graph: the body and the NetworkObject
                //have to exist before the netcode components that depend on them.
                var rigidbody = root.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = root.AddComponent<Rigidbody>();
                    rigidbody.mass                 = DeriveMass(root);
                    rigidbody.linearDamping        = 0.05f;
                    rigidbody.angularDamping       = 0.05f;
                    rigidbody.useGravity           = true;
                    rigidbody.interpolation        = RigidbodyInterpolation.None;
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    added.Append("Rigidbody(").Append(rigidbody.mass.ToString("0.#")).Append("kg) ");
                }

                Ensure<NetworkObject>(root, added);
                Ensure<ClientNetworkTransform>(root, added);
                Ensure<NetworkRigidbody>(root, added);
                //SaveableEntity alone: SaveableTransformComponent and SaveableRigidbodyComponent are
                //[Obsolete] - it handles that state itself now. Older props still carry them.
                Ensure<SaveableEntity>(root, added);
                Ensure<ErasableBehavior>(root, added);

                var impact = root.GetComponent<PlaySoundOnImpactBehavior>();
                if (impact == null)
                {
                    impact = root.AddComponent<PlaySoundOnImpactBehavior>();
                    var sound = PickImpactSound(root);
                    //Serialized-private on purpose, so it goes through SerializedObject.
                    using (var so = new SerializedObject(impact))
                    {
                        so.FindProperty("_sound").stringValue = sound;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    added.Append("PlaySoundOnImpactBehavior(").Append(sound).Append(") ");
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return added.Length == 0 ? "nothing (already a prop)" : added.ToString().TrimEnd();
        }

        private static void Ensure<T>(GameObject root, StringBuilder added) where T : Component
        {
            if (root.GetComponent<T>() != null)
                return;

            root.AddComponent<T>();
            added.Append(typeof(T).Name).Append(' ');
        }

        /// <summary>
        /// A Rigidbody cannot use a concave MeshCollider, and art prefabs routinely ship one. Flip
        /// an existing MeshCollider to convex rather than replacing it — the mesh is usually a
        /// purpose-built low-poly collision hull. Only when there is no collider at all does this
        /// fall back to a box around the renderer bounds.
        /// </summary>
        private static void EnsureCollider(GameObject root, StringBuilder added)
        {
            var meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                if (meshCollider.convex == false)
                {
                    meshCollider.convex = true;
                    added.Append("MeshCollider->convex ");
                }
                return;
            }

            if (root.GetComponent<Collider>() != null)
                return;

            var bounds = TryGetLocalBounds(root);
            var box    = root.AddComponent<BoxCollider>();
            if (bounds.HasValue)
            {
                box.center = bounds.Value.center;
                box.size   = bounds.Value.size;
            }
            added.Append("BoxCollider ");
        }

        /// <summary>
        /// Longest edge rather than volume: a 9 m neon sign is a big thing to hit even though it is
        /// paper thin, and volume would call it small.
        /// </summary>
        private static string PickImpactSound(GameObject root)
        {
            var bounds = TryGetLocalBounds(root);
            if (bounds.HasValue == false)
                return ImpactSoundMedium;

            var size       = bounds.Value.size;
            var longestEdge = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

            return longestEdge >= LargePropSizeThreshold ? ImpactSoundLarge : ImpactSoundMedium;
        }

        private static float DeriveMass(GameObject root)
        {
            var bounds = TryGetLocalBounds(root);
            if (bounds.HasValue == false)
                return 5f;

            var size   = bounds.Value.size;
            var volume = Mathf.Max(size.x, 0.01f) * Mathf.Max(size.y, 0.01f) * Mathf.Max(size.z, 0.01f);

            return Mathf.Round(Mathf.Clamp(volume * MassPerCubicMetre, MinMass, MaxMass) * 10f) / 10f;
        }

        private static Bounds? TryGetLocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return null;

            //Renderer bounds are world-space; the prefab root sits at the origin with identity
            //rotation while it is open for editing, so they double as local here.
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        // ── Definition ────────────────────────────────────────────────────

        /// <summary>
        /// Points the prefab's <see cref="SaveableEntity"/> back at its definition. Done in a
        /// second prefab open, after the definition asset exists.
        /// </summary>
        private static void LinkSaveableEntity(string prefabPath, PropEntityDefinition definition)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var saveable = root.GetComponent<SaveableEntity>();
                if (saveable == null)
                    return;

                using (var so = new SerializedObject(saveable))
                {
                    var property = so.FindProperty("_entityDefinition");
                    if (property.objectReferenceValue == definition)
                        return;

                    property.objectReferenceValue = definition;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static PropEntityDefinition CreateOrUpdatePropDefinition(GameObject prefabAsset, Sprite icon, string prefabPath)
        {
            var prefabFileName = Path.GetFileNameWithoutExtension(prefabPath);
            var definitionPath = $"{Path.GetDirectoryName(prefabPath)}/{prefabFileName}.asset".Replace('\\', '/');

            var definition = AssetDatabase.LoadAssetAtPath<PropEntityDefinition>(definitionPath);
            if (definition == null)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(definitionPath) != null)
                {
                    Debug.LogWarning($"Skipping PropEntityDefinition for '{prefabFileName}' as '{definitionPath}' already contains an asset of another type");
                    return null;
                }

                definition = ScriptableObject.CreateInstance<PropEntityDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            var entityDefinitionType = AssetDatabase.LoadAssetAtPath<EntityDefinitionType>(PropEntityDefinitionTypePath);
            if (entityDefinitionType == null)
                Debug.LogWarning($"Could not find the Prop EntityDefinitionType at '{PropEntityDefinitionTypePath}' - set it by hand on '{prefabFileName}', or the item lands on no inventory tab");

            using (var so = new SerializedObject(definition))
            {
                so.FindProperty("_id").stringValue                        = prefabFileName;
                so.FindProperty("_prefabGameObject").objectReferenceValue = prefabAsset;

                //Only fill a name that is still the ScriptableObject default, so a hand-written one survives.
                var itemName = so.FindProperty("_itemName");
                if (string.IsNullOrWhiteSpace(itemName.stringValue) || itemName.stringValue == "Untitled Entity Definition")
                    itemName.stringValue = ToDisplayName(prefabFileName);

                if (entityDefinitionType != null)
                    so.FindProperty("_entityDefinitionType").objectReferenceValue = entityDefinitionType;

                //Keep an already assigned icon if generation failed, so updating never wipes a working reference.
                if (icon != null)
                    so.FindProperty("_icon").objectReferenceValue = icon;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(definition);

            return definition;
        }

        /// <summary>
        /// 'SM_Prop_Candy_Cane_01' becomes 'Candy Cane' — a starting point someone will improve.
        ///
        /// A trailing '_01' is dropped as art-pipeline noise, but '_02' and up are KEPT as a
        /// trailing number: those are separate props, not variants of one, and dropping them made
        /// 'Neon_Sleigh_01' and 'Neon_Sleigh_02' collide on the same name and id.
        /// </summary>
        public static string ToDisplayName(string prefabFileName)
        {
            var name = prefabFileName;

            foreach (var prefix in KnownPropPrefabPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            var parts   = name.Split('_');
            var text    = new StringBuilder();
            var trailing = string.Empty;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                if (int.TryParse(part, out var number))
                {
                    //Only a number in the LAST position is a variant marker; one in the middle is
                    //part of the name ('Text_2025_Sign').
                    if (i == parts.Length - 1)
                        trailing = number <= 1 ? string.Empty : " " + number;
                    else
                    {
                        if (text.Length > 0) text.Append(' ');
                        text.Append(part);
                    }
                    continue;
                }

                if (text.Length > 0)
                    text.Append(' ');
                text.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            }

            text.Append(trailing);

            return text.Length == 0 ? prefabFileName : text.ToString();
        }

        private static bool IsEditablePrefabAsset(GameObject gameObject)
        {
            if (gameObject == null || EditorUtility.IsPersistent(gameObject) == false)
                return false;

            var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
            return prefabAssetType != PrefabAssetType.NotAPrefab && prefabAssetType != PrefabAssetType.Model;
        }
    }
}
