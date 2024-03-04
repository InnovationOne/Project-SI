using UnityEngine.EventSystems;
using UnityEngine.UI;

// This script disables clicking and dragging in a scroll view
public class PreventClickDrag : ScrollRect {
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}
