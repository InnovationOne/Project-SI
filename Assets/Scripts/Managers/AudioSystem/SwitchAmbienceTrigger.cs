using UnityEngine;

public class SwitchAmbienceTrigger : MonoBehaviour {
    public enum AmbienceLocationName {
        Outside, Cave
    }

    [SerializeField] private AmbienceLocationName _locationName;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            switch (_locationName) {
                case AmbienceLocationName.Outside:
                    GameManager.Instance.AudioManager.PlayAmbience(GameManager.Instance.FMODEvents.Weather);
                    break;
                case AmbienceLocationName.Cave:
                    GameManager.Instance.AudioManager.PlayAmbience(GameManager.Instance.FMODEvents.CaveAmbience);
                    break;
            }
        }
    }
}
