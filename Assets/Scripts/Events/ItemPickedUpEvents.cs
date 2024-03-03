using System;

public class ItemPickedUpEvents {
    public event Action<int> OnPickedUpItemId;
    public void PickedUpItemId(int id) {
        OnPickedUpItemId?.Invoke(id);
    }
}
