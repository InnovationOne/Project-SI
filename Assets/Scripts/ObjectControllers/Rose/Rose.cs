using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rose : Interactable, IObjectDataPersistence {
    public int ItemId { get; private set; }
    public Vector3 PartnerPosition { get; private set; }

    private ObjectVisual _visual;
    private Vector3Int _localPosition;
    private int _timer;
    private int _roseToSpawnId = -1;
    private Animator _animator;
    private string _destroyAnimationName = "Smoke_Transition";
    private float _destroyRoseDelay = 0.5f;
    private float _rosePositionSpread = 0.1f;


    private Vector3Int[] possiblePartnerPositions = new Vector3Int[] {
        new(0, 1), new(0, -1), new(1, 0), new(-1, 0), new(1, 1), new(-1, -1), new(1, -1), new(-1, 1)
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
    private void Start() {
        TimeAndWeatherManager.Instance.OnNextDayStarted += OnNextDayStarted;
    }

    private void OnDestroy() {
        TimeAndWeatherManager.Instance.OnNextDayStarted -= OnNextDayStarted;
    }

    /// <summary>
    /// Initializes the rose with the specified item ID.
    /// </summary>
    /// <param name="itemId">The ID of the rose item.</param>
    public void Initialize(int itemId, int galaxyRoseId, Vector3Int position) {
        _animator = GetComponentInChildren<Animator>();
        _animator.enabled = false;
        _animator.transform.position += new Vector3(0, 0.5f);

        ItemId = itemId;
        _visual = GetComponentInChildren<ObjectVisual>();

        if (ItemId == galaxyRoseId) {
            // Galaxy Rose
            StartCoroutine(SpawnWithSmoke());
        } else {
            _visual.SetSprite(RoseSO.InactiveSprite);
        }
                
        Vector2[] vectors = new Vector2[] {
            new(-0.42f, -0.62f),
            new(-0.42f, -1f),
            new(0.42f, -1f),
            new(0.42f, -0.62f)
        };
        _visual.SetCollider(1, true);
        _visual.SetPath(0, vectors);
        _visual.transform.position += new Vector3(0, 0.5f);
        _localPosition = position;
        transform.position += GenerateRandomSpriteRendererPosition();
    }

    private IEnumerator SpawnWithSmoke() {
        _animator.enabled = true;
        _animator.Play(_destroyAnimationName);
        StartCoroutine(DelayToShowRoseSprite());
        yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
        _animator.enabled = false;
        _animator.gameObject.GetComponent<SpriteRenderer>().sprite = null;
    }

    private IEnumerator DelayToShowRoseSprite() {
        yield return new WaitForSeconds(_destroyRoseDelay / 4.5f);
        _visual.SetSprite(RoseSO.InactiveSprite);
    }

    /// <summary>
    /// Generates a random position within a specified range for the SpriteRenderer.
    /// </summary>
    /// <returns>A Vector3 representing the generated position.</returns>
    private Vector3 GenerateRandomSpriteRendererPosition() {
        // Generate and return a new Vector3 with random x and y coordinates within the specified range
        return new Vector3(
            Random.Range(-_rosePositionSpread, _rosePositionSpread),
            Random.Range(-_rosePositionSpread, _rosePositionSpread)
        );
    }

    public void PostLoading() {
        if (PartnerPosition == Vector3.zero) {
            LookForPartner();
        }
    }
    #endregion


    #region Rose Logic
    public override void Interact(Player player) {
        if (RoseSO.RoseRecipes[^1].NewRose.ItemForGalaxyRose.ItemId == PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().ItemId) {
            var list = new List<RoseSO>();
            for (int x = -2; x <= 2; x++) {
                for (int y = -2; y <= 2; y++) {
                    var pos = new Vector3Int(_localPosition.x + x, _localPosition.y + y);
                    if (PlaceableObjectsManager.Instance.POContainer.PlaceableObjects.ContainsKey(pos)) {
                        var rose = PartnerRoseSO(PlaceableObjectsManager.Instance.POContainer[pos].ObjectId);
                        list.Add(rose);
                    }
                }
            }

            foreach (var rose in RoseSO.RoseRecipes[^1].Roses) {
                var item = ItemManager.Instance.ItemDatabase[rose.ItemId] as RoseSO;
                if (!list.Contains(item)) {
                    return;
                }
            }

            StartCoroutine(DestroyObjectsCoroutine());
        }
    }
        
    private IEnumerator DestroyObjectsCoroutine() {
        foreach (var position in spiralPositions) {
            Vector3Int pos = new Vector3Int(_localPosition.x + position.x, _localPosition.y + position.y);
            if (PlaceableObjectsManager.Instance.POContainer.PlaceableObjects.ContainsKey(pos)) {
                var objectId = PlaceableObjectsManager.Instance.POContainer[pos].ObjectId;
                if (ItemManager.Instance.ItemDatabase[objectId] != null) {
                    if (position == spiralPositions[^1]) {
                        PlaceableObjectsManager.Instance.PlaceObjectOnMapDelayed(pos, _destroyRoseDelay);
                    }
                    PlaceableObjectsManager.Instance.DestroyObjectServerRPC(pos);
                    yield return new WaitForSeconds(_destroyRoseDelay);
                }
            }
        }
    }

    private void LookForPartner() {
        foreach (var position in possiblePartnerPositions) {
            Vector3Int partnerPosition = _localPosition + position;
            var partner = PlaceableObjectsManager.Instance.POContainer[partnerPosition];
            if (partner != null && partner.ObjectId != ItemId && partner.Prefab.GetComponent<Rose>().PartnerPosition == Vector3.zero) {
                var recipe = SelectRecipe(partner.ObjectId);
                if (recipe != null) {
                    Debug.Log($"{RoseSO.ItemName} found partner {PartnerRoseSO(partner.ObjectId).ItemName} with recipe {recipe.name}");
                    PartnerPosition = partnerPosition;
                    _roseToSpawnId = recipe.NewRose.ItemId;
                    SetTimer(recipe.Time);
                    PlaceableObjectsManager.Instance.POContainer[new Vector3Int((int)PartnerPosition.x, (int)PartnerPosition.y)].Prefab.GetComponent<Rose>().SetPartner(_localPosition);
                    break;
                }
            }
        }
    }

    private RoseRecipeSO SelectRecipe(int partnerItemId) {
        foreach (var recipe in RoseSO.RoseRecipes) {
            if (recipe.Roses.Contains(PartnerRoseSO(partnerItemId)) &&
                recipe.Roses.Contains(RoseSO) &&
                recipe.Roses.Count == 2) {
                return recipe;
            }
        }
        return null;
    }

    private void SetTimer(int timer) => _timer = timer;

    private void OnNextDayStarted() {
        if (_timer > 0) {
            _timer--;
        } else {
            if (_roseToSpawnId >= 0) {
                _timer = 0;
                SpawnNewRose();
            }
        }
    }

    private void SpawnNewRose() {
        HashSet<Vector3Int> emptyPositions = new HashSet<Vector3Int>();
        foreach (var position in possiblePartnerPositions) {
            Vector3Int emptyPosition = _localPosition + position;
            if (PlaceableObjectsManager.Instance.POContainer[emptyPosition] == null) {
                emptyPositions.Add(emptyPosition);
            }
        }
        foreach (var position in possiblePartnerPositions) {
            Vector3Int emptyPosition = new Vector3Int((int)PartnerPosition.x, (int)PartnerPosition.y) + position;
            if (PlaceableObjectsManager.Instance.POContainer[emptyPosition] == null) {
                emptyPositions.Add(emptyPosition);
            }
        }

        if (emptyPositions.Count > 0) {
            Vector3Int spawnPosition = emptyPositions.ElementAt(Random.Range(0, emptyPositions.Count));
            PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(_roseToSpawnId, spawnPosition);
        }
    }

    public void SetPartner(Vector3Int position) {
        PartnerPosition = position;
    }
    #endregion


    #region Reset and Destroy
    private void ResetRose() {
        PartnerPosition = Vector3.zero;
        _timer = 0;
        _roseToSpawnId = -1;
    }

    public override void PickUpItemsInPlacedObject(Player player) {
        if (PartnerPosition != Vector3.zero) {
            PlaceableObjectsManager.Instance.POContainer[new Vector3Int((int)PartnerPosition.x, (int)PartnerPosition.y)].Prefab.GetComponent<Rose>().ResetRose();
        }
    }

    public void OnObjectDestroyed() {
        _animator.enabled = true;
        StartCoroutine(DelayToHideRoseSprite());
        _animator.Play(_destroyAnimationName);
    }

    private IEnumerator DelayToHideRoseSprite() {
        yield return new WaitForSeconds(_destroyRoseDelay / 4.5f);
        _visual.SetSprite(null);
    }
    #endregion


    /// <summary>
    /// Gets the RoseSO associated with this Rose instance.
    /// </summary>
    private RoseSO RoseSO => ItemManager.Instance.ItemDatabase[ItemId] as RoseSO;

    /// <summary>
    /// Gets the RoseSO associated with the partner Rose.
    /// </summary>
    private RoseSO PartnerRoseSO(int partnerItemId) => ItemManager.Instance.ItemDatabase[partnerItemId] as RoseSO;

    #region Save & Load
    public class RoseData {
        public int ItemId;
        public int Timer;
        public int RoseToSpawnId;
        public Vector3 PartnerPosition;
    }

    public string SaveObject() {
        var roseDataJson = new RoseData {
            ItemId = ItemId,
            Timer = _timer,
            RoseToSpawnId = _roseToSpawnId,
            PartnerPosition = PartnerPosition
        };

        return JsonConvert.SerializeObject(roseDataJson);
    }

    public void LoadObject(string data) {
        if (!string.IsNullOrEmpty(data)) {
            var roseData = JsonConvert.DeserializeObject<RoseData>(data);
            ItemId = roseData.ItemId;
            _timer = roseData.Timer;
            _roseToSpawnId = roseData.RoseToSpawnId;
            PartnerPosition = roseData.PartnerPosition;
        }
    }
    #endregion
}
