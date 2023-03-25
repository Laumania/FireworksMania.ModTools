using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New StartupPrefab Definition", menuName = "Fireworks Mania/Definitions/StartupPrefab Definition")]
    public class StartupPrefabDefinition : ScriptableObject
    {
        [Tooltip("A single instance of the provided prefab will be instansiated in the map after all mods have been loaded. You can have scripts on this prefab to run custom logic on Start() and OnDestroy() for instance.")]
        [SerializeField]
        private GameObject _prefabGameObject;

        [Tooltip("StartupPrefabs are instansiated in the map sorted by the SortOrder, lowest number loaded first.")]
        [SerializeField]
        private int _sortOrder = 0;

        public GameObject PrefabGameObject => _prefabGameObject;
        public int SortOrder               => _sortOrder;
    }
}
