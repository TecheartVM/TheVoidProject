using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimingIK : MonoBehaviour
{
    private Transform bulletEmitter;

    [SerializeField] [Range(0, 1)] private float lookWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float bodyWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float headWeightNormal = 0.3f;
    [SerializeField] [Range(0, 1)] private float lookWeightAiming = 1f;
    [SerializeField] [Range(0, 1)] private float bodyWeightAiming = 0.0f;
    [SerializeField] [Range(0, 1)] private float headWeightAiming = 1f;

    [SerializeField] private bool showDebugInfo = true;

    public Transform pivot;
    public Transform target;

    private Animator animator;

    private ThirdPersonControl characterControlScript;

    private Transform rightShoulder;

    private Transform leftHandTarget;
    private Transform rightHandTarget;

    private float rightHandIKWeight = 0;

    private bool enableIK = false;
    public bool doIK 
    { 
        set 
        {
            if (value)
            {
                if (!characterControlScript.isClimbing) animator.SetLayerWeight(animator.GetLayerIndex("Hands"), 1);
            }
            else animator.SetLayerWeight(animator.GetLayerIndex("Hands"), 0);
            enableIK = value;
        }
        get
        {
            return enableIK;
        }
    }

    private bool holdingStateUpdateFlag = true;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        doIK = false;

        characterControlScript = PlayerManager.instance.player.GetComponent<ThirdPersonControl>();

        if (animator != null)
        {
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            pivot.position = rightShoulder.position;
        }
        else
        {
            Debug.LogError("Animator isn't attached!");
        }

        characterControlScript.onWeaponSwitched += SetupTemporaryObjects;
        characterControlScript.onGrounding += () => doIK = doIK;

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

        leftHandTarget = characterControlScript.currentWeapon.GetLeftHandIKTarget();
        rightHandTarget = characterControlScript.currentWeapon.GetRightHandIKTarget();

        bulletEmitter = characterControlScript.currentWeapon.GetBulletEmitter();

        SetAimingState(characterControlScript.isHoldingAim);
    }

    public void SetAimingState(bool value)
    {
        IWeapon weapon = characterControlScript.currentWeapon;
        if (weapon == null) return;
        Transform weaponTransform = weapon.GetWeaponTransform();
        if (value)
        {
            weaponTransform.parent = pivot;
            weaponTransform.localPosition = weapon.GetHoldingConfig().posRightShoulderRelative;
            weaponTransform.localRotation = Quaternion.Euler(weapon.GetHoldingConfig().rotRightShoulderRelative);
        }
        else
        {
            weaponTransform.parent = animator.GetBoneTransform(HumanBodyBones.RightHand);
            weaponTransform.localPosition = weapon.GetHoldingConfig().posInRightHand;
            weaponTransform.localRotation = Quaternion.Euler(weapon.GetHoldingConfig().rotInRightHand);
        }
    }

    void Update()
    {
        if (enableIK)
        {
            if (characterControlScript.isHoldingAim) rightHandIKWeight = Mathf.Lerp(rightHandIKWeight, 1, Time.deltaTime * characterControlScript.aimingSpeed);
            else rightHandIKWeight = 0;

            if (showDebugInfo)
            {
                Debug.DrawRay(bulletEmitter.position, bulletEmitter.forward * 20, Color.red);
                Debug.DrawRay(pivot.position, pivot.forward * 20);
            }
        }
    }

    private void LateUpdate()
    {
        pivot.position = rightShoulder.position;
        pivot.rotation = Quaternion.Lerp(pivot.rotation, Quaternion.LookRotation(target.position - pivot.position), Time.deltaTime * characterControlScript.aimingSpeed);

        if (characterControlScript.isHoldingAim != holdingStateUpdateFlag)
        {
            SetAimingState(characterControlScript.isHoldingAim);
            holdingStateUpdateFlag = characterControlScript.isHoldingAim;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (enableIK && !(characterControlScript.isClimbing && !characterControlScript.isHoldingAim))
        {
            if (characterControlScript.isHoldingAim)
            {
                animator.SetLookAtWeight(lookWeightAiming, bodyWeightAiming, headWeightAiming);
            }
            else
            {
                animator.SetLookAtWeight(lookWeightNormal, bodyWeightNormal, headWeightNormal);
            }
            animator.SetLookAtPosition(characterControlScript.currentAimPoint);

            if (!characterControlScript.isClimbing)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
    }
}
