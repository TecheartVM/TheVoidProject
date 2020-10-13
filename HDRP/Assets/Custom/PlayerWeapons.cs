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

    public int GetFirstOneHandedWeaponIndex(int startIndex, bool reverse)
    {
        if (startIndex > weaponsList.Count || startIndex < 0)
        {
            Debug.LogError("Incorrect weapon index!");
            return 0;
        }
        if (!reverse)
        {
            int i = startIndex + 1;
            while (i != startIndex)
            {
                if (i >= weaponsList.Count) i = 0;
                GameObject weaponObj = weaponsList[i];
                IWeapon weapon = weaponObj.GetComponent<IWeapon>();
                if (weapon != null)
                {
                    if (!weapon.IsTwoHanded()) return i;
                }
                else
                {
                    return 0;
                }
                i++;
            }
        }
        else
        {
            int i = startIndex - 1;
            while (i != startIndex)
            {
                if (i < 0) i = weaponsList.Count - 1;
                GameObject weaponObj = weaponsList[i];
                IWeapon weapon = weaponObj.GetComponent<IWeapon>();
                if (weapon != null)
                {
                    if (!weapon.IsTwoHanded()) return i;
                }
                else
                {
                    return 0;
                }
                i--;
            }
        }
        return 0;
    }
}
