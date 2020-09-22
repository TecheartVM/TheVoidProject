using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponBackup : MonoBehaviour
{
    public WeaponHoldingConfig holdingConfig;
    public Transform leftHandIKTarget;
    public Transform bulletEmitter;
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected GameObject muzzlePrefab;

    [SerializeField] protected float bulletSpeed = 50;
    [SerializeField] protected float bulletDamage = 1;
    [SerializeField] protected float fireRate = 10;
    [SerializeField] protected int magazineSize = 30;

    public bool isAutomatic = true;

    [SerializeField] protected bool infiniteAmmo = false;

    [SerializeField] protected float bulletLifeTime = 3;
    [SerializeField] protected float muzzleLifeTime = 1;

    protected float nextBulletTime = 0;
    [SerializeField]
    protected int bulletCount = 0;
    protected int currentBulletCount
    {
        set
        {
            bulletCount = value;
            UpdateUIText();
        }
        get { return bulletCount; }
    }

    [SerializeField] private Transform activeWeaponHolder;
    [SerializeField] private Transform inactiveWeaponHolder;

    [SerializeField] private Text ammoDisplayField;

    protected virtual void Start()
    {
        UpdateUIText();
    }

    protected virtual void Update()
    {
        if (nextBulletTime > 0) nextBulletTime -= Time.deltaTime;
    }

    protected virtual void UpdateUIText()
    {
        if (ammoDisplayField != null) ammoDisplayField.text = $"⋮ {currentBulletCount}/{magazineSize}";
    }

    public virtual void Shoot(Vector3 aimPosition, int layersToHit)
    {
        if (currentBulletCount > 0 || infiniteAmmo)
        {
            if (nextBulletTime <= 0)
            {
                nextBulletTime = 1 / fireRate;
                //SummonMuzzleEffect();
                SummonBullet(aimPosition, layersToHit);
                if (!infiniteAmmo) currentBulletCount--;
            }
        }
    }

    public virtual void Reload()
    {
        currentBulletCount = magazineSize;
    }

    private void SummonMuzzleEffect()
    {
        GameObject muzzle = Instantiate(muzzlePrefab, bulletEmitter.position, bulletEmitter.rotation);
        Destroy(muzzle, muzzleLifeTime);
    }

    protected void SummonBullet(Vector3 aimPosition, int layersToHit)
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletEmitter.position, Quaternion.LookRotation(aimPosition - bulletEmitter.position, Vector3.up));
        bullet.GetComponent<Bullet>().SetParameters(bulletSpeed, bulletDamage, layersToHit);
        Destroy(bullet, bulletLifeTime);
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            transform.parent = activeWeaponHolder;
            transform.localPosition = holdingConfig.weaponOriginalLocalPosition;
            transform.localRotation = Quaternion.Euler(holdingConfig.weaponOriginalLocalRotation);
            UpdateUIText();
        }
        else
        {
            transform.parent = inactiveWeaponHolder;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            transform.localPosition = Vector3.zero;
        }
    }
}
