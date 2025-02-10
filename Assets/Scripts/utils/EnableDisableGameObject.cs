using UnityEngine;

// Enables or disables a HarvestCrop's SpriteRenderer when entering/exiting a trigger collider.
public class EnableDisableGameObject : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<HarvestCrop>(out _)) {
            if (collision.gameObject.TryGetComponent<SpriteRenderer>(out var sr)) {
                sr.enabled = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.TryGetComponent<HarvestCrop>(out _)) {
            if (collision.gameObject.TryGetComponent<SpriteRenderer>(out var sr)) {
                sr.enabled = false;
            }
        }
    }
}
