using Unity.Netcode;

using UnityEngine;

/// <summary>
/// Replaces AnimalNavigation by using NPCMovementController for pathfinding.
/// AnimalController now directly invokes MoveTo and checks IsMoving.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(AnimalStateMachine))]
[RequireComponent(typeof(NPCMovementController))]
[RequireComponent(typeof(AnimalVisual))]
public class AnimalController : AnimalBase {
    private AnimalStateMachine _stateMachine;
    private NPCMovementController _movement;
    private AnimalVisual _visual;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    private void Awake() {
        _stateMachine = GetComponent<AnimalStateMachine>();
        _movement = GetComponent<NPCMovementController>();
        _visual = GetComponent<AnimalVisual>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            _stateMachine.Initialize(_animalSO, transform, this, _movement);
            _stateMachine.SetStateIdle();
        }
    }

    private void Update() {
        if (!IsServer) return;
        _stateMachine.Tick();
    }

    public override void Interact(PlayerController player) {
        base.Interact(player);
        _visual.ShowHighlight(true);
    }
}