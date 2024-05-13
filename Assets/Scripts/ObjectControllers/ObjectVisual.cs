using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(ZDepth))]
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

    public void SetCollider(PolygonCollider2D collider) {
        _collider2D.pathCount = collider.pathCount;
        _collider2D.points = collider.points;
    }
}
