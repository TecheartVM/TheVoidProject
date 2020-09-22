using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loot : Interactable
{
    [SerializeField] private GameObject targetObject;

    protected override void Interact()
    {
        base.Interact();
        if(targetObject.GetComponent<IWeapon>() != null)
        {
            PlayerManager.instance.player.GetComponent<PlayerWeapons>().AddWeapon(targetObject);
            Destroy(gameObject);
        }
    }
}
