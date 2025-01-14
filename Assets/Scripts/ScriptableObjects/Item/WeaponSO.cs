using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemSO/WeaponSO")]
public class WeaponSO : ItemSO {
    [Serializable]
    public struct V2Array {
        public Vector2[] Points;
    }

    public enum WeaponTypes {
        Melee, Ranged, Magic,
    }

    public enum DamageTypes {
        Physical, Magical, Fire, Ice, Poison, Lightning,
    }

    [Header("Weapon Info")]
    public WeaponTypes WeaponType;
    public DamageTypes DamageType;

    [Header("Additional Status Effects")]
    public List<ScriptableObject> AdditionalEffects;

    [Header("Attack")]
    public int AttackDamage;
    public int AttackEnergyCost;
    public float AttackSpeed;
    public float KnockbackForce;

    [Header("Combo Settings")]
    public float ComboMaxDelay;
    public int ComboMaxCount;

    [Header("Crit Stats")]
    public int CritChance;
    public int CritDamage;

    [Header("Melee Hitboxes")]
    public List<V2Array> ComboPointsAttack;

    [Header("Animator")]
    public bool HasBowAnimation;
    public bool HasHurtAnimation;
    public bool HasSlashAnimation;
    public bool HasSlashReverseAnimation;
    public bool HasSpellcastAnimation;
    public bool HasThrustAnimation;

    public RuntimeAnimatorController Animator;
}
