using UnityEngine;

public class Damageable : MonoBehaviour {
    private IDamageable damageable;


    internal void ApplyDamage(int damage) {
        if (damageable == null) {
            damageable = GetComponent<IDamageable>(); // Wert beibehalten, wenn null zurückgegeben wird
        }

        damageable.CalculateDamage(ref damage);
        damageable.ApplyDamage(damage);

        // SHOW DAMAGE OBOVE GAME OBJECT

        damageable.CheckState();
    }
}
