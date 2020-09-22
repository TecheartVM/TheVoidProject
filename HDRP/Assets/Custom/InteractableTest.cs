using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableTest : Interactable
{
    [SerializeField] private Transform targetObject;

    protected override void Interact()
    {
        base.Interact();
        StartCoroutine(Rotate());
    }

    private IEnumerator Rotate()
    {
        int i = 45;
        while(i > 0)
        {
            targetObject.Rotate(new Vector3(0, 1, 0));
            i--;
            yield return null;
        }
    }
}
