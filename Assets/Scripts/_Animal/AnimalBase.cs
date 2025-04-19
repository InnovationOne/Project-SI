using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for all animals, handling naming, friendship, feeding, petting, mating, and production.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public abstract class AnimalBase : NetworkBehaviour, IInteractable {
    [Header("Config")]
    [SerializeField] protected AnimalSO _animalSO;

    [Header("Items")]
    [SerializeField] protected ItemSO items; // Items that can be used on this animal

    // Networked state
    private NetworkVariable<FixedString64Bytes> _animalName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _friendship = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _wasFed = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _wasPetted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _gaveItem = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Breeding
    private NetworkVariable<bool> _isPregnant = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _daysPregnant = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Adult status
    private NetworkVariable<bool> _isAdult = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _daysAsJuvenile = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Cow clicks
    private NetworkVariable<int> _cowClicks = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float _lastClickTime;
    private const float TAP_WINDOW = 2f;
    private Animator animator;

    // Parameters
    private const float MATING_CHANCE_PER_DAY = 0.02f; // example: 2%
    private int GestationDays => _animalSO.CanLayEggs ? _animalSO.GrowthDays : 7; // eggs incubated via Incubator
    

    public override void OnNetworkSpawn() {
        if (IsServer) {
            _friendship.Value = _animalSO.InitialFriendship;
            _daysAsJuvenile.Value = 0;
            TimeManager.Instance.OnNextDayStarted += NextDay;
        }
    }
    public override void OnNetworkDespawn() {
        if (IsServer) TimeManager.Instance.OnNextDayStarted -= NextDay;
    }

    #region Interaction
    public abstract float MaxDistanceToPlayer { get; }
    public abstract bool CircleInteract { get; }

    public virtual void Interact(PlayerController player) {
        if (!IsServer) return;

        // Determine tool
        int toolId = player.PlayerToolbeltController.GetCurrentlySelectedToolbeltItemSlot().ItemId;
        var itemSO = ItemManager.Instance.ItemDatabase[toolId];

        // Feeding
        if (!_wasFed.Value && itemSO == _animalSO.FeedItem) {
            _wasFed.Value = true;
            ChangeFriendship(5);
            return;
        }
        // Petting
        if (!_wasPetted.Value) {
            ChangeFriendship(itemSO == _animalSO.PetItem ? 10 : 5);
            _wasPetted.Value = true;
            return;
        }
        // Production
        if (!_gaveItem.Value && TryGiveProduct(player, toolId)) {
            _gaveItem.Value = true;
            return;
        }
        // Tipping for cows
        if (_animalSO.AnimalBase == AnimalCategory.Cow && _cowClicks.Value < 5) {
            float now = TimeManager.Instance.LocalTime;
            if (now - _lastClickTime < TAP_WINDOW) _cowClicks.Value++;
            else _cowClicks.Value = 1;
            _lastClickTime = now;

            if (_cowClicks.Value >= 5) {
                animator.SetTrigger("Fall");  // Trigger Animator‑Parameter “Fall”
            }
            return;
        }

        // Renaming
        OpenRenameUI(player);
    }
    #endregion

    #region Production
    protected virtual bool TryGiveProduct(PlayerController player, int toolId) {
        var toolSO = GameManager.Instance.ItemManager.ItemDatabase[toolId];
        if (toolSO != _animalSO.ProductTool) return false;
        
        switch (_animalSO.AnimalBase) {
            case AnimalCategory.Chicken:
            case AnimalCategory.Goose:
            case AnimalCategory.Duck:
                return false;
            case AnimalCategory.Cow:
                if (_cowClicks.Value >= 5) {
                    _cowClicks.Value = 0;
                    SpawnItemServer(player, _animalSO.PrimaryProductItem.ItemId);
                    return true;
                }
                SpawnItemServer(player, _animalSO.PrimaryProductItem.ItemId);
                return true;
            case AnimalCategory.Sheep:
            case AnimalCategory.Alpaca:
                SpawnItemServer(player, _animalSO.PrimaryProductItem.ItemId);
                return true;
            case AnimalCategory.Goat:
                SpawnItemServer(player, _animalSO.PrimaryProductItem.ItemId);
                return true;
            case AnimalCategory.Pig:
                // Handled in PigDigComponent.
                return false;
            default:
                return false;
        }
    }

    protected void SpawnItemServer(PlayerController player, int itemId) {
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            new ItemSlot(itemId, 1, GetQuality()),
            transform.position,
            Vector2.up);
    }

    private int GetQuality() {
        float norm = (float)_friendship.Value / _animalSO.MaxFriendship;
        if (norm > 0.98f) return 3;
        if (norm > 0.92f) return 2;
        if (norm > 0.80f) return 1;
        return 0;
    }
    #endregion

    #region Daily Reset & Growth
    private void NextDay() {
        // Reset interactions
        ProduceDailyProduct();

        _wasFed.Value = false;
        _wasPetted.Value = false;
        _gaveItem.Value = false;

        // Growth
        if (!_isAdult.Value) {
            _daysAsJuvenile.Value++;
            if (_daysAsJuvenile.Value >= _animalSO.GrowthDays) {
                _isAdult.Value = true;
            }
        }

        // Mating / Pregnancy for large animals
        var building = GetComponentInParent<AnimalBuilding>();
        if (_isAdult.Value && !_isPregnant.Value && IsLargeAnimal() && building != null && building.HasFreeSpace) {
            if (UnityEngine.Random.value < MATING_CHANCE_PER_DAY) {
                _isPregnant.Value = true;
                building.ReserveSlot();
            }
        } else if (_isPregnant.Value) {
            _daysPregnant.Value++;
            if (_daysPregnant.Value >= GestationDays) {
                GiveBirth();
                building.UnreserveSlot();
            }
        }
    }

    private void ProduceDailyProduct() {
        // nur Small Animals
        switch (_animalSO.AnimalBase) {
            case AnimalCategory.Chicken:
            case AnimalCategory.Goose:
            case AnimalCategory.Duck:
                if (_wasFed.Value) {
                    var product = new ItemSlot(
                        _animalSO.PrimaryProductItem.ItemId,
                        1,
                        GetQuality());
                    var building = GetComponentInParent<AnimalBuilding>();
                    if (building != null) {
                        building.SpawnProductInside(product);
                    }
                }
                break;
            default:
                break;
        }
    }

    private bool IsLargeAnimal() => _animalSO.AnimalSize == AnimalSize.Large;

    private void GiveBirth() {
        _isPregnant.Value = false;
        _daysPregnant.Value = 0;
        AnimalManager.Instance.RequestSpawnJuvenile(_animalSO.AnimalId, transform.position);
    }

    public void SetNameServer(string newName) {
        if (!IsServer) return;
        _animalName.Value = newName;
    }

    #endregion

    #region Friendship
    public void ChangeFriendship(int delta) {
        _friendship.Value = Mathf.Clamp(_friendship.Value + delta, 0, _animalSO.MaxFriendship);
    }
    #endregion

    #region Renaming
    private void OpenRenameUI(PlayerController player) {
        UIManager.Instance.ShowRenameAnimalDialog(
            _animalName.Value.ToString(),
            newName => RenameServerRpc(newName)
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void RenameServerRpc(string newName) {
        _animalName.Value = newName;
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeSOServerRpc(FixedString64Bytes soGuid, FixedString64Bytes name) {
        _animalSO = AnimalSORepository.Instance.GetByGuid(soGuid.ToString());
        _animalName.Value = name;
        _friendship.Value = _animalSO.InitialFriendship;
    }

    #endregion

    public void PickUpItemsInPlacedObject(PlayerController player) { }
    public void InitializePreLoad(int itemId) { }
}