using Unity.Collections;
using UnityEngine;

public enum ScarecrowType {
    None,
    ScarecrowV1,
    ScarecrowV2,
    ScarecrowV3
}

public class Scarecrow : PlaceableObject {
    // Animation constants for advanced scarecrow behavior.
    const string SCARE_OFF = "ScareOff";
    const string MOVE = "Move";
    const string HORIZONTAL = "Horizontal";
    const string VERTICAL = "Vertical";
    const string ACTION_VERSION = "ActionVersion";
    const string GREET = "Greet";

    [SerializeField] ScarecrowSO _scarecrowSO;
    public ScarecrowSO ScarecrowSO => _scarecrowSO;

    public override float MaxDistanceToPlayer => 2f;
    public override bool CircleInteract => false;

    public override void InitializePostLoad() { }

    public override void InitializePreLoad(int itemId) { }

    public override void Interact(PlayerController player) { }

    public override void LoadObject(string data) { }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }

    public override string SaveObject() { return string.Empty; }
}
