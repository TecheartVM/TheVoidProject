using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifeTime = 1;

    private float speed = 0;
    private float damage = 0;
    private LayerMask hitableLayer;

    private Vector3 previousPosition;

    public void SetParameters(float speed, float damage, int layersToHit)
    {
        this.speed = speed;
        this.damage = damage;
        this.hitableLayer = layersToHit;
    }

    private void Update()
    {
        previousPosition = transform.position;
        transform.position += transform.forward * speed * Time.deltaTime;

        RaycastHit hit;
        if(Physics.Linecast(previousPosition, transform.position, out hit, hitableLayer))
        {
            //Debug.Log("Hit!");
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, hitEffectLifeTime);
            }

            Entity entity = hit.transform.gameObject.GetComponent<Entity>();
            if (entity != null)
            {
                entity.DealDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
