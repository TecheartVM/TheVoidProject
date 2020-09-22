using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTurret : Entity
{
    [SerializeField] private float bulletSpeed = 50;
    [SerializeField] private float bulletLifeTime = 2;
    [SerializeField] private LayerMask layersToHit;

    [SerializeField] protected float fireRate = 1;
    [SerializeField] protected float magazineSize = 10;
    [SerializeField] protected float cooldownTime = 10;
    [SerializeField] protected float rotationSpeed = 1;
    [SerializeField] protected float bulletDamage = 1;
    [SerializeField] protected float range = 30;
    [SerializeField] protected float maxBulletDeviation = 1;

    [SerializeField] protected float eyesYOffset = 0;

    [SerializeField] protected Transform headPivot;
    [SerializeField] protected Transform gunPivot;
    [SerializeField] protected Transform bulletEmitter;

    [SerializeField] protected GameObject bulletObject;

    public List<Transform> deathEffects;

    private Transform target;

    private float nextBulletTime = 0f;
    [SerializeField] private float currentBulletAmount = 0;
    [SerializeField] private float cooldown = 0;

    private bool isShooting = false;

    private void Start()
    {
        target = PlayerManager.instance.player.transform;
        cooldown = cooldownTime;
        currentBulletAmount = magazineSize;

        this.onDeath += SpawnDeathEffects;
    }

    void Update()
    {
        if (!isDead)
        {
            if(SeeTarget(target))
            {
                FaceTarget(target);
                if(!isShooting && cooldown <= 0) StartCoroutine(Shoot());
            }

            if (cooldown > 0) cooldown -= Time.deltaTime;
        }
    }

    protected IEnumerator Shoot()
    {
        //print("Start shooting");

        isShooting = true;

        while(currentBulletAmount > 0)
        {
            if (isDead) break;

            if (nextBulletTime <= 0)
            {
                nextBulletTime += 1 / fireRate;

                if (cooldown <= 0)
                {
                    currentBulletAmount--;
                    SummonBullet();
                }
            }
            else
            {
                nextBulletTime -= Time.deltaTime;
            }

            yield return null;
        }

        if (!isDead)
        {
            cooldown = cooldownTime;
            currentBulletAmount = magazineSize;
        }
        isShooting = false;
    }

    protected bool SeeTarget(Transform target)
    {
        Vector3 eyes = transform.position;
        eyes.y += eyesYOffset;

        if (Vector3.Distance(eyes, target.position + Vector3.up) > range) return false;

        RaycastHit hit;
        if(Physics.Linecast(eyes, target.position + Vector3.up, out hit, 9))
        {
            if (hit.transform != null) return false;
        }

        return true;
    }

    protected void FaceTarget(Transform target)
    {
        Vector3 targetXZ = new Vector3(target.position.x, headPivot.position.y, target.position.z);
        headPivot.rotation = Quaternion.LookRotation(Vector3.Slerp(headPivot.forward, targetXZ - headPivot.position, Time.deltaTime * rotationSpeed));
        gunPivot.rotation = Quaternion.LookRotation(Vector3.Slerp(gunPivot.forward, target.position + Vector3.up - gunPivot.position, Time.deltaTime * rotationSpeed));
    }

    protected void SummonBullet()
    {
        Vector3 shootDirection = bulletEmitter.forward;
        shootDirection = Quaternion.AngleAxis(Random.Range(0f, maxBulletDeviation), bulletEmitter.up) * shootDirection;
        shootDirection = Quaternion.AngleAxis(Random.Range(-180f, 180f), bulletEmitter.forward) * shootDirection;
        //Debug.DrawRay(bulletEmitter.position, shootDirection, Color.red);
        GameObject bullet = Instantiate(bulletObject, bulletEmitter.position, Quaternion.LookRotation(shootDirection, bulletEmitter.up));
        bullet.GetComponent<Bullet>().SetParameters(bulletSpeed, bulletDamage, layersToHit);
        Destroy(bullet, bulletLifeTime);
    }

    protected void SpawnDeathEffects()
    {
        //print($"{gameObject} is dead");
    }
}
