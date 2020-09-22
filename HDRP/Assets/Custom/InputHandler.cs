using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float x, y;
    public float xRaw, yRaw;
    public float jump;
    public float crouch;
    public float sprint;

    public float mouseL;
    public float mouseR;
    //public float mouseM;

    void Start()
    {
        
    }

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        xRaw = Input.GetAxisRaw("Horizontal");
        yRaw = Input.GetAxisRaw("Vertical");
        jump = Input.GetAxis("Jump");
        crouch = Input.GetAxis("Crouch");
        sprint = Input.GetAxis("Sprint");

        mouseL = Input.GetAxis("Fire");
        mouseR = Input.GetAxis("Aim");

        Debug.Log("input");
    }
}
