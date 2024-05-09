using UnityEngine;

public class Fence : AdjustingObject {
    private FenceSO FenceSO => ItemManager.Instance.ItemDatabase[_itemId] as FenceSO;

    protected override void UpdateVisualBasedOnNeighbors() {
        base.UpdateVisualBasedOnNeighbors();
        UpdateColliderAndSprite();
    }

    /// <summary>
    /// Updates the collider and sprite of the fence object based on the current pattern index.
    /// </summary>
    private void UpdateColliderAndSprite() {
        int colliderPatternIndex = DetermineColliderPatternIndex();

        if (colliderPatternIndex >= FenceSO.PolygonColliderPaths.Length || _patternIndex >= FenceSO.Sprites.Length) {
            Debug.LogError("Index out of range for PolygonColliderPaths or Sprites.");
            return;
        }

        _visual.SetSprite(FenceSO.Sprites[_patternIndex]);

        PolygonCollider2D collider = new PolygonCollider2D();
        collider.SetPath(0, FenceSO.PolygonColliderPaths[colliderPatternIndex]);
        _visual.SetCollider(collider);
    }

    /// <summary>
    /// Determines the index of the collider pattern based on the current pattern index.
    /// </summary>
    /// <returns>The index of the collider pattern.</returns>
    private int DetermineColliderPatternIndex() {
        return _patternIndex switch {
            3 => 0,
            6 => 1,
            8 => 2,
            10 => 3,
            11 => 4,
            13 => 6,
            14 => 5,
            15 => 7,
            _ => _patternIndex
        };
    }
}
