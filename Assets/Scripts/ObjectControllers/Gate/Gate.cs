/// <summary>
/// Represents a gate object that can be opened and closed.
/// </summary>
public class Gate : AdjustingObject {
    private bool _isOpened;
    private bool _isOpenedLeftToRight;

    /// <summary>
    /// Gets the GateSO (Gate ScriptableObject) associated with this gate.
    /// </summary>
    private GateSO GateSO => ItemManager.Instance.ItemDatabase[_itemId] as GateSO;

    /// <summary>
    /// Interacts with the gate, toggling its state and updating the visuals based on neighbors.
    /// </summary>
    /// <param name="player">The player interacting with the gate.</param>
    public override void Interact(Player player) {
        ToggleGateState(player);
        UpdateVisualBasedOnNeighbors();
    }

    /// <summary>
    /// Toggles the state of the gate based on the player's position.
    /// </summary>
    /// <param name="player">The player object.</param>
    private void ToggleGateState(Player player) {
        _isOpened = !_isOpened;
        _isOpenedLeftToRight = _isOpened && gameObject.transform.position.x <= player.transform.position.x;
    }

    /// <summary>
    /// Updates the visual appearance of the gate based on its neighbors.
    /// </summary>
    protected override void UpdateVisualBasedOnNeighbors() {
        base.UpdateVisualBasedOnNeighbors();
        UpdateSpriteAndCollider();
    }

    /// <summary>
    /// Updates the sprite and collider of the gate based on its current state.
    /// </summary>
    private void UpdateSpriteAndCollider() {
        int spriteIndex = DetermineSpriteIndex();
        UpdateCollider();
        _visual.SetSprite(GateSO.Sprites[spriteIndex]);
    }

    /// <summary>
    /// Determines the index of the sprite to use based on the current pattern index and gate state.
    /// </summary>
    /// <returns>The index of the sprite to use.</returns>
    private int DetermineSpriteIndex() {
        var spriteIndex = 0;

        switch (_patternIndex) {
            case 1:
            case 3:
            case 6:
                _patternIndex = _isOpened ? (_isOpenedLeftToRight ? 2 : 4) : 1;
                spriteIndex = _isOpened ? (_isOpenedLeftToRight ? 3 : 4) : 2;
                break;
            default:
                _patternIndex = 0; // Horizontal
                spriteIndex = _isOpened ? 1 : spriteIndex;
                break;
        }

        return spriteIndex;
    }

    /// <summary>
    /// Updates the collider of the gate based on its current state.
    /// </summary>
    private void UpdateCollider() {
        int pathCount = _isOpened ? 2 : 1;
        _visual.SetCollider(pathCount);
        _visual.SetPath(0, GateSO.OpenPolygonColliderPaths[_patternIndex]);
        if (_isOpened) {
            _visual.SetPath(1, GateSO.OpenPolygonColliderPaths[++_patternIndex]);
        }
    }

    /// <summary>
    /// Overrides the PickUpItemsInPlacedObject method if necessary.
    /// </summary>
    /// <param name="player">The player picking up the gate.</param>
    public override void PickUpItemsInPlacedObject(Player player) {
        base.PickUpItemsInPlacedObject(player);
    }

    /// <summary>
    /// Optionally override InitializePreLoad if Gate needs specific initialization
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    public override void InitializePreLoad(int itemId) {
        base.InitializePreLoad(itemId);
    }
}
