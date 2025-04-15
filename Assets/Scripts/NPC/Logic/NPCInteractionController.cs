using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NPC))]
public class NPCInteractionController : MonoBehaviour {
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private float interactionCooldown = 10f;

    private NPC _self;
    private float _lastInteractionTime = -Mathf.Infinity;

    private void Awake() {
        _self = GetComponent<NPC>();
    }

    private void Update() {
        if (Time.time - _lastInteractionTime < interactionCooldown) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position + new Vector3(0, 0.2f), interactionRadius);
        foreach (var hit in hits) {
            if (hit.gameObject == gameObject) continue;
            if (hit.TryGetComponent<NPC>(out var other)) {
                if (Random.value < 0.33f) {
                    TriggerNPCDialogue(other);
                    _lastInteractionTime = Time.time;
                    break;
                }
            }
        }
    }

    private void TriggerNPCDialogue(NPC other) {
        string line = GenerateLineFor(other);
        _self.TryQuickChat(line);

        // Optional: Have the other NPC respond
        StartCoroutine(RespondBack(other, "Yeah, see you there."));
    }

    private IEnumerator RespondBack(NPC other, string reply) {
        yield return new WaitForSeconds(Random.Range(2f, 3f));
        other.TryQuickChat(reply);
    }

    private string GenerateLineFor(NPC other) {
        // TODO: Load from per-NPC data, or random pool
        return $"Hey {other.Definition.NPCName}, big plans today?";
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 0.2f), interactionRadius);
    }
}
