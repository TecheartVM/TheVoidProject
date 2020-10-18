using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    #region Camera Properties
    [SerializeField] private float minFOV = 48;
    [SerializeField] private float runFOV = 70;
    [SerializeField] private float fOVChangeSpeed = 0.07f;
    [SerializeField] private float sideSwitchSpeed = 2;

    [SerializeField] private float recoilDuration = 0.1f;
    [SerializeField] private float recoilMultiplierVertical = 0.1f;
    [SerializeField] private float recoilMultiplierHorizontal = 0.1f;

    [SerializeField] private float crouchCameraDownOffset = 1f;

    [SerializeField] private CinemachineVirtualCamera virtualCameraMain;

    [SerializeField] private List<CinemachineVirtualCameraBase> virtualCameras;
    [SerializeField] private int cameraStartPriority = 10;
    [SerializeField] private int cameraPriorityIncrement = 10;

    [SerializeField] private int climbAimCamRotationSpeed = 4;

    [SerializeField] public CinemachineVirtualCameraBase currentVirtualCamera { get; private set; }
    public int currentVirtualCameraIndex { get; private set; }

    private CinemachineBrain cinemachineBrain;

    private float initialFOV;
    private CinemachineBasicMultiChannelPerlin runCamNoise;
    private ThirdPersonControl controller;

    private bool useLeftCam = false;
    public bool useZoom = false;
    private bool useRunEffects = false;

    private Cinemachine3rdPersonFollow sideCamera;
    public AxisState xAxis;
    public AxisState yAxis;
    public Transform cameraLookAt;
    #endregion

    #region Public Variables
    public Transform mainCamera;

    public Vector3 lookForward { get; private set; }
    public Vector3 lookRight { get; private set; }
    #endregion

    private float recoilTime = 0;
    private float recoilStrength = 0;

    private float totalXAxisBias = 0;
    private Vector3 prevForward;

    private float crouchCameraInitialYOffset = 0;

    void Awake()
    {
        instance = this;

        controller = GetComponent<ThirdPersonControl>();
        sideCamera = virtualCameraMain.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        runCamNoise = virtualCameraMain.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();

        initialFOV = virtualCameraMain.m_Lens.FieldOfView;
        runCamNoise.m_AmplitudeGain = 0;

        currentVirtualCamera = virtualCameraMain;
        currentVirtualCameraIndex = 0;
        virtualCameras.Insert(0, virtualCameraMain);
        SwitchCamera(0);

        prevForward = Maths.GetVectorIgnoreY(transform.forward);

        crouchCameraInitialYOffset = sideCamera.ShoulderOffset.y;
    }

    void Update()
    {
        HandleCameraSwitch();
        HandleCameraRotation();
        HandlePublicVariables();
        if(currentVirtualCameraIndex == 0) HandleCameraZoom();
        HandleCrouchCamera();
        HandleCameraSideSwitch();
        HandleFieldOfView();
        HandleCameraShake();
        HandleRecoil();
    }

    private void UpdateClimbingAimCamera()
    {
        float curBias = ((CinemachineFreeLook)virtualCameras[2]).m_Heading.m_Bias;
        if (curBias >= 360)
        {
            ((CinemachineFreeLook)virtualCameras[2]).m_Heading.m_Bias -= 360;
            totalXAxisBias -= 360;
        }
        else if (curBias <= -360)
        {
            ((CinemachineFreeLook)virtualCameras[2]).m_Heading.m_Bias += 360;
            totalXAxisBias += 360;
        }

        float angle = Vector3.SignedAngle(prevForward, Maths.GetVectorIgnoreY(controller.characterForward), Vector3.up);
        totalXAxisBias += angle;
        ((CinemachineFreeLook)virtualCameras[2]).m_Heading.m_Bias = Mathf.Lerp(((CinemachineFreeLook)virtualCameras[2]).m_Heading.m_Bias, totalXAxisBias, Time.deltaTime * climbAimCamRotationSpeed);

        prevForward = Maths.GetVectorIgnoreY(controller.characterForward);
    }

    public void SwitchCamera(int index)
    {
        if (currentVirtualCameraIndex == index) return;
        if (index >= virtualCameras.Count || index < 0)
        {
            Debug.LogError("Camera switch error: no such camera with index '" + index + "'!");
            return;
        }
        if (currentVirtualCamera != null)
        {
            currentVirtualCamera.m_Priority = cameraStartPriority;
        }

        currentVirtualCameraIndex = index;
        currentVirtualCamera = virtualCameras[currentVirtualCameraIndex];
        currentVirtualCamera.m_Priority += cameraPriorityIncrement;
    }

    private void HandleCameraSwitch()
    {
        if (controller.isClimbing)
        {
            if (!controller.isHoldingAim && !controller.isHoldingWeapon)
            {
                SwitchCamera(1);
            }
            else
            {
                SwitchCamera(2);
            }
        }
        else
        {
            if (controller.isGrounded)
            {
                if (currentVirtualCameraIndex != 0)
                {
                    UpdateMainCameraPos();
                    controller.DropTarget();
                }
                SwitchCamera(0);
            }
        }
    }

    private void HandleCameraRotation()
    {
        if (currentVirtualCameraIndex == 0)
        {
            xAxis.Update(Time.deltaTime);
            yAxis.Update(Time.deltaTime);
            cameraLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);
        }
        if (currentVirtualCameraIndex == 2)
        {
            UpdateClimbingAimCamera();
        }
    }

    void HandlePublicVariables()
    {
        lookForward = Maths.GetVectorIgnoreY(mainCamera.forward);
        lookRight = Maths.GetVectorIgnoreY(mainCamera.right);
    }

    private void HandleCameraZoom()
    {
        if (controller.isSprinting) return;
        virtualCameraMain.m_Lens.FieldOfView = Mathf.Lerp(virtualCameraMain.m_Lens.FieldOfView, controller.isAiming ? minFOV : initialFOV, Time.deltaTime * fOVChangeSpeed);
    }

    private void HandleCameraSideSwitch()
    {
        if(Input.GetButtonDown("CamSwitch"))
        {
            useLeftCam = !useLeftCam;
        }

        sideCamera.CameraSide = Mathf.Lerp(sideCamera.CameraSide, useLeftCam ? 0 : 1, Time.deltaTime * sideSwitchSpeed);
    }

    private void HandleCrouchCamera()
    {
        if(controller.isCrouching || controller.isSliding || controller.isRolling)
        {
            sideCamera.ShoulderOffset.y = Mathf.Lerp(sideCamera.ShoulderOffset.y, -crouchCameraDownOffset, Time.deltaTime * sideSwitchSpeed);
        }
        else
        {
            sideCamera.ShoulderOffset.y = Mathf.Lerp(sideCamera.ShoulderOffset.y, crouchCameraInitialYOffset, Time.deltaTime * sideSwitchSpeed);
        }
    }

    private void HandleCameraShake()
    {
        if (runCamNoise == null) return;
        if (controller.isSprinting) runCamNoise.m_AmplitudeGain = Mathf.Abs(controller.horizontalInputTotal * controller.sprintInput);
        else
        {
            runCamNoise.m_AmplitudeGain = 0;
        }
    }

    private void HandleFieldOfView()
    {
        if (controller.isAiming) return;
        if (controller.isSprinting)
        {
            virtualCameraMain.m_Lens.FieldOfView = Mathf.Lerp(virtualCameraMain.m_Lens.FieldOfView, runFOV, 0.07f);
        }
        else
        {
            if (virtualCameraMain != null) virtualCameraMain.m_Lens.FieldOfView = Mathf.Lerp(virtualCameraMain.m_Lens.FieldOfView, initialFOV, 0.07f);
        }
    }

    public void DoRecoil(float strength)
    {
        recoilStrength = strength;
        recoilTime = recoilDuration;
    }

    private void HandleRecoil()
    {
        if(recoilTime > 0)
        {
            if(currentVirtualCameraIndex == 0)
            {
                yAxis.Value -= (recoilStrength * recoilMultiplierVertical * Time.deltaTime) / recoilDuration;
                xAxis.Value -= (recoilStrength * recoilMultiplierHorizontal * Time.deltaTime) / recoilDuration * Random.Range(-1f, 1f);
            }
            else
            {
                ((CinemachineFreeLook)virtualCameras[2]).m_XAxis.Value -= (recoilStrength * recoilMultiplierHorizontal * Time.deltaTime) / recoilDuration * Random.Range(-1f, 1f);
            }
            
            recoilTime -= Time.deltaTime;
        }
    }

    private void UpdateMainCameraPos()
    {
        float turnAngle = Vector2.SignedAngle(new Vector2(cameraLookAt.forward.x, cameraLookAt.forward.z), new Vector2(mainCamera.forward.x, mainCamera.forward.z));
        xAxis.Value -= turnAngle;

        Vector3 camTargetPos = virtualCameraMain.transform.position;
        camTargetPos.y = mainCamera.position.y;
        Vector3 lookAtPos = virtualCameraMain.LookAt.position;
        float distanceToTarget = (lookAtPos - camTargetPos).magnitude;
        float heightDifference = camTargetPos.y - virtualCameraMain.transform.position.y;

        turnAngle = Mathf.Rad2Deg * Mathf.Atan(heightDifference / distanceToTarget);
        yAxis.Value += turnAngle / 2;
    }
}
