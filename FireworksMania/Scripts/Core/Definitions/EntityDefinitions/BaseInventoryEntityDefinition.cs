using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Persistence;
using UnityEngine;
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
using UnityEngine.Localization;
#endif
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

        [Header("Brand Collection")]
        [Tooltip("Optional reference to Brand Collection this item is part of.")]
        [SerializeField]
        private BrandCollection _collectionDefinition;
        public BrandCollection? CollectionDefinition => _collectionDefinition;

#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
        //Base game only for now (#2901). The Mod Tools do not depend on com.unity.localization, so a LocalizedString
        //here would stop every mod project compiling - and a mod's own string table could not be loaded anyway. Comes
        //out of the define once the Mod Tools carry the package and the game can load mods' tables (#3178). Until
        //then it is set only on items authored in THIS project: the fireworks imported from the content project are
        //exported by a Core without the field, so a value set on them here is gone at the next import
        [Header("Localization")]
        [Tooltip("OPTIONAL flavour text shown under the item's name in the Shop's and the Inventory's details panes. Leave it unset and the panes show nothing - most items never have one. Only for items authored in this project: a firework imported from the content project loses it at its next import (#3178).")]
        [SerializeField]
        private LocalizedString _itemDescriptionLocalizedString = new LocalizedString();

        /// <summary>
        /// The raw reference, for UI that binds <c>StringChanged</c> rather than reading once - a pane left open
        /// across a language switch needs that. Check <see cref="HasDescription"/> first.
        /// </summary>
        public LocalizedString DescriptionLocalizedString => _itemDescriptionLocalizedString;

        /// <summary>False when no entry is assigned, which is the normal case.</summary>
        public bool HasDescription => _itemDescriptionLocalizedString != null && _itemDescriptionLocalizedString.IsEmpty == false;
#endif

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
