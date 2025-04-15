using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(QuickChatController))]
public class NPC : MonoBehaviour, IInteractable {
    public NPCDefinition Definition;

    private float _lastQuickChatTime;
    private float _quickChatCooldown = 8f;

    public float MaxDistanceToPlayer => 1.2f;
    public bool CircleInteract => false;

    private void Awake() {
        if (Definition == null) {
            Debug.LogError($"[NPC] Definition missing on {gameObject.name}");
            return;
        }

        name = $"NPC_{Definition.NPCName}";
    }

    private void Start() {
        _lastQuickChatTime = -Random.Range(0f, _quickChatCooldown);
    }

    /// <summary>
    /// Classic ink dialogue (triggered by player interaction).
    /// </summary>
    public void Interact(PlayerController player) {
        if (Definition.DialogueScript != null) {
            DialogueManager.Instance.EnterDialogueMode(Definition.DialogueScript);
        } else {
            Debug.LogWarning($"{Definition.NPCName} has no dialogue assigned.");
        }
    }

    /// <summary>
    /// Optional short chat bubble (used when passing by).
    /// </summary>
    public void TryQuickChat(string message) {
        if (Time.time - _lastQuickChatTime >= _quickChatCooldown) {
            DialogueManager.Instance.StartTextBubble(message, gameObject);
            _lastQuickChatTime = Time.time;
        }
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }
    public void InitializePreLoad(int itemId) { }
}
