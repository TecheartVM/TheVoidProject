using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTest : MonoBehaviour
{
    public bool enableFeetIK = true;
    [Range(0f, 2f)] [SerializeField] private float raycastDistanceFromGround = 1;
    [Range(0f, 2f)] [SerializeField] private float raycastDistance = 1;
    [Range(0f, 1f)] [SerializeField] private float feetMoveSpeed = 1;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private float pelvisOffsetY = 0;
    [Range(0f, 1f)] [SerializeField] private float pelvisMoveSpeed = 1;
    public string leftFootAnimatorVariableName = "leftFootIKWeight";
    public string rightFootAnimatorVariableName = "rightFootIKWeight";
    public bool showDebugInfo = true;

    private Vector3 leftFootPosition, rightFootPosition;
    private Vector3 leftFootTargetPosition, rightFootTargetPosition;
    private Quaternion leftFootTargetRotation, rightFootTargetRotation;
    private float leftFootPrevPositionY = 0, rightFootPrevPositionY = 0;
    private float pelvisPrevPositionY = 0;

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null) return;

        if (!enableFeetIK) return;

        AdjustFootTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);
        AdjustFootTarget(ref rightFootPosition, HumanBodyBones.RightFoot);

        setFeetIKTarget(leftFootPosition, ref leftFootTargetPosition, ref leftFootTargetRotation);
        setFeetIKTarget(rightFootPosition, ref rightFootTargetPosition, ref rightFootTargetRotation);
    }

    private void OnAnimatorIK(int layerMask)
    {
        if (!enableFeetIK) return;
        if (animator == null) return;

        MovePelvisHeight();

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(leftFootAnimatorVariableName));
        MoveFootToTargetPos(AvatarIKGoal.LeftFoot, leftFootTargetPosition, leftFootTargetRotation, ref leftFootPrevPositionY);

        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rightFootAnimatorVariableName));
        MoveFootToTargetPos(AvatarIKGoal.RightFoot, rightFootTargetPosition, rightFootTargetRotation, ref rightFootPrevPositionY);
    }

    protected void MoveFootToTargetPos(AvatarIKGoal foot, Vector3 IKPosition, Quaternion IKRotation, ref float prevYPosition)
    {
        Vector3 targetPosition = animator.GetIKPosition(foot);

        if(IKPosition != Vector3.zero)
        {
            targetPosition = transform.InverseTransformPoint(targetPosition);
            IKPosition = transform.InverseTransformPoint(IKPosition);

            prevYPosition = Mathf.Lerp(prevYPosition, IKPosition.y, feetMoveSpeed);
            targetPosition.y += prevYPosition;

            targetPosition = transform.TransformPoint(targetPosition);

            animator.SetIKRotation(foot, IKRotation);
        }

        animator.SetIKPosition(foot, targetPosition);
    }

    protected void MovePelvisHeight()
    {
        if(pelvisPrevPositionY == 0 || leftFootTargetPosition == Vector3.zero || rightFootTargetPosition == Vector3.zero)
        {
            pelvisPrevPositionY = animator.bodyPosition.y;
            return;
        }

        float transformY = transform.position.y;
        float targetPelvisOffset = Mathf.Min(leftFootTargetPosition.y - transformY, rightFootTargetPosition.y - transformY);

        Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * targetPelvisOffset;
        newPelvisPosition.y = Mathf.Lerp(pelvisPrevPositionY, newPelvisPosition.y, pelvisMoveSpeed);
        animator.bodyPosition = newPelvisPosition;
        pelvisPrevPositionY = animator.bodyPosition.y;
    }

    protected void setFeetIKTarget(Vector3 raycastOrigin, ref Vector3 footIKPosition, ref Quaternion footIKRotation)
    {
        RaycastHit hit;

        if (showDebugInfo) Debug.DrawLine(raycastOrigin, raycastOrigin + Vector3.down * (raycastDistance + raycastDistanceFromGround), Color.yellow);

        if(Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance + raycastDistanceFromGround, environmentLayer))
        {
            footIKPosition = raycastOrigin;
            footIKPosition.y = hit.point.y + pelvisOffsetY;
            footIKRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;

            return;
        }

        footIKPosition = Vector3.zero;
    }

    protected void AdjustFootTarget(ref Vector3 footPosition, HumanBodyBones foot)
    {
        footPosition = animator.GetBoneTransform(foot).position;
        footPosition.y = transform.position.y + raycastDistanceFromGround;
    }
}
