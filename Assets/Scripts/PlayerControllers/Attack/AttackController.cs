using UnityEngine;

public class AttackController : MonoBehaviour {
    [SerializeField] private Vector2 sizeOfAttackableArea = new(2f, 2f);


    public void Attack(int damage, Vector2 position) {
        Collider2D[] targets = Physics2D.OverlapBoxAll(position, sizeOfAttackableArea, 0f);

        foreach (Collider2D target in targets) {
            if (target.TryGetComponent<Damageable>(out var damageable) && target.GetComponent<Player>() == null) {
                damageable.ApplyDamage(damage);
            }
        }
    }
}
