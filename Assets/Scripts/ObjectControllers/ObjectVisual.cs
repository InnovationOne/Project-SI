using UnityEngine;

public class ObjectVisual : MonoBehaviour {
    [Header("ItemProducer Visual")]
    [SerializeField] private SpriteRenderer _itemProducerHighlight;
    [SerializeField] private SpriteRenderer _itemProducerVisual;

    /// <summary>
    /// Initializes the item producer by deactivating the highlight on start.
    /// </summary>
    private void Awake() => _itemProducerHighlight.gameObject.SetActive(false);

    /// <summary>
    /// Sets the sprite for the item producer visual representation.
    /// </summary>
    /// <param name="sprite">The sprite to apply to the item producer visual.</param>
    public void SetSprite(Sprite sprite) => _itemProducerVisual.sprite = sprite;
}
