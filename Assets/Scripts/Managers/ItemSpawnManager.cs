using Unity.Netcode;
using UnityEngine;

public enum SpreadType {
    Circle, Line
}


// Script spawns an item on the map
public class ItemSpawnManager : NetworkBehaviour {
    public static ItemSpawnManager Instance { get; private set; }

    [SerializeField] private PickUpItem _pickUpItemPrefab;

    private const float PLAYER_THROW_DISTANCE = 2f;
    private const float CIRCLE_SPREAD_AMOUNT = 0.6f;
    private const float LINE_SPREAD_AMOUNT = 0.15f;
    private const float TREE_HIGHT = 2f;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of ItemSpawnManager in the scene!");
            return;
        }
        Instance = this;
    }

    public void SpawnItemFromInventory(ItemSO item, int amount, int rarityID, Vector3 playerPosition, Vector3 lastMotionDirection) {
        // Set the spawn position to the player's current position,
        // but with a z value of -5 to ensure it appears in front of the playerp
        Vector3 spawnPosition = playerPosition;
        spawnPosition.z = -5f;

        for (int i = 0; i < amount; i++) {
            SpawnItemFromInventoryServerRpc(item.ItemID, rarityID, spawnPosition, CalculateThrowPosition(playerPosition, lastMotionDirection));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemFromInventoryServerRpc(int itemID, int rarityID, Vector3 spawnPosition, Vector3 throwPosition) {
        PickUpItem pickUpItem = Instantiate(_pickUpItemPrefab, spawnPosition, Quaternion.identity);

        NetworkObject pickUpItemNO = pickUpItem.GetComponent<NetworkObject>();
        pickUpItemNO.Spawn(true);

        SpawnItemFromInventoryClientRpc(itemID, rarityID, throwPosition, pickUpItemNO);
    }

    [ClientRpc]
    private void SpawnItemFromInventoryClientRpc(int itemID, int rarityID, Vector3 throwPosition, NetworkObjectReference pickUpItemNOReference) {
        pickUpItemNOReference.TryGet(out NetworkObject pickUpItemNO);
        PickUpItem pickUpItem = pickUpItemNO.GetComponent<PickUpItem>();

        pickUpItem.Set(new ItemSlot(itemID, 1, rarityID));
        pickUpItem.transform.LeanMoveLocal(throwPosition, .8f).setEaseOutQuint(); // When changing the time (.8f) also change the time in PickUpItem.cs
    }

    private Vector3 CalculateThrowPosition(Vector3 playerPosition, Vector3 lastMotionDirection) {
        // Add the throw distance to the player's position to get the final throw position
        Vector3 throwPosition = playerPosition + lastMotionDirection * PLAYER_THROW_DISTANCE;

        // Set the z-coordinate
        throwPosition.z = throwPosition.y * 0.0001f;
        return throwPosition += CalculateCircleSpread();
    }

    public void SpawnItemAtPosition(Vector3 spawnPosition, Vector3 lastMotionDirection, ItemSO item, int amount, int rarityID, SpreadType spreadType) {
        spawnPosition = TilemapManager.Instance.FixPositionOnGrid(spawnPosition);

        switch (spreadType) {
            case SpreadType.Circle:
                for (int i = 0; i < amount; i++) {
                    SpawnInstance(spawnPosition, item, rarityID, spawnPosition + CalculateCircleSpread());
                }
                break;
            case SpreadType.Line:
                Vector3 differenceVector = lastMotionDirection * TREE_HIGHT;

                float stepLenght = differenceVector.magnitude / amount;
                float currentStepLenght = stepLenght;

                for (int i = 0; i < amount; i++) {
                    Vector3 neu = differenceVector * currentStepLenght;
                    currentStepLenght += stepLenght;

                    SpawnInstance(spawnPosition, item, rarityID, spawnPosition + neu + CalculateCircleSpread(LINE_SPREAD_AMOUNT));
                }
                break;
            default:
                Debug.LogError("Not Defined Spread Type");
                break;
        }
    }

    private Vector3 CalculateCircleSpread(float spread = 0f) {
        if (spread == 0f) { 
            spread = CIRCLE_SPREAD_AMOUNT; 
        }

        return new(Random.Range(-spread, spread), Random.Range(-spread, spread));
    }

    private void SpawnInstance(Vector3 spawnPosition, ItemSO item, int rarityID, Vector3 calculatedPosition) {
        SpawnInstanceServerRpc(spawnPosition, item.ItemID, rarityID, calculatedPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnInstanceServerRpc(Vector3 spawnPosition, int itemID, int rarityID, Vector3 calculatedPosition) {
        PickUpItem pickUpItem = Instantiate(_pickUpItemPrefab, spawnPosition, Quaternion.identity);

        NetworkObject pickUpItemNO = pickUpItem.GetComponent<NetworkObject>();
        pickUpItemNO.Spawn(true);

        SpawnInstanceClientRpc(spawnPosition, itemID, rarityID, calculatedPosition, pickUpItemNO);
    }

    [ClientRpc]
    private void SpawnInstanceClientRpc(Vector3 spawnPosition, int itemID, int rarityID, Vector3 calculatedPosition, NetworkObjectReference pickUpItemNOReference) {
        pickUpItemNOReference.TryGet(out NetworkObject pickUpItemNO);
        PickUpItem pickUpItem = pickUpItemNO.GetComponent<PickUpItem>();

        pickUpItem.Set(new ItemSlot(itemID, 1, rarityID));
        pickUpItem.ParabolaAnimation(spawnPosition, calculatedPosition - new Vector3(0.5f, 0.5f));
    }
}
