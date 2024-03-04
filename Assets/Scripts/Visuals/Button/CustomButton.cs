using UnityEngine;
using UnityEngine.UI;

// This script is for a custom button, the image of the button needs to be read/write
public class CustomButton : MonoBehaviour {
    private void Start() {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}
