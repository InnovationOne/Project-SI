using UnityEngine;

public class MechanicalEnemy : Enemy {
    public override void Attack(Vector2 enemyPos) {
        if (_enemySO != null && _enemySO.ProjectilePrefab != null) {
            Vector2 directionToPlayer = (enemyPos - (Vector2)transform.position).normalized;
            GameObject proj = Instantiate(_enemySO.ProjectilePrefab, transform.position, Quaternion.identity);

            if (proj != null) {
                proj.GetComponent<LightningProjectile>().Init(directionToPlayer, this);
            }
        }
    }

    protected override void Die() {
        Debug.Log("Mechanical Enemy destroyed!");
        Destroy(gameObject);
    }
}
