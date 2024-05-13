using UnityEngine;

// This script is attached to every object that is placed into the world
public class ZDepth : MonoBehaviour {
    [SerializeField] private bool _isObjectStationary = true;
    [Header("Spezial collider if necessary")]
    [SerializeField] private Collider2D _objectCollider2D;

    private Transform _objectTransform;


    private void Awake() {
        _objectTransform = transform;

        if (_objectCollider2D == null) {
            _objectCollider2D = GetComponent<Collider2D>();
        }
    }

    private void LateUpdate() {
        Vector3 position = transform.position;

        // If the BoxCollider is not null, add the offset of the collider to the y-position
        if (_objectCollider2D != null) {
            position.z = (position.y + _objectCollider2D.offset.y) * 0.0001f;
        } else {
            position.z = position.y * 0.0001f;
        }

        _objectTransform.position = position;

        // If the object is stationary, remove this script
        if (_isObjectStationary) {
            Destroy(this);
        }
    }
}
