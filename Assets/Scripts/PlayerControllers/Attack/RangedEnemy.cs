using UnityEngine;

public class RangedEnemy : Enemy {
    public float attackRange;
    public GameObject projectilePrefab;

    public override void Attack() {
        /*
        if (Vector2.Distance(transform.position, target.position) <= attackRange) {
            Shoot();
        }
        */
    }

    private void Shoot() {
        // Instantiate and shoot projectile
        Instantiate(projectilePrefab, transform.position, Quaternion.identity);
    }

    public override void Die() {
        // Handle death
    }
}