using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ObjectSO/ProducerSO")]
public class ProducerSO : ObjectSO {
    [Header("Recipe Settings")]
    public RecipeSO Recipe;
}
