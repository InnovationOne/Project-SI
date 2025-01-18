using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/ClothingSO")]
public class ClothingSO : ItemSO {
    public enum ClothingSet {
        MedievalFantasy,
    }

    public enum ClothingType {
        Belt, Feet, Hands, Helmet, Legs, Torso
    }

    [Header("General")]
    public ClothingSet Set;
    public ClothingType Type;
    public Sprite PlayerClothingUiSprite;
    public int HealthBoost;
    public int EnergyBoost;

    [Header("Protection")]
    public int Armor;
    public int Shield;
    public int FireResistance;
    public int IceResistance;
    public int PoisonResistance;
    public int LightningResistance;

    [Header("Movement")]
    public float SpeedBoost;

    [Header("Animation")]
    public RuntimeAnimatorController Animator;
}
