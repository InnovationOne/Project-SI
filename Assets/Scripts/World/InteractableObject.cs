using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractableObject : MonoBehaviour, IInteractable {
    public float MaxDistanceToPlayer => 1.5f;
    public bool CircleInteract => false;

    public abstract void Interact(PlayerController player);
    public abstract void Interact(NPC npc);

    public virtual void PickUpItemsInPlacedObject(PlayerController player) { }
    public virtual void InitializePreLoad(int itemId) { }
}
