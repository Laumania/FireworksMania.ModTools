using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Persistence;
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    base.OnValidate();

                    if (this.EntityDefinitionType == null)
                    {
                        Debug.LogError($"Missing '{nameof(EntityDefinitionType)}' on '{this.name}'", this);
                        return;
                    }

                    if (this.PrefabGameObject == null)
                    {
                        Debug.LogError($"Missing '{nameof(PrefabGameObject)}' on '{this.name}'", this);
                        return;
                    }

                    //var componentsWithEntityDefinition = this.PrefabGameObject.GetComponents<IHaveBaseEntityDefinition>();
                    //foreach (var haveBaseEntityDefinition in componentsWithEntityDefinition)
                    //{
                    //    if (haveBaseEntityDefinition != null && haveBaseEntityDefinition.EntityDefinition != this)
                    //    {
                    //        haveBaseEntityDefinition.EntityDefinition = this;

                    //        Debug.Log($"Changed '{nameof(haveBaseEntityDefinition.EntityDefinition)}' on '{this.PrefabGameObject.gameObject.name}' to '{this.name}'", this.PrefabGameObject.gameObject);

                    //        UnityEditor.EditorUtility.SetDirty(this.PrefabGameObject.gameObject);
                    //        UnityEditor.EditorUtility.SetDirty(this);
                    //    }
                    //}
                }
            };
#endif
        }

        public string ItemName                           => _itemName;
        public Sprite Icon                               => _icon;
        public EntityDefinitionType EntityDefinitionType => _entityDefinitionType;
    }
}
