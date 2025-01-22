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

    [Header("Melee Hitbox")]

    [ConditionalHide("HasSlashAnimation", true)]
    public int SlashHitFrameIndex;
    [ConditionalHide("HasThrustAnimation", true)]
    public int ThrustHitFrameIndex;

    [Header("Animator")]
    public bool HasBowAnimation;
    public bool HasSlashAnimation;
    public bool HasSlashReverseAnimation;
    public bool HasSpellcastAnimation;
    public bool HasThrustAnimation;

    [Header("Animator Controllers")]
    public RuntimeAnimatorController AnimatorBG;
    public RuntimeAnimatorController AnimatorFG;
}
