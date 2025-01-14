using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Status Effects/StunEffect")]
public class StunEffectSO : ScriptableObject, IStatusEffect {
    [Tooltip("Duration of the stun in seconds.")]
    public float StunDuration = 2f;

    public void ApplyEffect(IDamageable target) {
        MonoBehaviour mb = target as MonoBehaviour;
        if (mb != null) {
            mb.StartCoroutine(ApplyStun(target));
        }
    }

    private IEnumerator ApplyStun(IDamageable target) {
        if (target is Enemy enemyTarget) {
            var enemyAI = enemyTarget.GetComponent<EnemyAI>();
            var lastState = enemyAI.GetState();
            enemyAI.ChangeState(EnemyAI.EnemyState.Stunned);
            yield return new WaitForSeconds(StunDuration);
            enemyAI.ChangeState(lastState);
        } else if (target is PlayerHealthAndEnergyController playerTarget) {
            var pMC = playerTarget.GetComponent<PlayerAnimationController>();
            pMC.ChangeState(PlayerAnimationController.PlayerState.Stunned);
            yield return new WaitForSeconds(StunDuration);
            pMC.ChangeState(PlayerAnimationController.PlayerState.Idle);
        }
    }
}
