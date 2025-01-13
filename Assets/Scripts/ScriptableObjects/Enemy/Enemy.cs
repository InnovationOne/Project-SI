using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAI))]
public abstract class Enemy : MonoBehaviour, IDamageable {
    [SerializeField] protected EnemySO _enemySO;
    public EnemySO EnemySO => _enemySO;

    private float _currentHealth;
    public float CurrentHealth => _currentHealth;
    private Rigidbody2D _rb;
    private EnemyAI _enemyAI;

    protected string _playerTag = "Player";


    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _enemyAI = GetComponent<EnemyAI>();
        _currentHealth = _enemySO.Health;
    }

    public abstract void Attack(Vector2 enemyPos);

    public void TakeDamage(Vector2 attackerPos, int amount, WeaponSO.DamageTypes type, WeaponSO.AttackMode attackMode) {
        if (_enemyAI.GetState() == EnemyAI.EnemyState.Stunned) {
            // TODO: e.g. Extralogic: If stunned, do you get bonus damage?
        }

        int damage = CalculateDamage(amount, type);
        _currentHealth -= damage;
        Debug.Log($"[GreenSlimeEnemy] took {damage} damage. Current HP = {_currentHealth}");

        // Knockback effect
        StartCoroutine(HitStopCoroutine(0.5f, 0.0f));
        ApplyKnockback(attackerPos);

        if (_currentHealth <= 0) {
            Die();
        } else {
            // Wechsle kurz in Hit-State
            _enemyAI.OnHitState();
        }
    }

    IEnumerator HitStopCoroutine(float duration, float timeScale) {
        float originalTimeScale = Time.timeScale;
        //Time.timeScale = timeScale;

        yield return new WaitForSecondsRealtime(duration);

        //Time.timeScale = originalTimeScale;
    }

    private void ApplyKnockback(Vector2 attackerPosition) {
        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;
        _rb.linearVelocity = knockbackDirection * _enemySO.KnockbackForce;
        StopCoroutine(ReduceKnockback());
        StartCoroutine(ReduceKnockback());
    }

    private IEnumerator ReduceKnockback() {
        float knockbackDuration = 0.5f; // Total duration of knockback effect
        float elapsedTime = 0f;

        while (elapsedTime < knockbackDuration) {
            float damping = Mathf.Lerp(_enemySO.KnockbackForce, 0f, elapsedTime / knockbackDuration);
            _rb.linearVelocity = _rb.linearVelocity.normalized * damping;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
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

    protected virtual void Die() {
        Destroy(gameObject);

        // Sum of all probabilities
        float totalProbability = 0f;
        foreach (var item in _enemySO.ItemsToDrop) {
            totalProbability += item.Probability;
        }

        // Weighted selection
        float randomValue = Random.Range(0f, totalProbability);
        float cumulativeProbability = 0;
        foreach (var item in _enemySO.ItemsToDrop) {
            cumulativeProbability += item.Probability;
            if (randomValue <= cumulativeProbability) {
                GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                    new ItemSlot(item.ItemId, Random.Range(item.MinAmount, item.MaxAmount), item.RarityId),
                    transform.position,
                    Vector2.zero,
                    spreadType: ItemSpawnManager.SpreadType.Circle);
            }
        }
    }
}