#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Provides a custom Editor Window to generate animation clips and override controllers from sliced textures.
/// Supports both single-sheet and FG/BG modes, with optional reversed slash frames.
/// </summary>
public class AnimationGenerator : EditorWindow {
    [Header("Mode")]
    public bool hasFgBg = false;
    public bool useSlashReverseFromSlash = false;

    [Header("Single-Sheet Mode")]
    public AnimatorController baseControllerSingle;

    [Header("Textures (Single-Sheet)")]
    public Texture2D bowTexture;
    public Texture2D hurtTexture;
    public Texture2D slashTexture;
    public Texture2D slashReverseTexture;
    public Texture2D spellcastTexture;
    public Texture2D thrustTexture;
    public Texture2D walkcycleTexture;

    [Header("FG/BG Mode")]
    public AnimatorController baseControllerFG;
    public AnimatorController baseControllerBG;
    public AnimatorOverrideController overrideControllerFG;
    public AnimatorOverrideController overrideControllerBG;

    [Header("Textures (FG)")]
    public Texture2D bowTextureFG;
    public Texture2D hurtTextureFG;
    public Texture2D slashTextureFG;
    public Texture2D slashReverseTextureFG;
    public Texture2D spellcastTextureFG;
    public Texture2D thrustTextureFG;
    public Texture2D walkcycleTextureFG;

    [Header("Textures (BG)")]
    public Texture2D bowTextureBG;
    public Texture2D hurtTextureBG;
    public Texture2D slashTextureBG;
    public Texture2D slashReverseTextureBG;
    public Texture2D spellcastTextureBG;
    public Texture2D thrustTextureBG;
    public Texture2D walkcycleTextureBG;

    [Header("Output Settings")]
    public string outputControllerDirectory;
    public string outputControllerName;

    private const float FPS = 15f;
    private Vector2 scrollPosition;

    private enum Direction { Up, Left, Down, Right }
    private enum PositionType { None, FG, BG }

    [MenuItem("Tools/Animation Generator")]
    public static void ShowWindow() {
        GetWindow<AnimationGenerator>("Animation Generator");
    }

    private void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Mode Settings", EditorStyles.boldLabel);
        hasFgBg = EditorGUILayout.Toggle("Has FG/BG?", hasFgBg);
        useSlashReverseFromSlash = EditorGUILayout.Toggle("Use SlashReverse from Slash?", useSlashReverseFromSlash);

        EditorGUILayout.Space();
        if (!hasFgBg) DrawSingleSheetGUI();
        else DrawFgBgGUI();

        EditorGUILayout.Space();
        outputControllerDirectory = EditorGUILayout.TextField("Output Directory", outputControllerDirectory);
        outputControllerName = EditorGUILayout.TextField("Output Controller Name", outputControllerName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Animations & Override(s)")) {
            if (!ValidateInputs()) return;
            CreateOutputDirectoryIfNeeded();

            if (!hasFgBg) GenerateSingleSheetOverride();
            else GenerateFgBgOverrides();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Shows Single-Sheet configuration fields in the Editor.
    /// </summary>
    private void DrawSingleSheetGUI() {
        EditorGUILayout.LabelField("Single-Sheet Mode", EditorStyles.boldLabel);
        baseControllerSingle = (AnimatorController)EditorGUILayout.ObjectField("Base Controller (Single)", baseControllerSingle, typeof(AnimatorController), false);

        EditorGUILayout.Space();
        bowTexture = (Texture2D)EditorGUILayout.ObjectField("Bow Texture", bowTexture, typeof(Texture2D), false);
        hurtTexture = (Texture2D)EditorGUILayout.ObjectField("Hurt Texture", hurtTexture, typeof(Texture2D), false);
        slashTexture = (Texture2D)EditorGUILayout.ObjectField("Slash Texture", slashTexture, typeof(Texture2D), false);
        if (!useSlashReverseFromSlash)
            slashReverseTexture = (Texture2D)EditorGUILayout.ObjectField("SlashReverse Texture", slashReverseTexture, typeof(Texture2D), false);

        spellcastTexture = (Texture2D)EditorGUILayout.ObjectField("Spellcast Texture", spellcastTexture, typeof(Texture2D), false);
        thrustTexture = (Texture2D)EditorGUILayout.ObjectField("Thrust Texture", thrustTexture, typeof(Texture2D), false);
        walkcycleTexture = (Texture2D)EditorGUILayout.ObjectField("Walkcycle Texture", walkcycleTexture, typeof(Texture2D), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Clear Textures")) {
            RemoveTextures();
        }
    }

    /// <summary>
    /// Shows FG/BG configuration fields in the Editor.
    /// </summary>
    private void DrawFgBgGUI() {
        EditorGUILayout.LabelField("FG/BG Mode", EditorStyles.boldLabel);
        baseControllerFG = (AnimatorController)EditorGUILayout.ObjectField("Base Controller (FG)", baseControllerFG, typeof(AnimatorController), false);
        baseControllerBG = (AnimatorController)EditorGUILayout.ObjectField("Base Controller (BG)", baseControllerBG, typeof(AnimatorController), false);

        overrideControllerFG = (AnimatorOverrideController)EditorGUILayout.ObjectField("OverrideController FG", overrideControllerFG, typeof(AnimatorOverrideController), false);
        overrideControllerBG = (AnimatorOverrideController)EditorGUILayout.ObjectField("OverrideController BG", overrideControllerBG, typeof(AnimatorOverrideController), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture2D (FG)", EditorStyles.boldLabel);
        bowTextureFG = (Texture2D)EditorGUILayout.ObjectField("Bow FG", bowTextureFG, typeof(Texture2D), false);
        hurtTextureFG = (Texture2D)EditorGUILayout.ObjectField("Hurt FG", hurtTextureFG, typeof(Texture2D), false);
        slashTextureFG = (Texture2D)EditorGUILayout.ObjectField("Slash FG", slashTextureFG, typeof(Texture2D), false);
        if (!useSlashReverseFromSlash)
            slashReverseTextureFG = (Texture2D)EditorGUILayout.ObjectField("SlashReverse FG", slashReverseTextureFG, typeof(Texture2D), false);

        spellcastTextureFG = (Texture2D)EditorGUILayout.ObjectField("Spellcast FG", spellcastTextureFG, typeof(Texture2D), false);
        thrustTextureFG = (Texture2D)EditorGUILayout.ObjectField("Thrust FG", thrustTextureFG, typeof(Texture2D), false);
        walkcycleTextureFG = (Texture2D)EditorGUILayout.ObjectField("Walkcycle FG", walkcycleTextureFG, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture2D (BG)", EditorStyles.boldLabel);
        bowTextureBG = (Texture2D)EditorGUILayout.ObjectField("Bow BG", bowTextureBG, typeof(Texture2D), false);
        hurtTextureBG = (Texture2D)EditorGUILayout.ObjectField("Hurt BG", hurtTextureBG, typeof(Texture2D), false);
        slashTextureBG = (Texture2D)EditorGUILayout.ObjectField("Slash BG", slashTextureBG, typeof(Texture2D), false);
        if (!useSlashReverseFromSlash)
            slashReverseTextureBG = (Texture2D)EditorGUILayout.ObjectField("SlashReverse BG", slashReverseTextureBG, typeof(Texture2D), false);

        spellcastTextureBG = (Texture2D)EditorGUILayout.ObjectField("Spellcast BG", spellcastTextureBG, typeof(Texture2D), false);
        thrustTextureBG = (Texture2D)EditorGUILayout.ObjectField("Thrust BG", thrustTextureBG, typeof(Texture2D), false);
        walkcycleTextureBG = (Texture2D)EditorGUILayout.ObjectField("Walkcycle BG", walkcycleTextureBG, typeof(Texture2D), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Clear Textures")) {
            RemoveTextures();
        }
    }

    private void RemoveTextures() {
        bowTexture = null;
        hurtTexture = null;
        slashTexture = null;
        slashReverseTexture = null;
        spellcastTexture = null;
        thrustTexture = null;
        walkcycleTexture = null;

        bowTextureFG = null;
        hurtTextureFG = null;
        slashTextureFG = null;
        slashReverseTextureFG = null;
        spellcastTextureFG = null;
        thrustTextureFG = null;
        walkcycleTextureFG = null;

        bowTextureBG = null;
        hurtTextureBG = null;
        slashTextureBG = null;
        slashReverseTextureBG = null;
        spellcastTextureBG = null;
        thrustTextureBG = null;
        walkcycleTextureBG = null;

        overrideControllerFG = null;
        overrideControllerBG = null;
    }

    /// <summary>
    /// Verifies if the mandatory fields are properly filled.
    /// </summary>
    private bool ValidateInputs() {
        if (!hasFgBg) {
            if (baseControllerSingle == null) {
                Debug.LogError("No base controller assigned (Single-Sheet mode).");
                return false;
            }
        } else {
            if (baseControllerFG == null || baseControllerBG == null) {
                Debug.LogError("Please assign both base controllers (FG & BG).");
                return false;
            }
        }
        if (string.IsNullOrEmpty(outputControllerDirectory)) {
            Debug.LogError("Please specify an output directory.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Ensures the output directory exists before generating assets.
    /// </summary>
    private void CreateOutputDirectoryIfNeeded() {
        if (!Directory.Exists(outputControllerDirectory)) {
            Directory.CreateDirectory(outputControllerDirectory);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Generates a single override controller and animations for single-sheet usage.
    /// </summary>
    private void GenerateSingleSheetOverride() {
        var singleOverride = new AnimatorOverrideController(baseControllerSingle);
        var overridePath = SaveAnimatorOverrideController(singleOverride, outputControllerDirectory, outputControllerName);
        singleOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);

        var folderPath = Path.GetDirectoryName(overridePath);
        var animFolder = Path.Combine(folderPath, outputControllerName);
        if (!Directory.Exists(animFolder)) {
            Directory.CreateDirectory(animFolder);
            AssetDatabase.Refresh();
        }

        var generated = new Dictionary<string, AnimationClip>();
        GenerateBowAnimations(LoadAndSortSprites(bowTexture), generated, animFolder, PositionType.None);
        GenerateHurtAnimation(LoadAndSortSprites(hurtTexture), generated, animFolder, PositionType.None);

        var slashSprites = LoadAndSortSprites(slashTexture);
        var slashRevSprites = LoadAndSortSprites(slashReverseTexture);
        GenerateSlashAnimations(slashSprites, slashRevSprites, useSlashReverseFromSlash, generated, animFolder, PositionType.None);

        GenerateSpellcastAnimation(LoadAndSortSprites(spellcastTexture), generated, animFolder, PositionType.None);
        GenerateThrustAnimation(LoadAndSortSprites(thrustTexture), generated, animFolder, PositionType.None);
        GenerateWalkcycleAnimation(LoadAndSortSprites(walkcycleTexture), generated, animFolder, PositionType.None);

        PopulateOverridesExact(singleOverride, generated);
        RemoveTextures();
        Debug.Log("Single-Sheet Override generation complete!");
    }

    /// <summary>
    /// Generates override controllers for both FG and BG, including animations.
    /// </summary>
    private void GenerateFgBgOverrides() {
        if (overrideControllerFG == null) {
            overrideControllerFG = new AnimatorOverrideController(baseControllerFG);
            var pathFG = SaveAnimatorOverrideController(overrideControllerFG, outputControllerDirectory, outputControllerName + "_FG");
            overrideControllerFG = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathFG);
        }

        if (overrideControllerBG == null) {
            overrideControllerBG = new AnimatorOverrideController(baseControllerBG);
            var pathBG = SaveAnimatorOverrideController(overrideControllerBG, outputControllerDirectory, outputControllerName + "_BG");
            overrideControllerBG = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathBG);
        }

        var fgPath = AssetDatabase.GetAssetPath(overrideControllerFG);
        var fgFolder = Path.GetDirectoryName(fgPath);
        var animFolderFG = Path.Combine(fgFolder, outputControllerName + "/FG");
        if (!Directory.Exists(animFolderFG)) Directory.CreateDirectory(animFolderFG);

        var bgPath = AssetDatabase.GetAssetPath(overrideControllerBG);
        var bgFolder = Path.GetDirectoryName(bgPath);
        var animFolderBG = Path.Combine(bgFolder, outputControllerName + "/BG");
        if (!Directory.Exists(animFolderBG)) Directory.CreateDirectory(animFolderBG);

        AssetDatabase.Refresh();

        var fgClips = new Dictionary<string, AnimationClip>();
        GenerateBowAnimations(LoadAndSortSprites(bowTextureFG), fgClips, animFolderFG, PositionType.FG);
        GenerateHurtAnimation(LoadAndSortSprites(hurtTextureFG), fgClips, animFolderFG, PositionType.FG);
        GenerateSlashAnimations(LoadAndSortSprites(slashTextureFG), LoadAndSortSprites(slashReverseTextureFG), useSlashReverseFromSlash, fgClips, animFolderFG, PositionType.FG);
        GenerateSpellcastAnimation(LoadAndSortSprites(spellcastTextureFG), fgClips, animFolderFG, PositionType.FG);
        GenerateThrustAnimation(LoadAndSortSprites(thrustTextureFG), fgClips, animFolderFG, PositionType.FG);
        GenerateWalkcycleAnimation(LoadAndSortSprites(walkcycleTextureFG), fgClips, animFolderFG, PositionType.FG);

        var bgClips = new Dictionary<string, AnimationClip>();
        GenerateBowAnimations(LoadAndSortSprites(bowTextureBG), bgClips, animFolderBG, PositionType.BG);
        GenerateHurtAnimation(LoadAndSortSprites(hurtTextureBG), bgClips, animFolderBG, PositionType.BG);
        GenerateSlashAnimations(LoadAndSortSprites(slashTextureBG), LoadAndSortSprites(slashReverseTextureBG), useSlashReverseFromSlash, bgClips, animFolderBG, PositionType.BG);
        GenerateSpellcastAnimation(LoadAndSortSprites(spellcastTextureBG), bgClips, animFolderBG, PositionType.BG);
        GenerateThrustAnimation(LoadAndSortSprites(thrustTextureBG), bgClips, animFolderBG, PositionType.BG);
        GenerateWalkcycleAnimation(LoadAndSortSprites(walkcycleTextureBG), bgClips, animFolderBG, PositionType.BG);

        PopulateOverridesExact(overrideControllerFG, fgClips);
        PopulateOverridesExact(overrideControllerBG, bgClips);
        RemoveTextures();
        Debug.Log("FG/BG Overrides generation complete!");
    }

    /// <summary>
    /// Loads all Sprites in ascending name order (e.g., sprite_0, sprite_1).
    /// </summary>
    private List<Sprite> LoadAndSortSprites(Texture2D texture) {
        var sprites = new List<Sprite>();
        if (texture == null) return sprites;

        var path = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(path)) {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets) {
                if (obj is Sprite s) sprites.Add(s);
            }
            sprites = sprites.OrderBy(s => s.name, new NaturalStringComparer()).ToList();
        }
        return sprites;
    }

    /// <summary>
    /// Generates four directional clips using specified start frames and counts.
    /// </summary>
    private void GenerateDirectionalAnimation(
        List<Sprite> sprites,
        string baseName,
        int[] startFrames,
        int frameCount,
        Dictionary<string, AnimationClip> output,
        string folder,
        PositionType pos) {
        for (var i = 0; i < startFrames.Length && i < 4; i++) {
            var startIndex = startFrames[i];
            CreateAnimationClip(sprites, baseName, startIndex, frameCount, GetDirectionName(i), output, folder, pos);
        }
    }

    /// <summary>
    /// Splits the sprite list into 4 directions in consecutive blocks.
    /// </summary>
    private void GenerateSingleDirectionAnimation(
        List<Sprite> sprites,
        string baseName,
        int frameCount,
        Dictionary<string, AnimationClip> output,
        string folder,
        PositionType pos) {
        for (var dir = 0; dir < 4; dir++) {
            var startIndex = dir * frameCount;
            CreateAnimationClip(sprites, baseName, startIndex, frameCount, GetDirectionName(dir), output, folder, pos);
        }
    }

    /// <summary>
    /// Reverses the frames for each direction if no separate reverse sprites exist.
    /// </summary>
    private void GenerateReversedAnimation(
        List<Sprite> sprites,
        string baseName,
        int frameCount,
        Dictionary<string, AnimationClip> output,
        string folder,
        PositionType pos) {
        for (var dir = 0; dir < 4; dir++) {
            var startFrame = dir * frameCount;
            var endFrame = startFrame + frameCount - 1;
            var reversed = new List<Sprite>();

            for (var i = endFrame; i >= startFrame; i--)
                if (i >= 0 && i < sprites.Count) reversed.Add(sprites[i]);

            var clipName = BuildClipName(baseName, GetDirectionName(dir), pos);
            var clip = SaveAnimationClip(clipName, reversed.ToArray(), folder);
            if (clip != null) output[clip.name] = clip;
        }
    }

    /// <summary>
    /// Creates and saves an animation clip from a sub-range of sprites.
    /// </summary>
    private void CreateAnimationClip(
        List<Sprite> sprites,
        string baseName,
        int startIndex,
        int frameCount,
        string direction,
        Dictionary<string, AnimationClip> output,
        string folder,
        PositionType pos) {
        var frames = new List<Sprite>();
        for (var i = startIndex; i < startIndex + frameCount; i++)
            if (i >= 0 && i < sprites.Count) frames.Add(sprites[i]);

        if (frames.Count == 0) return;
        var clipName = BuildClipName(baseName, direction, pos);
        var clip = SaveAnimationClip(clipName, frames.ToArray(), folder);
        if (clip != null) output[clip.name] = clip;
    }

    /// <summary>
    /// Automatically creates clips for bow animations based on known frame segments.
    /// </summary>
    private void GenerateBowAnimations(List<Sprite> sprites, Dictionary<string, AnimationClip> output, string folder, PositionType pos) {
        if (sprites == null || sprites.Count == 0) return;
        GenerateDirectionalAnimation(sprites, "Bow_RaiseBowAndAim", new[] { 0, 13, 26, 39 }, 9, output, folder, pos);
        GenerateDirectionalAnimation(sprites, "Bow_LooseArrow", new[] { 9, 22, 35, 48 }, 1, output, folder, pos);
        GenerateDirectionalAnimation(sprites, "Bow_GrabNewArrow", new[] { 10, 23, 36, 49 }, 3, output, folder, pos);
        GenerateDirectionalAnimation(sprites, "Bow_AimNewArrow", new[] { 4, 17, 30, 43 }, 5, output, folder, pos);
    }

    /// <summary>
    /// Creates the hurt animation with equal frames per direction.
    /// </summary>
    private void GenerateHurtAnimation(List<Sprite> sprites, Dictionary<string, AnimationClip> output, string folder, PositionType pos) {
        if (sprites == null || sprites.Count == 0) return;
        GenerateSingleDirectionAnimation(sprites, "Hurt", 6, output, folder, pos);
    }

    /// <summary>
    /// Handles slash and slash-reverse logic, either reversing frames or using a separate texture.
    /// </summary>
    private void GenerateSlashAnimations(
        List<Sprite> slashSprites,
        List<Sprite> slashReverseSprites,
        bool slashReverseFromSlash,
        Dictionary<string, AnimationClip> output,
        string folder,
        PositionType pos) {
        if (slashSprites == null || slashSprites.Count == 0) return;
        GenerateSingleDirectionAnimation(slashSprites, "Slash", 6, output, folder, pos);

        if (slashReverseFromSlash)
            GenerateReversedAnimation(slashSprites, "SlashReverse", 6, output, folder, pos);
        else {
            if (slashReverseSprites == null || slashReverseSprites.Count == 0) return;
            GenerateSingleDirectionAnimation(slashReverseSprites, "SlashReverse", 6, output, folder, pos);
        }
    }

    /// <summary>
    /// Creates spellcast animation with equal frames per direction.
    /// </summary>
    private void GenerateSpellcastAnimation(List<Sprite> sprites, Dictionary<string, AnimationClip> output, string folder, PositionType pos) {
        if (sprites == null || sprites.Count == 0) return;
        GenerateSingleDirectionAnimation(sprites, "Spellcast", 7, output, folder, pos);
    }

    /// <summary>
    /// Splits thrust animations into multiple segments by direction.
    /// </summary>
    private void GenerateThrustAnimation(List<Sprite> sprites, Dictionary<string, AnimationClip> output, string folder, PositionType pos) {
        if (sprites == null || sprites.Count == 0) return;
        GenerateDirectionalAnimation(sprites, "Thrust_RaiseStaff", new[] { 0, 8, 16, 24 }, 4, output, folder, pos);
        GenerateDirectionalAnimation(sprites, "Thrust_ThrustLoop", new[] { 4, 12, 20, 28 }, 4, output, folder, pos);
        SetAnimationLooping(output, "Thrust_ThrustLoop", true);
    }

    /// <summary>
    /// Creates idle and walk cycle animations.
    /// </summary>
    private void GenerateWalkcycleAnimation(List<Sprite> sprites, Dictionary<string, AnimationClip> output, string folder, PositionType pos) {
        if (sprites == null || sprites.Count == 0) return;
        GenerateDirectionalAnimation(sprites, "Walkcycle_Idle", new[] { 0, 9, 18, 27 }, 1, output, folder, pos);
        SetAnimationLooping(output, "Walkcycle_Idle", true);
        GenerateDirectionalAnimation(sprites, "Walkcycle_Walkcycle", new[] { 1, 10, 19, 28 }, 8, output, folder, pos);
        SetAnimationLooping(output, "Walkcycle_Walkcycle", true);
    }

    /// <summary>
    /// Sets the looping property for a specific animation clip.
    /// </summary>
    private void SetAnimationLooping(Dictionary<string, AnimationClip> animations, string baseName, bool isLooping) {
        foreach (var anim in animations) {
            if (anim.Key.StartsWith(baseName)) {
                var settings = AnimationUtility.GetAnimationClipSettings(anim.Value);
                settings.loopTime = isLooping;
                AnimationUtility.SetAnimationClipSettings(anim.Value, settings);
            }
        }
    }

    /// <summary>
    /// Builds the final clip name, adding position type and direction if needed.
    /// </summary>
    private string BuildClipName(string baseName, string direction, PositionType pos) {
        return pos switch {
            PositionType.None => string.IsNullOrEmpty(direction) ? baseName : $"{baseName}_{direction}",
            _ => string.IsNullOrEmpty(direction) ? $"{baseName}_{pos}" : $"{baseName}_{pos}_{direction}"
        };
    }

    /// <summary>
    /// Saves an AnimationClip to the specified folder and returns the created clip.
    /// </summary>
    private AnimationClip SaveAnimationClip(string clipName, Sprite[] frames, string folderPath) {
        var clip = new AnimationClip();
        var binding = new EditorCurveBinding {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            keyframes[i] = new ObjectReferenceKeyframe {
                time = i / FPS,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var path = Path.Combine(folderPath, clipName + ".anim");
        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    /// <summary>
    /// Maps generated clips to the override controller's base clips by matching names.
    /// </summary>
    private void PopulateOverridesExact(AnimatorOverrideController overrideController, Dictionary<string, AnimationClip> generatedClips) {
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        var assignedCount = 0;
        foreach (var pair in overrides) {
            var baseClip = pair.Key;
            var searchName = baseClip.name;
            if (!hasFgBg) searchName = RemoveFgBgSuffix(searchName);

            if (generatedClips.TryGetValue(searchName, out var foundClip)) {
                overrideController[baseClip] = foundClip;
                assignedCount++;
            } else {
                Debug.Log($"No generated animation found for '{searchName}'. Skipped override.");
            }
        }

        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();
        Debug.Log($"{overrideController.name}: {assignedCount} clips assigned.");
    }

    /// <summary>
    /// Removes _FG and _BG suffixes if present for single-sheet usage.
    /// </summary>
    private string RemoveFgBgSuffix(string name) {
        return name.Replace("_FG", "").Replace("_BG", "");
    }

    /// <summary>
    /// Saves the given override controller to the disk in the specified directory.
    /// </summary>
    private string SaveAnimatorOverrideController(AnimatorOverrideController controller, string directory, string fileName) {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var assetPath = Path.Combine(directory, fileName + ".overrideController");

        AssetDatabase.CreateAsset(controller, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return assetPath;
    }

    /// <summary>
    /// Converts a numerical direction index to a string (Up, Left, Down, or Right).
    /// </summary>
    private string GetDirectionName(int dir) => dir switch {
        0 => Direction.Up.ToString(),
        1 => Direction.Left.ToString(),
        2 => Direction.Down.ToString(),
        3 => Direction.Right.ToString(),
        _ => ""
    };

    /// <summary>
    /// Simple comparator for sorting numeric segments properly (e.g. "_2" < "_10").
    /// </summary>
    private class NaturalStringComparer : IComparer<string> {
        public int Compare(string x, string y) {
            var xParts = SplitAlphaNum(x);
            var yParts = SplitAlphaNum(y);
            var minCount = Mathf.Min(xParts.Count, yParts.Count);

            for (var i = 0; i < minCount; i++) {
                var left = xParts[i];
                var right = yParts[i];
                if (int.TryParse(left, out var lx) && int.TryParse(right, out var rx)) {
                    var c = lx.CompareTo(rx);
                    if (c != 0) return c;
                } else {
                    var c = left.CompareTo(right);
                    if (c != 0) return c;
                }
            }
            return xParts.Count.CompareTo(yParts.Count);
        }

        private List<string> SplitAlphaNum(string input) {
            var parts = new List<string>();
            if (string.IsNullOrEmpty(input)) return parts;

            var current = new System.Text.StringBuilder();
            bool? isNum = null;
            foreach (var c in input) {
                var cIsDigit = char.IsDigit(c);
                if (isNum == null) {
                    isNum = cIsDigit;
                    current.Append(c);
                } else if (isNum == cIsDigit) {
                    current.Append(c);
                } else {
                    parts.Add(current.ToString());
                    current.Clear();
                    current.Append(c);
                    isNum = cIsDigit;
                }
            }
            if (current.Length > 0) parts.Add(current.ToString());
            return parts;
        }
    }
}
#endif