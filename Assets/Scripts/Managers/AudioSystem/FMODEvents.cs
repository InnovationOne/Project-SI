using UnityEngine;
using FMODUnity;
using Unity.VisualScripting;

public class FMODEvents : MonoBehaviour {
    [field: Header("Music")]
    [field: SerializeField] public EventReference Title { get; private set; }
    [field: SerializeField] public EventReference Loading { get; private set; }
    [field: SerializeField] public EventReference Seasons { get; private set; }
    [field: SerializeField] public EventReference CaveMusic { get; private set; }
    [field: SerializeField] public EventReference BossFight { get; private set; }


    [field: Header("Ambience")]
    [field: SerializeField] public EventReference Church_Bells { get; private set; }
    [field: SerializeField] public EventReference Ocean_Shore { get; private set; }
    [field: SerializeField] public EventReference River { get; private set; }
    [field: SerializeField] public EventReference Thunder { get; private set; }
    [field: SerializeField] public EventReference Waterfall { get; private set; }
    [field: SerializeField] public EventReference Weather { get; private set; }
    [field: SerializeField] public EventReference CaveAmbience { get; private set; }


    [field: Header("SFX")]
    [field: SerializeField] public EventReference Footsteps { get; private set; }
    [field: SerializeField] public EventReference Teleport { get; private set; }

    // Tool
    [field: SerializeField] public EventReference Axe_Hit_Wood { get; private set; }
    [field: SerializeField] public EventReference Axe_Breake_Wood { get; private set; }
    [field: SerializeField] public EventReference Pickaxe_Hit_Rock { get; private set; }
    [field: SerializeField] public EventReference Pickaxe_Breake_Rock { get; private set; }
    [field: SerializeField] public EventReference Fishing_Water_Drop { get; private set; }
    [field: SerializeField] public EventReference Fishing_Reel_Backwards { get; private set; }
    [field: SerializeField] public EventReference Fishing_Quickly_Reel_In { get; private set; }
    
    // Weapon
    [field: SerializeField] public EventReference Hit_Unhittable_Object { get; private set; }
    [field: SerializeField] public EventReference Pull_Weapon { get; private set; }
    [field: SerializeField] public EventReference Shoot_Arrow { get; private set; }
    [field: SerializeField] public EventReference Whip_Weapon { get; private set; }
}
