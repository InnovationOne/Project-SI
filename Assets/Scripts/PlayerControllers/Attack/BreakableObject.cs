using UnityEngine;

public class BreakableObject : MonoBehaviour, IDamageable {
    [SerializeField] private int Hp = 10;


    public void ApplyDamage(int damage) {
        Hp -= damage;
    }

    public void CalculateDamage(ref int damage) {
        // Damage booster oder sowas
        // Animationen und so
    }

    public void CheckState() {
        if (Hp <= 0) {
            Destroy(gameObject);
        }
    }
}
