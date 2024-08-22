using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour, IDamageable {
    [SerializeField] protected EnemySO _enemySO;
    public EnemySO EnemySO => _enemySO;

    private float health;
    private Rigidbody2D _rb;
    private EnemyAI _enemyAI;
    private float _knockbackForce = 5f;


    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _enemyAI = GetComponent<EnemyAI>();
        health = _enemySO.Health;
    }

    public abstract void Attack();

    public void TakeDamage(Vector2 attackerPosition, int amount, WeaponSO.DamageTypes type) {
        int damage = CalculateDamage(amount, type);
        health -= damage;

        // Knockback effect
        _enemyAI.ChangeState(EnemyAI.EnemyState.Hit);
        ApplyKnockback(attackerPosition);

        if (health <= 0) {
            Die();
        }
    }

    private void ApplyKnockback(Vector2 attackerPosition) {
        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;
        _rb.velocity = knockbackDirection * _knockbackForce;
        StopCoroutine(ReduceKnockback());
        StartCoroutine(ReduceKnockback());
    }

    private IEnumerator ReduceKnockback() {
        float knockbackDuration = 0.5f; // Total duration of knockback effect
        float elapsedTime = 0f;

        while (elapsedTime < knockbackDuration) {
            float damping = Mathf.Lerp(_knockbackForce, 0f, elapsedTime / knockbackDuration);
            _rb.velocity = _rb.velocity.normalized * damping;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _rb.velocity = Vector2.zero;
    }

    protected int CalculateDamage(int amount, WeaponSO.DamageTypes type) {
        int finalDamage = amount;
        finalDamage -= type switch {
            WeaponSO.DamageTypes.Physical => _enemySO.Armor,
            WeaponSO.DamageTypes.Magical => _enemySO.Shield,
            WeaponSO.DamageTypes.Fire => _enemySO.FireResistance,
            WeaponSO.DamageTypes.Ice => _enemySO.IceResistance,
            WeaponSO.DamageTypes.Poison => _enemySO.PoisonResistance,
            WeaponSO.DamageTypes.Lightning => _enemySO.LightningResistance,
            _ => 0
        };

        return Mathf.Max(finalDamage, 0);
    }

    public abstract void Die();
}