using UnityEngine;

public class GreenSlimeEnemy : Enemy {
    public override void Attack(Vector2 enemyPos) {
        var tempHitbox = new GameObject("TempMeleeHitbox");
        tempHitbox.transform.position = transform.position;

        var circleCollider = tempHitbox.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = EnemySO.AttackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, EnemySO.AttackRange);

        foreach (var hit in hits) {
            if (hit.CompareTag(_playerTag) && hit.TryGetComponent(out IDamageable target)) {
                target.TakeDamage(transform.position, EnemySO.AttackDamage, WeaponSO.DamageTypes.Physical, 0);
                Debug.Log($"[GreenSlimeEnemy] Hit {hit.name} for {EnemySO.AttackDamage} damage.");
            }
        }

        Destroy(tempHitbox);
    }

    protected override void Die() {
        base.Die();

        Debug.Log("[GreenSlimeEnemy] destroyed!");
        Destroy(gameObject);
    }
}
