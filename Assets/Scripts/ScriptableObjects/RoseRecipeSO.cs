using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Recipe/RoseRecipeSO")]
public class RoseRecipeSO : ScriptableObject {
    public List<RoseSO> Roses;
    public int Time;
    public RoseSO NewRose; 
}
