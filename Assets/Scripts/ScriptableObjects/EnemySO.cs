using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject {
    public int Health;
    public int Armor;
    public int Shield;
    public int FireResistance;
    public int IceResistance;
    public int PoisonResistance;
    public int LightningResistance;

    public float Speed;
    public float MinIdleTime;
    public float MaxIdleTime;

    public WeaponSO WeaponSO;
}
