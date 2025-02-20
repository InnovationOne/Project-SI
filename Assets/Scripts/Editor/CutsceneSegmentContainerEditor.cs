using UnityEditor;
using UnityEngine;
using static CutsceneSegmentContainer; // for SegmentTypes, SegmentCompleteTypes, etc.

// Place this script in an "Editor" folder, e.g. "Assets/Editor/CutsceneSegmentContainerEditor.cs"
[CustomPropertyDrawer(typeof(CutsceneSegmentContainer))]
public class CutsceneSegmentContainerEditor : PropertyDrawer {
    // Layout constants – tweak as needed
    private const float SECTION_SPACING = 8f;   // Extra vertical space before section headers
    private const float BOX_PADDING = 5f;   // Padding inside HelpBox
    private const float LINE_SPACING = 4f;   // Vertical space between property fields
    private const float LABEL_WIDTH = 160f; // Label width so fields aren’t too narrow

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        // Temporarily override label widths for more comfortable fields
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LABEL_WIDTH;

        bool oldWideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = false;

        // We’ll track vertical layout with lineRect
        Rect lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // ------------------ GENERAL SETTINGS ------------------
        lineRect.y += SECTION_SPACING;
        lineRect = DrawSectionHeader(lineRect, "General Settings", position.width);

        EditorGUI.indentLevel++;
        // Draw "Description" as a multiline text field (2 lines)
        SerializedProperty descProp = property.FindPropertyRelative("SegmentDescription");
        lineRect = DrawMultilineTextField(lineRect, descProp, "Description", 2);

        // SegmentType, CompleteType, StartDelay
        lineRect = DrawPropertyField(lineRect, property, "SegmentType", "Segment Type");
        lineRect = DrawPropertyField(lineRect, property, "CompleteType", "Complete Type");
        lineRect = DrawPropertyField(lineRect, property, "SegmentStartDelay", "Start Delay (sec)");

        // If Timer, draw a separate sub-section for Timer Settings
        var completeTypeProp = property.FindPropertyRelative("CompleteType");
        if ((SegmentCompleteTypes)completeTypeProp.enumValueIndex == SegmentCompleteTypes.Timer) {
            // Add space before the timer sub-section
            lineRect.y += SECTION_SPACING;
            lineRect = DrawSectionHeader(lineRect, "Timer Settings", position.width);

            // Draw the "SegmentTimer" property
            lineRect = DrawPropertyField(lineRect, property, "SegmentTimer", "Timer (sec)");

            // Extra spacing after Timer Settings so it doesn't collide
            lineRect.y += SECTION_SPACING;
        }
        EditorGUI.indentLevel--;

        // ------------------ SEGMENT-SPECIFIC SETTINGS ------------------
        var segTypeProp = property.FindPropertyRelative("SegmentType");
        var segType = (SegmentTypes)segTypeProp.enumValueIndex;

        lineRect.y += SECTION_SPACING;
        lineRect = DrawSectionHeader(lineRect, $"Segment-Specific Settings ({segType})", position.width);

        EditorGUI.indentLevel++;
        lineRect = DrawSegmentSpecificFields(lineRect, property, segType);
        EditorGUI.indentLevel--;

        // Restore
        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUIUtility.wideMode = oldWideMode;

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Draws a help box with a bold label for section headers. Returns updated lineRect below it.
    /// </summary>
    private Rect DrawSectionHeader(Rect lineRect, string title, float width) {
        float boxHeight = EditorGUIUtility.singleLineHeight + 2 * BOX_PADDING;
        Rect boxRect = new Rect(lineRect.x, lineRect.y, width, boxHeight);

        // Draw an empty help box
        EditorGUI.HelpBox(boxRect, "", MessageType.None);

        // Bold label inside
        Rect labelRect = new Rect(
            boxRect.x + BOX_PADDING,
            boxRect.y + BOX_PADDING,
            boxRect.width - 2 * BOX_PADDING,
            EditorGUIUtility.singleLineHeight
        );
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Move lineRect below the box
        lineRect.y += boxRect.height + LINE_SPACING;
        return lineRect;
    }

    /// <summary>
    /// Draws a multiline text field (e.g. for "Description") with the specified number of lines.
    /// </summary>
    private Rect DrawMultilineTextField(Rect lineRect, SerializedProperty prop, string label, int lineCount = 3) {
        if (prop == null) return lineRect;

        float textFieldHeight = EditorGUIUtility.singleLineHeight * lineCount;
        Rect fieldRect = new Rect(lineRect.x, lineRect.y, lineRect.width, textFieldHeight);

        // Label
        EditorGUI.LabelField(
            new Rect(fieldRect.x, fieldRect.y, LABEL_WIDTH, EditorGUIUtility.singleLineHeight),
            label
        );

        // Text area to the right
        float textAreaX = fieldRect.x + LABEL_WIDTH + 5f;
        float textAreaW = fieldRect.width - LABEL_WIDTH - 5f;
        Rect textAreaRect = new Rect(textAreaX, fieldRect.y, textAreaW, textFieldHeight);

        // Draw text area
        string oldVal = prop.stringValue;
        string newVal = EditorGUI.TextArea(textAreaRect, oldVal);
        if (newVal != oldVal) {
            prop.stringValue = newVal;
        }

        lineRect.y += textFieldHeight + LINE_SPACING;
        return lineRect;
    }

    /// <summary>
    /// Draws a single property field by name. Returns updated lineRect.
    /// </summary>
    private Rect DrawPropertyField(Rect lineRect, SerializedProperty parent, string propertyName, string label) {
        SerializedProperty prop = parent.FindPropertyRelative(propertyName);
        if (prop == null) return lineRect;

        Rect fieldRect = new Rect(lineRect.x, lineRect.y, lineRect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(fieldRect, prop, new GUIContent(label), true);

        lineRect.y += EditorGUIUtility.singleLineHeight + LINE_SPACING;
        return lineRect;
    }

    /// <summary>
    /// Overload: draws a property field given a direct SerializedProperty reference (for arrays).
    /// </summary>
    private Rect DrawPropertyField(Rect lineRect, SerializedProperty prop, string label) {
        if (prop == null) return lineRect;

        Rect fieldRect = new Rect(lineRect.x, lineRect.y, lineRect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(fieldRect, prop, new GUIContent(label), true);

        lineRect.y += EditorGUIUtility.singleLineHeight + LINE_SPACING;
        return lineRect;
    }

    /// <summary>
    /// Draws fields depending on SegmentType. Returns updated lineRect.
    /// </summary>
    private Rect DrawSegmentSpecificFields(Rect lineRect, SerializedProperty property, SegmentTypes segType) {
        switch (segType) {
            case SegmentTypes.MainNPCChat:
                lineRect = DrawPropertyField(lineRect, property, "InkJSON", "Ink JSON");
                lineRect = DrawPropertyField(lineRect, property, "DialogueText", "Dialogue Text");
                break;

            case SegmentTypes.TextBubbleChat:
                lineRect = DrawPropertyField(lineRect, property, "DialogueText", "Dialogue Text");
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Target Object");
                break;

            case SegmentTypes.SpawnCharacter:
                lineRect = DrawPropertyField(lineRect, property, "CharacterPrefab", "Character Prefab");
                lineRect = DrawPropertyField(lineRect, property, "SpawnPoint", "Spawn Point");
                break;

            case SegmentTypes.RemoveCharacter:
            case SegmentTypes.CharacterAnimate:
            case SegmentTypes.CameraFollow:
            case SegmentTypes.OpenDoor:
            case SegmentTypes.CloseDoor:
            case SegmentTypes.ShowWall:
            case SegmentTypes.HideWall:
            case SegmentTypes.HideObject:
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Target Object");
                break;

            case SegmentTypes.CharacterMove:
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Moving Object");
                lineRect = DrawPropertyField(lineRect, property, "MoveDestination", "Move Destination");
                lineRect = DrawPropertyField(lineRect, property, "ArrivalThreshold", "Arrival Threshold");
                break;

            case SegmentTypes.CharacterTargetFurniture:
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Moving Object");
                lineRect = DrawPropertyField(lineRect, property, "TargetFurniture", "Target Furniture");
                lineRect = DrawPropertyField(lineRect, property, "ArrivalThreshold", "Arrival Threshold");
                break;

            case SegmentTypes.CharacterTargetExit:
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Moving Object");
                lineRect = DrawPropertyField(lineRect, property, "ExitPoint", "Exit Point");
                lineRect = DrawPropertyField(lineRect, property, "ExitPointID", "Exit Point ID");
                lineRect = DrawPropertyField(lineRect, property, "ArrivalThreshold", "Arrival Threshold");
                break;

            case SegmentTypes.CameraMove:
                lineRect = DrawPropertyField(lineRect, property, "TargetPosition", "Target Position");
                break;

            case SegmentTypes.PlayAudio:
                lineRect = DrawPropertyField(lineRect, property, "AudioEvent", "Audio Event");
                lineRect = DrawPropertyField(lineRect, property, "TargetPosition", "Sound Position");
                break;

            case SegmentTypes.RedirectPlayerSpawn:
                lineRect = DrawPropertyField(lineRect, property, "TargetPosition", "New Spawn Position");
                break;

            case SegmentTypes.NPCGiveGift:
                lineRect = DrawPropertyField(lineRect, property, "ItemSlot", "Item Slot");
                break;

            case SegmentTypes.MoneyUpdate:
                lineRect = DrawPropertyField(lineRect, property, "MoneyAmount", "Money Amount");
                break;

            case SegmentTypes.CharacterEmoji:
                lineRect = DrawPropertyField(lineRect, property, "PrimaryTarget", "Character");
                lineRect = DrawPropertyField(lineRect, property, "DialogueText", "Emoji Key");
                break;

            case SegmentTypes.CharacterDirection:
                lineRect = DrawPropertyField(lineRect, property, "Direction", "Direction (2D)");
                break;

            case SegmentTypes.Letterboxing:
                lineRect = DrawPropertyField(lineRect, property, "LetterboxingDuration", "Duration");
                var letterboxProp = property.FindPropertyRelative("LetterboxElements");
                lineRect = DrawArrayField(lineRect, letterboxProp, "Letterbox Elements");
                lineRect = DrawPropertyField(lineRect, property, "EnableLetterboxing", "Enable Letterboxing");
                break;

            case SegmentTypes.ChangeScene:
                lineRect = DrawPropertyField(lineRect, property, "SceneName", "Scene Name");
                break;

            // SegmentTypes.End or anything else
            default:
                break;
        }

        return lineRect;
    }

    /// <summary>
    /// Draws an array with a bold label and +/- buttons. Returns updated lineRect.
    /// </summary>
    private Rect DrawArrayField(Rect lineRect, SerializedProperty arrayProp, string label) {
        EditorGUI.LabelField(lineRect, label, EditorStyles.boldLabel);
        lineRect.y += EditorGUIUtility.singleLineHeight + LINE_SPACING;

        EditorGUI.indentLevel++;
        for (int i = 0; i < arrayProp.arraySize; i++) {
            SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
            lineRect = DrawPropertyField(lineRect, element, $"Element {i}");
        }
        EditorGUI.indentLevel--;

        lineRect = DrawArrayButtons(lineRect, arrayProp);
        return lineRect;
    }

    /// <summary>
    /// Renders Add/Remove buttons to modify array size. Returns updated lineRect.
    /// </summary>
    private Rect DrawArrayButtons(Rect lineRect, SerializedProperty arrayProp) {
        float btnWidth = 50f;
        Rect addRect = new Rect(lineRect.x, lineRect.y, btnWidth, EditorGUIUtility.singleLineHeight);
        Rect removeRect = new Rect(lineRect.x + 55f, lineRect.y, btnWidth, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(addRect, "+")) {
            arrayProp.arraySize++;
        }
        if (GUI.Button(removeRect, "-") && arrayProp.arraySize > 0) {
            arrayProp.arraySize--;
        }

        lineRect.y += EditorGUIUtility.singleLineHeight + LINE_SPACING;
        return lineRect;
    }

    // ------------------ HEIGHT CALCULATION ------------------
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float h = 0f;
        float lineH = EditorGUIUtility.singleLineHeight + LINE_SPACING;

        // Space above "General Settings"
        h += SECTION_SPACING;

        // General Settings header
        h += EditorGUIUtility.singleLineHeight + 2 * BOX_PADDING + LINE_SPACING;

        // "Description" multiline (2 lines)
        float descHeight = (EditorGUIUtility.singleLineHeight * 2) + LINE_SPACING;
        h += descHeight;

        // SegmentType, CompleteType, StartDelay
        h += 3 * lineH;

        // If Timer
        var completeTypeProp = property.FindPropertyRelative("CompleteType");
        if ((SegmentCompleteTypes)completeTypeProp.enumValueIndex == SegmentCompleteTypes.Timer) {
            // Extra space before sub-section
            h += SECTION_SPACING;

            // Timer Settings header
            h += EditorGUIUtility.singleLineHeight + 2 * BOX_PADDING + LINE_SPACING;

            // Timer field
            h += lineH;

            // Extra spacing after Timer
            h += SECTION_SPACING;
        }

        // Space before Segment-Specific
        h += SECTION_SPACING;

        // Segment-Specific header
        h += EditorGUIUtility.singleLineHeight + 2 * BOX_PADDING + LINE_SPACING;

        // Segment-specific fields
        var segTypeProp = property.FindPropertyRelative("SegmentType");
        var segType = (SegmentTypes)segTypeProp.enumValueIndex;
        h += GetSegmentSpecificHeight(property, segType);

        return h;
    }

    private float GetSegmentSpecificHeight(SerializedProperty property, SegmentTypes segType) {
        float height = 0f;
        float lineH = EditorGUIUtility.singleLineHeight + LINE_SPACING;

        switch (segType) {
            case SegmentTypes.MainNPCChat:
                height += 2 * lineH; // InkJSON, DialogueText
                break;
            case SegmentTypes.TextBubbleChat:
                height += 2 * lineH;
                break;
            case SegmentTypes.SpawnCharacter:
                height += 2 * lineH;
                break;
            case SegmentTypes.RemoveCharacter:
            case SegmentTypes.CharacterAnimate:
            case SegmentTypes.CameraFollow:
            case SegmentTypes.OpenDoor:
            case SegmentTypes.CloseDoor:
            case SegmentTypes.ShowWall:
            case SegmentTypes.HideWall:
            case SegmentTypes.HideObject:
                height += 1 * lineH;
                break;
            case SegmentTypes.CharacterMove:
            case SegmentTypes.CharacterTargetFurniture:
                height += 3 * lineH;
                break;
            case SegmentTypes.CharacterTargetExit:
                height += 4 * lineH;
                break;
            case SegmentTypes.CameraMove:
                height += 1 * lineH;
                break;
            case SegmentTypes.PlayAudio:
                height += 2 * lineH;
                break;
            case SegmentTypes.RedirectPlayerSpawn:
            case SegmentTypes.NPCGiveGift:
            case SegmentTypes.MoneyUpdate:
            case SegmentTypes.CharacterDirection:
                height += 1 * lineH;
                break;
            case SegmentTypes.CharacterEmoji:
                height += 2 * lineH;
                break;
            case SegmentTypes.Letterboxing:
                // Duration + array + enable bool
                var letterboxProp = property.FindPropertyRelative("LetterboxElements");
                float arrayH = EditorGUI.GetPropertyHeight(letterboxProp, true) + LINE_SPACING;

                height += lineH;  // Duration
                height += arrayH; // array
                height += lineH;  // Enable Letterboxing
                break;
            case SegmentTypes.ChangeScene:
                height += 1 * lineH;
                break;
            default:
                // SegmentTypes.End or unhandled
                break;
        }

        return height;
    }
}
