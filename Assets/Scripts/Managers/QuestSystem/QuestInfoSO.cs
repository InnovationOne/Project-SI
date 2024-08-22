using UnityEngine;

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "Scriptable Objects/QuestInfoSO", order = 1)]
public class QuestInfoSO : ScriptableObject {
    [field: SerializeField] public string Id { get; private set; }

    [Header("General")]
    public string DisplayName;
    public string QuestGiverName;

    [Header("Requirements")]
    public QuestInfoSO[] QuestPrerequisires;

    [Header("Steps")]
    public GameObject[] QuestStepPrefabs;

    [Header("Rewards")]
    public int GoldReward;
    public ItemSlot[] ItemRewards;

    private void OnValidate() {
#if UNITY_EDITOR
        Id = name;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
