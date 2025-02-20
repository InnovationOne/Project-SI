using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CutsceneSegmentContainer))]
public class CutsceneSegmentContainerDrawer : PropertyDrawer {
    private const float SPACING = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        // Draw foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

        if (property.isExpanded) {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + SPACING;

            // 1) Always-drawn base fields
            y = DrawProperty(property, "SegmentDescription", position, y);
            y = DrawProperty(property, "SegmentType", position, y);
            y = DrawProperty(property, "CompleteType", position, y);
            y = DrawProperty(property, "SegmentStartDelay", position, y);

            // 2) Draw segment-specific fields
            var segmentTypeProp = property.FindPropertyRelative("SegmentType");
            var segmentType = (CutsceneSegmentContainer.SegmentTypes)segmentTypeProp.enumValueIndex;
            y = DrawSegmentSpecificFields(property, segmentType, position, y);

            // 3) Draw complete-type-specific fields
            var completeTypeProp = property.FindPropertyRelative("CompleteType");
            var completeType = (CutsceneSegmentContainer.SegmentCompleteTypes)completeTypeProp.enumValueIndex;
            y = DrawCompleteTypeFields(property, completeType, position, y);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Dynamically calculates the total height by summing the heights of all fields we plan to draw.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        // Always at least one line for the foldout
        float totalHeight = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded) {
            // If foldout is closed, just return one line
            return totalHeight;
        }

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        // Helper function to get the height of a sub-property
        float PropH(string propName) {
            SerializedProperty p = property.FindPropertyRelative(propName);
            return p != null ? EditorGUI.GetPropertyHeight(p, true) + spacing : 0f;
        }

        // Add spacing for the lines after foldout
        totalHeight += spacing;

        // Base fields
        totalHeight += PropH("SegmentDescription");
        totalHeight += PropH("SegmentType");
        totalHeight += PropH("CompleteType");
        totalHeight += PropH("SegmentStartDelay");

        // Segment-specific
        var segmentType = (CutsceneSegmentContainer.SegmentTypes)
            property.FindPropertyRelative("SegmentType").enumValueIndex;

        switch (segmentType) {
            case CutsceneSegmentContainer.SegmentTypes.End:
            case CutsceneSegmentContainer.SegmentTypes.ShowGameCanvas:
            case CutsceneSegmentContainer.SegmentTypes.HideGameCanvas:
            case CutsceneSegmentContainer.SegmentTypes.ShowLetterboxing:
            case CutsceneSegmentContainer.SegmentTypes.HideLetterboxing:
                // no extra fields
                break;

            case CutsceneSegmentContainer.SegmentTypes.MainNPCChat:
                totalHeight += PropH("InkJSON");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.TextBubbleChat:
                totalHeight += PropH("DialogueText");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.SpawnCharacter:
                totalHeight += PropH("CharacterPrefab");
                totalHeight += PropH("SpawnPoint");
                break;

            case CutsceneSegmentContainer.SegmentTypes.RemoveCharacter:
            case CutsceneSegmentContainer.SegmentTypes.CharacterAnimate:
            case CutsceneSegmentContainer.SegmentTypes.CameraFollow:
            case CutsceneSegmentContainer.SegmentTypes.OpenDoor:
            case CutsceneSegmentContainer.SegmentTypes.CloseDoor:
            case CutsceneSegmentContainer.SegmentTypes.ShowWall:
            case CutsceneSegmentContainer.SegmentTypes.HideWall:
            case CutsceneSegmentContainer.SegmentTypes.HideObject:
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterMove:
                totalHeight += PropH("MoveDestination");
                totalHeight += PropH("ArrivalThreshold");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterTargetFurniture:
                totalHeight += PropH("TargetFurniture");
                totalHeight += PropH("ArrivalThreshold");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterTargetExit:
                totalHeight += PropH("ExitPoint");
                totalHeight += PropH("ExitPointID");
                totalHeight += PropH("ArrivalThreshold");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CameraMove:
                totalHeight += PropH("TargetPosition");
                break;

            case CutsceneSegmentContainer.SegmentTypes.PlayAudio:
                totalHeight += PropH("AudioEvent");
                totalHeight += PropH("TargetPosition");
                break;

            case CutsceneSegmentContainer.SegmentTypes.RedirectPlayerSpawn:
                totalHeight += PropH("TargetPosition");
                break;

            case CutsceneSegmentContainer.SegmentTypes.NPCGiveGift:
                totalHeight += PropH("ItemSlot");
                break;

            case CutsceneSegmentContainer.SegmentTypes.MoneyUpdate:
                totalHeight += PropH("MoneyAmount");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterEmoji:
                totalHeight += PropH("PrimaryTarget");
                totalHeight += PropH("DialogueText");
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterDirection:
                totalHeight += PropH("Direction");
                totalHeight += PropH("PrimaryTarget");
                break;

            case CutsceneSegmentContainer.SegmentTypes.ChangeScene:
                totalHeight += PropH("SceneName");
                break;
        }

        // CompleteType fields
        var completeType = (CutsceneSegmentContainer.SegmentCompleteTypes)
            property.FindPropertyRelative("CompleteType").enumValueIndex;
        switch (completeType) {
            case CutsceneSegmentContainer.SegmentCompleteTypes.Timer:
                // Potentially drawn again if not already, but let's keep it simple
                totalHeight += PropH("SegmentTimer");
                break;
            case CutsceneSegmentContainer.SegmentCompleteTypes.Movement:
                totalHeight += PropH("ArrivalThreshold");
                break;
            case CutsceneSegmentContainer.SegmentCompleteTypes.Chat:
                // no extra property
                break;
        }

        // Add some final padding
        totalHeight += spacing;

        return totalHeight;
    }

    /// <summary>
    /// Helper method to draw a single property and return the updated Y position.
    /// </summary>
    private float DrawProperty(SerializedProperty parent, string propName, Rect outerRect, float y) {
        SerializedProperty prop = parent.FindPropertyRelative(propName);
        if (prop == null) return y; // in case property is missing

        float height = EditorGUI.GetPropertyHeight(prop, true);
        Rect r = new Rect(outerRect.x, y, outerRect.width, height);
        EditorGUI.PropertyField(r, prop, true);
        return y + height + SPACING;
    }

    /// <summary>
    /// Draw segment-type-specific fields based on the chosen enum.
    /// </summary>
    private float DrawSegmentSpecificFields(SerializedProperty property,
        CutsceneSegmentContainer.SegmentTypes segmentType, Rect position, float y) {
        switch (segmentType) {
            case CutsceneSegmentContainer.SegmentTypes.End:
            case CutsceneSegmentContainer.SegmentTypes.ShowGameCanvas:
            case CutsceneSegmentContainer.SegmentTypes.HideGameCanvas:
            case CutsceneSegmentContainer.SegmentTypes.ShowLetterboxing:
            case CutsceneSegmentContainer.SegmentTypes.HideLetterboxing:
                // no extra fields
                break;

            case CutsceneSegmentContainer.SegmentTypes.MainNPCChat:
                y = DrawProperty(property, "InkJSON", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.TextBubbleChat:
                y = DrawProperty(property, "DialogueText", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.SpawnCharacter:
                y = DrawProperty(property, "CharacterPrefab", position, y);
                y = DrawProperty(property, "SpawnPoint", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.RemoveCharacter:
            case CutsceneSegmentContainer.SegmentTypes.CharacterAnimate:
            case CutsceneSegmentContainer.SegmentTypes.CameraFollow:
            case CutsceneSegmentContainer.SegmentTypes.OpenDoor:
            case CutsceneSegmentContainer.SegmentTypes.CloseDoor:
            case CutsceneSegmentContainer.SegmentTypes.ShowWall:
            case CutsceneSegmentContainer.SegmentTypes.HideWall:
            case CutsceneSegmentContainer.SegmentTypes.HideObject:
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterMove:
                y = DrawProperty(property, "MoveDestination", position, y);
                y = DrawProperty(property, "ArrivalThreshold", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterTargetFurniture:
                y = DrawProperty(property, "TargetFurniture", position, y);
                y = DrawProperty(property, "ArrivalThreshold", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterTargetExit:
                y = DrawProperty(property, "ExitPoint", position, y);
                y = DrawProperty(property, "ExitPointID", position, y);
                y = DrawProperty(property, "ArrivalThreshold", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CameraMove:
                y = DrawProperty(property, "TargetPosition", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.PlayAudio:
                y = DrawProperty(property, "AudioEvent", position, y);
                y = DrawProperty(property, "TargetPosition", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.RedirectPlayerSpawn:
                y = DrawProperty(property, "TargetPosition", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.NPCGiveGift:
                y = DrawProperty(property, "ItemSlot", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.MoneyUpdate:
                y = DrawProperty(property, "MoneyAmount", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterEmoji:
                y = DrawProperty(property, "PrimaryTarget", position, y);
                y = DrawProperty(property, "DialogueText", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.CharacterDirection:
                y = DrawProperty(property, "Direction", position, y);
                y = DrawProperty(property, "PrimaryTarget", position, y);
                break;

            case CutsceneSegmentContainer.SegmentTypes.ChangeScene:
                y = DrawProperty(property, "SceneName", position, y);
                break;
        }

        return y;
    }

    /// <summary>
    /// Draw fields based on the CompleteType enum.
    /// </summary>
    private float DrawCompleteTypeFields(SerializedProperty property,
        CutsceneSegmentContainer.SegmentCompleteTypes completeType, Rect position, float y) {
        switch (completeType) {
            case CutsceneSegmentContainer.SegmentCompleteTypes.Timer:
                // Possibly re-draw SegmentTimer if not already
                y = DrawProperty(property, "SegmentTimer", position, y);
                break;
            case CutsceneSegmentContainer.SegmentCompleteTypes.Movement:
                // Possibly re-draw ArrivalThreshold if not already
                y = DrawProperty(property, "ArrivalThreshold", position, y);
                break;
            case CutsceneSegmentContainer.SegmentCompleteTypes.Chat:
                // No extra property
                break;
        }
        return y;
    }
}
