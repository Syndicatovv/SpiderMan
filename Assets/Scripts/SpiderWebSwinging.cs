using UnityEngine;
using Invector.vCharacterController;

public class SpiderWebSwinging : MonoBehaviour
{
    [Header("Input")]
    private KeyCode modifierKey = KeyCode.LeftShift;
    private KeyCode leftHandKey = KeyCode.Mouse0;
    private KeyCode rightHandKey = KeyCode.Mouse1;

    [Header("References")]
    public LineRenderer lr;

    public Transform handTip, cam, player;
    public Transform leftHandTip;
    public Transform orientation;

    public LayerMask whatIsGrappleable;

    public vThirdPersonController cc;
    public vThirdPersonInput ccInput;
    public Animator anim;

    [Header("Swing")]
    private float maxDistance = 35f;

    private float horizontalThrustForce = 20f;
    private float forwardThrustForce = 45f;
    private float maxSwingSpeed = 60f;

    private float airControlForce = 12f;
    private float maxAirSpeed = 55f;

    private Vector3 swingPoint;
    private Vector3 currentGrapplePosition; 

    private bool IsSwinging;
    private float ropeLength;

    private Transform activeHandTip;

    private KeyCode activeMouseButton = KeyCode.None;

    private Vector2 swingInput;

    private bool isSwinging => IsSwinging;

    [Header("Flight Body")]
    [SerializeField] private float bodyLeanSpeed = 6f;

    [Header("Release Flight")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float fastFlightHeightThreshold = 25f;
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float animationCrossFade = 0.12f;

    private bool releaseSequenceActive;
    private bool shouldUseFastFlight;
    private bool fastFlightActive;
    private bool freeFallActive;

    private float releaseTimer;
        
    void Start()
    {
        InitializeReferences();

        activeHandTip = handTip;

        if (lr != null) lr.positionCount = 0;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void InitializeReferences()
    {
        if (cc == null) cc = GetComponent<vThirdPersonController>();
        if (ccInput == null) ccInput = GetComponent<vThirdPersonInput>();
        if (anim == null) anim = GetComponent<Animator>();
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (player == null) player = transform;
    }

    void Update()
    {
        ReadSwingInput();
        HandleInput();

        if (isSwinging)
        {
            UpdateFlightBodyAnimation();
        }

        UpdateReleaseSequence();
    }

    void ReadSwingInput()
    {
        swingInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
            swingInput.y += 1f;

        if (Input.GetKey(KeyCode.S))
            swingInput.y -= 1f;

        if (Input.GetKey(KeyCode.D))
            swingInput.x += 1f;

        if (Input.GetKey(KeyCode.A))
            swingInput.x -= 1f;

        swingInput = Vector2.ClampMagnitude(swingInput, 1f);
    }

    void UpdateFlightBodyAnimation()
    {   
        if (!isSwinging) return;
        if (anim == null || cc == null || cc._rigidbody == null) return;

        Rigidbody rb = cc._rigidbody;

        Vector3 localVelocity =
            transform.InverseTransformDirection(rb.linearVelocity);
        float speedReference =
            isSwinging
                ? maxSwingSpeed
                : maxAirSpeed;

        float targetForward =
            Mathf.Clamp(
                localVelocity.z / speedReference,
                -1f,
                1f
            );

        float targetRight =
            Mathf.Clamp(
                localVelocity.x / speedReference,
                -1f,
                1f
            );

        anim.SetFloat(
            "FlyForward",
            targetForward,
            0.1f,
            Time.deltaTime
        );

        anim.SetFloat(
            "FlyRight",
            targetRight,
            0.1f,
            Time.deltaTime
        );

        float targetLean =
            Mathf.Clamp(
                -localVelocity.x / maxSwingSpeed,
                -1f,
                1f
            );

        float currentLean =
            anim.GetFloat("FlyLean");

        float newLean =
            Mathf.Lerp(
                currentLean,
                targetLean,
                Time.deltaTime * bodyLeanSpeed
            );

        anim.SetFloat(
            "FlyLean",
            newLean
        );
    }

    void LateUpdate()
    {
        UpdateRopeVisual();
    }

    void UpdateRopeVisual()
    {
        if (!isSwinging || lr == null || activeHandTip == null)
        {
            if (lr != null) lr.positionCount = 0;
            return;
        } 
        lr.positionCount = 2;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 20f);

        lr.SetPosition(0, activeHandTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }


    void HandleInput()
    {
        if (!isSwinging) HandleStartInput();
        else HandleStopInput();
    }
    void HandleStartInput()
    {
        if (!Input.GetKey(modifierKey))
            return;
        if (Input.GetKeyDown(leftHandKey))
        {
            Transform hand =
                leftHandTip != null
                    ? leftHandTip
                    : handTip;

            StartSwing(leftHandKey, hand);
            return;
        }
        if (Input.GetKeyDown(rightHandKey))
        {
            StartSwing(rightHandKey, handTip);
        }
    }
    private void HandleStopInput()
    {
        if (Input.GetKeyUp(modifierKey))
        {
            StopSwing();
            return;
        }


        if (activeMouseButton != KeyCode.None &&
            Input.GetKeyUp(activeMouseButton))
        {
            StopSwing();
        }
    }

    void StartSwing(KeyCode mouseButton, Transform hand)
    {
        if (cc == null || cc._rigidbody == null) return;
        if (cam == null) return;
        if (hand == null) return;

        if (!Physics.Raycast(
            cam.position + Vector3.up * 12f,
            cam.forward,
            out RaycastHit hit,
            maxDistance,
            whatIsGrappleable,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }


        swingPoint = hit.point;
        activeHandTip = hand;
        activeMouseButton = mouseButton;

        Rigidbody rb = cc._rigidbody;

        ropeLength = Vector3.Distance(
            rb.position,
            swingPoint
        );

        if (ropeLength < 0.5f)
            return;

        IsSwinging = true;

        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;

        if (lr != null)
        {
            lr.positionCount = 2;
            currentGrapplePosition = activeHandTip.position;
        }

        EnterSwingAnimation();
    }

    void StopSwing()
    {
        if (!IsSwinging)
            return;


        IsSwinging = false;


        if (lr != null)
            lr.positionCount = 0;


        activeMouseButton = KeyCode.None;

        activeHandTip = handTip;


        BeginReleaseSequence();
    }

    void PlayOverrideState(string stateName, float transitionDuration = -1f){
        if (anim == null) return;

        int layerIndex = anim.GetLayerIndex("OverrideFallLayer");

        if (transitionDuration < 0f) transitionDuration = animationCrossFade;

        anim.SetLayerWeight(layerIndex, 1f);

        string fullStateName = "OverrideFallLayer." + stateName;

        int stateHash =
        Animator.StringToHash(fullStateName);


        anim.SetLayerWeight(layerIndex, 1f);

        anim.CrossFadeInFixedTime(
            fullStateName,
            transitionDuration,
            layerIndex,
            0f
        );
    }

    bool TryGetGroundDistance(out float distance){
        distance = float.PositiveInfinity;

        if (cc == null || cc._rigidbody == null) return false;

        Vector3 origin = cc._rigidbody.position + Vector3.up * 0.5f;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0f, hit.distance - 0.5f);
            return true;
        }

        return false;
    }

    void BeginReleaseSequence(){
        if (anim == null) return;

        releaseSequenceActive = true;

        fastFlightActive = false;
        freeFallActive = false;

        releaseTimer = 0f;

        if (cc != null && cc._rigidbody != null) cc._rigidbody.useGravity = true;

        TurnOnFlyAnimation();
        TurnOffFallAnimation();

        PlayOverrideState("RollState", 0.08f);
    }

    void UpdateReleaseSequence(){
        if (!releaseSequenceActive) return;
        if (cc == null || cc._rigidbody == null) return;

        releaseTimer += Time.deltaTime;

        if (!fastFlightActive && !freeFallActive && releaseTimer >= rollDuration){
            if (Input.GetKey(KeyCode.LeftShift)){
                fastFlightActive = true;
                PlayOverrideState("FlyingFastState");
            }
            else{
                freeFallActive = true;
                PlayOverrideState("FreeFallState");
            }
        }
        if (fastFlightActive){
            if(TryGetGroundDistance(out float groundDistance))
            {
                if(groundDistance <= fastFlightHeightThreshold){
                    fastFlightActive = false;
                    freeFallActive = true;

                    PlayOverrideState("FreeFallState");
                }
            }
    
    
        }
        if ((fastFlightActive || freeFallActive) &&
            cc.isGrounded)
        {
            FinishReleaseSequence();
        }
    }

    void FinishReleaseSequence()
    {
        releaseSequenceActive = false;

        fastFlightActive = false;
        freeFallActive = false;

        releaseTimer = 0f;

        if (anim == null)
            return;

        int layerIndex =
        anim.GetLayerIndex("OverrideFallLayer");

        if (layerIndex >= 0)
        {
            anim.SetLayerWeight(layerIndex, 0f);
        }

        TurnOnFallAnimation();
    }

    void FixedUpdate()
    {
        if (isSwinging)
        {
            ApplySwingPhysics();
            ApplyRopeConstraint();
            return;
        }

        if (releaseSequenceActive) ApplyAirMovement();
    }

    void ApplyRopeConstraint()
    {
        if (!isSwinging) return;
        if (cc == null || cc._rigidbody == null) return;

        Rigidbody rb = cc._rigidbody;
        Vector3 fromPoint = rb.position - swingPoint;
        float currentDistance = fromPoint.magnitude;
        if (currentDistance <= ropeLength) return;
        Vector3 ropeDirection = fromPoint / currentDistance;

        Vector3 correctedPosition =
        swingPoint +
        ropeDirection * ropeLength;

        rb.position = correctedPosition;

        Vector3 velocity =
            rb.linearVelocity;


        float radialVelocity =
            Vector3.Dot(
                velocity,
                ropeDirection
            );

        if (radialVelocity > 0f)
        {
            velocity -=
                ropeDirection * radialVelocity;

            rb.linearVelocity = velocity;
        }
    }

    void ApplySwingPhysics()
    {
        if (cc == null || cc._rigidbody == null)
            return;

        ReadMovementForces(cc._rigidbody);
    }

    void ApplyAirMovement()
    {
        if (cc == null || cc._rigidbody == null)
            return;

        Rigidbody rb = cc._rigidbody;

        if (cc.isGrounded)
            return;


        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cam.forward,
                Vector3.up
            );

        Vector3 cameraRight =
            Vector3.ProjectOnPlane(
                cam.right,
                Vector3.up
            );


        if (cameraForward.sqrMagnitude > 0.001f)
            cameraForward.Normalize();

        if (cameraRight.sqrMagnitude > 0.001f)
            cameraRight.Normalize();


        Vector3 moveDirection =
            cameraForward * swingInput.y +
            cameraRight * swingInput.x;


        if (moveDirection.sqrMagnitude > 0.001f)
        {
            moveDirection.Normalize();


            rb.AddForce(
                moveDirection *
                airControlForce,
                ForceMode.Acceleration
            );
        }

        if (rb.linearVelocity.sqrMagnitude >
            maxAirSpeed * maxAirSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized *
                maxAirSpeed;
        }
    }

    void ReadMovementForces(Rigidbody rb)
    {
        Vector3 ropeDirection =
            (rb.position - swingPoint).normalized;


        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cam.forward,
                Vector3.up
            );

        Vector3 cameraRight =
            Vector3.ProjectOnPlane(
                cam.right,
                Vector3.up
            );


        if (cameraForward.sqrMagnitude > 0.001f)
            cameraForward.Normalize();

        if (cameraRight.sqrMagnitude > 0.001f)
            cameraRight.Normalize();

        Vector3 horizontalInput =
            cameraRight * swingInput.x;


        Vector3 steeringDirection =
            Vector3.ProjectOnPlane(
                horizontalInput,
                ropeDirection
            );


        if (steeringDirection.sqrMagnitude > 0.001f)
        {
            steeringDirection.Normalize();


            rb.AddForce(
                steeringDirection *
                horizontalThrustForce,
                ForceMode.Acceleration
            );
        }

        if (Mathf.Abs(swingInput.y) > 0.01f)
        {
            ApplyForwardSwing(
                rb,
                ropeDirection,
                cameraForward
            );
        }

        float maxSpeedSqr =
            maxSwingSpeed *
            maxSwingSpeed;


        if (rb.linearVelocity.sqrMagnitude > maxSpeedSqr)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized *
                maxSwingSpeed;
        }
    }

    void ApplyForwardSwing(
        Rigidbody rb,
        Vector3 ropeDirection,
        Vector3 cameraForward)
    {
        float input =
            swingInput.y;


        Vector3 tangentVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                ropeDirection
            );


        Vector3 swingDirection;

        if (tangentVelocity.sqrMagnitude > 1f)
        {
            swingDirection =
                tangentVelocity.normalized;
        }
        else
        {

            swingDirection =
                Vector3.ProjectOnPlane(
                    cameraForward,
                    ropeDirection
                );


            if (swingDirection.sqrMagnitude < 0.001f)
                return;


            swingDirection.Normalize();
        }

        float bottomFactor =
            Vector3.Dot(
                ropeDirection,
                Vector3.down
            );


        bottomFactor =
            Mathf.Clamp01(bottomFactor);

        bottomFactor *= bottomFactor;


        float pumpMultiplier =
            Mathf.Lerp(
                0.35f,
                2.5f,
                bottomFactor
            );
        rb.AddForce(
            swingDirection *
            input *
            forwardThrustForce *
            pumpMultiplier,
            ForceMode.Acceleration
        );
    }
    void EnterSwingAnimation()
    {
        if (anim == null) return;

        releaseSequenceActive = false;

        fastFlightActive = false;
        freeFallActive = false;

        releaseTimer = 0f;

        anim.applyRootMotion = false;

        int layerIndex = anim.GetLayerIndex("OverrideFallLayer");

        if (layerIndex >= 0)
        {
            anim.SetLayerWeight(layerIndex, 1f);

            anim.Play("FlyingState", layerIndex, 0f);
        }

        anim.SetBool("isFlying", true);
        anim.SetBool("BlockFallVisual", true);
    }

    void ExitSwingAnimation()
    {
        if (anim == null)
            return;

        TurnOnFlyAnimation();
    }
    void TurnOffFallAnimation()
    {
        if (anim == null) return;
        anim.SetBool("BlockFallVisual", true);
    }
    public void TurnOnFallAnimation()
    {
        if (anim == null) return;
        anim.SetBool("BlockFallVisual", false);
    }

    void TurnOffFlyAnimation()
    {
        if (anim == null) return;
        anim.SetBool("isFlying", true);
    }
    public void TurnOnFlyAnimation()
    {
        if (anim == null) return;
        anim.SetBool("isFlying", false);
    }
    void TurnOffGroundFlyAnimation()
    {
        if (anim == null) return;
        anim.SetBool("IsGrounded", true);
    }
    public void TurnOnGroundFlyAnimation()
    {
        if (anim == null) return;
        anim.SetBool("IsGrounded", false);
    }

    void OnDrawGizmosSelected()
    {
        if (!isSwinging) return;

        Gizmos.DrawSphere(swingPoint, 0.25f);
        Gizmos.DrawLine(transform.position, swingPoint);
    }
}
