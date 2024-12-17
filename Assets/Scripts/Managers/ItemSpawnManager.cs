using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


public class ItemSpawnManager : NetworkBehaviour {
    public static ItemSpawnManager Instance { get; private set; }

    public enum SpreadType {
        Circle, 
        Line, 
        None
    }

    [SerializeField] PickUpItem _pickUpItemPrefab;
    [SerializeField] Tilemap _targetTilemap;

    const float PLAYER_THROW_DISTANCE = 2f;
    const float CIRCLE_SPREAD_AMOUNT = 0.6f;
    const float LINE_SPREAD_AMOUNT = 0.15f;
    const float TREE_HEIGHT = 2f;
    const float LEAN_MOVE_TIME = 0.75f; // Ensure synchronization with PickUpItem.cs
    const float DEFAULT_END_POSITION_Z_MULTIPLIER = 0.0001f;


    void Awake() {
        if (Instance != null) {
            Debug.LogError("Multiple ItemSpawnManager instances!");
            return;
        }
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemServerRpc(
        ItemSlot itemSlot,
        Vector2 initialPosition,
        Vector2 motionDirection,
        bool useInventoryPosition = false,
        SpreadType spreadType = SpreadType.None) {

        Vector3 spawnPosition = useInventoryPosition
            ? new Vector3(initialPosition.x, initialPosition.y, -5f)
            : _targetTilemap.GetCellCenterWorld(_targetTilemap.WorldToCell(initialPosition));

        for (int i = 0; i < itemSlot.Amount; i++) {
            Vector3 finalPos = CalculateFinalPosition(spawnPosition, initialPosition, motionDirection, spreadType, i, itemSlot.Amount, useInventoryPosition);

            if (NetworkManager.Singleton.SpawnManager != null) {
                PickUpItem pickUp = Instantiate(_pickUpItemPrefab, spawnPosition, Quaternion.identity, transform);
                pickUp.InitializeItem(new ItemSlot(itemSlot.ItemId, 1, itemSlot.RarityId));
                NetworkObject no = pickUp.GetComponent<NetworkObject>();
                no.Spawn(true);
                SpawnItemClientRpc(finalPos, no.NetworkObjectId, spawnPosition, useInventoryPosition);
            } else {
                Debug.LogError("NetworkManager SpawnManager is null. Cannot spawn items.");
            }
        }
    }

    Vector3 CalculateFinalPosition(
        Vector3 spawnPos,
        Vector2 initialPos,
        Vector2 motionDir,
        SpreadType spread,
        int index,
        int total,
        bool useInventoryPos) {

        if (useInventoryPos) {
            return CalculateThrowPosition(initialPos, motionDir);
        }

        return spread switch {
            SpreadType.Circle => spawnPos + CalculateSpread(CIRCLE_SPREAD_AMOUNT),
            SpreadType.Line => spawnPos + CalculateLineSpread(motionDir, index, total),
            SpreadType.None => spawnPos,
            _ => spawnPos
        };
    }

    Vector3 CalculateThrowPosition(Vector2 playerPos, Vector2 motionDir) {
        Vector3 throwPos = new(
            playerPos.x + motionDir.x * PLAYER_THROW_DISTANCE,
            playerPos.y + motionDir.y * PLAYER_THROW_DISTANCE,
            playerPos.y * DEFAULT_END_POSITION_Z_MULTIPLIER);

        return throwPos + CalculateSpread(CIRCLE_SPREAD_AMOUNT);
    }

    Vector3 CalculateSpread(float amount) => new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount), 0f);

    Vector3 CalculateLineSpread(Vector2 motionDir, int index, int total) {
        Vector3 dir = motionDir.normalized;
        Vector3 spreadVec = dir * TREE_HEIGHT;
        float step = spreadVec.magnitude / (total > 1 ? total - 1 : 1);
        Vector3 linePos = index * step * dir;
        return linePos + CalculateSpread(LINE_SPREAD_AMOUNT);
    }

    [ClientRpc]
    void SpawnItemClientRpc(Vector3 finalPos, ulong pickUpItemNOId, Vector3 spawnPos, bool useInventoryPos) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pickUpItemNOId, out var no)) {
            if (no.TryGetComponent<PickUpItem>(out var pickUp)) {
                if (useInventoryPos) {
                    pickUp.transform.LeanMoveLocal(finalPos, LEAN_MOVE_TIME).setEaseOutQuint();
                } else {
                    Vector3 motionOffset = finalPos - new Vector3(0.5f, 0.5f, 0f);
                    pickUp.StartParabolaAnimation(spawnPos, motionOffset);
                }
            } else {
                Debug.LogError("PickUpItem component missing on spawned object.");
            }
        } else {
            Debug.LogError($"NetworkObject {pickUpItemNOId} not found.");
        }
    }
}
