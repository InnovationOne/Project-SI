using UnityEngine;

public class Bed : Interactable {
    [SerializeField] private ObjectVisual _visual;

    private int _itemId;

    public override void Initialize(int itemId) {
        _itemId = itemId;
        _visual.SetSprite(GetObjectSO().InactiveSprite);
    }

    public override void Interact(Player player) {
        player.SetPlayerInBed(!player.InBed);
        PlayerMovementController.LocalInstance.SetCanMoveAndTurn(player.InBed);
    }

    private ObjectSO GetObjectSO() => ItemManager.Instance.ItemDatabase[_itemId] as ObjectSO;
}
