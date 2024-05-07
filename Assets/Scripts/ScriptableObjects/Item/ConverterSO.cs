using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ConverterSO")]
public class ConverterSO : ObjectSO {
    [Header("Recipe Settings")]
    public List<RecipeSO> Recipes;
}
