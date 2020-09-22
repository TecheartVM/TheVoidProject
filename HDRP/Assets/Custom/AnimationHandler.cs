using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    Animator animator;

    ThirdPersonControl player;

    public string forwardInputVariable = "forwardInput";
    public string sprintVariable = "sprintInput";
    public string jumpVariable = "jumpInput";
    public string climbMoveVariable = "climbInput";
    public string climbIdleVariable = "climb";
    public string aimingVariable = "aimingInput";

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = PlayerManager.instance.player.gameObject.GetComponent<ThirdPersonControl>();
    }

    void Update()
    {
        animator.SetFloat(forwardInputVariable, Mathf.Clamp(Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal")), 0, 1));
        animator.SetFloat(sprintVariable, Input.GetAxis("Sprint"));
        animator.SetFloat(jumpVariable, Input.GetAxis("Jump"));
        animator.SetFloat(climbMoveVariable, Input.GetAxis("Horizontal"));
        animator.SetBool(climbIdleVariable, player.isClimbing);
        animator.SetFloat(aimingVariable, Input.GetAxis("Aim"));
    }
}
