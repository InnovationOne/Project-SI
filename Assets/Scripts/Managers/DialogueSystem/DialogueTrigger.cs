using UnityEngine;

public class DialogueTrigger : MonoBehaviour {
    [Header("Visual Cue")]
    [Tooltip("The visual cue that appears over the NPC when the player is in range.")]
    [SerializeField] GameObject _visualCue;

    [Header("Ink JSON")]
    [Tooltip("The JSON file that contains the dialogue for this NPC.")]
    [SerializeField] TextAsset _inkJSON;

    bool _playerInRange;

    private void Awake() {
        _visualCue.SetActive(false);
    }

    private void Start() {
        GameManager.Instance.InputManager.OnInteractAction += InputManager_OnInteractAction;
    }

    private void OnDestroy() {
        if (GameManager.Instance != null && GameManager.Instance.InputManager != null) {
            GameManager.Instance.InputManager.OnInteractAction -= InputManager_OnInteractAction;
        }
    }

    private void InputManager_OnInteractAction() {
        if (_playerInRange && !GameManager.Instance.DialogueManager.DialogueIsPlaying) {
            GameManager.Instance.DialogueManager.EnterDialogueMode(_inkJSON);
        }
    }

    private void Update() {
        _visualCue.SetActive(_playerInRange && !GameManager.Instance.DialogueManager.DialogueIsPlaying);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<PlayerController>(out var player)) {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.TryGetComponent<PlayerController>(out var player)) {
            _playerInRange = false;
        }
    }
}
