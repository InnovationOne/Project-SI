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

    public override float MaxDistanceToPlayer => throw new System.NotImplementedException();

    public override void InitializePostLoad() { }

    public override void InitializePreLoad(int itemId) { }

    public override void Interact(PlayerController player) { }

    public override void LoadObject(FixedString4096Bytes data) { }

    public override void PickUpItemsInPlacedObject(PlayerController player) { }

    public override FixedString4096Bytes SaveObject() { return new FixedString4096Bytes(string.Empty); }
}
