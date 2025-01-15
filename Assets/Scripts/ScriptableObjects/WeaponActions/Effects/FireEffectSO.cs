using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Status Effects/FireEffect")]
public class FireEffectSO : ScriptableObject, IStatusEffect {
    [Tooltip("Damage per tick.")]
    public float damageOverTime = 2f;
    [Tooltip("Total duration of action in seconds.")]
    public float duration = 5f;
    [Tooltip("Time interval between two damage ticks.")]
    public float tickInterval = 1f;

    public void ApplyEffect(IDamageable target) {
        MonoBehaviour mb = target as MonoBehaviour;
        if (mb != null) {
            mb.StartCoroutine(ApplyFireDamage(target));
        }
    }

    private IEnumerator ApplyFireDamage(IDamageable target) {
        float elapsed = 0f;

        while (elapsed < duration) {
            int dmg = Mathf.RoundToInt(damageOverTime);
            target.TakeDamage(Vector2.zero, dmg, WeaponSO.DamageTypes.Fire, 0);

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
    }
}
