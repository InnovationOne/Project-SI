using Ink.Parsed;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Animator))]
public class DoorInteractable : MonoBehaviour, IInteractable {
    public float MaxDistanceToPlayer => 1.5f;
    public bool CircleInteract => false;

    [SerializeField] private float openDelay = 0.3f;
    [SerializeField] private Collider2D _collider;

    private Animator _animator;
    private bool _isOpen = false;

    private void Awake() {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.TryGetComponent<NPCMovementController>(out var npcMover)) {
            StartCoroutine(OpenDoorForNPC(npcMover));
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.TryGetComponent<NPCMovementController>(out var npcMover)) {
            ToggleDoor();
        }
    }

    public void Interact(PlayerController player) {
        ToggleDoor();
    }

    private IEnumerator OpenDoorForNPC(NPCMovementController npcMover) {
        npcMover.PauseMovement(true);
        ToggleDoor();
        yield return new WaitForSeconds(openDelay);
        npcMover.PauseMovement(false);
    }

    private void ToggleDoor() {
        _isOpen = !_isOpen;
        _collider.enabled = !_isOpen;
        //_animator.SetBool("Open", _isOpen);
    }

    public void PickUpItemsInPlacedObject(PlayerController player) { }

    public void InitializePreLoad(int itemId) { }
}
