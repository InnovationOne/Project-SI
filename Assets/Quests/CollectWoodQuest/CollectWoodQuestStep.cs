using UnityEngine;

public class CollectWoodQuestStep : QuestStep {
    [SerializeField] private ItemSO _woodItem;
    [SerializeField] private int _woodToCollect = 2;

    private int _woodCollected = 0;
    

    private void OnEnable() {
        EventsManager.Instance.ItemPickedUpEvents.OnPickedUpItemId += ItemCollected;
    }

    private void OnDisable() {
        EventsManager.Instance.ItemPickedUpEvents.OnPickedUpItemId -= ItemCollected;
    }

    private void ItemCollected(int id) {
        if (!WoodCollected(id)) {
            return;
        }

        if (_woodCollected < _woodToCollect) {
            _woodCollected++;
            UpdateState();

            if (_woodCollected >= _woodToCollect) {
                FinishQuestStep();
            }
        }
    }

    private bool WoodCollected(int id) {
        return id.Equals(_woodItem.ItemId);
    }

    private void UpdateState() {
        string state = _woodCollected.ToString();
        ChangeState(state);
    }

    protected override void SetQuestStepState(string state) {
        _woodCollected = int.Parse(state);
        UpdateState();
    }
}
