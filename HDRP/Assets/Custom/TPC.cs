using Cinemachine;
using System;
using System.Collections;
using UnityEngine;

public class TPC : MonoBehaviour
{
    #region External Objects

    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform characterSkin;

    private CharacterController controller;

    #endregion

    #region Movement Variables

    [Header("Basic Movements")]
    [SerializeField] private float movementSpeed = 5;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float rotationSpeed = 10;
    [SerializeField] private float jumpForce = 16;
    [SerializeField] private float gravityMultiplier = 90;

    #endregion

    #region Movement Additional Variables

    private float verticalMotion = 0;
    private Vector3 movement = Vector3.zero;
    private float inputX, inputY;
    private Vector3 lookForward, lookRight;
    private Vector3 characterForward, characterRight;

    public Vector3 cameraLookDirection { get; private set; } = Vector3.zero;

    #endregion

    #region Climbing Variables

    [Header("Climbing System")]
    [SerializeField] private string ledgeTag = "Climb";
    [SerializeField] private LayerMask ledgesLayer;
    [SerializeField] private LayerMask wallsAndLedgesLayer;
    [SerializeField] [Range(0, 2)] private float climbStep = 0.5f;
    [SerializeField] [Range(0, 2)] private float ledgeCheckForwardDistance = 0.5f;
    [SerializeField] [Range(0, 1)] private float characterToLedgeDistance = 0.333f;
    [SerializeField] [Range(0, 4)] private float characterYOffsetOnledge = 2f;
    [SerializeField] [Range(0, 4)] private float maxDistanceBetweenLedges = 2.4f;
    [SerializeField] [Range(0, 1)] private float climbCooldownTime = 0.2f;
    [SerializeField] private float sideCheckerStep = 0.05f;
    [SerializeField] private bool climbingSystemDebug = false;

    #endregion

    #region Climbing Additional Variables

    public bool isClimbing = false;
    private float climbCooldown = 0;
    private Vector3 currentLedgePoint = Vector3.zero;
    private Vector3 targetPoint;
    private Vector3 targetNormal;
    private float currentCheckOffset = 0;
    private ClimbingStates currentClimbingState = ClimbingStates.Idle;
    float time = 0;

    #endregion

    #region Shooting Variables

    [Header("Shooting")]
    public float aimingMaxDistance = 60;
    [SerializeField] private float aimingMaxAngle = 30;
    public float aimingSpeed = 15;
    [SerializeField] private LayerMask aimableLayer;

    public bool isAiming { get; private set; } = false;

    public bool cameraZoom { get; private set; } = false;

    public Vector3 currentAimPoint { get; private set; }

    [SerializeField] private int currentWeaponIndex = 0;

    private PlayerWeapons weapons;
    public WeaponBasic currentWeapon { get; private set; }

    [SerializeField] private bool debugAim = false;

    public event Action onWeaponSwitched;

    [SerializeField] private float aimingTime = 3;
    private float aimingTimer = 0;

    private bool holdingWeapon = false;

    #endregion

    [SerializeField] private float interactionFieldAngle = 30;

    [SerializeField]
    private CinemachineVirtualCamera playerCameraAim;

    [SerializeField]
    private int aimCameraPriorityBase = 9;
    [SerializeField]
    private int aimCameraPriorityIncrement = 10;

    private EntityPlayer playerEntity;

    #region Main 

    void Awake()
    {
        playerEntity = GetComponent<EntityPlayer>();

        controller = GetComponent<CharacterController>();

        weapons = GetComponent<PlayerWeapons>();
        if (weapons == null)
            Debug.LogError("Missing weapon list!");
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.weaponsList.Count)
            Debug.LogError("Invalid weapon index");
        currentWeapon = weapons.weaponsList[currentWeaponIndex].GetComponent<WeaponBasic>();
        int i = 1;
        while (i < weapons.weaponsList.Count)
        {
            WeaponBasic weapon = weapons.weaponsList[i].GetComponent<WeaponBasic>();
            if (weapon != null)
            {
                weapon.SetActive(false);
            }
            i++;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        inputX = Input.GetAxis("Horizontal");
        inputY = Input.GetAxis("Vertical");

        HandleCamera();
        if (!isClimbing)
        {
            ControllerMove();
            if (!isClimbing && climbCooldown <= 0 && !cameraZoom) ClimbStartCheck();

            HandleShooting();

            HandleWeaponSwitch();
        }
        else
        {
            Climb();
            verticalMotion = 0;
        }

        if (climbCooldown > 0) climbCooldown -= Time.deltaTime;
    }

    #endregion

    #region Movement

    void ControllerMove()
    {
        if (controller.isGrounded)
        {
            movement = (lookForward * inputY + lookRight * inputX).normalized * movementSpeed;
            movement = Vector3.ClampMagnitude(movement, movementSpeed);
            if (cameraZoom) movement *= 0.5f;
            else if (Input.GetButton("Sprint")) movement *= sprintMultiplier;

            RotateSkin(movement);
        }
        HandleJump();

        movement.y = verticalMotion;

        Debug.DrawRay(transform.position, movement * 10);
        controller.Move(movement * Time.deltaTime);
    }

    void HandleJump()
    {
        if (controller.isGrounded)
        {
            verticalMotion = 0;
            if (Input.GetButtonDown("Jump"))
            {
                verticalMotion = jumpForce;
            }
        }
        verticalMotion -= gravityMultiplier * Time.deltaTime;
    }

    void RotateSkin(Vector3 targetLookDirection)
    {
        characterSkin.LookAt(characterSkin.position + Vector3.Slerp(characterSkin.forward, targetLookDirection, Time.deltaTime * rotationSpeed));
        characterForward = characterSkin.forward;
        characterRight = characterSkin.right;
    }

    void HandleCamera()
    {
        lookForward = mainCamera.forward;
        lookForward.y = 0;
        Debug.DrawRay(mainCamera.position, lookForward, Color.blue);

        lookRight = mainCamera.right;
        lookRight.y = 0;
        Debug.DrawRay(mainCamera.position, lookRight, Color.red);

        cameraLookDirection = mainCamera.forward;
    }

    void ResetCamera()
    {
        if (playerCameraAim != null) playerCameraAim.m_Priority = aimCameraPriorityBase;
        cameraZoom = false;
    }

    #endregion

    #region Climbing

    void ClimbStartCheck()
    {
        RaycastHit hit;
        if (Physics.CapsuleCast(transform.position + Vector3.up * controller.radius, transform.position + Vector3.up * (controller.height - controller.radius), controller.radius, characterForward, out hit, ledgeCheckForwardDistance, ledgesLayer))
        {
            SwitchWeapon(0);
            DropTarget();
            AimingTick(false);
            ResetCamera();

            SetTargetPoint(hit.point);
            isClimbing = true;
        }

        if (climbingSystemDebug) Debug.DrawRay(hit.point, Vector3.up, Color.red, 1);
    }

    void MoveCharacterToPoint(Vector3 targetPoint, Vector3 targetNormal, float movementSpeed)
    {
        Vector3 finalTarget = targetPoint + targetNormal * characterToLedgeDistance - Vector3.up * characterYOffsetOnledge;

        transform.position = Vector3.Slerp(transform.position, finalTarget, Time.deltaTime * movementSpeed);

        RotateSkin(-targetNormal);

        //end of translation
        if (Vector3.Distance(transform.position, finalTarget) <= 0.04f && Vector3.Angle(characterForward, -targetNormal) < 1)
        {
            transform.position = finalTarget;
            characterSkin.rotation = Quaternion.LookRotation(-targetNormal, Vector3.up);

            currentLedgePoint = targetPoint;

            this.targetPoint = Vector3.zero;
            this.targetNormal = Vector3.zero;

            if (currentClimbingState != ClimbingStates.Turning) currentClimbingState = ClimbingStates.Idle;
        }
    }

    IEnumerator MoveCharacterToPoint(Vector3 targetPoint, Vector3 targetNormal, float movementSpeed, bool useCurve)
    {
        Vector3 finalTarget = targetPoint + targetNormal * characterToLedgeDistance - Vector3.up * characterYOffsetOnledge;

        Vector3 velocity = GetStartJumpVelocity(transform.position, finalTarget, 0.3f, -gravityMultiplier);

        DrawPath(currentLedgePoint, velocity, time, 30);

        Debug.DrawRay(finalTarget, Vector3.down, Color.blue, 10);

        while (Vector3.Distance(transform.position, finalTarget) > 0.2f)
        {
            Debug.DrawLine(transform.position, finalTarget, Color.red);

            RotateSkin(-targetNormal);

            if (!useCurve)
            {
                transform.position = Vector3.Slerp(transform.position, finalTarget, Time.deltaTime * movementSpeed);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, transform.position + velocity, velocity.magnitude * Time.deltaTime);
                velocity -= Vector3.up * gravityMultiplier * Time.deltaTime;
            }

            yield return null;
        }

        transform.position = finalTarget;
        characterSkin.rotation = Quaternion.LookRotation(-targetNormal, Vector3.up);

        currentLedgePoint = targetPoint;

        this.targetPoint = Vector3.zero;
        this.targetNormal = Vector3.zero;

        if (currentClimbingState != ClimbingStates.Turning) currentClimbingState = ClimbingStates.Idle;
    }

    Vector3 GetLedgeTopPoint(Vector3 ledgeHitPoint)
    {
        RaycastHit hit;
        if (Physics.Raycast(ledgeHitPoint + Vector3.up, Vector3.down, out hit, 2, ledgesLayer))
        {
            return hit.point - Vector3.up * 0.01f;
        }

        return ledgeHitPoint;
    }

    Vector3 GetLedgeFaceNormal(Vector3 ledgeTopPoint)
    {
        ledgeTopPoint.y -= 0.05f;
        Vector3 checkStartPoint = transform.position;
        checkStartPoint.y = ledgeTopPoint.y;
        RaycastHit hit;
        if (Physics.Raycast(checkStartPoint, ledgeTopPoint - checkStartPoint, out hit, characterToLedgeDistance * 2, ledgesLayer))
        {
            return hit.normal;
        }
        return -characterForward;
    }

    Vector3 GetLedgeExtention(Vector3 direction)
    {
        Vector3 point = Vector3.zero;

        Vector3 checkerPos = transform.position;
        checkerPos.y = currentLedgePoint.y - 0.05f;
        checkerPos += direction * currentCheckOffset;

        currentCheckOffset += sideCheckerStep;

        RaycastHit hit;
        if (IsLedgeEnd(checkerPos, direction, climbStep) || currentCheckOffset >= climbStep)
        {
            currentCheckOffset = 0;
            if (Physics.Raycast(checkerPos, characterForward, out hit, characterToLedgeDistance * 1.1f, ledgesLayer))
            {
                point = GetLedgeTopPoint(hit.point + characterForward * 0.05f);

                //debug
                if (climbingSystemDebug)
                    Debug.DrawRay(point, Vector3.up, Color.green, 0.5f);
            }
        }

        return point;
    }

    bool IsLedgeEnd(Vector3 checkStartPosition, Vector3 checkDirection, float checkDistance)
    {
        if (!Physics.Raycast(checkStartPosition + checkDirection * checkDistance, characterForward, characterToLedgeDistance * 2, ledgesLayer))
        {
            if (climbingSystemDebug) Debug.DrawRay(checkStartPosition, checkDirection * checkDistance, Color.red);
            return true;
        }

        return false;
    }

    Vector3 GetSideLedgePoint(Vector3 checkStartPosition, Vector3 checkDirection, float checkUpDistance, out Vector3 ledgeFaceNormal)
    {
        if (climbingSystemDebug)
        {
            Debug.DrawRay(checkStartPosition, Vector3.up * checkUpDistance, Color.cyan);
            Debug.DrawRay(checkStartPosition, checkDirection * maxDistanceBetweenLedges, Color.cyan);
        }

        Vector3 ledgePoint = currentLedgePoint;
        ledgeFaceNormal = -characterForward;

        RaycastHit hit;
        Vector3 point1 = checkStartPosition + checkDirection * climbStep + Vector3.up * controller.radius;
        Vector3 point2 = checkStartPosition + checkDirection * (maxDistanceBetweenLedges - 2 * controller.radius);
        if (Physics.CapsuleCast(point1, point2, controller.radius, Vector3.up, out hit, checkUpDistance, ledgesLayer))
        {
            if (climbingSystemDebug) Debug.DrawRay(hit.point, Vector3.down, Color.blue, 1);

            //try to find a way in the same direction
            bool goodPos = false;
            RaycastHit hit1;
            Vector3 checkPosition = hit.point - characterForward * (characterToLedgeDistance - 0.05f) + checkDirection * 0.05f;
            if (Physics.Raycast(checkPosition, characterForward, out hit1, characterToLedgeDistance * 1.1f))
            {
                if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.red, 1);
                if (Physics.Raycast(checkPosition + checkDirection * climbStep, characterForward, out hit1, characterToLedgeDistance * 1.1f))
                {
                    if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.yellow, 1);
                    if (hit1.transform.CompareTag(ledgeTag))
                    {
                        ledgePoint = GetLedgeTopPoint(hit1.point - checkDirection * climbStep * 0.5f + characterForward * 0.05f);
                        ledgeFaceNormal = hit1.normal;
                        goodPos = true;
                    }
                }

                if (!goodPos)
                {
                    if (Physics.Raycast(checkPosition - checkDirection * climbStep, characterForward, out hit1, characterToLedgeDistance * 1.1f))
                    {
                        if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.yellow, 1);
                        if (hit1.transform.CompareTag(ledgeTag))
                        {
                            ledgePoint = GetLedgeTopPoint(hit1.point + checkDirection * climbStep * 0.5f + characterForward * 0.05f);
                            ledgeFaceNormal = hit1.normal;
                            goodPos = true;
                        }
                    }
                }
            }

            //try to find another way
            if (!goodPos)
            {
                checkPosition = hit.point - checkDirection * (characterToLedgeDistance - 0.05f) + characterForward * 0.05f;
                if (Physics.Raycast(checkPosition, checkDirection, out hit1, characterToLedgeDistance * 1.1f))
                {
                    if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.red, 1);
                    if (Physics.Raycast(checkPosition - characterForward * climbStep, checkDirection, out hit1, characterToLedgeDistance * 1.1f))
                    {
                        if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.yellow, 1);
                        if (hit1.transform.CompareTag(ledgeTag))
                        {
                            ledgePoint = GetLedgeTopPoint(hit1.point + characterForward * climbStep * 0.5f + checkDirection * 0.05f);
                            ledgeFaceNormal = hit1.normal;
                            goodPos = true;
                        }
                    }

                    if (!goodPos)
                    {
                        if (Physics.Raycast(checkPosition + characterForward * climbStep, checkDirection, out hit1, characterToLedgeDistance * 1.1f))
                        {
                            if (climbingSystemDebug) Debug.DrawRay(hit1.point, Vector3.up, Color.yellow, 1);
                            if (hit1.transform.CompareTag(ledgeTag))
                            {
                                ledgePoint = GetLedgeTopPoint(hit1.point - characterForward * climbStep * 0.5f + checkDirection * 0.05f);
                                ledgeFaceNormal = hit1.normal;
                            }
                        }
                    }
                }
            }
        }

        return ledgePoint;
    }

    Vector3 GetBackwardsLedgePoint(Vector3 checkBottomPosition, float checkUpDistance, float checkBackwardsDistance, out Vector3 ledgeNormal)
    {
        ledgeNormal = -characterForward;

        RaycastHit hit;
        if (Physics.CapsuleCast(checkBottomPosition + Vector3.up * controller.radius, checkBottomPosition + Vector3.up * (checkUpDistance - controller.radius), controller.radius, -characterForward, out hit, checkBackwardsDistance, wallsAndLedgesLayer))
        {
            if (hit.transform.CompareTag(ledgeTag))
            {
                if (climbingSystemDebug) Debug.DrawRay(hit.point, hit.normal, Color.green, 1);
                ledgeNormal = hit.normal;
                return GetLedgeTopPoint(hit.point);
            }
        }

        return Vector3.zero;
    }

    Vector3 GetVerticalJumpPoint(Vector3 checkPos, Vector3 direction, float maxDistance)
    {
        RaycastHit hit;
        if (Physics.Raycast(checkPos, direction, out hit, maxDistance, ledgesLayer))
        {
            if (climbingSystemDebug) Debug.DrawLine(checkPos, hit.point, Color.green, 1);
            return hit.point;
        }
        return Vector3.zero;
    }

    Vector3 GetOuterCornerPoint(Vector3 checkStartPoint, Vector3 direction, float distanceToEdge, float startPointToLedgeDistance, out Vector3 ledgeNormal)
    {
        ledgeNormal = -characterForward;

        Vector3 checkingPos = checkStartPoint + direction * (distanceToEdge + 0.05f);

        if (climbingSystemDebug) Debug.DrawRay(checkingPos, characterForward, Color.red, 1);

        if (!Physics.Raycast(checkingPos, characterForward, startPointToLedgeDistance + distanceToEdge, wallsAndLedgesLayer))
        {
            checkingPos += characterForward * (startPointToLedgeDistance + distanceToEdge);

            if (climbingSystemDebug) Debug.DrawRay(checkingPos, -direction, Color.yellow, 1);

            if (Physics.Raycast(checkingPos, -direction, distanceToEdge, ledgesLayer))
            {
                checkingPos -= characterForward * (distanceToEdge - 0.05f);

                if (climbingSystemDebug) Debug.DrawRay(checkingPos, -direction, Color.yellow, 1);

                if (Physics.Raycast(checkingPos, -direction, distanceToEdge, ledgesLayer))
                {
                    RaycastHit hit;
                    checkingPos += characterForward * (distanceToEdge * 0.5f);
                    if (Physics.Raycast(checkingPos, -direction, out hit, distanceToEdge, ledgesLayer))
                    {
                        if (climbingSystemDebug) Debug.DrawRay(checkingPos, -direction, Color.green, 1);

                        ledgeNormal = hit.normal;
                        return GetLedgeTopPoint(hit.point);
                    }
                }
            }
        }

        return Vector3.zero;
    }

    Vector3 GetInnerCornerPoint(Vector3 checkStartPoint, Vector3 direction, float distanceToEdge, float startPointToLedgeDistance, out Vector3 ledgeNormal)
    {
        ledgeNormal = -characterForward;

        Vector3 checkingPos = checkStartPoint + characterForward * startPointToLedgeDistance * 0.5f;

        if (climbingSystemDebug) Debug.DrawRay(checkingPos, direction, Color.red);

        if (Physics.Raycast(checkingPos, direction, distanceToEdge + 0.05f, ledgesLayer))
        {
            checkingPos -= characterForward * distanceToEdge;

            if (climbingSystemDebug) Debug.DrawRay(checkingPos, direction, Color.yellow);

            if (Physics.Raycast(checkingPos, direction, distanceToEdge + 0.05f, ledgesLayer))
            {
                checkingPos += characterForward * distanceToEdge * 0.5f;

                if (climbingSystemDebug) Debug.DrawRay(checkingPos, direction, Color.yellow);

                RaycastHit hit;
                if (Physics.Raycast(checkingPos, direction, out hit, distanceToEdge + 0.05f, ledgesLayer))
                {
                    if (climbingSystemDebug) Debug.DrawRay(checkingPos, hit.normal, Color.green, 1);
                    ledgeNormal = hit.normal;
                    return GetLedgeTopPoint(hit.point);
                }
            }
        }

        return Vector3.zero;
    }

    void UpdateCurrentPoint()
    {
        Vector3 checkerPos = transform.position;
        checkerPos.y = currentLedgePoint.y - 0.05f;
        RaycastHit hit;
        if (Physics.Raycast(checkerPos, characterForward, out hit, characterToLedgeDistance * 2, ledgesLayer))
        {
            currentLedgePoint = GetLedgeTopPoint(hit.point + characterForward * 0.05f);
        }
    }

    void SetTargetPoint(Vector3 value)
    {
        Vector3 point = GetLedgeTopPoint(value);
        targetPoint = point;
        targetNormal = GetLedgeFaceNormal(point);
    }

    void Climb()
    {
        if (currentClimbingState != ClimbingStates.Jumping)
        {
            movement = Vector3.zero;

            //check upper ledge
            if (inputY > 0)
            {
                Vector3 upperLedgePoint = GetVerticalJumpPoint(currentLedgePoint, Vector3.up, maxDistanceBetweenLedges);
                if (upperLedgePoint != Vector3.zero)
                {
                    if (currentClimbingState != ClimbingStates.Jumping && Input.GetButtonDown("Jump"))
                    {
                        UpdateCurrentPoint();

                        currentClimbingState = ClimbingStates.Jumping;

                        SetTargetPoint(upperLedgePoint);
                    }
                }
            }
            //check backwards ledge
            else if (inputY < 0)
            {
                if (currentClimbingState != ClimbingStates.Jumping && Input.GetButtonDown("Jump"))
                {
                    Vector3 nextLedgeNormal;
                    Vector3 nextLedgePoint = GetBackwardsLedgePoint(transform.position, maxDistanceBetweenLedges, maxDistanceBetweenLedges, out nextLedgeNormal);
                    if (nextLedgePoint != Vector3.zero)
                    {
                        currentClimbingState = ClimbingStates.Jumping;
                        StartCoroutine(MoveCharacterToPoint(nextLedgePoint, nextLedgeNormal, jumpForce, true));
                    }
                }
            }
            else
            {
                float horizontalAxisRaw = Input.GetAxisRaw("Horizontal");
                if (horizontalAxisRaw != 0)
                {
                    //move along current ledge
                    Vector3 checkerPos = transform.position;
                    checkerPos.y = currentLedgePoint.y - 0.05f;

                    Vector3 nextLedgeNormal;
                    Vector3 nextLedgePoint;

                    if (IsLedgeEnd(checkerPos, characterRight * horizontalAxisRaw, climbStep))
                    {
                        UpdateCurrentPoint();

                        checkerPos = transform.position;
                        checkerPos.y = currentLedgePoint.y - 0.05f;

                        nextLedgePoint = GetOuterCornerPoint(checkerPos, characterRight * horizontalAxisRaw, climbStep, characterToLedgeDistance, out nextLedgeNormal);

                        if (currentClimbingState != ClimbingStates.Turning && nextLedgePoint != Vector3.zero)
                        {
                            currentClimbingState = ClimbingStates.Turning;
                            targetPoint = nextLedgePoint;
                            targetNormal = nextLedgeNormal;
                        }
                        else
                        {
                            if (Input.GetButtonDown("Jump"))
                            {
                                checkerPos = transform.position + characterForward * characterToLedgeDistance + characterRight * horizontalAxisRaw * climbStep;
                                nextLedgePoint = GetSideLedgePoint(checkerPos, characterRight * horizontalAxisRaw, maxDistanceBetweenLedges, out nextLedgeNormal);

                                Debug.DrawRay(nextLedgePoint, -characterForward, Color.cyan, 10);

                                currentClimbingState = ClimbingStates.Jumping;
                                StartCoroutine(MoveCharacterToPoint(nextLedgePoint, nextLedgeNormal, jumpForce, true));
                            }
                        }
                    }
                    else
                    {
                        nextLedgePoint = GetInnerCornerPoint(checkerPos, characterRight * horizontalAxisRaw, climbStep, characterToLedgeDistance, out nextLedgeNormal);
                        if (currentClimbingState != ClimbingStates.Turning && nextLedgePoint != Vector3.zero)
                        {
                            currentClimbingState = ClimbingStates.Turning;
                            targetPoint = nextLedgePoint;
                            targetNormal = nextLedgeNormal;
                        }
                        else
                        {
                            nextLedgePoint = GetLedgeExtention(characterRight * horizontalAxisRaw);
                            if (nextLedgePoint != Vector3.zero)
                            {
                                currentClimbingState = ClimbingStates.MovingAlongLedge;
                                currentLedgePoint = nextLedgePoint;
                            }
                            else
                            {
                                nextLedgePoint = currentLedgePoint;
                            }
                            MoveCharacterToPoint(nextLedgePoint, GetLedgeFaceNormal(nextLedgePoint), movementSpeed);
                        }
                    }
                }
                else
                {
                    currentClimbingState = ClimbingStates.Idle;
                    UpdateCurrentPoint();
                }
            }
        }

        //show current ledge point in debug
        if (climbingSystemDebug) Debug.DrawRay(currentLedgePoint, Vector3.up, Color.magenta);

        //jump to lower ledge / jump off a ledge
        if (currentClimbingState != ClimbingStates.Jumping && Input.GetButtonDown("Crouch"))
        {
            UpdateCurrentPoint();
            Vector3 nextLedgePoint = GetVerticalJumpPoint(currentLedgePoint - Vector3.up * 0.5f, Vector3.down, maxDistanceBetweenLedges);
            if (nextLedgePoint != Vector3.zero)
            {
                currentClimbingState = ClimbingStates.Jumping;

                SetTargetPoint(nextLedgePoint);
            }
            else
            {
                isClimbing = false;
                currentLedgePoint = Vector3.zero;
                climbCooldown = climbCooldownTime;
                currentClimbingState = ClimbingStates.Idle;
            }
        }

        if (targetPoint != Vector3.zero)
        {
            MoveCharacterToPoint(targetPoint, targetNormal, jumpForce);
        }
    }

    #endregion

    #region Math

    public Vector3 GetVectorIgnoreY(Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    Vector3 GetStartJumpVelocity(Vector3 startPoint, Vector3 endPoint, float topPointHeightOffset, float gravity)
    {
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = endPoint - startPoint;
        displacementXZ.y = 0;

        float height = Mathf.Max(startPoint.y, endPoint.y) - Mathf.Min(startPoint.y, endPoint.y) + topPointHeightOffset;
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        float time = (Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity));
        this.time = time;
        Vector3 velocityXZ = displacementXZ / time;

        return velocityXZ + velocityY;
    }

    void DrawPath(Vector3 startPos, Vector3 initialVelocity, float timeToTarget, int resolution)
    {
        Vector3 previousDrawPoint = startPos;

        for (int i = 1; i <= resolution; i++)
        {
            float simulationTime = i / (float)resolution * timeToTarget;
            Vector3 displacement = initialVelocity * simulationTime - Vector3.up * gravityMultiplier * simulationTime * simulationTime / 2;
            Vector3 drawPoint = startPos + displacement;
            Debug.DrawLine(previousDrawPoint, drawPoint, Color.green, 10);
            previousDrawPoint = drawPoint;
        }
    }

    #endregion

    #region Shooting

    private void HandleShooting()
    {
        if (holdingWeapon)
        {
            HandleAimingCamera();

            AimingTick(Input.GetButton("Aim") || debugAim);

            if (Input.GetButtonDown("Fire") || /*currentWeapon.IsAutomatic &&*/ Input.GetButton("Fire"))
            {
                TakeAim();
                Shoot();
            }

            if (Input.GetButtonDown("Reload Weapon"))
            {
                Reload();
                DropTarget();
            }
        }
        else
        {
            if (Input.GetButtonDown("Aim"))
            {
                if (weapons.weaponsList.Count > 1)
                {
                    SwitchWeapon(1);
                    if (!debugAim && !isClimbing)
                    {
                        if (playerCameraAim != null) playerCameraAim.m_Priority = aimCameraPriorityBase + aimCameraPriorityIncrement;
                        cameraZoom = true;
                    }
                }
            }
            else if (Input.GetButton("Fire"))
            {
                if (weapons.weaponsList.Count > 1)
                {
                    SwitchWeapon(1);
                }
            }
        }
    }

    void HandleAimingCamera()
    {
        if (Input.GetButtonDown("Aim"))
        {
            if (!debugAim && !isClimbing)
            {
                if (playerCameraAim != null) playerCameraAim.m_Priority = aimCameraPriorityBase + aimCameraPriorityIncrement;
                cameraZoom = true;
            }
        }
        if (Input.GetButtonUp("Aim"))
        {
            ResetCamera();
        }
    }

    void Reload()
    {
        currentWeapon.Reload();
    }

    void Shoot()
    {
        //currentWeapon.Shoot(currentAimPoint, aimableLayer);
    }

    void TakeAim()
    {
        aimingTimer = aimingTime;
        if (!isAiming) isAiming = true;
        HoldAim();
    }

    void HoldAim()
    {
        currentAimPoint = GetAimPoint();
        if (Mathf.Abs(Vector3.Angle(GetVectorIgnoreY(currentAimPoint - transform.position), characterForward)) > aimingMaxAngle) RotateSkin(lookForward);
    }

    void DropTarget()
    {
        aimingTimer = 0;
        isAiming = false;
    }

    void AimingTick(bool aimingInput)
    {
        if (aimingInput) TakeAim();

        if (aimingTimer > 0)
        {
            HoldAim();
            if (!aimingInput)
            {
                aimingTimer -= Time.deltaTime;
            }
        }
        else if (isAiming)
        {
            isAiming = false;
        }
    }

    private Vector3 GetAimPoint(Vector3 raycastOrigin, Vector3 raycastDirection, float distance, int layerMask)
    {
        RaycastHit hit;
        if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, distance, layerMask))
        {
            return hit.point;
        }
        return raycastOrigin + raycastDirection * distance;
    }

    public Vector3 GetAimPoint()
    {
        return GetAimPoint(mainCamera.position, mainCamera.forward, aimingMaxDistance, aimableLayer);
    }

    void HandleWeaponSwitch()
    {
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll != 0)
        {
            int nextWeaponIndex;
            if (mouseScroll > 0)
            {
                nextWeaponIndex = currentWeaponIndex >= weapons.weaponsList.Count - 1 ? 0 : currentWeaponIndex + 1;
            }
            else
            {
                nextWeaponIndex = currentWeaponIndex <= 0 ? weapons.weaponsList.Count - 1 : currentWeaponIndex - 1;
            }
            SwitchWeapon(nextWeaponIndex);
        }
    }

    public void SwitchWeapon(int targetIndex)
    {
        if (targetIndex == currentWeaponIndex) return;
        currentWeaponIndex = targetIndex;

        if (currentWeapon != null && currentWeapon.GetComponent<WeaponBasic>() != null)
            currentWeapon.SetActive(false);

        GameObject nextWeaponObj = weapons.weaponsList[targetIndex];
        if (nextWeaponObj != null)
        {
            currentWeapon = nextWeaponObj.GetComponent<WeaponBasic>();
            if (currentWeapon != null)
            {
                holdingWeapon = true;
                currentWeapon.SetActive(true);
            }
            else
            {
                holdingWeapon = false;
            }
        }
        else
        {
            holdingWeapon = false;
            currentWeapon = null;
        }

        if (onWeaponSwitched != null) onWeaponSwitched();
    }

    #endregion

    public float GetInteractionFieldAngle()
    {
        return interactionFieldAngle;
    }

    public Transform GetCameraTransform()
    {
        return mainCamera;
    }

    public void Kill()
    {

    }
}

public enum Climbing_States
{
    Idle,
    MovingAlongLedge,
    Turning,
    Jumping
}