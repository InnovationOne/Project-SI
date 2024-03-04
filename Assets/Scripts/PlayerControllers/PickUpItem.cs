using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

// This script handels the item prefab that is a item on the map
public class PickUpItem : NetworkBehaviour {
    [Header("Debug-Settings")]
    [SerializeField] private float itemMoveSpeed = 0.2f;
    [SerializeField] private float itemSpeedAcceleration = 0.01f;
    [SerializeField] private float pickUpDistance = 1.25f;
    [SerializeField] private float _canPickUpTimer = 0.75f; // When changing the time also change the time in ItemSpawnManager.cs
    private float _currentCanPickUpTimer;

    private ItemSlot _itemSlot;
    private Player _closestPlayer;

    [SerializeField] private SpriteRenderer pickUpItemSR;
    [SerializeField] private SpriteRenderer pickUpItemToolRaritySR;

    private float distanceToPlayer;

    [Header("Up and down movement settings")]
    [SerializeField] private float upAndDownSpeed = 1f;
    [SerializeField] private float timeForLoop = 1f;
    [SerializeField] private float upAndDownHight = 0.01f;

    [Header("Parabola animation settings")]
    [SerializeField] private float maxParabolaAnimationTime = 0.4f;
    [SerializeField] private float parabolaAnimationHight = 0.2f;

    private float parabolaAnimationTime;
    private Vector3 spawnPosition, endPosition;


    private void Awake() {
        parabolaAnimationTime = maxParabolaAnimationTime;

        _itemSlot = new ItemSlot();
    }

    private void Start() {
        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
    }

    private void OnDisable() {
        TimeAndWeatherManager.Instance.OnNextDayStarted -= TimeAndWeatherManager_OnNextDayStarted;
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        if (gameObject != null) {
            DestroyGameObjectServerRpc();
        }
    }

    private void Update() {
        if (parabolaAnimationTime < maxParabolaAnimationTime) {
            parabolaAnimationTime += Time.deltaTime;

            transform.position = MathParabola.Parabola(spawnPosition, endPosition, parabolaAnimationHight, parabolaAnimationTime / maxParabolaAnimationTime);
        }

        _currentCanPickUpTimer += Time.deltaTime;
        if (_currentCanPickUpTimer < _canPickUpTimer) {
            return;
        }

        CalculateDistanceAndPickUp();
    }

    private void FixedUpdate() {
        // Up and down animation
        float yPos = Mathf.PingPong(Time.time * upAndDownSpeed, timeForLoop) * (upAndDownHight * 2) - upAndDownHight;
        GetComponent<Transform>().position = new Vector3(transform.position.x, transform.position.y + yPos, transform.position.z);

        // If the distance is greater than the pick up distance, return
        if (_currentCanPickUpTimer < _canPickUpTimer
            || distanceToPlayer > pickUpDistance
            || _closestPlayer == null
            || !_closestPlayer.GetComponent<PlayerInventoryController>().InventoryContainer.CheckToAddItemToItemContainer(_itemSlot.Item.ItemID, _itemSlot.Amount, _itemSlot.RarityID)) {
            return;
        }

        // Move the item towards the character
        MoveGameObjectServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveGameObjectServerRpc() {
        MoveGameObjectClientRpc();
    }

    [ClientRpc]
    private void MoveGameObjectClientRpc() {
        transform.position = Vector3.MoveTowards(transform.position, _closestPlayer.transform.position, itemMoveSpeed);
        itemMoveSpeed += itemSpeedAcceleration;
    }

    private void CalculateDistanceAndPickUp() {
        FindClosestPlayer();

        var playerInventoryController = _closestPlayer.GetComponent<PlayerInventoryController>();
        var playerItemDragAndDropController = _closestPlayer.GetComponent<PlayerItemDragAndDropController>();

        // If the distance between the item and character is less than the threshold, add the item to the inventory and destroy the game object
        if (distanceToPlayer < 0.1f && playerInventoryController.InventoryContainer.CheckToAddItemToItemContainer(_itemSlot.Item.ItemID, _itemSlot.Amount, _itemSlot.RarityID)) {
            if (_closestPlayer != Player.LocalInstance) {
                DestroyGameObjectServerRpc();
                return;
            }

            for (int i = 0; i < _itemSlot.Amount; i++) {
                EventsManager.Instance.ItemPickedUpEvents.PickedUpItemId(_itemSlot.Item.ItemID);
            }

            // Try to add the item to the drag item.
            if (DragItemPanel.Instance.gameObject.activeSelf) {
                _itemSlot.Amount = playerItemDragAndDropController.TryToAddItemToDragItem(_itemSlot);
            }

            // Add the item to the inventory
            if (_itemSlot.Amount > 0) {
                _itemSlot.Amount = playerInventoryController.InventoryContainer.AddItemToItemContainer(_itemSlot.Item.ItemID, _itemSlot.Amount, _itemSlot.RarityID, false);
            }

            DestroyGameObjectServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyGameObjectServerRpc() {
        GetComponent<NetworkObject>().Despawn(true);
    }

    private void FindClosestPlayer() {
        var distanceToClosestPlayer = float.MaxValue;
        _closestPlayer = null;

        foreach (Player player in PlayerDataManager.Instance.CurrentlyConnectedPlayers) {
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToClosestPlayer > distanceToPlayer) {
                _closestPlayer = player;
                distanceToClosestPlayer = distanceToPlayer;
            }
        }

        distanceToPlayer = distanceToClosestPlayer;
    }

    // This function sets the details on the item to spawn in the map
    public void Set(ItemSlot itemSlot) {
        // Set the item and count for this slot
        _itemSlot.Copy(itemSlot.Item.ItemID, itemSlot.Amount, itemSlot.RarityID);

        // Shows the tools rarity
        if (_itemSlot.Item.ItemType == ItemTypes.Tools) {
            pickUpItemToolRaritySR.sprite = _itemSlot.Item.ToolItemRarity[_itemSlot.RarityID - 1];
        }

        // Set the sprite for the item icon
        pickUpItemSR.sprite = _itemSlot.Item.ItemIcon;

        
    }

    public void ParabolaAnimation(Vector3 start, Vector3 end) {
        parabolaAnimationTime = 0f;
        _canPickUpTimer = 0.4f;

        spawnPosition = start;
        spawnPosition.z = 5f;
        endPosition = end;
        endPosition.z = endPosition.y * 0.0001f;
    }
}
