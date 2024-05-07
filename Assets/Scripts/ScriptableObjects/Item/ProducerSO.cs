using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Object")]
public class ProducerSO : ObjectSO {
    [Header("Recipe Settings")]
    public RecipeSO Recipe;
}
