using UnityEngine;

public interface IDamageable {
    public void TakeDamage(Vector2 attackerPosition, int amount, WeaponSO.DamageTypes type, WeaponSO.AttackMode attackMode = WeaponSO.AttackMode.Light);
}