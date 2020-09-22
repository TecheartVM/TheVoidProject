using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    private bool canInteract = false;

    private ThirdPersonControl characterController;
    private CameraController cameraController;

    private void Start()
    {
        characterController = PlayerManager.instance.player.GetComponent<ThirdPersonControl>();
        cameraController = PlayerManager.instance.player.GetComponent<CameraController>();
    }

    protected virtual void Interact()
    {
        //print("Interacting!");
    }

    void Update()
    {
        if(canInteract)
        {
            if(Vector3.Angle(cameraController.mainCamera.forward, transform.position - cameraController.mainCamera.position) <= characterController.GetInteractionFieldAngle())
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
