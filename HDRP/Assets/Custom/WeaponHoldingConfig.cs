using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Holding Config", fileName = "New Holding Config")]
public class WeaponHoldingConfig : ScriptableObject
{
    public Vector3 rightHandPosition;
    public Vector3 rightHandRotation;
    public Vector3 weaponOriginalLocalPosition;
    public Vector3 weaponOriginalLocalRotation;
    public Vector3 inactiveWeaponPosition;
    public Vector3 inactiveWeaponRotation;
}
