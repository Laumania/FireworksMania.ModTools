using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    public abstract class BaseEntityDefinition : ScriptableObject
    {
        private const string _defaultDefinitionIdValue = "INSERT UNIQUE DEFINITION ID";

        [Header("General")]
        [Tooltip("Global unique definition id for this entity. This is used to save/load this entity in Blueprints, so avoid changing this Id once it have been set, as it will break users Blueprints.")]
        [SerializeField]
        [FormerlySerializedAs("Id")]
        private string _id                                 = _defaultDefinitionIdValue;

        [Tooltip("The Prefab that represent this entity in the game. This is the prefab that will be spawned in the game.")]
        [SerializeField]
        [FormerlySerializedAs("PrefabGameObject")]
        private GameObject _prefabGameObject;

        protected virtual void OnValidate()
        {
            if (this._id == _defaultDefinitionIdValue)
            {
                Debug.LogError("Please update unique id to something unique", this);
            }
        }

        [ContextMenu("Set Id to filename")]
        private void SetIdToFilename()
        {
            this._id = this.name;
        }

        public string Id                   => _id;
        public GameObject PrefabGameObject => _prefabGameObject;
    }
}