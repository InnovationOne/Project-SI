using Unity.Netcode;
using UnityEngine;


public class ItemSpawnManager : NetworkBehaviour {
    public static ItemSpawnManager Instance { get; private set; }

    public enum SpreadType {
        Circle, Line, None
    }

    [SerializeField] private PickUpItem _pickUpItemPrefab;

    private const float PLAYER_THROW_DISTANCE = 2f;
    private const float CIRCLE_SPREAD_AMOUNT = 0.6f;
    private const float LINE_SPREAD_AMOUNT = 0.15f;
    private const float TREE_HEIGHT = 2f;
    private const float LEAN_MOVE_TIME = 0.8f; // When changing the time (.8f) also change the time in PickUpItem.cs


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ItemSpawnManager in the scene!");
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spawns an item on the server and synchronizes it with the clients.
    /// </summary>
    /// <param name="itemSlot">The item slot spawn.</param>
    /// <param name="initialPosition">The initial position of the spawned item.</param>
    /// <param name="motionDirection">The motion direction of the spawned item.</param>
    /// <param name="useInventoryPosition">Whether to use the inventory position for spawning.</param>
    /// <param name="spreadType">The type of spread for the spawned items.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemServerRpc(ItemSlot itemSlot, Vector3 initialPosition, Vector3 motionDirection, bool useInventoryPosition = false, SpreadType spreadType = SpreadType.None) {
        Vector3 spawnPosition = useInventoryPosition ? new Vector3(initialPosition.x, initialPosition.y, -5f) : TilemapManager.Instance.AlignPositionToGridCenter(initialPosition);

        for (int i = 0; i < itemSlot.Amount; i++) {
            Vector3 finalPosition = spawnPosition;
            if (useInventoryPosition) {
                finalPosition = CalculateThrowPosition(initialPosition, motionDirection);
            } else {
                switch (spreadType) {
                    case SpreadType.Circle:
                        finalPosition += CalculateSpread();
                        break;
                    case SpreadType.Line:
                        Vector3 differenceVector = motionDirection * TREE_HEIGHT;
                        float stepLength = differenceVector.magnitude / itemSlot.Amount;
                        finalPosition += i * stepLength * differenceVector + CalculateSpread(LINE_SPREAD_AMOUNT);
                        break;
                    default:
                        Debug.LogError("Undefined Spread Type");
                        break;
                }
            }

            PickUpItem pickUpItem = Instantiate(_pickUpItemPrefab, spawnPosition, Quaternion.identity);
            pickUpItem.InitializeItem(new ItemSlot(itemSlot.ItemId, 1, itemSlot.RarityId));
            NetworkObject pickUpItemNO = pickUpItem.GetComponent<NetworkObject>();
            pickUpItemNO.Spawn(true);

            SpawnItemClientRpc(finalPosition, pickUpItemNO, spawnPosition, useInventoryPosition);
        }
    }

    /// <summary>
    /// Represents a three-dimensional vector.
    /// </summary>
    private Vector3 CalculateThrowPosition(Vector3 playerPosition, Vector3 lastMotionDirection) {
        Vector3 throwPosition = playerPosition + lastMotionDirection * PLAYER_THROW_DISTANCE;
        throwPosition.z = throwPosition.y * 0.0001f;
        return throwPosition += CalculateSpread();
    }

    /// <summary>
    /// Represents a three-dimensional vector.
    /// </summary>
    private Vector3 CalculateSpread(float spread = CIRCLE_SPREAD_AMOUNT) => new(Random.Range(-spread, spread), Random.Range(-spread, spread));

    /// <summary>
    /// Spawns an item on the client using a Remote Procedure Call (RPC).
    /// </summary>
    /// <param name="calculatedPosition">The calculated position where the item should be spawned.</param>
    /// <param name="pickUpItemNOReference">The NetworkObjectReference of the pick-up item.</param>
    /// <param name="spawnPosition">The position where the item should be spawned.</param>
    /// <param name="useInventoryPosition">A flag indicating whether to use the inventory position for the item.</param>
    [ClientRpc]
    private void SpawnItemClientRpc(Vector3 calculatedPosition, NetworkObjectReference pickUpItemNOReference, Vector3 spawnPosition, bool useInventoryPosition) {
        pickUpItemNOReference.TryGet(out NetworkObject pickUpItemNO);

        if (useInventoryPosition) {
            pickUpItemNO.GetComponent<PickUpItem>().transform.LeanMoveLocal(calculatedPosition, LEAN_MOVE_TIME).setEaseOutQuint(); 
        } else {
            pickUpItemNO.GetComponent<PickUpItem>().StartParabolaAnimation(spawnPosition, calculatedPosition - new Vector3(0.5f, 0.5f));
        }
    }
}
