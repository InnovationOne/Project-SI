using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    public static FMODEvents Instance { get; private set; }

    [field: Header("Music")]
    [field: SerializeField] public EventReference SeasonMusic { get; private set; }

    [field: Header("Ambience")]
    [field: SerializeField] public EventReference WeatherAmbience { get; private set; }
    [field: SerializeField] public EventReference Thunder { get; private set; }

    [field: Header("Hit Tree SFX")]
    [field: SerializeField] public EventReference HitTreeSFX { get; private set; }

    [field: Header("Player Walk Grass SFX")]
    [field: SerializeField] public EventReference PlayerWalkGrassSFX { get; private set; }


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one instance of FMODEvents in the scene!");
            return;
        }
        Instance = this;
    }
}
