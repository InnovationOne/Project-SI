#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// This script clears an item container
[CustomEditor(typeof(ItemContainerSO))]
public class ItemContainerEditor : Editor {
    public override void OnInspectorGUI() {
        var itemContainer = target as ItemContainerSO;
        if (GUILayout.Button("Clear container")) {
            for (int i = 0; i < itemContainer.ItemSlots.Count; i++) {
                itemContainer.ItemSlots[i].Clear();
            }
        }

        DrawDefaultInspector();
    }
}

#endif