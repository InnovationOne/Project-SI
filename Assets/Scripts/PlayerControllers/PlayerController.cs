using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerCraftController))]
[RequireComponent(typeof(PlayerHealthAndEnergyController))]
[RequireComponent(typeof(PlayerInteractController))]
[RequireComponent(typeof(PlayerInventoryController))]
[RequireComponent(typeof(PlayerItemDragAndDropController))]
[RequireComponent(typeof(PlayerMarkerController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerToolbeltController))]
[RequireComponent(typeof(PlayerToolsAndWeaponController))]
[RequireComponent(typeof(PlayerCameraController))]
[RequireComponent(typeof(PlayerFishingController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : NetworkBehaviour {
    public static PlayerController LocalInstance { get; private set; }

    public PlayerCraftController PlayerCraftController;
    public PlayerHealthAndEnergyController PlayerHealthAndEnergyController;
    public PlayerInteractController PlayerInteractionController;
    public PlayerInventoryController PlayerInventoryController;
    public PlayerItemDragAndDropController PlayerItemDragAndDropController;
    public PlayerMarkerController PlayerMarkerController;
    public PlayerMovementController PlayerMovementController;
    public PlayerToolbeltController PlayerToolbeltController;
    public PlayerToolsAndWeaponController PlayerToolsAndWeaponController;
    public PlayerCameraController PlayerCameraController;
    public PlayerFishingController PlayerFishingController;
    public RaindropController RaindropController;

    GameManager _gameManager;

    public override void OnNetworkSpawn() {
        _gameManager = GameManager.Instance;
        _gameManager.AddPlayerToSleepingDict(OwnerClientId);
        _gameManager.AddPlayer(this);

        if (IsOwner) {
            if (LocalInstance != null) {
                Debug.LogError("There is more than one local instance of Player in the scene!");
                return;
            }
            LocalInstance = this;
        }

        PlayerCraftController = GetComponent<PlayerCraftController>();
        PlayerHealthAndEnergyController = GetComponent<PlayerHealthAndEnergyController>();
        PlayerInteractionController = GetComponent<PlayerInteractController>();
        PlayerInventoryController = GetComponent<PlayerInventoryController>();
        PlayerItemDragAndDropController = GetComponent<PlayerItemDragAndDropController>();
        PlayerMarkerController = GetComponent<PlayerMarkerController>();
        PlayerMovementController = GetComponent<PlayerMovementController>();
        PlayerToolbeltController = GetComponent<PlayerToolbeltController>();
        PlayerToolsAndWeaponController = GetComponent<PlayerToolsAndWeaponController>();
        PlayerCameraController = GetComponent<PlayerCameraController>();
        PlayerFishingController = GetComponent<PlayerFishingController>();

        RaindropController = GetComponentInChildren<RaindropController>();
    }

    public override void OnNetworkDespawn() {
        _gameManager.RemovePlayerFromSleepingDict(OwnerClientId);
        _gameManager.RemovePlayer(this);
    }

    public void TogglePlayerInBed() {
        if (PlayerMovementController.ActivePlayerState == PlayerMovementController.PlayerState.Sleeping) {
            _gameManager.SetPlayerSleepingStateServerRpc(false);
            PlayerMovementController.ChangeState(PlayerMovementController.PlayerState.Idle, true);
            return;
        }

        PlayerMovementController.ChangeState(PlayerMovementController.PlayerState.Sleeping, true);
        _gameManager.SetPlayerSleepingStateServerRpc(true);
    }
}
