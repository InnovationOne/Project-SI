using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class ObjectVisual : MonoBehaviour {
    private SpriteRenderer _itemProducerVisual;
    private PolygonCollider2D _collider2D;

    private void Awake() {
        _itemProducerVisual = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// Sets the sprite for the item producer visual representation.
    /// </summary>
    /// <param name="sprite">The sprite to apply to the item producer visual.</param>
    public void SetSprite(Sprite sprite) => _itemProducerVisual.sprite = sprite;

    /// <summary>
    /// Sets the collider for the object.
    /// </summary>
    /// <param name="pathCount">The number of paths in the collider.</param>
    /// <param name="isTrigger">Whether the collider is a trigger or not. Default is false.</param>
    public void SetCollider(int pathCount, bool isTrigger = false) {
        _collider2D.pathCount = pathCount;
        _collider2D.isTrigger = isTrigger;
    }

    /// <summary>
    /// Sets the path for a specific index in the collider.
    /// </summary>
    /// <param name="index">The index of the path to set.</param>
    /// <param name="vectors">The array of vectors representing the path.</param>
    public void SetPath(int index, Vector2[] vectors) {
        _collider2D.SetPath(index, vectors);
    }
}
