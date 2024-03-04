using UnityEngine;

public class ChaseEnemy : MonoBehaviour {
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector2 attackSize = Vector2.one;
    [SerializeField] private int damage = 5;
    [SerializeField] private float timeToAttack = 1f;

    private float attackTimer;


    private void Start() {
        attackTimer = Random.Range(0, timeToAttack);
    }

    private void Update() {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0) {
            attackTimer = timeToAttack;

            Collider2D[] targets = Physics2D.OverlapBoxAll(transform.position, attackSize, 0f);

            foreach (Collider2D target in targets) {
                if (target.TryGetComponent<PlayerHealthAndEnergyController>(out var hpAndEnergyController)) {
                    hpAndEnergyController.RemoveHp(damage);
                }
            }
        }
    }

    private void FixedUpdate() {
        // Chase Closest Player or Player with aggro
        //transform.position = Vector3.MoveTowards(transform.position, Player.Instance.transform.position, speed * 0.01f);
    }
}
