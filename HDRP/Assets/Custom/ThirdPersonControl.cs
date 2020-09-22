using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonControl : MonoBehaviour
{
    #region External Objects
    [SerializeField] private Transform characterSkin;

    private CharacterController characterController;
    private CameraController cameraController;

    private EntityPlayer playerEntity;
    #endregion

    #region Movement System Properties
    [Header("Basic Movements")]
    [SerializeField] private float movementSpeed = 5;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float rotationSpeed = 10;
    [SerializeField] private float jumpForce = 14;
    [SerializeField] private float gravityMultiplier = 90;
    [SerializeField] private float jumpCooldownTime = 0.5f;
    #endregion

    #region Movement System Variables
    private float verticalMotion = 0;
    private Vector3 movement = Vector3.zero;
    private Vector3 characterForward, characterRight;
    private float jumpCooldown = 0;
    #endregion

    #region Climbing System Properties
    [Header("Climbing System")]
    [SerializeField] private string ledgeTag = "Climb";
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private LayerMask ledgesLayer;
    [SerializeField] private LayerMask wallsAndLedgesLayer;
    [SerializeField] private float climbingSpeed = 7;
    [SerializeField] private float climbJumpForce = 7;
    [SerializeField] [Range(0, 2)] private float climbStep = 0.2f;
    [SerializeField] [Range(0, 2)] private float ledgeCheckForwardDistance = 0.5f;
    [SerializeField] [Range(0, 1)] private float characterToLedgeDistance = 0.36f;
    [SerializeField] [Range(0, 4)] private float characterYOffsetOnledge = 1.98f;
    [SerializeField] [Range(0, 4)] private float maxDistanceBetweenLedges = 2.4f;
    [SerializeField] [Range(0, 1)] private float climbCooldownTime = 0.2f;
    [SerializeField] [Range(1,45)] private float ledgeMaxAngle = 30;
    #endregion

    #region Climbing System Variables
    private float climbCooldown = 0;
    private float angledLedgeCheckOffset = 0;
    private LedgePoint currentLedgePoint = new LedgePoint();
    public ClimbingStates currentClimbingState = ClimbingStates.Idle;
    float jumpTime = 0;
    #endregion

    #region Shooting System Properties
    [Header("Shooting System")]
    public float aimingMaxDistance = 60;
    [SerializeField] private float aimingMaxAngle = 30;
    public float aimingSpeed = 15;
    [SerializeField] private float aimingTime = 3;
    [SerializeField] private LayerMask aimableLayer;

    public bool debugAimHolding = false;
    #endregion

    #region Shooting System Variables
    public Vector3 currentAimPoint { get; private set; }
    private PlayerWeapons weapons;
    [SerializeField] private int currentWeaponIndex = 0;
    public IWeapon currentWeapon { get; private set; }

    private float aimingTimer = 0;
    #endregion

    #region Player States
    public bool isSprinting { get; private set; } = false;
    public bool isClimbing { get; private set; } = false;
    public bool isAiming { get; private set; } = false;
    public bool isHoldingAim { get; private set; } = false;
    public bool isHoldingWeapon { get; private set; } = false;

    public float horizontalInputTotal { get; private set; } = 0;
    #endregion

    #region Player Events
    public event Action onWeaponSwitched;
    #endregion

    [SerializeField] private float interactionFieldAngle = 30;

    #region Main 

    void Awake()
    {
        playerEntity = GetComponent<EntityPlayer>();
        characterController = GetComponent<CharacterController>();
        cameraController = GetComponent<CameraController>();
        weapons = GetComponent<PlayerWeapons>();

        angledLedgeCheckOffset = climbStep / (1 / Mathf.Tan(ledgeMaxAngle));

        if (weapons == null) 
            Debug.LogError("Missing weapon list!");
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.weaponsList.Count)
            Debug.LogError("Invalid weapon index");
        currentWeapon = weapons.weaponsList[currentWeaponIndex].GetComponent<IWeapon>();
        int i = 1;
        while(i < weapons.weaponsList.Count)
        {
            IWeapon weapon = weapons.weaponsList[i].GetComponent<IWeapon>();
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
        isSprinting = Input.GetButton("Sprint") && horizontalInputTotal > 0 && !isAiming && !isClimbing;

        if (!isClimbing)
        {
            ControllerMove();
            if (!characterController.isGrounded && climbCooldown <= 0 && !isAiming) ClimbStartCheck();

            HandleShooting();
            HandleWeaponSwitch();
        }
        else
        {
            HandleClimbing();
            if (jumpCooldown > 0)
            {
                jumpCooldown -= Time.deltaTime;
            }
            verticalMotion = 0;
        }

        WriteHorizontalMotion();

        if (climbCooldown > 0) climbCooldown -= Time.deltaTime;

        if (debugAimHolding) isHoldingAim = true;
    }

    #endregion

    #region Movement
    void ControllerMove()
    {
        if (characterController.isGrounded)
        {
            movement = (cameraController.lookForward * Input.GetAxis("Vertical") + cameraController.lookRight * Input.GetAxis("Horizontal")).normalized * movementSpeed;
            movement = Vector3.ClampMagnitude(movement, movementSpeed);

            if (isAiming)
            {
                movement *= 0.5f;
            }
            else
            {
                if (isSprinting)
                {
                    movement *= sprintMultiplier;
                    //if (isHoldingAim) DropTarget();
                }
            }

            RotateSkin(isHoldingAim && !isSprinting && isHoldingWeapon ? cameraController.lookForward : movement);
        }
        HandleJump();

        movement.y = verticalMotion;

        characterController.Move(movement * Time.deltaTime);
    }

    void HandleJump()
    {
        if (characterController.isGrounded)
        {
            verticalMotion = 0;
            if (jumpCooldown <= 0)
            {
                if (Input.GetButtonDown("Jump"))
                {
                    verticalMotion = jumpForce;
                    jumpCooldown = jumpCooldownTime;
                }
            }
            else
            {
                jumpCooldown -= Time.deltaTime;
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

    void SetSkinRotation(Vector3 targetLookDirection)
    {
        characterSkin.LookAt(characterSkin.position + targetLookDirection);
        characterForward = characterSkin.forward;
        characterRight = characterSkin.right;
    }

    private void WriteHorizontalMotion()
    {
        horizontalInputTotal = Mathf.Clamp(Math.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal")), 0, 1);
    }
    #endregion

    #region Climbing
    private void HandleClimbing()
    {
        //Debug.DrawRay(currentLedgePoint.topPoint, characterSkin.up, Color.magenta);

        if (currentClimbingState != ClimbingStates.Jumping)
        {
            if (Input.GetAxis("Horizontal") != 0)
            {
                Vector3 playerPos = transform.position;
                playerPos.y = currentLedgePoint.topPoint.y;
                Vector3 movementDirection = characterRight * Input.GetAxisRaw("Horizontal");

                //check for wall on the way
                RaycastHit hit;
                if (Physics.Raycast(playerPos, movementDirection, out hit, characterToLedgeDistance, wallsAndLedgesLayer))
                {
                    UpdateCurrentLedgePoint();

                    LedgePoint sidePoint = GetInnerCornerPoint(playerPos, movementDirection, currentLedgePoint.faceNormal, Input.GetAxisRaw("Horizontal"), characterToLedgeDistance);
                    if (sidePoint.topPoint != Vector3.zero)
                    {
                        //Debug.DrawRay(sidePoint.topPoint, sidePoint.faceNormal, Color.green, 3);
                        StartCoroutine(MoveCharacterToPoint(sidePoint.topPoint, sidePoint.faceNormal, climbingSpeed, false));
                    }
                }
                //no wall on the way
                else
                {
                    //trying to move along ledge
                    LedgePoint sidePoint = GetLedgeSidePoint(playerPos, movementDirection, climbStep, currentLedgePoint.faceNormal, characterToLedgeDistance);

                    if (sidePoint.topPoint != Vector3.zero)
                    {
                        MoveCharacterAlongLedge(sidePoint.topPoint, sidePoint.faceNormal, climbingSpeed * Mathf.Abs(Input.GetAxis("Horizontal")));
                    }
                    //can't move along the ledge
                    else
                    {
                        UpdateCurrentLedgePoint();

                        //check for outer corner
                        sidePoint = GetOuterCornerPoint(currentLedgePoint.topPoint, movementDirection, characterToLedgeDistance);
                        if (sidePoint.topPoint != Vector3.zero)
                        {
                            //Debug.DrawRay(sidePoint.topPoint, sidePoint.faceNormal, Color.green, 3);
                            StartCoroutine(MoveCharacterToPoint(sidePoint.topPoint, sidePoint.faceNormal, climbingSpeed, false));
                        }
                        else
                        {
                            sidePoint = GetSideLedge(currentLedgePoint.topPoint, movementDirection, (climbJumpForce / gravityMultiplier) * 10, characterController.height, maxDistanceBetweenLedges);
                            if (sidePoint.topPoint != Vector3.zero)
                            {
                                //Debug.DrawRay(sidePoint.topPoint, sidePoint.faceNormal, Color.green);

                                if (Input.GetButtonDown("Jump") && jumpCooldown <= 0)
                                {
                                    StartCoroutine(MoveCharacterToPoint(sidePoint.topPoint, sidePoint.faceNormal, climbJumpForce, true));
                                    jumpCooldown = jumpCooldownTime;
                                }
                            }
                        }
                    }
                }
            }
            
            if (Input.GetAxis("Vertical") > 0)
            {
                UpdateCurrentLedgePoint();

                LedgePoint upperLedge = GetUpperLedge(currentLedgePoint.topPoint, maxDistanceBetweenLedges);
                if(upperLedge.topPoint != Vector3.zero)
                {
                    //Debug.DrawRay(upperLedge.topPoint, upperLedge.faceNormal, Color.green);

                    if (Input.GetButtonDown("Jump") && jumpCooldown <= 0)
                    { 
                        StartCoroutine(MoveCharacterToPoint(upperLedge.topPoint, upperLedge.faceNormal, climbJumpForce, true));
                        jumpCooldown = jumpCooldownTime;
                    }
                }
                else
                {
                    Vector3 plateauPos = GetPlateauPosition(currentLedgePoint.topPoint, characterForward, characterController.radius + movementSpeed * 0.02f);
                    if (plateauPos != Vector3.zero && Input.GetButtonDown("Vertical"))
                    {
                        transform.position = plateauPos;
                        StopClimbing();
                    }
                }
             }
            else if(Input.GetAxis("Vertical") < 0)
            {
                UpdateCurrentLedgePoint();

                LedgePoint backwardLedge = GetBackwardLedge((climbJumpForce / gravityMultiplier) * 10, 0, maxDistanceBetweenLedges);
                if (backwardLedge.topPoint != Vector3.zero)
                {
                    //Debug.DrawRay(backwardLedge.topPoint, backwardLedge.faceNormal, Color.green);

                    if (Input.GetButtonDown("Jump") && jumpCooldown <= 0)
                    {
                        StartCoroutine(MoveCharacterToPoint(backwardLedge.topPoint, backwardLedge.faceNormal, climbJumpForce, true));
                        jumpCooldown = jumpCooldownTime;
                    }
                }
            }
            
        }

        if (Input.GetButtonDown("Crouch"))
        {
            StopClimbing();
        }
    }

    void ClimbStartCheck()
    {
        RaycastHit hit;
        if (Physics.CapsuleCast(transform.position + Vector3.up * characterController.radius * 2, transform.position + Vector3.up * characterController.height, characterController.radius, characterForward, out hit, ledgeCheckForwardDistance, ledgesLayer))
        {
            Vector3 ledgeTopPoint = GetLedgeTopPoint(hit.point - hit.normal * 0.05f);
            Vector3 ledgeFaceNormal = GetLedgeFaceNormal(ledgeTopPoint);

            Vector3 characterTargetPos = GetCharacterPosOnLedge(ledgeTopPoint, ledgeFaceNormal);
            characterTargetPos += Vector3.up * characterController.radius;
            if (!Physics.CheckCapsule(characterTargetPos, characterTargetPos + Vector3.up * characterController.height, characterController.radius, environmentLayer))
            {
                LedgePoint correctPoint = GetNonEdgeLedgePoint(ledgeTopPoint, ledgeFaceNormal, characterSkin.up, climbStep * 1.5f, characterToLedgeDistance);
                ledgeTopPoint = correctPoint.topPoint;
                ledgeFaceNormal = correctPoint.faceNormal;

                StartClimbing(ledgeTopPoint, ledgeFaceNormal);
            }
        }
    }

    void StartClimbing(Vector3 ledgePoint, Vector3 ledgeFaceNormal)
    {
        SwitchWeapon(0);
        DropTarget();

        movement = Vector3.zero;

        isClimbing = true;
        StartCoroutine(MoveCharacterToPoint(ledgePoint, ledgeFaceNormal, jumpForce, false));
        currentLedgePoint = new LedgePoint(ledgePoint, ledgeFaceNormal);
    }

    bool UpdateCurrentLedgePoint()
    {
        Vector3 playerPos = transform.position;
        playerPos.y += characterYOffsetOnledge - 0.01f;

        RaycastHit hit;
        if(Physics.Raycast(playerPos, characterForward, out hit, characterToLedgeDistance + 0.1f, ledgesLayer))
        {
            currentLedgePoint.faceNormal = hit.normal;
            currentLedgePoint.topPoint = GetLedgeTopPoint(hit.point - hit.normal * 0.05f);

            return true;
        }

        return false;
    }

    void StopClimbing()
    {
        StopCoroutine("MoveCharacterToPoint");
        this.jumpTime = 0;
        currentClimbingState = ClimbingStates.Idle;
        isClimbing = false;
        climbCooldown = climbCooldownTime;
    }

    LedgePoint GetNonEdgeLedgePoint(Vector3 ledgeTopPoint, Vector3 ledgeFaceNormal, Vector3 ledgeUp, float pointToEdgeDistance, float characterToLedgeDistance)
    {
        Vector3 ledgeRight = Quaternion.AngleAxis(-90, ledgeUp) * ledgeFaceNormal;
        Vector3 checkStartPos = ledgeTopPoint + ledgeFaceNormal * characterToLedgeDistance;

        bool leftCheck = Physics.Raycast(checkStartPos - ledgeRight * pointToEdgeDistance, -ledgeFaceNormal, characterToLedgeDistance, ledgesLayer);
        bool rightCheck = Physics.Raycast(checkStartPos + ledgeRight * pointToEdgeDistance, -ledgeFaceNormal, characterToLedgeDistance, ledgesLayer);

        //Debug.DrawRay(checkStartPos - ledgeRight * pointToEdgeDistance, -ledgeFaceNormal, Color.yellow, 3);
        //Debug.DrawRay(checkStartPos + ledgeRight * pointToEdgeDistance, -ledgeFaceNormal, Color.yellow, 3);

        RaycastHit hit, hit1;
        if (!leftCheck && !rightCheck)
        {
            leftCheck = Physics.Raycast(ledgeTopPoint - ledgeRight * pointToEdgeDistance, ledgeRight, out hit, pointToEdgeDistance + pointToEdgeDistance, ledgesLayer);
            rightCheck = Physics.Raycast(ledgeTopPoint + ledgeRight * pointToEdgeDistance, -ledgeRight, out hit1, pointToEdgeDistance + pointToEdgeDistance, ledgesLayer);

            if (leftCheck && rightCheck)
            {
                return new LedgePoint(Vector3.Lerp(hit.point, hit1.point, 0.5f), ledgeFaceNormal);
            }
        }
        else
        {
            if (!leftCheck)
            {
                leftCheck = Physics.Raycast(ledgeTopPoint - ledgeRight * pointToEdgeDistance, ledgeRight, out hit, pointToEdgeDistance + pointToEdgeDistance, ledgesLayer);

                if (leftCheck)
                {
                    Vector3 newLedgePoint = hit.point + ledgeRight * pointToEdgeDistance;
                    return new LedgePoint(newLedgePoint, ledgeFaceNormal);
                }
            }
            else if (!rightCheck)
            {
                rightCheck = Physics.Raycast(ledgeTopPoint + ledgeRight * pointToEdgeDistance, -ledgeRight, out hit1, pointToEdgeDistance + pointToEdgeDistance, ledgesLayer);

                if (rightCheck)
                {
                    Vector3 newLedgePoint = hit1.point - ledgeRight * pointToEdgeDistance;
                    return new LedgePoint(newLedgePoint, ledgeFaceNormal);
                }
            }
        }

        return new LedgePoint(ledgeTopPoint, ledgeFaceNormal);
    }

    LedgePoint GetUpperLedge(Vector3 checkStartPos, float distanceToLedge)
    {
        //Debug.DrawRay(checkStartPos, Vector3.up * distanceToLedge, Color.blue, 3);

        RaycastHit hit;
        if(Physics.Raycast(checkStartPos, characterSkin.up, out hit, distanceToLedge, wallsAndLedgesLayer))
        {
            if(hit.transform.tag == ledgeTag)
            {
                Vector3 topPoint = GetLedgeTopPoint(hit.point);
                Vector3 faceNormal = GetLedgeFaceNormal(topPoint);
                if (faceNormal == Vector3.zero) return new LedgePoint();
                return GetNonEdgeLedgePoint(topPoint, faceNormal, characterSkin.up, climbStep * 1.5f, characterToLedgeDistance);
            }
        }

        return new LedgePoint();
    }

    LedgePoint GetSideLedge(Vector3 currentPoint, Vector3 checkDirection, float checkUpOffset, float checkDownOffset, float maxDistanceToLedge)
    {
        Vector3 checkStartPos = transform.position + checkDirection * climbStep;

        //Debug.DrawRay(checkStartPos + Vector3.up * controller.radius, checkDirection * maxDistanceToLedge, Color.blue);
        //Debug.DrawRay(checkStartPos + Vector3.up * (controller.height + checkUpOffset), checkDirection * maxDistanceToLedge, Color.blue);

        RaycastHit hit;
        if(Physics.CapsuleCast(checkStartPos + Vector3.up * characterController.radius, checkStartPos + Vector3.up * (characterController.height - characterController.radius + checkUpOffset), characterController.radius, checkDirection, out hit, maxDistanceToLedge, wallsAndLedgesLayer))
        {
            if (hit.transform.tag != ledgeTag)
            {
                return new LedgePoint();
            }
            else
            {
                Vector3 topPoint = GetLedgeTopPoint(hit.point - hit.normal * 0.05f);
                Vector3 faceNormal = GetLedgeFaceNormal(topPoint);
                return GetNonEdgeLedgePoint(topPoint, faceNormal, characterSkin.up, climbStep * 1.5f, characterToLedgeDistance);
            }
        }

        currentPoint += checkDirection * (climbStep + 0.5f) + characterForward * 0.05f;
        if (Physics.CapsuleCast(currentPoint + Vector3.up * checkUpOffset, currentPoint - Vector3.up * checkDownOffset, 0, checkDirection, out hit, maxDistanceToLedge, ledgesLayer))
        {
            Vector3 topPoint = GetLedgeTopPoint(hit.point + checkDirection * 0.05f);
            Vector3 faceNormal = -characterForward;
            if(Physics.Raycast(hit.point + checkDirection * 0.05f - characterForward * characterToLedgeDistance, characterForward, out hit, characterToLedgeDistance + 0.1f, ledgesLayer))
            {
                faceNormal = hit.normal;
            }
            return GetNonEdgeLedgePoint(topPoint, faceNormal, characterSkin.up, climbStep * 1.5f, characterToLedgeDistance);
        }

        return new LedgePoint();
    }

    LedgePoint GetBackwardLedge(float checkUpOffset, float checkDownOffset, float maxDistanceToLedge)
    {
        Vector3 checkStartPos = transform.position;

        //Debug.DrawRay(checkStartPos + Vector3.up * controller.radius, -characterForward * maxDistanceToLedge, Color.blue);
        //Debug.DrawRay(checkStartPos + Vector3.up * (controller.height + checkUpOffset), -characterForward * maxDistanceToLedge, Color.blue);

        RaycastHit hit;
        if(Physics.CapsuleCast(checkStartPos + Vector3.up * (characterController.radius - checkDownOffset), checkStartPos + Vector3.up * (characterController.height - characterController.radius + checkUpOffset), characterController.radius, -characterForward, out hit, maxDistanceToLedge, wallsAndLedgesLayer))
        {
            if (hit.transform.tag == ledgeTag)
            {
                Vector3 topPoint = GetLedgeTopPoint(hit.point - hit.normal * 0.05f);
                Vector3 faceNormal = GetLedgeFaceNormal(topPoint);
                return GetNonEdgeLedgePoint(topPoint, faceNormal, characterSkin.up, climbStep * 1.5f, characterToLedgeDistance);
            }
        }

        return new LedgePoint();
    }

    LedgePoint GetLedgeSidePoint(Vector3 checkStartPos, Vector3 checkDirection, float checkOffset, Vector3 ledgeFaceNormal, float distanceToLedge)
    {
        RaycastHit hit;

        //check for ledge extention
        if (Physics.Raycast(checkStartPos + checkDirection * checkOffset, -ledgeFaceNormal, out hit, distanceToLedge * 1.1f, ledgesLayer))
        {
            LedgePoint newPoint = new LedgePoint(GetLedgeTopPoint(hit.point - hit.normal * 0.05f), hit.normal);

            if (Vector3.Angle(ledgeFaceNormal, hit.normal) > 0)
            {
                currentLedgePoint = newPoint;
            }

            //Debug.DrawRay(hit.point, hit.normal, Color.green);
            return newPoint;
        }
        //check for angled ledge
        else if(angledLedgeCheckOffset > 0 && Physics.Raycast(checkStartPos + checkDirection * checkOffset - Vector3.up * angledLedgeCheckOffset, -ledgeFaceNormal, out hit, distanceToLedge * 1.1f, ledgesLayer))
        {
            LedgePoint newPoint = new LedgePoint(GetLedgeTopPoint(hit.point - hit.normal * 0.05f), hit.normal);

            //Debug.DrawRay(hit.point, hit.normal, Color.green);
            return newPoint;
        }

        //ledge edge found
        //Debug.DrawRay(checkStartPos + checkDirection * checkOffset, -ledgeFaceNormal, Color.red);
        return new LedgePoint();
    }

    LedgePoint GetOuterCornerPoint(Vector3 checkStartPos, Vector3 checkDirection, float distanceToLedge)
    {
        RaycastHit hit;
        checkStartPos += checkDirection * (distanceToLedge + climbStep * 0.5f);
        if (Physics.Raycast(checkStartPos, -checkDirection, out hit, distanceToLedge, ledgesLayer))
        {
            Vector3 ledgeTopPoint = GetLedgeTopPoint(hit.point - hit.normal * 0.05f);
            Vector3 characterTargetPos = GetCharacterPosOnLedge(ledgeTopPoint, hit.normal);

            if (!Physics.CheckCapsule(characterTargetPos, characterTargetPos + Vector3.up * characterController.height, characterController.radius, environmentLayer))
            {
                LedgePoint correctPoint = GetNonEdgeLedgePoint(ledgeTopPoint, hit.normal, characterSkin.up, climbStep, distanceToLedge);

                return correctPoint;
            }
        }

        return new LedgePoint();
    }

    LedgePoint GetInnerCornerPoint(Vector3 checkStartPos, Vector3 checkDirection, Vector3 currentNormal, float horizontalInput, float distanceToLedge)
    {
        RaycastHit hit;
        if (Physics.Raycast(checkStartPos, checkDirection, out hit, distanceToLedge, ledgesLayer))
        {
            float angle = Vector3.Angle(hit.normal, currentNormal);
            if(angle == 0) return new LedgePoint();
            float hitToCurLedgeAngledDist = distanceToLedge / Mathf.Sin(Mathf.Deg2Rad * angle);

            Vector3 newLedgeDir = Quaternion.AngleAxis(horizontalInput * 90, characterSkin.up) * hit.normal;
            Vector3 newLedgePoint = hit.point + newLedgeDir.normalized * (hitToCurLedgeAngledDist - distanceToLedge);
            newLedgePoint = GetLedgeTopPoint(newLedgePoint);
            
            Vector3 characterTargetPos = GetCharacterPosOnLedge(newLedgePoint, hit.normal);
            if (!Physics.CheckCapsule(characterTargetPos, characterTargetPos + Vector3.up * characterController.height, characterController.radius, environmentLayer))
            {
                return new LedgePoint(newLedgePoint, hit.normal);
            }
        }

        return new LedgePoint();
    }

    Vector3 GetLedgeFaceNormal(Vector3 ledgeTopPoint)
    {
        Vector3 playerPos = transform.position;
        playerPos.y = ledgeTopPoint.y;

        RaycastHit hit;
        if (Physics.Linecast(playerPos, ledgeTopPoint, out hit, ledgesLayer))
        {
            return hit.normal;
        }
        return Vector3.zero;
    }

    Vector3 GetLedgeTopPoint(Vector3 ledgePoint)
    {
        RaycastHit hit;
        if(Physics.Raycast(ledgePoint + Vector3.up * 0.5f, Vector3.down, out hit, 0.7f, ledgesLayer))
        {
            return hit.point - Vector3.up * 0.05f;
        }

        return ledgePoint;
    }

    Vector3 GetCharacterPosOnLedge(Vector3 ledgeTopPoint, Vector3 ledgeFaceNormal)
    {
        return ledgeTopPoint + ledgeFaceNormal * characterToLedgeDistance - Vector3.up * characterYOffsetOnledge;
    }

    Vector3 GetPlateauPosition(Vector3 ledgeTopPoint, Vector3 checkDirection, float distanceFromEdge)
    {
        RaycastHit hit;
        if(Physics.Raycast(ledgeTopPoint + checkDirection * distanceFromEdge + Vector3.up, Vector3.down, out hit, characterController.height, environmentLayer))
        {
            if(!Physics.CheckCapsule(hit.point + Vector3.up * (characterController.radius + 0.01f), hit.point + Vector3.up * (characterController.height - characterController.radius), characterController.radius, environmentLayer))
            {
                //Debug.DrawRay(hit.point, hit.normal, Color.green, 3);
                return hit.point + Vector3.up * 0.01f;
            }
            else
            {
                //Debug.DrawRay(hit.point, hit.normal, Color.red, 3);
            }
        }

        return Vector3.zero;
    }

    void MoveCharacterAlongLedge(Vector3 targetPoint, Vector3 targetNormal, float movementSpeed)
    {
        Vector3 characterTargetPos = GetCharacterPosOnLedge(targetPoint, targetNormal);

        transform.position = Vector3.Slerp(transform.position, characterTargetPos, Time.deltaTime * movementSpeed);

        RotateSkin(-targetNormal);

        //end of translation
        if (Vector3.Distance(transform.position, characterTargetPos) <= 0.05f && Vector3.Angle(characterForward, -targetNormal) < 1)
        {
            transform.position = characterTargetPos;
            SetSkinRotation(-targetNormal);

            currentLedgePoint.topPoint = targetPoint;

            //if (currentClimbingState != ClimbingStates.Turning) currentClimbingState = ClimbingStates.Idle;
        }
    }

    IEnumerator MoveCharacterToPoint(Vector3 targetPoint, Vector3 targetNormal, float movementSpeed, bool useCurve)
    {
        Vector3 finalTarget = GetCharacterPosOnLedge(targetPoint, targetNormal);

        Vector3 velocity = GetStartJumpVelocity(transform.position, finalTarget, 0.3f, -gravityMultiplier);

        currentClimbingState = ClimbingStates.Jumping;

        while (Vector3.Distance(transform.position, finalTarget) > 0.05f && !(useCurve && this.jumpTime <= 0))
        {
            RotateSkin(-targetNormal);

            //transform.position = Vector3.Slerp(transform.position, finalTarget, Time.deltaTime * movementSpeed);

            if (!useCurve)
            {
                transform.position = Vector3.Slerp(transform.position, finalTarget, Time.deltaTime * movementSpeed);
            }
            else
            {
                this.jumpTime -= Time.deltaTime;

                transform.position = Vector3.MoveTowards(transform.position, transform.position + velocity, velocity.magnitude * Time.deltaTime);
                velocity -= Vector3.up * gravityMultiplier * Time.deltaTime;
            }

            if (!isClimbing) break;

            yield return null;
        }

        this.jumpTime = 0;

        if (isClimbing)
        {
            transform.position = finalTarget;
            SetSkinRotation(-targetNormal);

            UpdateCurrentLedgePoint();
        }

        currentClimbingState = ClimbingStates.Idle;

        //if (currentClimbingState != ClimbingStates.Turning) currentClimbingState = ClimbingStates.Idle;
    }

    #endregion

    #region Math
    Vector3 GetStartJumpVelocity(Vector3 startPoint, Vector3 endPoint, float topPointHeightOffset, float gravity)
    {
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = endPoint - startPoint;
        displacementXZ.y = 0;

        float height = Mathf.Max(startPoint.y, endPoint.y) - Mathf.Min(startPoint.y, endPoint.y) + topPointHeightOffset;
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        float time = (Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity));
        Vector3 velocityXZ = displacementXZ / time;
        this.jumpTime = time;

        //Debug jump curve
        //DrawPath(startPoint, velocityXZ + velocityY, time, 30);

        return velocityXZ + velocityY;
    }

    void DrawPath(Vector3 startPos, Vector3 initialVelocity, float timeToTarget, int resolution)
    {
        Vector3 previousDrawPoint = startPos;

        for(int i = 1; i <= resolution; i++)
        {
            float simulationTime = i / (float)resolution * timeToTarget;
            Vector3 displacement = initialVelocity * simulationTime - Vector3.up * gravityMultiplier * simulationTime * simulationTime / 2;
            Vector3 drawPoint = startPos + displacement;
            Debug.DrawLine(previousDrawPoint, drawPoint, Color.cyan, 3);
            previousDrawPoint = drawPoint;
        }
    }

    #endregion

    #region Shooting
    private void HandleShooting()
    {
        if (Input.GetButton("Aim") || Input.GetButton("Fire"))
        {
            if(isHoldingWeapon) TakeAim();
            else
            {
                if(weapons != null && weapons.weaponsList.Count > 1)
                {
                    SwitchWeapon(1);
                }
            }
        }

        if (isHoldingWeapon)
        {
            if (Input.GetButtonDown("Aim"))
            {
                isAiming = true;
            }

            if (Input.GetButtonDown("Fire"))
            {
                currentWeapon.StartFiring();
            }

            if(Input.GetButtonDown("Reload"))
            {
                DropTarget();
                currentWeapon.Reload();
            }
        }

        if (Input.GetButtonUp("Aim"))
        {
            isAiming = false;
        }

        if (Input.GetButtonUp("Fire"))
        {
            currentWeapon.StopFiring();
        }

        if(isHoldingAim && isHoldingWeapon)
        {
            HoldAim();
            aimingTimer -= Time.deltaTime;
            if (aimingTimer <= 0) isHoldingAim = false;
        }
    }

    void TakeAim()
    {
        aimingTimer = aimingTime;
        if (!isHoldingAim) isHoldingAim = true;
    }

    void HoldAim()
    {
        currentAimPoint = GetAimPoint();
        if (Mathf.Abs(Vector3.Angle(Maths.GetVectorIgnoreY(currentAimPoint - transform.position), characterForward)) > aimingMaxAngle) RotateSkin(cameraController.lookForward);
    }

    void DropTarget()
    {
        aimingTimer = 0;
        isHoldingAim = false;
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

        if(currentWeapon != null) 
            currentWeapon.SetActive(false);

        GameObject nextWeaponObj = weapons.weaponsList[targetIndex];
        if (nextWeaponObj != null)
        {
            currentWeapon = nextWeaponObj.GetComponent<IWeapon>();
            if (currentWeapon != null)
            {
                isHoldingWeapon = true;
                currentWeapon.SetActive(true);
            }
            else
            {
                aimingTimer = 0;
                isHoldingAim = false;
                isHoldingWeapon = false;
            }
        }
        else
        {
            aimingTimer = 0;
            isHoldingAim = false;
            isHoldingWeapon = false;
            currentWeapon = null;
        }

        if (onWeaponSwitched != null) onWeaponSwitched();
    }

    private Vector3 GetAimPoint(Vector3 raycastOrigin, Vector3 raycastDirection, float distance, int layerMask)
    {
        RaycastHit hit;
        if(Physics.Raycast(raycastOrigin, raycastDirection, out hit, distance, layerMask))
        {
            return hit.point;
        }
        return raycastOrigin + raycastDirection * distance;
    }

    public Vector3 GetAimPoint()
    {
        return GetAimPoint(cameraController.mainCamera.position, cameraController.mainCamera.forward, aimingMaxDistance, aimableLayer);
    }
    #endregion

    public float GetInteractionFieldAngle()
    {
        return interactionFieldAngle;
    }

    public void Kill()
    {

    }
}

public class LedgePoint
{
    public Vector3 topPoint = Vector3.zero;
    public Vector3 faceNormal = Vector3.zero;

    public LedgePoint() {}

    public LedgePoint(Vector3 topPoint, Vector3 faceNormal)
    {
        this.topPoint = topPoint;
        this.faceNormal = faceNormal;
    }
}

public enum ClimbingStates
{
    Idle,
    MovingAlongLedge,
    Turning,
    Jumping
}