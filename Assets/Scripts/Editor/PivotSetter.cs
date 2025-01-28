using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Linq;

public class PivotSetter: EditorWindow {
    private string folderPath = "Assets/Sprites";

    private enum PivotMode {
        BottomCenter,
        CustomOffsetFromCenter
    }
    private PivotMode pivotMode = PivotMode.BottomCenter;

    // For custom offset (N pixels below sprite’s vertical center)
    private int offsetDownPx = 32;

    // Typical pixel?art settings
    private int pixelsPerUnit = 32;
    private int maxTextureSize = 2048;

    [MenuItem("Tools/Pivot Setter")]
    private static void ShowWindow() {
        GetWindow<PivotSetter>("Pivot Setter");
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Bulk Pivot & Pixel-Art Import (New Data Provider API)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

        pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Pivot Mode", pivotMode);
        if (pivotMode == PivotMode.CustomOffsetFromCenter) {
            offsetDownPx = EditorGUILayout.IntField("Offset Down (pixels)", offsetDownPx);
            EditorGUILayout.HelpBox(
                "For example, 32 px down in a 64 px-high sprite places pivot at the bottom.\n" +
                "You can adapt this for partial offsets, e.g. tall frames, etc.",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);
        maxTextureSize = EditorGUILayout.IntField("Max Texture Size", maxTextureSize);

        EditorGUILayout.HelpBox(
            "For pixel art:\n" +
            " - Use Point Filter, No MipMaps, Uncompressed.\n" +
            " - maxTextureSize ? your largest sprite dimension.\n" +
            " - resizeAlgo=NearestNeighbor to avoid blurring if downscaled.\n",
            MessageType.Info);

        if (GUILayout.Button("Apply Pivot & Pixel-Art Settings")) {
            ApplyPivotAndPixelArtSettings();
        }
    }

    private void ApplyPivotAndPixelArtSettings() {
        if (string.IsNullOrEmpty(folderPath)) {
            Debug.LogError("Folder path is empty. Please specify a valid folder under Assets/.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        if (guids == null || guids.Length == 0) {
            Debug.LogWarning($"No Texture2D assets found in '{folderPath}'.");
            return;
        }

        int processedCount = 0;

        // We'll use the new SpriteDataProviderFactories approach
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Force "Sprite (2D and UI)" type
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = maxTextureSize;

            // Attempt to get an ISpriteEditorDataProvider from this importer
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null) {
                Debug.LogWarning($"No DataProvider found for '{path}'. " +
                                 "Ensure you have the 2D Sprite Editor installed.");
                continue;
            }

            // Initialize
            dataProvider.InitSpriteEditorDataProvider();

            // Grab the existing sprite rects (Single or Multiple).
            var spriteRects = dataProvider.GetSpriteRects();
            if (spriteRects == null || spriteRects.Length == 0) {
                // No sub-sprites means no pivot to update
                // but let's still reimport to apply pixel-art settings
                importer.SaveAndReimport();
                continue;
            }

            // For each rect, set alignment=Custom and compute pivot
            foreach (var sRect in spriteRects) {
                sRect.alignment = SpriteAlignment.Custom;
                // Convert the rect's size in pixels to compute a custom offset if needed
                float w = sRect.rect.width;
                float h = sRect.rect.height;

                Vector2 newPivot = CalculatePivot(w, h);
                sRect.pivot = newPivot;
            }

            // Write them back
            dataProvider.SetSpriteRects(spriteRects);
            var nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs = nameFileIdDataProvider.GetNameFileIdPairs().ToList();
            nameFileIdDataProvider.SetNameFileIdPairs(pairs);

            // Apply changes to the data provider
            dataProvider.Apply();

            // Finally reimport the asset to finalize
            importer.SaveAndReimport();
            processedCount++;
        }

        Debug.Log($"Processed {processedCount} Texture2D assets in '{folderPath}' with pivot & pixel-art settings.");
    }

    /// <summary>
    /// Calculates the pivot in normalized [0..1] coords,
    /// either (0.5, 0) for bottom center,
    /// or center minus 'offsetDownPx' for custom offset.
    /// </summary>
    private Vector2 CalculatePivot(float width, float height) {
        switch (pivotMode) {
            case PivotMode.BottomCenter:
                return new Vector2(0.5f, 0f);

            case PivotMode.CustomOffsetFromCenter:
                // If pivot is at center => (0.5, 0.5).
                // We'll subtract (offsetDownPx / height) from the Y.
                float offsetNorm = offsetDownPx / height;
                float pivotY = 0.5f - offsetNorm;
                return new Vector2(0.5f, pivotY);

            default:
                // Fallback (should never happen)
                return new Vector2(0.5f, 0f);
        }
    }
}
