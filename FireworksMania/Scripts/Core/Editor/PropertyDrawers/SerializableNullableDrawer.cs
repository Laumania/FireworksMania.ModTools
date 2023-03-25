using FireworksMania.Core.Common;
using UnityEditor;
using UnityEngine;

namespace FireworksMania.Core.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(SerializableNullable<>))]
    internal class SerializableNullableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var setRect = new Rect(position.x, position.y, 15, position.height);
            var consumed = setRect.width + 5;
            var valueRect = new Rect(position.x + consumed, position.y, position.width - consumed, position.height);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            var hasValueProp = property.FindPropertyRelative("hasValue");
            EditorGUI.PropertyField(setRect, hasValueProp, GUIContent.none);
            bool guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && hasValueProp.boolValue;
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("v"), GUIContent.none);
            GUI.enabled = guiEnabled;

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
