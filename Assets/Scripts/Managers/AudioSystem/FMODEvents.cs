using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour {

    [field: Header("Music")]
    [field: SerializeField] public EventReference SeasonTheme { get; private set; }
    [field: SerializeField] public EventReference TitleTheme { get; private set; }
    [field: SerializeField] public EventReference LoadingTheme { get; private set; }

    [field: Header("Ambience")]
    [field: SerializeField] public EventReference WeatherAmbience { get; private set; }
    [field: SerializeField] public EventReference Thunder { get; private set; }

    [field: Header("Hit Tree SFX")]
    [field: SerializeField] public EventReference HitTreeSFX { get; private set; }
    [field: SerializeField] public EventReference WaterDropSFX { get; private set; }
    [field: SerializeField] public EventReference FishBitSFX { get; private set; }


    [field: Header("Player Walk Grass SFX")]
    [field: SerializeField] public EventReference PlayerWalkGrassSFX { get; private set; }
}
