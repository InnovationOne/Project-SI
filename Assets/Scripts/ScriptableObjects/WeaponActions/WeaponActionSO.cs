using UnityEngine;
using static WeaponSO;

public abstract class WeaponActionSO : ScriptableObject {
    public virtual void OnUseWeapon(Vector2 moveDir, Vector2 origin, AttackMode attackMode, ItemSlot itemSlot) {
        throw new System.NotImplementedException();
    }
}
