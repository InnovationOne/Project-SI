using UnityEngine;

public class EnableDisableGameObject : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.GetComponent<HarvestCrop>() != null) {
            collision.gameObject.GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.GetComponent<HarvestCrop>() != null) {
            collision.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        }
    }
}
