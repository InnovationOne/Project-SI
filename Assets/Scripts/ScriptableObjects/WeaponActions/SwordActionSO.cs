using UnityEngine;
using static WeaponSO;

[CreateAssetMenu(menuName = "Weapon Action/Sword Action")]
public class SwordActionSO : WeaponActionSO {
    public float hitRadius = 1f;

    public override void OnUseWeapon(Vector2 moveDir, Vector2 origin, ItemSlot itemSlot) {
        WeaponSO weapon = GameManager.Instance.ItemManager.ItemDatabase[itemSlot.ItemId] as WeaponSO;
        if (weapon == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin + moveDir.normalized, hitRadius);
        foreach (Collider2D c in hits) {
            if (c.TryGetComponent<IDamageable>(out var dmg)) {
                dmg.TakeDamage(origin, weapon.AttackDamage, weapon.DamageType);

                foreach (var effectSo in weapon.AdditionalEffects) {
                    if (effectSo is IStatusEffect effect) {
                        effect.ApplyEffect(dmg);
                    }
                }
            }
        }
    }
}
