using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    bool isActive { get; }

    bool IsTwoHanded();

    float GetRecoilStrength();

    void SetActive(bool value);

    void StartFiring();
    void StopFiring();

    void Reload();

    WeaponHoldingConfig GetHoldingConfig();
    Transform GetWeaponTransform();
    Transform GetBulletEmitter();
    Transform GetLeftHandIKTarget();
    Transform GetRightHandIKTarget();
}
