using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/WeaponSO")]
public class WeaponSO : ItemSO {
    public enum AttackTypes {
        Melee, Ranged, Magic,
    }

    public enum WeaponTypes {
        Sword, Dagger, Axe, Mace, Bow, Crossbow, Staff, Wand,
    }
    
    public enum DamageTypes {
        Physical, Magical, Fire, Ice, Poison, Lightning,
    }

    [Header("Weapon Info")]
    public AttackTypes AttackType;
    public WeaponTypes WeaponType;
    public DamageTypes DamageType;
    public int Damage;
    public int Range;
    public int EnergyOnAction;
    public float AttackSpeed;
    public int CritChance;
    public int CritDamage;
}
