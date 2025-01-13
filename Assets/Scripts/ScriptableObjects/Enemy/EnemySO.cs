using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject {
    [Serializable]
    public struct ItemSlotToDrop {
        public float Probability;
        public int ItemId;
        public int MinAmount;
        public int MaxAmount;
        public int RarityId;
    }

    [Header("Enemy Info")]
    public string Name;
    public string Description;

    [Header("Enemy Stats")]
    public int Health;
    public int Armor;
    public int Shield;
    public int FireResistance;
    public int IceResistance;
    public int PoisonResistance;
    public int LightningResistance;

    [Header("Enemy Movement")]
    public float Speed;
    public float KnockbackForce;
    public float MinIdleTime;
    public float MaxIdleTime;

    [Header("Enemy Combat")]
    public int AttackDamage;
    public float AttackRange;
    public float AttackSpeed;

    [Header("Enemy Range Attack")]
    public GameObject ProjectilePrefab;

    [Header("Enemy Crit")]
    public int CritChance;
    public int CritDamage;

    [Header("Enemy AI")]
    public float PursueRange;
    public float RetreatHealthThreshold;

    [Header("Enemy Drops")]
    public ItemSlotToDrop[] ItemsToDrop;

    [Header("Melee Hitboxes")]
    public Vector2[] AttackPoints;
}
