using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapons : MonoBehaviour
{
    public List<GameObject> weaponsList;

    private void Awake()
    {

    }

    public void AddWeapon(GameObject weapon)
    {
        weaponsList.Add(weapon);
        gameObject.GetComponent<ThirdPersonControl>().SwitchWeapon(weaponsList.Count - 1);
    }
}
