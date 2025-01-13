using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/WeaponSO")]
public class WeaponSO : ItemSO {
    [Serializable]
    public struct V2Array {
        public Vector2[] Points;
    }

    public enum AttackTypes {
        Melee, Ranged, Magic,
    }

    public enum WeaponTypes {
        Sword, Dagger, Axe, Mace, Bow, Crossbow, Staff, Wand,
    }

    public enum DamageTypes {
        Physical, Magical, Fire, Ice, Poison, Lightning,
    }

    public enum AttackMode {
        Light, Heavy
    }

    [Header("Weapon Info")]
    public AttackTypes AttackType;
    public WeaponTypes WeaponType;
    public WeaponActionSO WeaponActionSO;
    public DamageTypes DamageType;

    [Header("Additional Status Effects")]
    public List<ScriptableObject> AdditionalEffects;

    public int Range;

    [Header("Light Attack")]
    public int LightAttackDamage;
    public int LightAttackEnergyCost;
    public float LightAttackSpeed;

    [Header("Heavy Attack")]
    public int HeavyAttackDamage;
    public int HeavyAttackEnergyCost;
    public float HeavyAttackChargeTime;
    public float HeavyAttackSpeed;

    [Header("Block")]
    public float BlockEnergyCost;

    [Header("Special Combo")]
    public float SpecialComboCost;

    [Header("Combo Settings")]
    public float ComboMaxDelay;
    public int ComboMaxCount;

    [Header("Crit Stats")]
    public int CritChance;
    public int CritDamage;

    [Header("Melee Hitboxes")]
    public List<V2Array> ComboPointsLightAttack;
    public List<V2Array> ComboPointsHeavyAttack;
}
