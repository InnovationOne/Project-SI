using UnityEngine;

public class GateVisual : MonoBehaviour {
    [SerializeField] private Sprite[] _openFenceSprites; // Array of fence sprites
    [SerializeField] private PolygonCollider2D[] _openGatePolygonCollider2D;
    [SerializeField] private Sprite[] _closedFenceSprites; // Array of fence sprites
    [SerializeField] private PolygonCollider2D[] _closedGatePolygonCollider2D;

    private SpriteRenderer spriteRenderer;
    [SerializeField] private PolygonCollider2D polygonCollider2D;


    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateVisual(int index, bool opened) {
        if (opened) {
            spriteRenderer.sprite = _openFenceSprites[index];
            polygonCollider2D.points = _openGatePolygonCollider2D[index].points;
            polygonCollider2D.pathCount = _openGatePolygonCollider2D[index].pathCount;
            polygonCollider2D.offset = _openGatePolygonCollider2D[index].offset;
        } else {
            spriteRenderer.sprite = _closedFenceSprites[index];
            polygonCollider2D.points = _closedGatePolygonCollider2D[index].points;
            polygonCollider2D.pathCount = _closedGatePolygonCollider2D[index].pathCount;
            polygonCollider2D.offset = _closedGatePolygonCollider2D[index].offset;
        }

        if (GetComponent<ZDepth>() == null) {
            gameObject.AddComponent<ZDepth>();
        }
    }
}
