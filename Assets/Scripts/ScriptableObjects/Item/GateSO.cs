using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/GateSO")]
public class GateSO : ObjectSO {
    [Header("RuleTile")]
    public RuleTile ClosedGateRuleTile;
    public RuleTile OpenGateRuleTile;
}
