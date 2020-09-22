using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimingIK : MonoBehaviour
{
    private Transform leftHandIKTarget;
    private Transform bulletEmitter;

    [SerializeField] [Range(0, 1)] private float lookWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float bodyWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float headWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float lookWeightAiming = 1f;
    [SerializeField] [Range(0, 1)] private float bodyWeightAiming = 0.4f;
    [SerializeField] [Range(0, 1)] private float headWeightAiming = 1f;

    [SerializeField] private bool showDebugInfo = true;

    private Animator animator;

    private ThirdPersonControl characterControlScript;

    private Transform rightShoulder;

    private Transform leftHandTarget;
    private Transform rightHandTarget;
    private Transform aimingPivot;

    private float rightHandIKWeight = 0;

    private bool enableIK = false;
    public bool doIK 
    { 
        set 
        { 
            if(value == true) animator.SetLayerWeight(animator.GetLayerIndex("Hands"), 1); 
            else animator.SetLayerWeight(animator.GetLayerIndex("Hands"), 0);
            enableIK = value;
        }
        get
        {
            return enableIK;
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        
        doIK = false;

        characterControlScript = PlayerManager.instance.player.GetComponent<ThirdPersonControl>();

        if (animator != null)
        {
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        }
        else
        {
            Debug.LogError("Animator isn't attached!");
        }

        characterControlScript.onWeaponSwitched += SetupTemporaryObjects;

        SetupTemporaryObjects();
    }

    private void SetupTemporaryObjects()
    {
        IWeapon currentWeapon = characterControlScript.currentWeapon;

        if (currentWeapon == null)
        {
            doIK = false;
            return;
        }

        WeaponHoldingConfig currentWeaponHoldingConfig = currentWeapon.GetHoldingConfig();

        if (currentWeaponHoldingConfig == null)
        {
            doIK = false;
            return;
        }

        doIK = true;

        leftHandIKTarget = characterControlScript.currentWeapon.GetLeftHandIKTarget();
        bulletEmitter = characterControlScript.currentWeapon.GetBulletEmitter();

        if (aimingPivot != null) Destroy(aimingPivot.gameObject);
        if (leftHandTarget != null) Destroy(leftHandTarget.gameObject);
        if (rightHandTarget != null) Destroy(rightHandTarget.gameObject);

        aimingPivot = new GameObject().transform;
        aimingPivot.name = "Aiming Pivot";
        aimingPivot.parent = transform;

        leftHandTarget = new GameObject().transform;
        leftHandTarget.name = "Left Hand Target";
        leftHandTarget.parent = aimingPivot;

        rightHandTarget = new GameObject().transform;
        rightHandTarget.name = "Right Hand Target";
        rightHandTarget.parent = aimingPivot;

        rightHandTarget.localPosition = currentWeaponHoldingConfig.rightHandPosition;
        rightHandTarget.localRotation = Quaternion.Euler(currentWeaponHoldingConfig.rightHandRotation);
    }

    void Update()
    {
        if (enableIK)
        {
            if (characterControlScript.isAiming) rightHandIKWeight = Mathf.Lerp(rightHandIKWeight, 1, Time.deltaTime * characterControlScript.aimingSpeed);
            else rightHandIKWeight = 0;

            leftHandTarget.position = leftHandIKTarget.position;
            leftHandTarget.rotation = leftHandIKTarget.rotation;

            if (showDebugInfo)
            {
                Debug.DrawRay(bulletEmitter.position, bulletEmitter.forward * 20, Color.red);
                Debug.DrawRay(aimingPivot.position, aimingPivot.forward * 20);
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (enableIK)
        {
            aimingPivot.position = rightShoulder.position;

            if (characterControlScript.isAiming)
            {
                animator.SetLookAtWeight(lookWeightAiming, bodyWeightAiming, headWeightAiming);

                aimingPivot.rotation = Quaternion.Slerp(aimingPivot.rotation, Quaternion.LookRotation(characterControlScript.currentAimPoint - aimingPivot.position), Time.deltaTime * characterControlScript.aimingSpeed);
            }
            else
            {
                animator.SetLookAtWeight(lookWeightNormal, bodyWeightNormal, headWeightNormal);
            }
            animator.SetLookAtPosition(characterControlScript.currentAimPoint);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);

            if (characterControlScript.isAiming)
            {
                if (Vector3.Distance(bulletEmitter.parent.position, characterControlScript.currentAimPoint) > 1)
                {
                    bulletEmitter.parent.rotation = Quaternion.LookRotation(characterControlScript.currentAimPoint - bulletEmitter.parent.position, Vector3.up);
                }
                else
                {
                    bulletEmitter.parent.localRotation = Quaternion.Euler(characterControlScript.currentWeapon.GetHoldingConfig().weaponOriginalLocalRotation);
                }
            }
        }
    }
}
