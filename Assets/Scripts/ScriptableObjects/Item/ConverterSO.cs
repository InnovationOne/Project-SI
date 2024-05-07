using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Object")]
public class ConverterSO : ObjectSO {
    [Header("Recipe Settings")]
    public List<RecipeSO> Recipes;
}
