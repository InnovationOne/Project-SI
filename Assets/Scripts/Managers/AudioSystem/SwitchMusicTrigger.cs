using UnityEngine;

public class SwitchMusicTrigger : MonoBehaviour {
    public enum MusicLocationName {
        Outside, Cave, BossFight
    }

    public enum BossFight {
        SteamyPuppet
    }

    public enum BossFightPhase {
        Intro, Phase1, Phase2
    }

    [SerializeField] private MusicLocationName _locationName;
    [ConditionalHide("_locationName", 2)]
    [SerializeField] private BossFight _bossFightName;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            switch (_locationName) {
                case MusicLocationName.Outside:
                    GameManager.Instance.AudioManager.PlayMusic(GameManager.Instance.FMODEvents.Seasons);
                    break;

                case MusicLocationName.Cave:
                    GameManager.Instance.AudioManager.PlayMusic(GameManager.Instance.FMODEvents.CaveMusic);
                    break;

                case MusicLocationName.BossFight:
                    GameManager.Instance.AudioManager.PlayMusic(GameManager.Instance.FMODEvents.BossFight);
                    GameManager.Instance.AudioManager.SetBossFight(_bossFightName);
                    break;
            }
        }
    }
}
