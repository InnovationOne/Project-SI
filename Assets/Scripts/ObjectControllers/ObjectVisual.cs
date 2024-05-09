using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(ZDepth))]
public class ObjectVisual : MonoBehaviour {
    [Header("ItemProducer Visual")]
    [SerializeField] private SpriteRenderer _itemProducerVisual;
    [SerializeField] private PolygonCollider2D _collider2D;

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
