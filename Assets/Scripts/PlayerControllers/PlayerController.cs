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

    public bool InBed { get; private set; }

    PlayerDataManager _playerDataManager;
    GameManager _gameManager;


    private void Awake() {
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

    private void Start() {
        _gameManager = GameManager.Instance;
        _playerDataManager = PlayerDataManager.Instance;
    }

    public override void OnNetworkSpawn() {
        _playerDataManager.AddPlayer(this);
        _gameManager.AddPlayerToSleepingDict(OwnerClientId);
    }

    public override void OnNetworkDespawn() {
        _playerDataManager.RemovePlayer(this);
        _gameManager.RemovePlayerFromSleepingDict(OwnerClientId);
    }

    public void SetPlayerInBed(bool inBed) {
        if (InBed == inBed) return;
        InBed = inBed;

        _gameManager.SetPlayerSleepingStateServerRpc(InBed);
    }
}
