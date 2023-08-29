using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementManager : MonoBehaviour
{
    PlayerManager playerManager;

    [Header("Player Movement")]
    public float moveSpeed = 1.5f;
    public float sprintSpeed = 4f;
    public float rotationSmoothTime = 0.12f;
    public float speedChangeRate = 100.0f;
    public bool isSprinting = false;
    public bool isClimbing = false;
    public bool climb = false;

    [Space(10)]
    public float camMoveSensitivity = 1f;

    [Space(10)]
    public float ClimbHeight = 2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;
    [Tooltip("着陆时，不能移动的时间间隔最大值，这个时间间隔随降落高度变化")]
    public float landSpeedTimeoutMax = 1.0f;
    [Tooltip("用于着陆时修正落地站立时间")]
    public float landStandMultiplier = 1.5f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    // player
    private float speed = 0.0f;
    private float animationBlend = 0.0f;
    private float targetRotation = 0.0f;
    private float rotationVelocity = 0.0f;
    private float verticalVelocity = 0.0f;
    private float terminalVelocity = 53.0f;
    private float minFallingSpeed = -15.0f;

    // timeout deltatime
    private float fallTimeoutDelta;
    private float landSpeedTimeoutDelta;
    private float inAirTime = 0.0f;

    // animation IDs
    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDClimb;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    private Animator animator;
    private CharacterController characterController;
    private PlayerInputManager playerInputManager;
    private GameObject mainCamera;
    private float deltaTime = 0f;
    // private Vector3 moveDirection;

    private const float _threshold = 0.01f;

    private bool hasAnimator;

#if ENABLEplayerInputManager_SYSTEM
    private PlayerInput playerInput;
#endif

    //     private bool IsCurrentDeviceMouse
    //     {
    //         get
    //         {
    // #if ENABLEplayerInputManager_SYSTEM
    //             return playerInput.currentControlScheme == "Keyboard";
    // #else
    // 				return false;
    // #endif
    //         }
    //     }

    [Header("Player Inputs")]
    public Vector2 movementInput;
    public float movementVertical;
    public float movementHorizontal;
    public float mouseVertical;
    public float mouseHorizontal;
    public float moveAmount;
    public float mouseMoveAmount;

    [Header("Audio Clips")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        hasAnimator = TryGetComponent(out animator);
        characterController = GetComponent<CharacterController>();
        playerInputManager = GetComponent<PlayerInputManager>();

        // reset our timeouts on start
        fallTimeoutDelta = FallTimeout;
        landSpeedTimeoutDelta = 0.0f;

        AssignAnimationIDs();
    }

    private void Update()
    {
        deltaTime = Time.deltaTime;
        if(landSpeedTimeoutDelta > 0.0f) landSpeedTimeoutDelta -= deltaTime;

        GetAllInputs();
        HandleGravity();
        GroundedCheck();
        Climb();
        if(landSpeedTimeoutDelta <= 0.0f) HandleMovement(); // 落地的短暂时间内不可移动
    }

    private void LateUpdate()
    {
        HandleCameraRotation();
    }

    private void GetAllInputs()
    {
        movementInput = playerInputManager.movementInput;
        movementVertical = playerInputManager.movementVertical;
        movementHorizontal = playerInputManager.movementHorizontal;
        moveAmount = playerInputManager.moveAmount;
        mouseVertical = playerInputManager.mouseVertical;
        mouseHorizontal = playerInputManager.mouseHorizontal;
        isSprinting = playerInputManager.isSprinting;
        climb = playerInputManager.climb;

        mouseMoveAmount = Mathf.Abs(mouseHorizontal) + Mathf.Abs(mouseVertical);
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers);

        // update animator if using character
        if (hasAnimator)
        {
            animator.SetBool(animIDGrounded, Grounded);
        }
    }

    private void HandleMovement()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (movementInput == Vector2.zero) targetSpeed = 0;

        float speedOffset = 0.1f;
        // float inputMagnitude = playerInputManager.analogMovement ? playerInputManager.movementInput.magnitude : 1f;
        float inputMagnitude = 1f;

        // 速度足够小的时候，重置奔跑的bool
        if (speed < 0.05f)
        {
            isSprinting = false;
            playerInputManager.isSprinting = false;
        }

        // 玩家在空中不可以改变速度
        if (Grounded)
        {
            if (speed < targetSpeed - speedOffset || speed > targetSpeed + speedOffset)
            {
                // 此处仅处理水平移动时，移动向量的长度，也就是移动速度，还没有考虑方向
                speed = Mathf.Lerp(speed, targetSpeed * inputMagnitude, deltaTime * speedChangeRate);

                // 保留三位小数
                speed = Mathf.Round(speed * 1000f) / 1000f;
            }
            else
            {
                speed = targetSpeed;
            }
        }

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, deltaTime * speedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        // 开始处理移动的方向
        // normalise input direction
        Vector3 inputDirection = new Vector3(movementHorizontal, 0.0f, movementVertical).normalized;

        // Vector2的!=运算符是采用近似计算的，所以不仅运行快，而且不会出现浮点错误
        if (movementInput != Vector2.zero)
        {
            // 加上主摄像机的旋转，来实现世界坐标转为本地坐标
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);

            // 相对于摄像机旋转角色
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        // y方向上主要是处理重力加速度
        characterController.Move(targetDirection.normalized * (speed * deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * deltaTime);

        // update animator if using character
        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, animationBlend);
            animator.SetFloat(animIDMotionSpeed, inputMagnitude);
        }
    }

    private void HandleGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (hasAnimator)
            {
                animator.SetBool(animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            // 计算在空中的时间
            inAirTime += deltaTime;

            // fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= deltaTime;
            }
            else
            {
                // update animator if using character
                if (hasAnimator)
                {
                    animator.SetBool(animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not climb
            climb = false;
        }

        // 施加加速度，但不高于最大速度 (multiply by delta time twice to linearly speed up over time)
        if (verticalVelocity > minFallingSpeed && verticalVelocity < terminalVelocity)
        {
            verticalVelocity += Gravity * deltaTime;
        }
    }

    private void Climb()
    {
        if (climb && !isClimbing)
        {
            Debug.Log("Climb!");
            // 判定成功并开始攀爬后
            isClimbing = true;
        }

        // 攀爬结束重置
        isClimbing = false;
    }

    private void HandleCameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (mouseMoveAmount >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            // float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : deltaTime;
            float deltaTimeMultiplier = 1.0f;

            cinemachineTargetYaw += mouseHorizontal * deltaTimeMultiplier * camMoveSensitivity;
            cinemachineTargetPitch += mouseVertical * deltaTimeMultiplier * camMoveSensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch + CameraAngleOverride,
            cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(characterController.center),
                FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        // 落地时清空速度
        speed = 0.0f;
        animator.SetFloat(animIDSpeed, 0.0f);
        // 落地时站立的时间，随落地时间变化
        inAirTime *= landStandMultiplier; // 修正落地时间
        landSpeedTimeoutDelta = Mathf.Clamp(inAirTime, 0.0f, landSpeedTimeoutMax);
        inAirTime = 0.0f;
        Debug.Log("Stand for " + landSpeedTimeoutDelta + " seconds.");
        
        AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(characterController.center), FootstepAudioVolume);
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDClimb = Animator.StringToHash("Climb");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }
}
