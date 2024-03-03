using UnityEngine;

//This script enables disables image on button click
public class EnableDisableImageOnButtonClick : MonoBehaviour {
    public void EnableDisable() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
