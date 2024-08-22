#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHidePropertyDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;
        if (enabled || !condHAtt.HideInInspector) {
            EditorGUI.PropertyField(position, property, label, true);
        }
        GUI.enabled = wasEnabled;
    }

    private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condAttr, SerializedProperty property) {
        bool enabled = true;
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(condAttr.ConditionalSourceField);

        if (sourcePropertyValue != null) {
            if (condAttr.EnumValue >= 0) // Handle enum value
            {
                enabled = sourcePropertyValue.enumValueIndex.Equals(condAttr.EnumValue);
            } else // Handle boolean value
              {
                enabled = sourcePropertyValue.boolValue;
            }
        } else {
            Debug.LogWarning("Unable to find the property with name: " + condAttr.ConditionalSourceField);
        }

        return enabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        ConditionalHideAttribute condAttr = (ConditionalHideAttribute)attribute;
        bool enabled = GetConditionalHideAttributeResult(condAttr, property);

        if (enabled || !condAttr.HideInInspector) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        return -EditorGUIUtility.standardVerticalSpacing; // Return a small negative number to reduce space when not visible
    }
}

#endif