using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    public static AnimationHandler instance;

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

    public string jumpTrigger = "jump";
    public string jumpFromWallTrigger = "jumpFromWall";

    private static List<int> scheduledAnimations = new List<int>();

    void Awake()
    {
        instance = this;
        animator = GetComponent<Animator>();
        player = PlayerManager.instance.player.gameObject.GetComponent<ThirdPersonControl>();

        player.onJump += TriggerJump;
    }

    void Update()
    {
        animator.SetFloat(totalHorizontalInputVariable, Mathf.Clamp(Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal")), 0, 1));
        animator.SetFloat(forwardInputVariable, Input.GetAxis("Vertical"));
        animator.SetFloat(rightInputVariable, Input.GetAxis("Horizontal"));
        animator.SetFloat(sprintVariable, Input.GetAxis("Sprint"));
        animator.SetFloat(jumpVariable, player.jumpCooldown <= 0 && Input.GetButtonDown("Jump") ? 1 : 0);
        animator.SetFloat(climbMoveVariable, player.edgeReached ? Mathf.Lerp(animator.GetFloat(climbMoveVariable), 0 , Time.deltaTime * 10) : Input.GetAxis("Horizontal"));
        animator.SetBool(climbIdleVariable, player.isClimbing);
        animator.SetFloat(aimingVariable, Mathf.Lerp(animator.GetFloat(aimingVariable), player.isHoldingAim && !player.isSprinting && player.isHoldingWeapon ? 1 : 0, Time.deltaTime * 10));

        if (Input.GetButtonDown("Jump"))
        {
            if(player.isClimbing && Input.GetAxis("Vertical") < 0 && player.jumpCooldown <= 0 && player.currentClimbingState != ClimbingStates.Jumping)
            { 
                animator.SetTrigger(jumpFromWallTrigger);
            }
        }
    }

    private void TriggerJump()
    {
        animator.SetTrigger(jumpTrigger);
    }

    public static void WaitForAnimation(int animationIndex, IEnumerator coroutineToRun)
    {
        scheduledAnimations.Add(animationIndex);
        instance.StartCoroutine(ScheduleEventAction(animationIndex, coroutineToRun));
    }

    public void DoAnimationEvent(int animationIndex)
    {
        scheduledAnimations.Remove(animationIndex);
    }

    public static bool AnimationEventDone(int animationIndex)
    {
        return !scheduledAnimations.Contains(animationIndex);
    }

    private static IEnumerator ScheduleEventAction(int animationIndex, IEnumerator action)
    {
        while (!AnimationEventDone(animationIndex)) { yield return null; }
        instance.StartCoroutine(action);
    }
}
