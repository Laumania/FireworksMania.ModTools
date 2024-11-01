using FireworksMania.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            string valueStr = "(not supported)";
            
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = prop.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    valueStr = prop.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    valueStr = prop.floatValue.ToString("0.00000");
                    break;
                case SerializedPropertyType.String:
                    valueStr = prop.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (prop.boxedValue is ScriptableObject scriptableObject)
                    {
                        valueStr = $"{scriptableObject.name} ({scriptableObject.GetType().Name})";
                    }
                    break;
                default:
                    valueStr = "(not supported)";
                    break;
            }

            GUI.enabled = false;
            EditorGUI.TextField(position, label, valueStr);
            GUI.enabled = true;
        }
    }
}
