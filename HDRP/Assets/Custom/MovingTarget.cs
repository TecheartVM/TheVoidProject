using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    public float movingSpeed = 1;
    public float movingRange = 3;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position = startPosition + transform.forward * Mathf.PingPong(Time.time * movingSpeed, movingRange);
    }
}
