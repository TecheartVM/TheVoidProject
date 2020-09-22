using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Holding Config", fileName = "New Holding Config")]
public class WeaponHoldingConfig : ScriptableObject
{
    public Vector3 posInRightHand;
    public Vector3 rotInRightHand;
    public Vector3 posRightShoulderRelative;
    public Vector3 rotRightShoulderRelative;
    public Vector3 posInactive;
    public Vector3 rotInactive;
}
