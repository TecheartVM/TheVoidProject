using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPart : Entity
{
    [SerializeField] GameObject parentEntity;

    private Entity entity;

    private float parentDamageMultiplier = 1.0f;

    private void Start()
    {
        entity = parentEntity.GetComponent<Entity>();
    }

    public override void DealDamage(float amount)
    {
        entity.DealDamage(amount * parentDamageMultiplier);
    }
}
