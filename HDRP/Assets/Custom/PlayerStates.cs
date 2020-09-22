using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStates : MonoBehaviour
{
    [SerializeField] public bool isRunning = false;
    [SerializeField] public bool isSprinting = false;
    [SerializeField] public bool isFalling = false;
    [SerializeField] public bool isClimbing = false;
    [SerializeField] public bool isAiming = false;
    [SerializeField] public bool isShooting = false;
    [SerializeField] public bool isDead = false;
}
