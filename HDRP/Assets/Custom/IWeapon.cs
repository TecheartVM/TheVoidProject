using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    float GetRecoilStrength();

    bool isActive { get; }

    void SetActive(bool value);

    void Shoot(Vector3 aimPosition, int overrideLayer);

    void Reload();

    WeaponHoldingConfig GetHoldingConfig();
    Transform GetBulletEmitter();
    Transform GetLeftHandIKTarget();
}
