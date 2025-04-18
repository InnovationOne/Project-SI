using Ink.Runtime;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Rose : PlaceableObject {
    private readonly NetworkVariable<int> _timer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _recipeIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3Int> _partnerCell = new(Vector3Int.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int _itemId;
    private Vector3Int _cellPosition;
    private Animator _animator;
    private SpriteRenderer _visual;

    private const string DestroyAnim = "Smoke_Transition";
    private const float DestroyDelay = 0.5f;
    private const float Spread = 0.1f;

    private static Vector3Int[] PartnerOffsets;
    private static readonly Vector3Int[] SpawnOffsets = GenerateSpawnOffsets();

    private static Vector3Int[] GenerateSpawnOffsets() {
        var list = new List<Vector3Int>();
        // x from -5 to +4 (10 cells), y from -2 to +2 (5 cells)
        for (int y = -2; y <= 2; y++) {
            for (int x = -5; x <= 4; x++) {
                list.Add(new Vector3Int(x, y, 0));
            }
        }
        return list.ToArray();
    }


    private RoseSO RoseSO => GameManager.Instance.ItemManager.ItemDatabase[_itemId] as RoseSO;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;


    #region Lifecycle

    private void Awake() {
        _visual = GetComponent<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>(true);
    }

    public override void InitializePreLoad(int itemId) {
        _itemId = itemId;

        var size = RoseSO.OccupiedSizeInCells;
        var w = size.x;
        var h = size.y;
        PartnerOffsets = new[]
        {
            new Vector3Int( w,  0,0), new Vector3Int(-w,  0,0),
            new Vector3Int( 0,  h,0), new Vector3Int( 0, -h,0),
            new Vector3Int( w,  h,0), new Vector3Int(-w,  h,0),
            new Vector3Int( w, -h,0), new Vector3Int(-w, -h,0)
        };
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var data = PlaceableObjectsManager.Instance.GetData(NetworkObject.NetworkObjectId);
        _cellPosition = data.Position;

        if (IsServer) {
            TryPairLocal();
            GameManager.Instance.TimeManager.OnNextDayStarted += OnNextDayStarted;
        }
    }

    public override void OnNetworkDespawn() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted -= OnNextDayStarted;
        base.OnNetworkDespawn();
    }

    #endregion

    #region Pairing

    private void TryPairLocal() {
        if (_partnerCell.Value != Vector3Int.zero) return;

        foreach (var off in PartnerOffsets) {
            var nbrPos = _cellPosition + off;
            if (PlaceableObjectsManager.Instance.TryGetNetworkIdAt(nbrPos, out var nbrId) &&
                nbrId != NetworkObject.NetworkObjectId &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects[nbrId].TryGetComponent<Rose>(out var nbrRose) &&
                nbrRose._partnerCell.Value == Vector3Int.zero) {
                // find recipe index
                var partnerData = PlaceableObjectsManager.Instance.GetData(nbrId);
                int idx = RoseSO.RoseRecipes.FindIndex(r =>
                    r.Roses.Contains(RoseSO) &&
                    r.Roses.Contains(GameManager.Instance.ItemManager.ItemDatabase[partnerData.ObjectId] as RoseSO));
                if (idx < 0) continue;

                // set both sides
                int time = RoseSO.RoseRecipes[idx].Time;
                _partnerCell.Value = nbrPos;
                _recipeIndex.Value = idx;
                _timer.Value = time;

                nbrRose._partnerCell.Value = _cellPosition;
                nbrRose._recipeIndex.Value = idx;
                nbrRose._timer.Value = time;
                break;
            }
        }
    }

    public override void PickUpItemsInPlacedObject(PlayerController player) {
        if (!IsServer) return;

        // 1. Partner abmelden, falls vorhanden
        if (_partnerCell.Value != Vector3Int.zero) {
            if (PlaceableObjectsManager.Instance.TryGetNetworkIdAt(_partnerCell.Value, out var partnerNetId) &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects[partnerNetId].TryGetComponent<Rose>(out var partnerRose)) {
                partnerRose._partnerCell.Value = Vector3Int.zero;
                partnerRose._recipeIndex.Value = -1;
                partnerRose._timer.Value = 0;
            }
        }
    }

    #endregion

    #region Day Tick & Growth

    private void OnNextDayStarted() {
        // attempt pairing if none
        if (_partnerCell.Value == Vector3Int.zero) {
            TryPairLocal();
            return;
        }

        // countdown timer
        if (_timer.Value > 0) {
            _timer.Value--;
            return;
        }

        if (_recipeIndex.Value < 0) return;

        // spawn new rose per recipe
        int newId = RoseSO.RoseRecipes[_recipeIndex.Value].NewRose.ItemId;

        // collect empty cells in 10×5 area
        var empties = new List<Vector3Int>();
        foreach (var so in SpawnOffsets) {
            var p = _cellPosition + so;
            if (!PlaceableObjectsManager.Instance.TryGetNetworkIdAt(p, out _))
                empties.Add(p);
        }
        if (empties.Count == 0) return;

        var pick = empties[UnityEngine.Random.Range(0, empties.Count)];
        PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(
            new Vector3IntSerializable(pick),
            newId,
            0);

        // reset recipe index
        _recipeIndex.Value = -1;
    }

    #endregion

    #region Interaction: Final Galaxy‑Rose

    public override void Interact(PlayerController player) {
        if (!IsServer) return;

        var finalRecipe = RoseSO.RoseRecipes[^1];
        int toolId = PlayerController.LocalInstance.PlayerToolbeltController
            .GetCurrentlySelectedToolbeltItemSlot()?.ItemId ?? -1;
        if (toolId != finalRecipe.NewRose.ItemForGalaxyRose.ItemId) return;

        // check all required roses in area
        var areaSOs = GetRosesInArea();
        var needed = finalRecipe.Roses;
        if (!needed.All(r => areaSOs.Contains(r))) return;

        StartDestroySequence();
    }

    private void StartDestroySequence() {
        IEnumerator Seq() {
            _animator.enabled = true;
            _animator.Play(DestroyAnim);

            foreach (var so in SpawnOffsets) {
                var pos = _cellPosition + so;
                if (PlaceableObjectsManager.Instance.TryGetNetworkIdAt(pos, out var id))
                    PlaceableObjectsManager.Instance.PickUpObjectServerRpc(id, dropItem: false);
                yield return new WaitForSeconds(DestroyDelay);
            }
            // spawn galaxy rose at center
            PlaceableObjectsManager.Instance.PlaceObjectOnMapServerRpc(
                new Vector3IntSerializable(_cellPosition),
                RoseSO.ItemForGalaxyRose.ItemId,
                0);
        }
        StartCoroutine(Seq());
    }

    private List<RoseSO> GetRosesInArea() {
        var list = new List<RoseSO>();
        foreach (var so in SpawnOffsets) {
            var p = _cellPosition + so;
            if (PlaceableObjectsManager.Instance.TryGetNetworkIdAt(p, out var id)) {
                var data = PlaceableObjectsManager.Instance.GetData(id);
                if (GameManager.Instance.ItemManager.ItemDatabase[data.ObjectId] is RoseSO roseSo)
                    list.Add(roseSo);
            }
        }
        return list;
    }

    #endregion

    #region Save & Load

    public class RoseData {
        public int ItemId;
        public int Timer;
        public int RecipeIdx;
        public Vector3Int PartnerCell;
    }

    public override string SaveObject() {
        return JsonConvert.SerializeObject(new RoseData {
            ItemId = _itemId,
            Timer = _timer.Value,
            RecipeIdx = _recipeIndex.Value,
            PartnerCell = _partnerCell.Value
        });
    }

    public override void LoadObject(string data) {
        if (string.IsNullOrEmpty(data)) return;
        var d = JsonConvert.DeserializeObject<RoseData>(data);
        _itemId = d.ItemId;
        _timer.Value = d.Timer;
        _recipeIndex.Value = d.RecipeIdx;
        _partnerCell.Value = d.PartnerCell;
    }

    #endregion

    public override void InitializePostLoad() { }
    
    public override void OnStateReceivedCallback(string callbackName) { }

    
}
