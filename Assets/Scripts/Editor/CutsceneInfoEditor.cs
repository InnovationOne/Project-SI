using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CutsceneInfo))]
public class CutsceneInfoEditor : Editor {
    private ReorderableList segmentsList;

    private void OnEnable() {
        // Initialize the reorderable list for the "Segments" array
        segmentsList = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("Segments"),
            draggable: true,  // allow drag to reorder
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true
        );

        // Draw list header
        segmentsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Cutscene Segments");
        };

        // Draw each element
        segmentsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = segmentsList.serializedProperty.GetArrayElementAtIndex(index);
            // Use PropertyField so the custom PropertyDrawer (CutsceneSegmentContainerDrawer) is used
            EditorGUI.PropertyField(rect, element, new GUIContent($"Segment {index}"), true);
        };

        // Calculate each element’s height by asking Unity how tall the property is
        segmentsList.elementHeightCallback = (int index) => {
            SerializedProperty element = segmentsList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true)
                   + EditorGUIUtility.standardVerticalSpacing;
        };
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        // Draw the top-level fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("CutsceneDescription"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("CutsceneId"));

        // Draw the reorderable list
        segmentsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
