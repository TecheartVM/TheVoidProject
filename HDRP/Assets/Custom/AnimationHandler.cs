using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    Animator animator;

    ThirdPersonControl player;

    public string totalHorizontalInputVariable = "totalHorizontalInput";
    public string forwardInputVariable = "forwardInput";
    public string rightInputVariable = "rightInput";
    public string sprintVariable = "sprintInput";
    public string jumpVariable = "jumpInput";
    public string climbMoveVariable = "climbInput";
    public string climbIdleVariable = "climb";
    public string aimingVariable = "aimingInput";

    public string jumpFromWallTrigger = "jumpFromWall";

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = PlayerManager.instance.player.gameObject.GetComponent<ThirdPersonControl>();
    }

    void Update()
    {
        animator.SetFloat(totalHorizontalInputVariable, Mathf.Clamp(Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal")), 0, 1));
        animator.SetFloat(forwardInputVariable, Input.GetAxis("Vertical"));
        animator.SetFloat(rightInputVariable, Input.GetAxis("Horizontal"));
        animator.SetFloat(sprintVariable, Input.GetAxis("Sprint"));
        animator.SetFloat(jumpVariable, Input.GetAxis("Jump"));
        animator.SetFloat(climbMoveVariable, Input.GetAxis("Horizontal"));
        animator.SetBool(climbIdleVariable, player.isClimbing);
        animator.SetFloat(aimingVariable, Mathf.Lerp(animator.GetFloat(aimingVariable), player.isHoldingAim && !player.isSprinting && player.isHoldingWeapon ? 1 : 0, Time.deltaTime * 10));

        if(Input.GetButtonDown("Jump") && player.isClimbing && Input.GetAxis("Vertical") < 0)
        {
            animator.SetTrigger(jumpFromWallTrigger);
        }
    }
}
