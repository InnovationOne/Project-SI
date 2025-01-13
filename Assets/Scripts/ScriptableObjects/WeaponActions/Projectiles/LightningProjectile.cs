using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LightningProjectile : MonoBehaviour {
    const float SPEED = 5f;
    const int DAMAGE = 10;
    const WeaponSO.DamageTypes DAMAGE_TYPE = WeaponSO.DamageTypes.Lightning;
    const float LIFE_TIME = 3f;

    Vector2 _direction;
    IDamageable _owner;

    public void Init(Vector2 direction, IDamageable owner) {
        _direction = direction.normalized;
        _owner = owner;
        Destroy(gameObject, LIFE_TIME);
    }

    private void Update() {
        transform.Translate(SPEED * Time.deltaTime * _direction);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null && dmg != _owner) {
            dmg.TakeDamage(transform.position, DAMAGE, DAMAGE_TYPE);
            Destroy(gameObject);
        } else {
            // TODO: For wall or terrain: destroy projectile if necessary
            // If (other.CompareTag(“Wall”)) ...
            Destroy(gameObject);
        }
    }
}
