using UnityEngine;

// Master script to interact with something
public class Interactable : MonoBehaviour {
    // To interact with something e.g. chest, smelter, NPC
    public virtual void Interact(Player player) { }

    // This highlights the object that can be interacted
    public virtual void ShowPossibleInteraction(bool show) { }

    // Spawns items that are in a placed item when it is picked up
    public virtual void PickUpItemsInPlacedObject(Player player) { }
}
