using Cinemachine;
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

    private ThirdPersonControl character;
    private CinemachineImpulseSource cinemachineImpulse;
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

    private bool isFiring = false;
    #endregion

    #region Main
    void Start()
    {
        character = PlayerManager.instance.player.GetComponent<ThirdPersonControl>();
        cinemachineImpulse = GetComponent<CinemachineImpulseSource>();
        if (cinemachineImpulse == null) Debug.LogWarning("Raycast Weapon " + gameObject + " doesn't have an impulse source!");
    }

    protected void Update()
    {
        if (isFiring && isAutomatic)
        {
            Shoot();
        }

        if (nextBulletTime > 0)
        {
            nextBulletTime -= Time.deltaTime;
        }
    }
    #endregion

    #region IWeapon
    public bool isActive { get; private set; }

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
        if (isAutomatic) isFiring = true;
        else Shoot();
    }

    public void StopFiring()
    {
        isFiring = false;
    }

    public void Reload()
    {
        currentBulletCount = magazineSize;
    }

    public bool IsTwoHanded()
    {
        return isTwoHanded;
    }

    public float GetRecoilStrength()
    {
        return recoilStrength;
    }

    public WeaponHoldingConfig GetHoldingConfig()
    {
        return holdingConfig;
    }

    public Transform GetBulletEmitter()
    {
        return bulletEmitter;
    }

    public Transform GetWeaponTransform()
    {
        return transform;
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
    
    public void Shoot()
    {
        Vector3 currentAimPos = character.GetAimPoint();
        if (nextBulletTime <= 0)
        {
            if (currentBulletCount > 0)
            {
                Destroy(Instantiate(hitEffectPrefab, currentAimPos, Quaternion.LookRotation(bulletEmitter.position - currentAimPos)), 2);
                if (!infiniteAmmo) currentBulletCount--;
                nextBulletTime = 1 / fireRate;

                DoRecoil();
            }
        }
    }

    private void DoRecoil()
    {
        if (cinemachineImpulse == null) return;

        /*additional recoil modifiers can be added here*/
        float finalStrength = recoilStrength * (character.isAiming ? 0.8f : character.isSprinting ? 1.4f : 1); 

        cinemachineImpulse.GenerateImpulse(CameraController.instance.mainCamera.forward * finalStrength);
        CameraController.instance.DoRecoil(finalStrength);
    }

    protected virtual void UpdateUIText()
    {
        if (ammoDisplayField != null) ammoDisplayField.text = $"⋮ {currentBulletCount}/{magazineSize}";
    }
}
