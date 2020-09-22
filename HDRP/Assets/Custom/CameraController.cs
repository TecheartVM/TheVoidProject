using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    #region Camera Variables
    [SerializeField] private List<CinemachineVirtualCamera> cameras;
    [SerializeField] private int startCameraIndex = 0;
    [SerializeField] private int cameraStartPriority = 10;
    [SerializeField] private int cameraPriorityIncrement = 10;
    [SerializeField] private float maxFOV = 48;
    [SerializeField] private float fOVChangeSpeed = 0.07f;

    private CinemachineBasicMultiChannelPerlin runCamNoise;
    public CinemachineVirtualCamera currentVirtualCamera { get; private set; }
    public int currentVirtualCameraIndex { get; private set; }

    private float initialFOV = 40;

    private ThirdPersonControl controller;

    private bool useLeftCam = false;
    public bool useZoom = false;
    private bool camUpdated = false;
    #endregion

    void Awake()
    {
        controller = GetComponent<ThirdPersonControl>();

        SetActiveCamera(startCameraIndex);

        for(int i = 0; i < cameras.Count; i++)
        {
            cameras[i].Priority = cameraStartPriority;
            runCamNoise = cameras[i].GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if(runCamNoise != null) runCamNoise.m_AmplitudeGain = 0;
        }
        if(currentVirtualCamera != null) initialFOV = currentVirtualCamera.m_Lens.FieldOfView;
    }

    void Update()
    {
        HandleCameraSideChange();
        HandleZoom();
        if(!camUpdated) HandleCameraSwitch();
        HandleFieldOfView();
        HandleCameraShake();
    }

    private void HandleZoom()
    {
        if (useZoom != Input.GetButton("Aim") && !controller.isClimbing)
        {
            useZoom = Input.GetButton("Aim");
            camUpdated = false;
        }
    }

    private void HandleCameraSideChange()
    {
        if(Input.GetButtonDown("CamSwitch"))
        {
            useLeftCam = !useLeftCam;
            camUpdated = false;
        }
    }

    private void HandleCameraSwitch()
    {
        if(!useLeftCam)
        {
            if(!useZoom)
            {
                SetActiveCamera(0);
            }
            else
            {
                SetActiveCamera(1);
            }
        }
        else
        {
            if (!useZoom)
            {
                SetActiveCamera(2);
            }
            else
            {
                SetActiveCamera(3);
            }
        }
        camUpdated = true;
    }

    public void SetActiveCamera(int index)
    {
        if (index < 0 || index >= cameras.Count || cameras[index] == null)
        {
            Debug.LogError($"Camera with index {index} is not found");
            return;
        }
        else
        {
            if (currentVirtualCamera != null)
            {
                currentVirtualCamera.Priority = cameraStartPriority;
            }
            currentVirtualCamera = cameras[index];
            currentVirtualCameraIndex = index;
            currentVirtualCamera.Priority = cameraStartPriority + cameraPriorityIncrement;

            runCamNoise = currentVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    private void HandleCameraShake()
    {
        if (runCamNoise == null) return;
        if (Input.GetAxis("Sprint") > 0 && !controller.isClimbing) runCamNoise.m_AmplitudeGain = Mathf.Abs(controller.horizontalInputTotal * Input.GetAxis("Sprint"));
        else
        {
            runCamNoise.m_AmplitudeGain = 0;
        }
    }

    private void HandleFieldOfView()
    {
        if (!useZoom)
        {
            if (Input.GetButton("Sprint") && controller.horizontalInputTotal > 0 && !controller.isClimbing)
            {
                currentVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(currentVirtualCamera.m_Lens.FieldOfView, maxFOV, fOVChangeSpeed);
            }
            else
            {
                if(currentVirtualCamera != null) currentVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(currentVirtualCamera.m_Lens.FieldOfView, initialFOV, fOVChangeSpeed);
            }
        }
    }
}
