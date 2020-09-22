using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    private bool canInteract = false; 

    protected virtual void Interact()
    {
        //print("Interacting!");
    }

    void Update()
    {
        if(canInteract)
        {
            ThirdPersonControl playerController = PlayerManager.instance.player.GetComponent<ThirdPersonControl>();
            if(Vector3.Angle(playerController.cameraLookDirection, transform.position - playerController.GetCameraTransform().position) <= playerController.GetInteractionFieldAngle())
            {
                if(Input.GetButtonDown("Interact"))
                {
                    Interact();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            canInteract = false;
        }
    }
}
