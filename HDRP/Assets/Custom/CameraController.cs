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

    [SerializeField] private List<CinemachineVirtualCamera> additionalVirtualCameras;
    [SerializeField] private int cameraStartPriority = 10;
    [SerializeField] private int cameraPriorityIncrement = 10;

    private CinemachineBasicMultiChannelPerlin runCamNoise;

    public CinemachineVirtualCamera currentVirtualCamera { get; private set; }
    public int currentVirtualCameraIndex { get; private set; }

    private float initialFOV;

    private ThirdPersonControl controller;

    private bool useLeftCam = false;
    public bool useZoom = false;
    private bool useRunEffects = false;

    private Cinemachine3rdPersonFollow sideCamera;
    [SerializeField] private CinemachineVirtualCamera virtualCameraMain;
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

    void Awake()
    {
        instance = this;

        controller = GetComponent<ThirdPersonControl>();
        sideCamera = virtualCameraMain.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        runCamNoise = virtualCameraMain.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        initialFOV = virtualCameraMain.m_Lens.FieldOfView;
        runCamNoise.m_AmplitudeGain = 0;
    }

    void Update()
    {
        HandleCameraRotation();
        HandlePublicVariables();
        HandleCameraZoom();
        HandleCameraSideSwitch();
        HandleFieldOfView();
        HandleCameraShake();
        HandleRecoil();
    }

    private void HandleCameraRotation()
    {
        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);
        cameraLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);
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
        //sideCamera.CameraDistance = Mathf.Lerp(sideCamera.CameraDistance, useZoom ? 0.9f : 1.3f, Time.deltaTime * fOVChangeSpeed);
    }

    private void HandleCameraSideSwitch()
    {
        if(Input.GetButtonDown("CamSwitch"))
        {
            useLeftCam = !useLeftCam;
        }

        sideCamera.CameraSide = Mathf.Lerp(sideCamera.CameraSide, useLeftCam ? 0 : 1, Time.deltaTime * sideSwitchSpeed);
    }

    private void HandleCameraShake()
    {
        if (runCamNoise == null) return;
        if (controller.isSprinting) runCamNoise.m_AmplitudeGain = Mathf.Abs(controller.horizontalInputTotal * Input.GetAxis("Sprint"));
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
            yAxis.Value -= (recoilStrength * recoilMultiplierVertical * Time.deltaTime) / recoilDuration;
            xAxis.Value -= (recoilStrength * recoilMultiplierHorizontal * Time.deltaTime) / recoilDuration * Random.Range(-1f, 1f);
            recoilTime -= Time.deltaTime;
        }
    }
}
