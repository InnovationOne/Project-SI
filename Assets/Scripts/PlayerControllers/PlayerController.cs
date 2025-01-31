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
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : NetworkBehaviour {
    public static PlayerController LocalInstance { get; private set; }

    public PlayerCraftController PlayerCraftController { get; private set; }
    public PlayerHealthAndEnergyController PlayerHealthAndEnergyController { get; private set; }
    public PlayerInteractController PlayerInteractionController { get; private set; }
    public PlayerInventoryController PlayerInventoryController { get; private set; }
    public PlayerItemDragAndDropController PlayerItemDragAndDropController { get; private set; }
    public PlayerMarkerController PlayerMarkerController { get; private set; }
    public PlayerMovementController PlayerMovementController { get; private set; }
    public PlayerToolbeltController PlayerToolbeltController { get; private set; }
    public PlayerToolsAndWeaponController PlayerToolsAndWeaponController { get; private set; }
    public PlayerCameraController PlayerCameraController { get; private set; }
    public PlayerFishingController PlayerFishingController { get; private set; }
    public RaindropController RaindropController { get; private set; }
    public PlayerAnimationController PlayerAnimationController { get; private set; }

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
        PlayerAnimationController = GetComponent<PlayerAnimationController>();

        RaindropController = GetComponentInChildren<RaindropController>();
    }

    public override void OnNetworkDespawn() {
        _gameManager.RemovePlayerFromSleepingDict(OwnerClientId);
        _gameManager.RemovePlayer(this);
    }

    public void TogglePlayerInBed() {
        if (PlayerAnimationController.ActivePlayerState == PlayerAnimationController.PlayerState.Sleeping) {
            _gameManager.SetPlayerSleepingStateServerRpc(false);
            PlayerAnimationController.ChangeState(PlayerAnimationController.PlayerState.Idle, true);
            return;
        }

        PlayerAnimationController.ChangeState(PlayerAnimationController.PlayerState.Sleeping, true);
        _gameManager.SetPlayerSleepingStateServerRpc(true);
    }

    public void SavePlayer(PlayerData playerData) {
        
    }

    public void LoadPlayer(PlayerData playerData) {
        
    }
}
