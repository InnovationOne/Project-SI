using UnityEngine;

public class Bed : Interactable {
    [SerializeField] private SpriteRenderer _bedHighlight;


    private void Awake() {
        _bedHighlight.gameObject.SetActive(false);
    }

    public override void Interact(Player player) {
        if (player.InBed) {
            // Enable movement again
            player.SetPlayerInBed(false);
            player.gameObject.GetComponent<PlayerMovementController>().SetCanMoveAndTurn(true);
        } else {
            // Block movement
            player.SetPlayerInBed(true);
            player.gameObject.GetComponent<PlayerMovementController>().SetCanMoveAndTurn(false);
        }

        
    }
}
