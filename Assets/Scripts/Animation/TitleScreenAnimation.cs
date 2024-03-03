using UnityEngine;

//This class handles the animation and visibility of the main menu in the game
public class TitleScreenAnimation : MonoBehaviour {
    // Toggles the menu between visible and hidden states
    public void ToggleMenu() {
        // Check if the menu is currently active
        if (gameObject.activeSelf) {
            // Animate the menu offscreen and deactivate it after the animation is finished
            transform.LeanMoveLocal(new Vector2(70, -350), 0.8f).setEaseOutQuint().setOnComplete(DeactivateMenu);
        } else {
            // Set the game object active and animate it onscreen
            gameObject.SetActive(true);
            transform.localPosition = new Vector2(70, 350);
            transform.LeanMoveLocal(new Vector2(70, 9), 0.8f).setEaseOutQuint();
        }
    }

    // Deactivates the menu game object
    private void DeactivateMenu() {
        gameObject.SetActive(false);
    }
}
