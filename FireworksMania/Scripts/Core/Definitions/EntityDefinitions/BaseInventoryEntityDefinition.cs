using FireworksMania.Core.Behaviors.Fireworks;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Definitions.EntityDefinitions
{
    public abstract class BaseInventoryEntityDefinition : BaseEntityDefinition
    {
        [Header("Inventory")]
        [SerializeField]
        [FormerlySerializedAs("ItemName")]
        private string _itemName  = "Untitled Entity Definition";

        [SerializeField]
        [FormerlySerializedAs("Icon")]
        private Sprite _icon;

        [Header("Type")]
        [SerializeField]
        [FormerlySerializedAs("EntityDefinitionType")]
        private EntityDefinitionType _entityDefinitionType;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (this.EntityDefinitionType == null)
            {
                Debug.LogError($"Missing '{nameof(EntityDefinitionType)}'", this);
                return;
            }
                
            if(this.PrefabGameObject == null)
            {
                Debug.LogError($"Missing '{nameof(PrefabGameObject)}'", this);
                return;
            }

            var baseFireworksBehavior = this.PrefabGameObject.GetComponent<BaseFireworkBehavior>();
            if(baseFireworksBehavior != null)
            {
                if (baseFireworksBehavior.EntityDefinition != this)
                    Debug.LogError($"EntityDefinition '{this.name}' have '{baseFireworksBehavior.gameObject.name}' as it's Prefab, but '{baseFireworksBehavior.gameObject.name}' doesn't have '{this.name}' as it's '{nameof(BaseEntityDefinition)}' - this mismatch need to be fixed!", this);
            }
        }

        public string ItemName                           => _itemName;
        public Sprite Icon                               => _icon;
        public EntityDefinitionType EntityDefinitionType => _entityDefinitionType;
    }
}
