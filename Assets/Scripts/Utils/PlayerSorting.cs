using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSorting : MonoBehaviour {
    private Collider2D _collider2D;

    private void Awake() {
        _collider2D = GetComponent<Collider2D>();

        if (_collider2D == null) {
            Debug.LogError("No Collider2D found! Please attach a Collider2D to the player.");
        }
    }

    private void LateUpdate() {
        if (_collider2D != null) {
            // Calculate Z based on the bottom of the Collider2D
            float feetYPosition = _collider2D.bounds.min.y;
            Vector3 currentPosition = transform.position;
            transform.position = new Vector3(currentPosition.x, currentPosition.y, feetYPosition * 0.0001f);
        }
    }
}
