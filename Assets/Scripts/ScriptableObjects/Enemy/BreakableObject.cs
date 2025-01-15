using UnityEngine;

public class BreakableObject : MonoBehaviour, IDamageable {
    [SerializeField] private int _hp = 10;
    [SerializeField] private ItemSlot _itemSlot;

    public void TakeDamage(Vector2 attackerPosition, int amount, WeaponSO.DamageTypes type, float knockbackForce) {
        _hp -= amount;

        if (_hp <= 0) {
            GameManager.Instance.ItemSpawnManager.SpawnItemServerRpc(
                itemSlot: _itemSlot,
                initialPosition: transform.position,
                motionDirection: Vector2.zero,
                spreadType: ItemSpawnManager.SpreadType.Circle);

            Destroy(gameObject);
        }
    }
}
