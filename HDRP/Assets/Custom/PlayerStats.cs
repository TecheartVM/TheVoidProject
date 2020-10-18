using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public CharacterStat shootingAccuracy;

    private ThirdPersonControl controller;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonControl>();
    }

    public float GetShootingInaccuracy()
    {
        float value = controller.isAiming ? 0.8f : controller.isSprinting ? 1.4f : 1;
        if (controller.isClimbing) value *= 1.3f;
        value *= shootingAccuracy.GetBaseValue() / shootingAccuracy.value;
        //Debug.Log(value);
        return value;
    }
}
