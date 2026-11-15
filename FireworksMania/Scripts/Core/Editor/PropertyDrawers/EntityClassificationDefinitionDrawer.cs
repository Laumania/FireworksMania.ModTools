using FireworksMania.Core.Definitions.EntityDefinitions;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.PropertyDrawers
{
    //Every diameter, caliber or class field shows an error box under it while it is empty (#1497), in the game and in
    //the Mod Tools. A drawer for the type rather than an attribute such as NaughtyAttributes' [Required]: the firework
    //behaviors are NetworkBehaviours, so Netcode's own NetworkBehaviourEditor draws them and NaughtyInspector never
    //does. That editor still draws every field with PropertyField, which is what reaches a property drawer.
    [CustomPropertyDrawer(typeof(EntityClassificationDefinition), true)]
    internal class EntityClassificationDefinitionDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (IsMissing(property))
                height += Spacing + HelpBoxHeight(EditorGUIUtility.currentViewWidth - 40f);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            //ObjectField, not PropertyField: PropertyField would come straight back to this drawer.
            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(fieldRect, property, FieldType(), label);

            if (IsMissing(property))
            {
                var boxRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, HelpBoxHeight(position.width));
                EditorGUI.HelpBox(boxRect, Message(), MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        private static bool IsMissing(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference &&
                   property.hasMultipleDifferentValues == false &&
                   property.objectReferenceValue == null;
        }

        private System.Type FieldType()
        {
            return fieldInfo != null && typeof(EntityClassificationDefinition).IsAssignableFrom(fieldInfo.FieldType)
                ? fieldInfo.FieldType
                : typeof(EntityClassificationDefinition);
        }

        private float HelpBoxHeight(float width)
        {
            return Mathf.Max(EditorGUIUtility.singleLineHeight * 2f, EditorStyles.helpBox.CalcHeight(new GUIContent(Message()), width));
        }

        private string Message()
        {
            var type = FieldType();

            if (type == typeof(EntityClassDefinition))
                return "No class set. Pick F2, F3 or F4: without one the inventory and Shop list this firework under no class, and the Mod Tools won't build the mod.";

            if (type == typeof(EntityCaliberDefinition))
                return "No caliber set. Pick one: without it the inventory and Shop list this firework under no caliber, and the Mod Tools won't build the mod.";

            if (type == typeof(EntityDiameterDefinition))
                return "No diameter set. Shells, mortars and mortar tubes need one to work.";

            return "Not set.";
        }
    }
}
