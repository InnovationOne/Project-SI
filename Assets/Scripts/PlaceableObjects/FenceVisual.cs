using UnityEngine;

public class FenceVisual : MonoBehaviour {
    [SerializeField] private Sprite[] fenceSprites; // Array of fence sprites
    [SerializeField] private PolygonCollider2D[] fencePolygonCollider2D;

    private SpriteRenderer spriteRenderer;
    [SerializeField] private PolygonCollider2D polygonCollider2D;


    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateVisual(int index) {
        spriteRenderer.sprite = fenceSprites[index];
        polygonCollider2D.points = fencePolygonCollider2D[index].points;
        polygonCollider2D.pathCount = fencePolygonCollider2D[index].pathCount;
        polygonCollider2D.offset = fencePolygonCollider2D[index].offset;

        if (GetComponent<ZDepth>() == null) {
            gameObject.AddComponent<ZDepth>();
        }
    }
}
