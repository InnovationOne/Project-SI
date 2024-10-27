using Unity.Netcode;
using UnityEngine;


public class ItemSpawnManager : NetworkBehaviour {
    public static ItemSpawnManager Instance { get; private set; }

    // Enum for Spread Types
    public enum SpreadType {
        Circle, 
        Line, 
        None
    }

    // Serialized Fields
    [SerializeField] private PickUpItem _pickUpItemPrefab;

    // Constants
    private const float PLAYER_THROW_DISTANCE = 2f;
    private const float CIRCLE_SPREAD_AMOUNT = 0.6f;
    private const float LINE_SPREAD_AMOUNT = 0.15f;
    private const float TREE_HEIGHT = 2f;
    private const float LEAN_MOVE_TIME = 0.75f; // Ensure synchronization with PickUpItem.cs
    private const float DEFAULT_END_POSITION_Z_MULTIPLIER = 0.0001f;


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
    /// <param name="itemSlot">The item slot to spawn.</param>
    /// <param name="initialPosition">The initial position for spawning.</param>
    /// <param name="motionDirection">The direction in which the item is thrown.</param>
    /// <param name="useInventoryPosition">Flag to use inventory position for spawning.</param>
    /// <param name="spreadType">Type of spread for item distribution.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemServerRpc(
        ItemSlot itemSlot, 
        Vector2 initialPosition, 
        Vector2 motionDirection, 
        bool useInventoryPosition = false, 
        SpreadType spreadType = SpreadType.None) {

        Vector3 spawnPosition = useInventoryPosition 
            ? new Vector3(initialPosition.x, initialPosition.y, -5f) 
            : TilemapManager.Instance.AlignPositionToGridCenter(initialPosition);

        for (int i = 0; i < itemSlot.Amount; i++) {
            Vector3 finalPosition = CalculateFinalPosition(spawnPosition, initialPosition, motionDirection, spreadType, i, itemSlot.Amount, useInventoryPosition);

            // Instantiate and initialize the PickUpItem
            if (NetworkManager.Singleton.SpawnManager != null) {
                PickUpItem pickUpItem = Instantiate(_pickUpItemPrefab, spawnPosition, Quaternion.identity, transform);
                pickUpItem.InitializeItem(new ItemSlot(itemSlot.ItemId, 1, itemSlot.RarityId));
                NetworkObject pickUpItemNO = pickUpItem.GetComponent<NetworkObject>();
                pickUpItemNO.Spawn(true);

                // Spawn on clients
                SpawnItemClientRpc(finalPosition, pickUpItemNO.NetworkObjectId, spawnPosition, useInventoryPosition);
            } else {
                Debug.LogError("NetworkManager SpawnManager is null. Cannot spawn items.");
            }
        }
    }

    /// <summary>
    /// Calculates the final position of the spawned item based on spread type.
    /// </summary>
    private Vector3 CalculateFinalPosition(
        Vector3 spawnPosition,
        Vector2 initialPosition,
        Vector2 motionDirection,
        SpreadType spreadType,
        int index,
        int totalAmount,
        bool useInventoryPosition) {
        Vector3 finalPosition = spawnPosition;

        if (useInventoryPosition) {
            finalPosition = CalculateThrowPosition(initialPosition, motionDirection);
        } else {
            switch (spreadType) {
                case SpreadType.Circle:
                    finalPosition += CalculateSpread(CIRCLE_SPREAD_AMOUNT);
                    break;
                case SpreadType.Line:
                    finalPosition += CalculateLineSpread(motionDirection, index, totalAmount);
                    break;
                case SpreadType.None:
                    // No spread; items spawn at the same position
                    break;
                default:
                    Debug.LogWarning($"Undefined Spread Type: {spreadType}. Items will spawn without spread.");
                    break;
            }
        }

        return finalPosition;
    }

    /// <summary>
    /// Calculates the throw position based on player position and motion direction.
    /// </summary>
    private Vector3 CalculateThrowPosition(Vector2 playerPosition, Vector2 motionDirection) {
        Vector3 throwPosition = new Vector3(
            playerPosition.x + motionDirection.x * PLAYER_THROW_DISTANCE,
            playerPosition.y + motionDirection.y * PLAYER_THROW_DISTANCE,
            playerPosition.y * DEFAULT_END_POSITION_Z_MULTIPLIER); // Maintain a slight Z offset

        return throwPosition + CalculateSpread(CIRCLE_SPREAD_AMOUNT);
    }

    /// <summary>
    /// Generates a random spread vector.
    /// </summary>
    private Vector3 CalculateSpread(float spreadAmount = CIRCLE_SPREAD_AMOUNT) {
        return new Vector3(
            Random.Range(-spreadAmount, spreadAmount),
            Random.Range(-spreadAmount, spreadAmount),
            0f);
    }

    /// <summary>
    /// Calculates spread for line distribution.
    /// </summary>
    private Vector3 CalculateLineSpread(Vector2 motionDirection, int index, int totalAmount) {
        Vector3 direction = (Vector3)motionDirection.normalized;
        Vector3 spreadVector = direction * TREE_HEIGHT;
        float stepLength = spreadVector.magnitude / (totalAmount > 1 ? totalAmount - 1 : 1);
        Vector3 linePosition = index * stepLength * direction;

        return linePosition + CalculateSpread(LINE_SPREAD_AMOUNT);
    }

    /// <summary>
    /// Spawns an item on the client using a Remote Procedure Call (RPC).
    /// </summary>
    /// <param name="calculatedPosition">The target position for the item.</param>
    /// <param name="pickUpItemNetworkId">The NetworkObjectId of the pick-up item.</param>
    /// <param name="spawnPosition">The initial spawn position of the item.</param>
    /// <param name="useInventoryPosition">Flag to determine movement behavior.</param>
    [ClientRpc]
    private void SpawnItemClientRpc(
        Vector3 calculatedPosition,
        ulong pickUpItemNetworkId,
        Vector3 spawnPosition,
        bool useInventoryPosition) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pickUpItemNetworkId, out NetworkObject pickUpItemNO)) {
            PickUpItem pickUpItem = pickUpItemNO.GetComponent<PickUpItem>();
            if (pickUpItem != null) {
                if (useInventoryPosition) {
                    // Use LeanTween for smooth movement
                    pickUpItem.transform.LeanMoveLocal(calculatedPosition, LEAN_MOVE_TIME).setEaseOutQuint();
                } else {
                    // Start a parabolic animation towards the calculated position
                    Vector3 motionOffset = calculatedPosition - new Vector3(0.5f, 0.5f, 0f);
                    pickUpItem.StartParabolaAnimation(spawnPosition, motionOffset);
                }
            } else {
                Debug.LogError("PickUpItem component not found on the NetworkObject.");
            }
        } else {
            Debug.LogError($"NetworkObject with ID {pickUpItemNetworkId} not found.");
        }
    }
}
