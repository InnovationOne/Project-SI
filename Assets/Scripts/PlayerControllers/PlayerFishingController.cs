using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static FishSO;

/// <summary>
/// Represents a range of button presses required to reel in different sizes of fish.
/// </summary>
[Serializable]
public struct FishButtonPressRange {
    public int MinPresses;
    public int MaxPresses;

    public FishButtonPressRange(int min, int max) {
        MinPresses = min;
        MaxPresses = max;
    }
}

/// <summary>
/// Controls the player's fishing mechanics, including casting, waiting for fish bites,
/// and reeling in fish through a button press minigame.
/// </summary>
public class PlayerFishingController : MonoBehaviour {
    #region Serialized Fields
    [Header("Fishing Setup")]
    [SerializeField] private GameObject _bobberPrefab; // Prefab for the bobber (float)
    [SerializeField] private LineRenderer _lineRendererPrefab; // Prefab for the fishing line
    [SerializeField] private FishDatabaseSO _fishDatabaseSO; // ScriptableObject containing fish data
    [SerializeField] private FishingRodToolSO _fishingRod;

    [Header("UI Elements")]
    [SerializeField] private Image _fishCatchText; // UI Image to display catch messages
    [SerializeField] private SpriteRenderer _alertPopup; // SpriteRenderer for alert popups

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] private float _bobberAlphaAdjustment = 0.5f;
    #endregion

    #region Constants
    private const string FISHING_TILEMAP_TAG = "FishingTilemap";
    private const string CATCH_TEXT_TAG = "CatchText";

    // Constants for fishing mechanics
    private const float MAX_CASTING_DISTANCE = 2.5f; // Maximum distance the rod can cast
    private const float CASTING_SPEED = 1.8f; // Speed at which casting distance increases
    private const float TIME_TO_BITE_MIN = 5f; // Minimum time before a fish bites
    private const float TIME_TO_BITE_MAX = 15f; // Maximum time before a fish bites
    private const float CAST_ARC_HEIGHT = 1.5f; // Height of the casting arc (parabola)
    private const float LINE_SAG_HEIGHT = 0.1f; // Sag height of the fishing line
    private const int SEGMENT_COUNT = 20; // Number of segments in the fishing line

    // UI timing constants
    private const float SHOW_TIME = 5f; // Duration to show catch text
    private const float FADE_DURATION = 0.2f; // Duration for UI fade animations
    private const float TIME_TO_CATCH_FISH = 4.5f; // Time allowed to catch a fish
    private const float TIME_TO_START_MINIGAME = 0.8f; // Time allowed to start reeling in
    private const float COOLDOWN_TO_FISH_AGAIN = 0.8f; // Cooldown after fishing

    // References to in-game objects and components
    private Tilemap _fishingTilemap; // Tilemap determining valid fishing areas
    private GameObject _bobber; // Instance of the bobber
    private LineRenderer _lineRenderer; // Instance of the fishing line
    private SpriteRenderer _bobberSpriteRenderer; // SpriteRenderer of the bobber
    private TextMeshProUGUI _catchTextTMP; // Text component for catch messages
    private PlayerToolbeltController _toolbelt;

    private static readonly FishButtonPressRange[] _pressRanges = new FishButtonPressRange[]
    {
        new(3, 5),    // VerySmall
        new(5, 8),    // Small
        new(8, 12),   // Medium
        new(12, 16),  // Large
        new(16, 20),  // VeryLarge
        new(20, 30)   // Leviathan
    };

    /// <summary>
    /// Enumerates the different types of fishing tiles.
    /// </summary>
    private enum TileType {
        Invalid = -1, // Represents an invalid or unrecognized tile
        Coast = 0,
        Sea = 1,
        DeepSea = 2,
        River = 3,
        Lake = 4
    }

    /// <summary>
    /// Represents the different states of the fishing process.
    /// </summary>
    private enum FishingState {
        Idle, // Player is not fishing
        Casting, // Player is in the process of casting
        Fishing, // Line is cast, waiting for a fish
        ReelingIn // Player is reeling in a fish
    }

    private FishingState _currentState = FishingState.Idle;
    #endregion

    #region Private Fields
    private Vector3 _fishingRodTip; // Position where the fishing line originates
    private float _currentCastingDistance = 0f; // Current casting distance during casting
    private bool _fishIsBiting = false; // Indicates if a fish is currently biting
    private FishSO _currentFish; // The current fish that has bitten
    private int _tileId = -1; // ID of the tile where the bobber landed
    private float _currentTimeToStartMinigame = TIME_TO_START_MINIGAME; // Timer for reeling in
    private int _presses = 0; // Number of required button presses to reel in
    private int _pressCount = 0; // Current number of button presses
    private float _timeToCatchFish = TIME_TO_CATCH_FISH; // Time left to successfully reel in a fish
    private float _currentCooldown = 0f; // Cooldown timer before the next fishing attempt

    private bool _isLeftClickHeld = false; // Flag for hold down left mouse button

    // Coroutine references to manage active coroutines
    private Coroutine _castLineCoroutine;
    private Coroutine _waitForFishCoroutine;

    // Preallocated arrays to minimize memory allocations
    private Vector3[] _linePositionsBuffer = new Vector3[SEGMENT_COUNT];
    #endregion

    #region Unity Callbacks
    private void Awake() {
        if (_alertPopup != null) {
            _alertPopup.enabled = false;
        } else {
            Debug.LogError("AlertPopup SpriteRenderer is not assigned.");
        }
    }

    /// <summary>
    /// Sets up initial references and validates component assignments.
    /// </summary>
    private void Start() {
        InitializeToolbelt();
        InitializeInput();
        InitializeFishData();
        AssignFishingTilemap();
        AssignCatchText();
    }

    /// <summary>
    /// Main update loop handling different fishing states and input.
    /// </summary>
    void Update() {
        if (_toolbelt != null && _fishingRod.ItemId != _toolbelt.GetCurrentlySelectedToolbeltItemSlot().ItemId) {
            return;
        }

        if (_currentCooldown > 0) {
            _currentCooldown -= Time.deltaTime;
            return;
        }

        // Update the fishing rod tip position based on player's current position
        _fishingRodTip = transform.position;


        if (_fishIsBiting && _currentState == FishingState.Fishing) {
            // Countdown timer while waiting to reel in
            _currentTimeToStartMinigame -= Time.deltaTime;
            if (_currentTimeToStartMinigame < 0) {
                ResetBitingState();
            }
        }

        // Handle current fishing state
        switch (_currentState) {
            case FishingState.Casting:
                HandleCastingState();
                break;
            case FishingState.ReelingIn:
                if (_timeToCatchFish <= 0) {
                    // Reset minigame variables and wait for another fish
                    _pressCount = 0;
                    _currentFish = null;
                    _fishIsBiting = false;
                    _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
                    _currentState = FishingState.Fishing;
                    _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
                    return;
                }
                break;
        }
    }

    private void OnDestroy() {
        UnsubscribeInput();
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initializes the player's toolbelt controller reference.
    /// </summary>
    private void InitializeToolbelt() {
        if (PlayerToolbeltController.LocalInstance == null) {
            Debug.LogError("No PlayerToolbeltController instance found.");
            return;
        }
        _toolbelt = PlayerToolbeltController.LocalInstance;
    }

    /// <summary>
    /// Initializes input system event subscriptions.
    /// </summary>
    private void InitializeInput() {
        if (InputManager.Instance == null) {
            Debug.LogError("No InputManager found.");
            return;
        }
        InputManager.Instance.OnLeftClickAction += OnLeftClickAction;
        InputManager.Instance.OnLeftClickStarted += OnLeftClickStarted;
        InputManager.Instance.OnLeftClickCanceled += OnLeftClickCanceled;
    }

    /// <summary>
    /// Unsubscribes from input system events to prevent memory leaks.
    /// </summary>
    private void UnsubscribeInput() {
        if (InputManager.Instance != null) {
            InputManager.Instance.OnLeftClickAction -= OnLeftClickAction;
            InputManager.Instance.OnLeftClickStarted -= OnLeftClickStarted;
            InputManager.Instance.OnLeftClickCanceled -= OnLeftClickCanceled;
        }
    }

    /// <summary>
    /// Initializes fish data from the database.
    /// </summary>
    private void InitializeFishData() {
        if (_fishDatabaseSO != null) {
            _fishDatabaseSO.InitializeFishData();
        } else {
            Debug.LogError("FishDatabaseSO is not assigned.");
        }
    }

    /// <summary>
    /// Assigns the fishing tilemap reference.
    /// </summary>
    private void AssignFishingTilemap() {
        GameObject fishingTilemapGO = GameObject.FindGameObjectWithTag(FISHING_TILEMAP_TAG);
        if (fishingTilemapGO != null) {
            if (!fishingTilemapGO.TryGetComponent(out _fishingTilemap)) {
                Debug.LogError("Tilemap component not found on FishingTilemap GameObject.");
            }
        } else {
            Debug.LogError($"No GameObject found with tag '{FISHING_TILEMAP_TAG}'.");
        }
    }

    /// <summary>
    /// Assigns the catch text UI references.
    /// </summary>
    private void AssignCatchText() {
        GameObject catchTextGO = GameObject.FindGameObjectWithTag(CATCH_TEXT_TAG);
        if (catchTextGO != null) {
            if (catchTextGO.TryGetComponent(out Image fishCatchImage)) {
                _fishCatchText = fishCatchImage;
                _catchTextTMP = fishCatchImage.GetComponentInChildren<TextMeshProUGUI>();
                if (_catchTextTMP == null) {
                    Debug.LogError("TextMeshProUGUI component not found in CatchText GameObject.");
                } else {
                    _fishCatchText.gameObject.SetActive(false); // Hide catch text initially
                }
            } else {
                Debug.LogError("Image component not found on CatchText GameObject.");
            }
        } else {
            Debug.LogError($"No GameObject found with tag '{CATCH_TEXT_TAG}'.");
        }
    }
    #endregion

    #region Input Handlers
    private void OnLeftClickAction() {
        if (_currentCooldown > 0 || (_toolbelt != null && _fishingRod.ItemId != _toolbelt.GetCurrentlySelectedToolbeltItemSlot().ItemId)) {
            return;
        }

        switch (_currentState) {
            case FishingState.Idle:
                PlayerMovementController.LocalInstance.SetCanMoveAndTurn(false);
                HandleIdleState();
                break;

            case FishingState.Fishing:
                HandleFishingState();
                break;

            case FishingState.ReelingIn:
                ReelInMinigame();
                break;
        }
    }

    private void OnLeftClickStarted() {
        if (_currentCooldown > 0) {
            return;
        }

        _isLeftClickHeld = true;
    }

    private void OnLeftClickCanceled() {
        if (_currentCooldown > 0) {
            return;
        }

        _isLeftClickHeld = false;

        if (_currentState == FishingState.Casting) {
            StopPreview();
            _currentState = FishingState.Fishing;
            _castLineCoroutine ??= StartCoroutine(CastLine());
        }
    }
    #endregion

    #region State Handlers
    /// <summary>
    /// Handles input and actions when in the Idle state.
    /// </summary>
    private void HandleIdleState() {
        _currentState = FishingState.Casting;
        StartPreview();
    }

    /// <summary>
    /// Handles casting mechanics, updating casting distance and finalizing cast.
    /// </summary>
    private void HandleCastingState() {
        if (_isLeftClickHeld) {
            // Increase casting distance while LMB is held down
            _currentCastingDistance += CASTING_SPEED * Time.deltaTime;
            _currentCastingDistance = Mathf.Clamp(_currentCastingDistance, 0, MAX_CASTING_DISTANCE);
            UpdatePreview();
        }
    }

    /// <summary>
    /// Handles fishing mechanics, including waiting for fish bites and reeling in without a catch.
    /// </summary>
    private void HandleFishingState() {
        // Reel in with a catch
        if (_fishIsBiting && _currentTimeToStartMinigame >= 0) {
            ReelInWithCatch();
            return;
        }

        ReelInWithoutCatch();
    }
    #endregion

    #region Casting Methods
    /// <summary>
    /// Initiates the preview of the fishing cast, displaying the bobber and fishing line.
    /// </summary>
    private void StartPreview() {
        // Instantiate and configure the bobber if it doesn't exist
        if (_bobber == null) {
            _bobber = Instantiate(_bobberPrefab, _fishingRodTip, Quaternion.identity);
            if (_bobber.TryGetComponent(out _bobberSpriteRenderer)) {
                // Adjust alpha to indicate preview state
                _bobberSpriteRenderer.color = new Color(
                    _bobberSpriteRenderer.color.r,
                    _bobberSpriteRenderer.color.g,
                    _bobberSpriteRenderer.color.b,
                    Mathf.Clamp01(_bobberSpriteRenderer.color.a - _bobberAlphaAdjustment)
                );
            } else {
                Debug.LogError("SpriteRenderer not found on bobber prefab.");
            }
        }

        // Instantiate and configure the fishing line if it doesn't exist
        if (_lineRenderer == null) {
            if (Instantiate(_lineRendererPrefab.gameObject, _fishingRodTip, Quaternion.identity).TryGetComponent(out _lineRenderer)) {
                _lineRenderer.startWidth = 0.04f;
            } else {
                Debug.LogError("LineRenderer component not found on LineRenderer prefab.");
            }
        }
    }

    /// <summary>
    /// Updates the preview of the fishing cast by moving the bobber and updating the fishing line.
    /// </summary>
    private void UpdatePreview() {
        // Ensure PlayerMovementController instance exists
        if (PlayerMovementController.LocalInstance == null) {
            Debug.LogError("PlayerMovementController.LocalInstance is null.");
            return;
        }

        // Calculate the direction and position of the cast based on player movement
        Vector3 castDirection = PlayerMovementController.LocalInstance.LastMotionDirection.normalized;
        Vector3 castPosition = _fishingRodTip + castDirection * _currentCastingDistance;
        _bobber.transform.position = castPosition;

        // Generate and set positions for the casting arc (parabola)
        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = (float)i / (SEGMENT_COUNT - 1);
            _linePositionsBuffer[i] = CalculateArcPoint(t, _fishingRodTip, castPosition, CAST_ARC_HEIGHT);
        }
        _lineRenderer.positionCount = SEGMENT_COUNT;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    /// <summary>
    /// Stops the casting preview by resetting the bobber's alpha.
    /// </summary>
    private void StopPreview() {
        if (_bobberSpriteRenderer != null) {
            // Restore the bobber's alpha after preview
            var color = _bobberSpriteRenderer.color;
            _bobberSpriteRenderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a + _bobberAlphaAdjustment));
        }
    }

    /// <summary>
    /// Coroutine that handles the casting of the fishing line, moving the bobber along the arc.
    /// </summary>
    private IEnumerator CastLine() {
        // Ensure PlayerMovementController instance exists
        if (PlayerMovementController.LocalInstance == null) {
            Debug.LogError("PlayerMovementController.LocalInstance is null.");
            ReelInWithoutCatch();
            yield break;
        }

        // Calculate the direction and final position of the cast
        Vector3 castDirection = PlayerMovementController.LocalInstance.LastMotionDirection.normalized;
        Vector3 castPosition = _fishingRodTip + castDirection * _currentCastingDistance;

        // Animate the bobber moving along the casting arc
        for (int i = 0; i < SEGMENT_COUNT; i++) {
            float t = (float)i / (SEGMENT_COUNT - 1);
            Vector3 arcPoint = CalculateArcPoint(t, _fishingRodTip, castPosition, CAST_ARC_HEIGHT);
            _bobber.transform.position = arcPoint;

            // Update the fishing line to follow the bobber
            DrawFishingLine(_fishingRodTip, _bobber.transform.position, SEGMENT_COUNT);

            yield return null; // Wait for the next frame
        }

        // Determine the tile where the bobber landed
        TileBase castTile = _fishingTilemap.GetTile(_fishingTilemap.WorldToCell(castPosition));
        if (castTile == null) {
            Debug.Log("The landed tile is not a valid fishing area.");
            ReelInWithoutCatch();
            yield break;
        }

        // Retrieve the tile type based on its name using the TileType enum
        if (!Enum.TryParse(castTile.name, out TileType tileType)) {
            tileType = TileType.Invalid;
            Debug.LogError($"Landed tile '{castTile.name}' has an invalid name.");
        }
        _tileId = (int)tileType;

        // Play a splash sound here
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WaterDropSFX, transform.position);

        // Start waiting for a fish to bite
        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());

        // Reset coroutine reference after completion
        _castLineCoroutine = null;
    }
    #endregion

    #region Fishing Methods
    /// <summary>
    /// Coroutine that waits for a random time before a fish bites.
    /// </summary>
    private IEnumerator WaitForFish() {
        // Determine a random time for the fish to bite within the defined range and fishing rod rarity
        float timeToBite = UnityEngine.Random.Range(TIME_TO_BITE_MIN, TIME_TO_BITE_MAX) * (1 - (_fishingRod.BiteRate[_toolbelt.GetCurrentlySelectedToolbeltItemSlot().RarityId - 1] / 100f));
        yield return new WaitForSeconds(timeToBite);

        // Retrieve a fish based on the tile ID and fishing method
        if (_fishDatabaseSO != null) {
            _currentFish = _fishDatabaseSO.GetFish(_fishingRod, _tileId, CatchingMethod.FishingRod);
            if (_currentFish == null) {
                Debug.LogError($"No fish found for tileId {_tileId} using FishingRod method.");
                ReelInWithoutCatch();
                yield break;
            }
        } else {
            Debug.LogError("FishDatabaseSO is not assigned.");
            ReelInWithoutCatch();
            yield break;
        }

        // Simulate a fish bite
        _fishIsBiting = true;

        // Play a alert sound here
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.FishBitSFX, transform.position);

        // Enable the alert popup
        if (_alertPopup != null) {
            _alertPopup.enabled = true;
        }

        // Reset coroutine reference after completion
        _waitForFishCoroutine = null;
    }

    /// <summary>
    /// Reels in the fishing line without catching a fish.
    /// </summary>
    private void ReelInWithoutCatch() {
        ResetVariables();
    }

    /// <summary>
    /// Initiates the minigame for reeling in a caught fish.
    /// </summary>
    private void ReelInWithCatch() {
        if (_currentFish == null) {
            Debug.LogError("Attempted to reel in with catch, but _fish is null.");
            return;
        }

        // Disable the alert popup as reeling is starting
        _alertPopup.enabled = false;

        // Update state to ReelingIn
        _currentState = FishingState.ReelingIn;

        // Set the timer for reeling in
        _timeToCatchFish = TIME_TO_CATCH_FISH;

        // Determine the number of required button presses based on fish size
        _presses = UnityEngine.Random.Range(
            _pressRanges[(int)_currentFish.FishSize].MinPresses,
            _pressRanges[(int)_currentFish.FishSize].MaxPresses + 1 // +1 to make MaxPresses inclusive
        );
    }

    /// <summary>
    /// Handles the minigame where the player must press LMB multiple times to catch the fish.
    /// </summary>
    private void ReelInMinigame() {
        // Check for LMB key press to count button presses
        _pressCount++;

        // Check if the required number of presses has been reached
        if (_pressCount >= _presses) {
            _currentState = FishingState.Fishing;

            // Display a message indicating a successful catch
            StartCoroutine(SetFishCatchText($"You caught a {_currentFish.FishItem.ItemName}. It is {_currentFish.CalculateFishSize()} cm long.\n{_currentFish.CatchText[UnityEngine.Random.Range(0, _currentFish.CatchText.Length)]}"));

            // Add the caught fish to the player's inventory
            if (PlayerInventoryController.LocalInstance != null && PlayerInventoryController.LocalInstance.InventoryContainer != null) {
                PlayerInventoryController.LocalInstance.InventoryContainer.AddItem(new ItemSlot(_currentFish.FishItem.ItemId, 1, 0), false);
            } else {
                Debug.LogError("PlayerInventoryController or InventoryContainer is null.");
            }

            // Set cooldown before next fishing attempt
            _currentCooldown = COOLDOWN_TO_FISH_AGAIN;

            // Reset fishing variables
            ResetVariables();
            return;
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Calculates a point along a parabolic arc between start and end points.
    /// </summary>
    /// <param name="t">Normalized time (0 to 1).</param>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <param name="height">Height of the arc.</param>
    /// <returns>Calculated position on the arc.</returns>
    private Vector3 CalculateArcPoint(float t, Vector3 start, Vector3 end, float height) =>
        Vector3.Lerp(start, end, t) + height * Mathf.Sin(t * Mathf.PI) * Vector3.up;

    /// <summary>
    /// Calculates a point with a sag effect between two points.
    /// </summary>
    /// <param name="t">Normalized time (0 to 1).</param>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <param name="sagHeight">Height of the sag.</param>
    /// <returns>Calculated position with sag.</returns>
    private Vector3 CalculateSagPoint(float t, Vector3 start, Vector3 end, float sagHeight) =>
        Vector3.Lerp(start, end, t) - Mathf.Sin(t * Mathf.PI) * sagHeight * Vector3.up;

    /// <summary>
    /// Draws the fishing line with a sag effect between two points.
    /// </summary>
    /// <param name="start">Start position of the line.</param>
    /// <param name="end">End position of the line.</param>
    /// <param name="segmentCount">Number of segments in the line.</param>
    private void DrawFishingLine(Vector3 start, Vector3 end, int segmentCount) {
        if (_lineRenderer == null) {
            return; // Prevent errors if LineRenderer is missing
        }

        for (int i = 0; i < segmentCount; i++) {
            float t = (float)i / (segmentCount - 1);
            _linePositionsBuffer[i] = CalculateSagPoint(t, start, end, LINE_SAG_HEIGHT);
        }
        _lineRenderer.positionCount = segmentCount;
        _lineRenderer.SetPositions(_linePositionsBuffer);
    }

    /// <summary>
    /// Resets all fishing-related variables and cleans up instantiated objects.
    /// </summary>
    private void ResetVariables() {
        // Destroy and nullify the bobber
        if (_bobber != null) {
            Destroy(_bobber);
            _bobber = null;
            _bobberSpriteRenderer = null;
        }

        // Destroy and nullify the fishing line
        if (_lineRenderer != null) {
            Destroy(_lineRenderer.gameObject);
            _lineRenderer = null;
        }

        // Reset all state variables
        _fishIsBiting = false;
        _currentCastingDistance = 0f;
        _currentFish = null;
        _timeToCatchFish = TIME_TO_CATCH_FISH;
        _pressCount = 0;
        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;
        _tileId = -1;

        // Reset state to Idle
        _currentState = FishingState.Idle;

        // Stoppe die CastLine-Coroutine, falls sie noch läuft
        if (_castLineCoroutine != null) {
            StopCoroutine(_castLineCoroutine);
            _castLineCoroutine = null;
        }

        // Stoppe die WaitForFish-Coroutine, falls sie noch läuft
        if (_waitForFishCoroutine != null) {
            StopCoroutine(_waitForFishCoroutine);
            _waitForFishCoroutine = null;
        }

        PlayerMovementController.LocalInstance.SetCanMoveAndTurn(true);
    }

    /// <summary>
    /// Resets the biting state and waits for another fish.
    /// </summary>
    private void ResetBitingState() {
        _fishIsBiting = false;

        // Disable the alert popup
        if (_alertPopup != null) {
            _alertPopup.enabled = false;
        }

        _currentTimeToStartMinigame = TIME_TO_START_MINIGAME;

        _waitForFishCoroutine ??= StartCoroutine(WaitForFish());
    }

    /// <summary>
    /// Displays a message on the UI for a set duration with fade-in and fade-out effects.
    /// </summary>
    /// <param name="text">Text to display.</param>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator SetFishCatchText(string text) {
        if (_fishCatchText != null && _catchTextTMP != null) {
            _fishCatchText.gameObject.SetActive(true); // Show the catch text UI
            _catchTextTMP.text = text; // Set the message text
            _fishCatchText.canvasRenderer.SetAlpha(0f); // Start fully transparent
            _fishCatchText.CrossFadeAlpha(1f, FADE_DURATION, false); // Fade in

            yield return new WaitForSeconds(SHOW_TIME); // Wait while text is visible

            _fishCatchText.CrossFadeAlpha(0f, FADE_DURATION, false); // Fade out
            yield return new WaitForSeconds(FADE_DURATION); // Wait for fade-out to complete
            _fishCatchText.gameObject.SetActive(false); // Hide the catch text UI
        } else {
            Debug.LogError("Fish catch text or TextMeshProUGUI component is null.");
        }
    }
    #endregion
}
