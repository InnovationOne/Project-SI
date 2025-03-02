using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Rose : PlaceableObject {
    public int ItemId { get; private set; }
    public Vector3 PartnerPosition { get; private set; }

    private Vector3Int _localPosition;
    private int _timer;
    private int _roseToSpawnId = -1;
    private Animator _animator;
    private SpriteRenderer _visual;

    private const string _destroyAnimationName = "Smoke_Transition";
    private const float _destroyRoseDelay = 0.5f;
    private const float _rosePositionSpread = 0.1f;


    private static readonly Vector3Int[] possiblePartnerPositions = new Vector3Int[] {
        new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
        new(1, 1), new(-1, -1), new(1, -1), new(-1, 1)
    };

    private static readonly Vector3Int[] spiralPositions = new Vector3Int[] {
    new(-2, 2), new(-1, 2), new(0, 2), new(1, 2), new(2, 2), // top row
    new(2, 1), new(2, 0), new(2, -1), new(2, -2), // right column
    new(1, -2), new(0, -2), new(-1, -2), new(-2, -2), // bottom row
    new(-2, -1), new(-2, 0), new(-2, 1), // left column
    new(-1, 1), new(0, 1), new(1, 1), // inner top row
    new(1, 0), new(1, -1), // inner right column
    new(0, -1), new(-1, -1), // inner bottom row
    new(-1, 0), // inner left column
    new(0, 0)}; // center


    #region Initialization
    private void Start() => GameManager.Instance.TimeManager.OnNextDayStarted += OnNextDayStarted;

    private new void OnDestroy() {
        GameManager.Instance.TimeManager.OnNextDayStarted -= OnNextDayStarted;
        base.OnDestroy();
    } 


    /// <summary>
    /// Initializes the Rose object with the specified item ID, galaxy Rose ID, and position.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="galaxyRoseId">The ID of the galaxy Rose.</param>
    /// <param name="position">The position of the Rose object.</param>
    public void Initialize(int itemId, int galaxyRoseId, Vector3Int position) {
        _animator = GetComponentInChildren<Animator>();
        _animator.enabled = false;
        _animator.transform.position += new Vector3(0, 0.5f);

        ItemId = itemId;
        _visual = GetComponent<SpriteRenderer>();

        if (ItemId == galaxyRoseId) {
            // Galaxy Rose
            StartCoroutine(SpawnWithSmoke());
        }

        SetupVisual();
        _localPosition = position;
        transform.position += GenerateRandomSpriteRendererPosition();
    }

    /// <summary>
    /// Sets up the visual representation of the Rose object.
    /// </summary>
    private void SetupVisual() {
        Vector2[] vectors = new Vector2[] {
            new(-0.42f, -0.62f),
            new(-0.42f, -1f),
            new(0.42f, -1f),
            new(0.42f, -0.62f)
        };
        _visual.transform.position += new Vector3(0, 0.5f);
    }

    /// <summary>
    /// Coroutine for spawning the rose with smoke effect.
    /// </summary>
    private IEnumerator SpawnWithSmoke() {
        _animator.enabled = true;
        _animator.Play(_destroyAnimationName);
        StartCoroutine(DelayToShowRoseSprite());
        yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
        _animator.enabled = false;
        _animator.gameObject.GetComponent<SpriteRenderer>().sprite = null;
    }

    /// <summary>
    /// Coroutine that delays showing the rose sprite.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the coroutine.</returns>
    private IEnumerator DelayToShowRoseSprite() {
        yield return new WaitForSeconds(_destroyRoseDelay / 4.5f);
    }

    /// <summary>
    /// Generates a random position within a specified range for the SpriteRenderer.
    /// </summary>
    /// <returns>A Vector3 representing the generated position.</returns>
    private Vector3 GenerateRandomSpriteRendererPosition() {
        return new Vector3(
            UnityEngine.Random.Range(-_rosePositionSpread, _rosePositionSpread),
            UnityEngine.Random.Range(-_rosePositionSpread, _rosePositionSpread)
        );
    }

    /// <summary>
    /// Performs post-loading operations for the Rose object.
    /// </summary>
    public override void InitializePostLoad() {
        if (PartnerPosition == Vector3.zero) {
            LookForPartner();
        }
    }
    #endregion


    #region Rose Logic
    /// <summary>
    /// Interacts with the player.
    /// </summary>
    /// <param name="player">The player object.</param>
    public override void Interact(PlayerController player) {
        if (RoseSO.RoseRecipes[^1].NewRose.ItemForGalaxyRose.ItemId == PlayerController.LocalInstance.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId &&
            RoseSO.RoseRecipes[^1].Roses.All(rose => GetRosesInArea().Contains(rose))) {
            StartCoroutine(DestroyObjectsCoroutine());
        }
    }

    /// <summary>
    /// Retrieves a list of RoseSO objects within a specified area.
    /// </summary>
    /// <returns>A list of RoseSO objects within the area.</returns>
    private List<RoseSO> GetRosesInArea() {
        var roses = new List<RoseSO>();
        for (int x = -2; x <= 2; x++) {
            for (int y = -2; y <= 2; y++) {
                var pos = new Vector3Int(_localPosition.x + x, _localPosition.y + y);
                PlaceableObjectData? placeableObjectData = GameManager.Instance.PlaceableObjectsManager.GetCropTileAtPosition(pos);
                if (placeableObjectData.HasValue) {
                    roses.Add(PartnerRoseSO(placeableObjectData.Value.ObjectId));
                }                
            }
        }
        return roses;
    }

    /// <summary>
    /// Coroutine that destroys objects in a spiral pattern.
    /// </summary>
    private IEnumerator DestroyObjectsCoroutine() {
        foreach (var position in spiralPositions) {
            Vector3Int pos = new Vector3Int(_localPosition.x + position.x, _localPosition.y + position.y);
            PlaceableObjectData? placeableObjectData = GameManager.Instance.PlaceableObjectsManager.GetCropTileAtPosition(pos);


            if (placeableObjectData.HasValue) {
                PlaceableObjectData placeableObject = placeableObjectData.Value;
                var objectId = placeableObject.ObjectId;
                if (GameManager.Instance.ItemManager.ItemDatabase[objectId] != null) {
                    if (position == spiralPositions[^1]) {
                        //PlaceableObjectsManager.Instance.PlaceObjectOnMapDelayed(pos, _destroyRoseDelay);
                    }
                    //PlaceableObjectsManager.Instance.DestroyObjectServerRPC(pos);
                    yield return new WaitForSeconds(_destroyRoseDelay);
                }
            }
        }
    }

    /// <summary>
    /// Looks for a partner for the Rose object.
    /// </summary>
    private void LookForPartner() {
        foreach (var position in possiblePartnerPositions) {
            Vector3Int partnerPosition = _localPosition + position;
            PlaceableObjectData? placeableObjectData = GameManager.Instance.PlaceableObjectsManager.GetCropTileAtPosition(partnerPosition);
            if (!placeableObjectData.HasValue) {
                Debug.LogWarning("No partner found at position: " + partnerPosition);
            }
            PlaceableObjectData partnerPlaceableObject = placeableObjectData.Value;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(placeableObjectData.Value.PrefabNetworkObjectId, out NetworkObject networkObject) &&
                partnerPlaceableObject.ObjectId != ItemId &&
                networkObject.GetComponent<Rose>().PartnerPosition == Vector3.zero) {

                var recipe = SelectRecipe(partnerPlaceableObject.ObjectId);
                if (recipe != null) {
                    SetPartnerDetails(partnerPosition, recipe);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Sets the partner details for the Rose object.
    /// </summary>
    /// <param name="partnerPosition">The position of the partner.</param>
    /// <param name="recipe">The recipe containing the details for the new Rose object.</param>
    private void SetPartnerDetails(Vector3Int partnerPosition, RoseRecipeSO recipe) {
        PartnerPosition = partnerPosition;
        _roseToSpawnId = recipe.NewRose.ItemId;
        _timer = recipe.Time;
        //PlaceableObjectsManager.Instance.POContainer[partnerPosition].Prefab.GetComponent<Rose>().SetPartner(_localPosition);
    }

    /// <summary>
    /// Represents a recipe for creating a new Rose.
    /// </summary>
    private RoseRecipeSO SelectRecipe(int partnerItemId) =>
        RoseSO.RoseRecipes.FirstOrDefault(recipe =>
            recipe.Roses.Contains(PartnerRoseSO(partnerItemId)) &&
            recipe.Roses.Contains(RoseSO) &&
            recipe.Roses.Count == 2);

    /// <summary>
    /// Called when the next day starts.
    /// Decreases the timer and spawns a new rose if the timer reaches zero and a rose to spawn ID is set.
    /// </summary>
    private void OnNextDayStarted() {
        if (_timer > 0) {
            _timer--;
        } else if (_roseToSpawnId >= 0) {
            _timer = 0;
            SpawnNewRose();
        }
    }

    /// <summary>
    /// Spawns a new rose object on the map at an empty position.
    /// </summary>
    private void SpawnNewRose() {
        var emptyPositions = GetEmptyPositions();
        if (emptyPositions.Any()) {
            var spawnPosition = emptyPositions.ElementAt(UnityEngine.Random.Range(0, emptyPositions.Count));
            //PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(_roseToSpawnId, spawnPosition);
        }
    }

    /// <summary>
    /// Retrieves a set of empty positions that can be occupied by the Rose object or its partner.
    /// </summary>
    /// <returns>A HashSet of Vector3Int representing the empty positions.</returns>
    private HashSet<Vector3Int> GetEmptyPositions() {
        var emptyPositions = new HashSet<Vector3Int>();

        foreach (var position in possiblePartnerPositions) {
            var localEmptyPosition = _localPosition + position;
            var partnerEmptyPosition = new Vector3Int((int)PartnerPosition.x, (int)PartnerPosition.y) + position;
            /*
            if (PlaceableObjectsManager.Instance.POContainer[localEmptyPosition] == null) {
                emptyPositions.Add(localEmptyPosition);
            }

            if (PlaceableObjectsManager.Instance.POContainer[partnerEmptyPosition] == null) {
                emptyPositions.Add(partnerEmptyPosition);
            }*/
        }

        return emptyPositions;
    }

    /// <summary>
    /// Sets the partner details for the Rose object.
    /// </summary>
    /// <param name="partnerPosition">The position of the partner.</param>
    public void SetPartner(Vector3Int position) => PartnerPosition = position;

    #endregion


    #region Reset and Destroy
    /// <summary>
    /// Resets the state of the Rose object.
    /// </summary>
    private void ResetRose() {
        PartnerPosition = Vector3.zero;
        _timer = 0;
        _roseToSpawnId = -1;
    }

    /// <summary>
    /// Picks up items in the placed object and performs additional actions if a partner Rose object is present.
    /// </summary>
    /// <param name="player">The player performing the action.</param>
    public override void PickUpItemsInPlacedObject(PlayerController player) {
        if (PartnerPosition != Vector3.zero) {
            //var partnerRose = PlaceableObjectsManager.Instance.POContainer[new Vector3Int((int)PartnerPosition.x, (int)PartnerPosition.y)].Prefab.GetComponent<Rose>();
            //partnerRose.ResetRose();
        }
    }

    /// <summary>
    /// Called when the object is destroyed.
    /// </summary>
    public void OnObjectDestroyed() {
        _animator.enabled = true;
        StartCoroutine(DelayToHideRoseSprite());
        _animator.Play(_destroyAnimationName);
    }

    /// <summary>
    /// Coroutine to delay hiding the rose sprite.
    /// </summary>
    /// <returns>An IEnumerator used for coroutine execution.</returns>
    private IEnumerator DelayToHideRoseSprite() {
        yield return new WaitForSeconds(_destroyRoseDelay / 4.5f);
        //_visual.SetSprite(null);
    }
    #endregion


    /// <summary>
    /// Gets the RoseSO associated with this Rose instance.
    /// </summary>
    private RoseSO RoseSO => GameManager.Instance.ItemManager.ItemDatabase[ItemId] as RoseSO;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;


    /// <summary>
    /// Gets the RoseSO associated with the partner Rose.
    /// </summary>
    private RoseSO PartnerRoseSO(int partnerItemId) => GameManager.Instance.ItemManager.ItemDatabase[partnerItemId] as RoseSO;

    #region Save & Load
    public class RoseData {
        public int ItemId;
        public int Timer;
        public int RoseToSpawnId;
        public Vector3 PartnerPosition;
    }

    public override string SaveObject() {
        var roseDataJson = new RoseData {
            ItemId = ItemId,
            Timer = _timer,
            RoseToSpawnId = _roseToSpawnId,
            PartnerPosition = PartnerPosition
        };

        return JsonConvert.SerializeObject(roseDataJson);
    }

    public override void LoadObject(string data) {
        if (!string.IsNullOrEmpty(data)) {
            var roseData = JsonConvert.DeserializeObject<RoseData>(data);
            ItemId = roseData.ItemId;
            _timer = roseData.Timer;
            _roseToSpawnId = roseData.RoseToSpawnId;
            PartnerPosition = roseData.PartnerPosition;
        }
    }

    #endregion


    public override void InitializePreLoad(int itemId) { }
}
