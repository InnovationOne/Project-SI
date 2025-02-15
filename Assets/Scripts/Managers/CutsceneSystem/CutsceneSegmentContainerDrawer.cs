using UnityEditor;
using UnityEngine;
using static CutsceneSegmentContainer;

[CustomPropertyDrawer(typeof(CutsceneSegmentContainer))]
public class CutsceneSegmentContainerDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        // Begin drawing the property.
        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = indent + 1;

        float y = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        Rect fieldRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

        // Draw the segment description.
        EditorGUI.PropertyField(fieldRect, property.FindPropertyRelative("segmentDescription"));
        y += lineHeight;

        // Draw the segment type dropdown.
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            property.FindPropertyRelative("segmentType"));
        y += lineHeight;

        // Draw the complete type dropdown.
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            property.FindPropertyRelative("completeType"));
        y += lineHeight;

        // Draw the segment start delay.
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            property.FindPropertyRelative("segmentStartDelay"));
        y += lineHeight;

        // Based on the selected segment type, display only the relevant fields.
        SerializedProperty segmentTypeProp = property.FindPropertyRelative("segmentType");
        SegmentTypes segType = (SegmentTypes)segmentTypeProp.enumValueIndex;

        switch (segType) {
            case SegmentTypes.MainNPCChat:
            case SegmentTypes.TextBubbleChat:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("dialogueText"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("dialogueSpeaker"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("inkJSON"));
                y += lineHeight;
                break;

            case SegmentTypes.SpawnCharacter:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("characterPrefab"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("spawnPoint"));
                y += lineHeight;
                break;

            case SegmentTypes.CharacterMove:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("targetPosition"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                    property.FindPropertyRelative("arrivalThreshold"));
                y += lineHeight;
                break;

            // Add additional cases for other segment types as needed.
            default:
                break;
        }

        // If the completion type is Timer, show the timer field.
        SerializedProperty completeTypeProp = property.FindPropertyRelative("completeType");
        SegmentCompleteTypes compType = (SegmentCompleteTypes)completeTypeProp.enumValueIndex;
        if (compType == SegmentCompleteTypes.Timer) {
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                property.FindPropertyRelative("segmentTimer"));
            y += lineHeight;
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    // Adjust the height so the inspector reserves enough space.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float height = EditorGUIUtility.singleLineHeight * 4; // Base: description, type, complete type, start delay.
        SerializedProperty segmentTypeProp = property.FindPropertyRelative("segmentType");
        SegmentTypes segType = (SegmentTypes)segmentTypeProp.enumValueIndex;
        switch (segType) {
            case SegmentTypes.MainNPCChat:
            case SegmentTypes.TextBubbleChat:
                height += EditorGUIUtility.singleLineHeight * 3; // dialogueText, dialogueSpeaker, inkJSON.
                break;
            case SegmentTypes.SpawnCharacter:
                height += EditorGUIUtility.singleLineHeight * 2; // characterPrefab, spawnPoint.
                break;
            case SegmentTypes.CharacterMove:
                height += EditorGUIUtility.singleLineHeight * 2; // targetPosition, arrivalThreshold.
                break;
            default:
                break;
        }
        SerializedProperty completeTypeProp = property.FindPropertyRelative("completeType");
        SegmentCompleteTypes compType = (SegmentCompleteTypes)completeTypeProp.enumValueIndex;
        if (compType == SegmentCompleteTypes.Timer) {
            height += EditorGUIUtility.singleLineHeight; // segmentTimer.
        }
        return height + EditorGUIUtility.standardVerticalSpacing;
    }
}
