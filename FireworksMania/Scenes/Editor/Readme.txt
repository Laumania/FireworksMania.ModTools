To use the PrefabEditorScene as the default scene when editing a prefab in Unity, do the following:

In Unity -> Edit -> Project Settings -> Editor -> Prefab Mode -> Editing Environment -> Regular Environment
Either drag in the PrefabEditorScene to this field, or click the little circle and select it by seraching for it.

Once you done that all prefabs in your project will be edited in this scene.

The first time you open the scene, you might be prompted with a TextMesh Pro popup - just select to import stuff as its needed for TextMeshPro to work.

If the lighting seems a bit bright, its most likely because you havent set the Color Space as the game has it.
In Unity -> Edit -> Project Settings -> Player -> Other Settings -> Rendering -> Color Space -> Select "Linear" in the dropdown. Unity will do some magic which takes a while and then it should all look better. 