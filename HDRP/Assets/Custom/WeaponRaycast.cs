using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponRaycast : MonoBehaviour, IWeapon
{
    #region External Objects
    public WeaponHoldingConfig holdingConfig;
    public Transform leftHandIKTarget;
    public Transform rightHandIKTarget;
    public Transform bulletEmitter;
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected GameObject muzzlePrefab;
    [SerializeField] protected GameObject hitEffectPrefab;

    [SerializeField] private Transform activeWeaponHolder;
    [SerializeField] private Transform inactiveWeaponHolder;

    [SerializeField] private Text ammoDisplayField;
    #endregion

    #region Weapon Properties
    [SerializeField] protected float bulletDamage = 1;
    [SerializeField] protected float fireRate = 10;
    [SerializeField] protected int magazineSize = 30;
    [SerializeField] protected float recoilStrength = 1;

    [SerializeField] protected bool isTwoHanded = true;
    [SerializeField] protected bool isAutomatic = true;
    [SerializeField] protected bool infiniteAmmo = false;
    #endregion

    #region Local Variables
    private int bulletCount;
    protected int currentBulletCount
    {
        set
        {
            bulletCount = value;
            UpdateUIText();
        }
        get
        {
            return bulletCount;
        }
    }
    private float nextBulletTime = 0;
    #endregion

    #region IWeapon
    public bool isActive { get; private set; }

    public bool IsTwoHanded()
    {
        return isTwoHanded;
    }

    public float GetRecoilStrength()
    {
        return recoilStrength;
    }

    public void Reload()
    {
        currentBulletCount = magazineSize;
    }

    public void SetActive(bool value)
    {
        if (value)
        {
            //transform.parent = activeWeaponHolder;
            //transform.localPosition = holdingConfig.weaponOriginalLocalPosition;
            //transform.localRotation = Quaternion.Euler(holdingConfig.weaponOriginalLocalRotation);
            UpdateUIText();
        }
        else
        {
            transform.parent = inactiveWeaponHolder;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            transform.localPosition = Vector3.zero;
        }
        isActive = value;
    }

    public void Shoot(Vector3 aimPosition, int overrideLayer)
    {
        if (nextBulletTime <= 0)
        {
            if (currentBulletCount > 0)
            {
                Destroy(Instantiate(hitEffectPrefab, aimPosition, Quaternion.LookRotation(bulletEmitter.position - aimPosition)), 2);
                if (!infiniteAmmo) currentBulletCount--;
                nextBulletTime = 1 / fireRate;
            }
        }
    }

    public void StartFiring()
    {

    }

    public void StopFiring()
    {

    }

    public WeaponHoldingConfig GetHoldingConfig()
    {
        return holdingConfig;
    }

    public Transform GetBulletEmitter()
    {
        return bulletEmitter;
    }

    public Transform GetLeftHandIKTarget()
    {
        return leftHandIKTarget;
    }

    public Transform GetRightHandIKTarget()
    {
        return rightHandIKTarget;
    }
    #endregion

    protected void Update()
    {
        if (nextBulletTime > 0)
        {
            nextBulletTime -= Time.deltaTime;
        }
    }

    protected virtual void UpdateUIText()
    {
        if (ammoDisplayField != null) ammoDisplayField.text = $"⋮ {currentBulletCount}/{magazineSize}";
    }

    public Transform GetWeaponTransform()
    {
        return transform;
    }

}
