using UnityEngine;

public class ZDepth : MonoBehaviour {
    public bool _isObjectStationary = true;
    [SerializeField] private Collider2D _objectCollider2D;

    private void Start() {
        if (_objectCollider2D == null) {
            _objectCollider2D = GetComponent<Collider2D>();
        }
    }

    private void LateUpdate() {
        Vector3 position = transform.position;

        // If the BoxCollider is not null, add the offset of the collider to the y-position
        if (_objectCollider2D != null) {
            position.z = (position.y + _objectCollider2D.bounds.center.y) * 0.0001f;
        } else {
            position.z = position.y * 0.0001f;
        }

        transform.position = position;

        // If the object is stationary, remove this script
        if (_isObjectStationary) {
            enabled = false;
        }
    }
}
