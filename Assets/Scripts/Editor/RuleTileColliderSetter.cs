using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class RuleTileColliderSetter : EditorWindow {
    public List<RuleTile> ruleTiles = new();

    [MenuItem("Tools/RuleTile Collider Setter")]
    public static void ShowWindow() {
        GetWindow<RuleTileColliderSetter>("RuleTile Collider Setter");
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Rule Tiles", EditorStyles.boldLabel);
        SerializedObject serializedObject = new(this);
        SerializedProperty ruleTilesProperty = serializedObject.FindProperty("ruleTiles");
        EditorGUILayout.PropertyField(ruleTilesProperty, true);
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Set Colliders to None")) {
            ResetColliders();
        }
    }

    private void ResetColliders() {
        foreach (var ruleTile in ruleTiles) {
            if (ruleTile == null) {
                Debug.LogWarning("One of the RuleTiles is null. Skipping...");
                continue;
            }

            Undo.RecordObject(ruleTile, "Set Colliders to None");

            ruleTile.m_DefaultColliderType = Tile.ColliderType.None;

            foreach (var rule in ruleTile.m_TilingRules) {
                rule.m_ColliderType = Tile.ColliderType.None;
            }

            EditorUtility.SetDirty(ruleTile);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Colliders reset to None for all selected RuleTiles.");
    }
}
