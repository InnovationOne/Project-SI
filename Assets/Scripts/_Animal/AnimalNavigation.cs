using UnityEngine;

/// <summary>
/// Einfache Navigation f�r Tiere. 
/// Hier sehr rudiment�r: Das Tier l�uft zu einem Zielpunkt und weicht Hindernissen aus.
/// In der Realit�t br�uchte man vermutlich ein Pfadfindungssystem oder ein NavMesh.
/// </summary>
public class AnimalNavigation : MonoBehaviour {
    private Vector3 _destination;
    private bool _hasDestination = false;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _arrivalThreshold = 0.1f;

    public void SetDestination(Vector3 dest) {
        _destination = dest;
        _hasDestination = true;
    }

    public bool HasReachedDestination() {
        if (!_hasDestination) return true;
        float dist = Vector3.Distance(transform.position, _destination);
        return dist <= _arrivalThreshold;
    }

    private void Update() {
        if (_hasDestination && !HasReachedDestination()) {
            Vector3 dir = (_destination - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;
        }
    }
}
