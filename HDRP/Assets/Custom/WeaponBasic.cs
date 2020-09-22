using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponBasic : MonoBehaviour, IWeapon
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
    [SerializeField] protected float bulletSpeed = 50;
    [SerializeField] protected float bulletDamage = 1;
    [SerializeField] protected float fireRate = 10;
    [SerializeField] protected int magazineSize = 30;
    [SerializeField] protected float recoilStrength = 1;

    [SerializeField] protected bool isTwoHanded = false;
    [SerializeField] protected bool isAutomatic = true;
    [SerializeField] protected bool infiniteAmmo = false;

    [SerializeField] protected float bulletLifeTime = 3;
    [SerializeField] protected float muzzleLifeTime = 1;
    #endregion

    #region Local Variables
    [SerializeField] protected int bulletCount = 0;
    protected float nextBulletTime = 0;
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

    protected bool canShoot = true;
    protected bool shotCooldown = false;
    #endregion

    #region IWeapon
    public bool isActive { get; private set; } = false;

    public bool IsTwoHanded()
    {
        return isTwoHanded;
    }

    public float GetRecoilStrength()
    {
        return recoilStrength;
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

    public void StartFiring()
    {

    }

    public void StopFiring()
    {

    }

    public void Reload()
    {
        currentBulletCount = magazineSize;
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
    
    public Transform GetWeaponTransform()
    {
        return transform;
    }
    #endregion

    #region Main
    protected void Update()
    {
        if (nextBulletTime > 0)
        {
            nextBulletTime -= Time.deltaTime;
        }
        if (!isAutomatic && Input.GetButtonUp("Fire")) shotCooldown = false;

        if (!isActive) return;

    }
    #endregion

    public void Shoot(Vector3 aimPosition, int overrideLayer)
    {
        if (bulletCount <= 0) return;

        if (isAutomatic || (!isAutomatic && !shotCooldown))
        {
            if (nextBulletTime <= 0)
            {
                StartCoroutine(HandleShot(aimPosition, overrideLayer));
                if(!infiniteAmmo) currentBulletCount--;
                nextBulletTime = 1 / fireRate;
            }
        }

        if(!isAutomatic) shotCooldown = true;
    }

    protected float GetTimeToHit(Vector3 hitPoint, float bulletSpeed)
    {
        if(bulletSpeed <= 0)
        {
            Debug.LogError("Incorrect bullet speed!");
            return 0;
        }
        float distance = Vector3.Distance(bulletEmitter.position, hitPoint);
        return distance / bulletSpeed;
    }

    protected IEnumerator HandleShot(Vector3 hitPoint, int overrideLayer)
    {
        float timeToHit = GetTimeToHit(hitPoint, bulletSpeed); ;
        GameObject bullet = Instantiate(bulletPrefab, bulletEmitter.position, Quaternion.LookRotation(hitPoint - bulletEmitter.position));
        Vector3 previousBulletPos = bulletEmitter.position;
        RaycastHit hit;
        Transform hitObj = null;
        while (timeToHit > 0)
        {
            if(Physics.Linecast(previousBulletPos, bullet.transform.position, out hit, overrideLayer))
            {
                hitPoint = hit.point;
                hitObj = hit.transform;
                break;
            }

            yield return null;

            timeToHit -= Time.deltaTime;
            bullet.transform.position += bullet.transform.forward * bulletSpeed * Time.deltaTime;
        }
        Destroy(bullet);

        GameObject hitEffect;
        if (hitObj != null) hitEffect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(bulletEmitter.position - hitPoint), hitObj);
        else hitEffect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(bulletEmitter.position - hitPoint));
        Destroy(hitEffect, 2);
    }

    protected virtual void UpdateUIText()
    {
        if (ammoDisplayField != null) ammoDisplayField.text = $"⋮ {currentBulletCount}/{magazineSize}";
    }
}