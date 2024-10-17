using Unity.Netcode;
using UnityEngine;

public enum ScarecrowType {
    None,
    ScarecrowV1,
    ScarecrowV2,
    ScarecrowV3
}

public class Scarecrow : NetworkBehaviour
{
    public ScarecrowType ScarecrowType;


}
