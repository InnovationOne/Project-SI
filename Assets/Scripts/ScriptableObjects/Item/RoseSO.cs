using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/RoseSO")]
public class RoseSO : ObjectSO {
    [Header("Rose Settings")]
    public List<RoseRecipeSO> RoseRecipes;

    public ItemSO ItemForGalaxyRose;
}
