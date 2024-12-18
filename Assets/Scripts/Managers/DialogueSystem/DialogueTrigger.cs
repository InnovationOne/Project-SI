using UnityEngine;

public class DialogueTrigger : MonoBehaviour {
    [Header("Visual Cue")]
    // Pop-Up over the NPC
    [SerializeField] private GameObject _visualCue;

    [Header("Ink JSON")]
    // JSON story file
    [SerializeField] private TextAsset _inkJSON;

    // Is the player in range of the NPC
    private bool _playerInRange;


    private void Awake() {
        _playerInRange = false;
        _visualCue.SetActive(false);
    }

    private void Start() {
        InputManager.Instance.OnInteractAction += InputManager_OnInteractAction;
    }

    private void InputManager_OnInteractAction() {
        // Check if the player is in range and if no dialogue is playing
        if (_playerInRange && !DialogueManager.Instance.DialogueIsPlaying) {
            DialogueManager.Instance.EnterDialogueMode(_inkJSON);
        }
    }

    private void Update() {
        // Check if the player is in range and if no dialogue is playing
        if (_playerInRange && !DialogueManager.Instance.DialogueIsPlaying) {
            // Activate the pop-up over the NPC
            _visualCue.SetActive(true);
        } else {
            // Disbale the pop-up over the NPC
            _visualCue.SetActive(false);
        }

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
