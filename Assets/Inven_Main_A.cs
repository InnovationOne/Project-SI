using UnityEngine;
using UnityEngine.UI;

public class Inven_Main_A : MonoBehaviour {
    [SerializeField] Image Button_Main_A_Normal;
    [SerializeField] Image Button_Main_A_Icon_Normal;
    [SerializeField] Image Button_Main_A_Pressed;
    [SerializeField] Image Button_Main_A_Icon_Pressed;

    public void ToggleButtonVisual(bool isActive) {
        Button_Main_A_Normal.gameObject.SetActive(!isActive);
        Button_Main_A_Icon_Normal.gameObject.SetActive(!isActive);
        Button_Main_A_Pressed.gameObject.SetActive(isActive);
        Button_Main_A_Icon_Pressed.gameObject.SetActive(isActive);
    }
}
