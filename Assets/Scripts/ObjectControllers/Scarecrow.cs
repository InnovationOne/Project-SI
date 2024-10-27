using UnityEngine;

public enum ScarecrowType {
    None,
    ScarecrowV1,
    ScarecrowV2,
    ScarecrowV3
}

public class Scarecrow : PlaceableObject {
    // Scarecrow Tier 2 & 3
    private const string SCARE_OFF = "ScareOff";

    // Scarecrow Tier 3
    private const string MOVE = "Move";
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string ACTION_VERSION = "ActionVersion";
    private const string GREET = "Greet";

    [SerializeField] private ScarecrowSO _scarecrowSO;
    public ScarecrowSO ScarecrowSO => _scarecrowSO;


}
