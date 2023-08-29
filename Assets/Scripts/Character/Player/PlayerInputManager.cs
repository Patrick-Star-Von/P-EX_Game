using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Singleton;

    PlayerControls playerControls;

    public Vector2 movementInput;
    public Vector2 mouseInput;
    public float movementVertical;
    public float movementHorizontal;
    public float mouseHorizontal;
    public float mouseVertical;
    public float moveAmount;
    public float sprintFloat;
    public float climbFloat;
    public bool climb;
    public bool interact;
    public bool isSprinting;
    public bool lightAttack;
    public bool heavyAttack;
    public bool unlockAttack;

    public bool testButton;

    [Space(10)]
    [Tooltip("奔跑和行走之间的时间间隔，仅用在用切换按键进行奔跑的情况下")]
    public float SprintTimeout = 1.0f;
    [SerializeField] float sprintTimeoutDelta;
    [Tooltip("可再次攀爬的时间间隔")]
    public float ClimbTimeout = 0.50f;
    [SerializeField] float climbTimeoutDelta;
    [Tooltip("是否是切换式按键奔跑")]
    public bool switchToSprint = true;
    [Tooltip("当玩家在空中时，锁定奔跑按键")]
    public bool SprintLock = false;

    [Space(10)]
    public bool cursorLocked = true;

    private float deltaTime;

    void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        // SceneManager.activeSceneChanged += OnSceneChange;
    }

    void Start()
    {
        sprintTimeoutDelta = SprintTimeout;
        climbTimeoutDelta = ClimbTimeout;
    }

    void Update()
    {
        deltaTime = Time.deltaTime;

        HandleUnlockAttack();
        HandleMovementInput();
        HandleMouseInput();
        HandleSprintInput();
        HandleClimbInput();
    }

    // void OnSceneChange(Scene oldScene, Scene newScene)
    // {
    //     // if load into world scene, enable player's controls
    //     if (newScene.buildIndex == WorldSaveGameManager.Singleton.GetWorldSceneIndex())
    //     {
    //         Singleton.enabled = true;
    //     }
    //     // otherwise must be at main menu, then disable it
    //     else
    //     {
    //         Singleton.enabled = false;
    //     }
    // }

    void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();

            playerControls.PlayerMovement.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
            playerControls.PlayerMovement.Look.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
            playerControls.PlayerMovement.Sprint.performed += ctx => sprintFloat = ctx.ReadValue<float>();
            playerControls.PlayerMovement.Climb.performed += ctx => climbFloat = ctx.ReadValue<float>();
            playerControls.PlayerMovement.Interact.performed += ctx => interact = ctx.ReadValueAsButton();
            playerControls.PlayerMovement.LightAttack.performed += ctx => lightAttack = ctx.ReadValueAsButton();
            playerControls.PlayerMovement.HeavyAttack.performed += ctx => heavyAttack = ctx.ReadValueAsButton();
            playerControls.PlayerMovement.TestButton.performed += ctx => testButton = ctx.ReadValueAsButton();
        }

        playerControls.Enable();
    }

    // void OnDestroy()
    // {
    //     SceneManager.activeSceneChanged -= OnSceneChange;
    // }

    // 拆解，处理移动向量
    void HandleMovementInput()
    {
        movementVertical = movementInput.y;
        movementHorizontal = movementInput.x;

        moveAmount = Mathf.Clamp01(Mathf.Abs(movementVertical) + Mathf.Abs(movementHorizontal));

        // Clamp moveAmount
        if (moveAmount > 0 && moveAmount <= 0.5f)
        {
            moveAmount = 0.5f;
        }
        else if (moveAmount > 0.5f && moveAmount <= 1f)
        {
            moveAmount = 1f;
        }
    }

    // 拆解，处理鼠标的向量
    void HandleMouseInput()
    {
        mouseHorizontal = Mathf.Clamp(mouseInput.x, -1f, 1f);
        mouseVertical = Mathf.Clamp(mouseInput.y, -1f, 1f);
    }

    void HandleSprintInput()
    {
        if (sprintTimeoutDelta > 0)
        {
            sprintTimeoutDelta -= deltaTime;
        }

        if (sprintTimeoutDelta <= 0)
        {
            if (sprintFloat == 1f)
            {
                isSprinting = isSprinting ? false : true;
                sprintTimeoutDelta = SprintTimeout;
            }
        }
    }

    void HandleClimbInput()
    {
        if (climbTimeoutDelta > 0)
        {
            climbTimeoutDelta -= deltaTime;
        }

        if (climbFloat >= 0.9f && climbTimeoutDelta <= 0)
        {
            climb = true;
            climbTimeoutDelta = ClimbTimeout;
        }
        else
        {
            climb = false;
        }
    }

    void HandleUnlockAttack()
    {
        if (!lightAttack && !heavyAttack)
        {
            unlockAttack = true;
        }
        else
        {
            unlockAttack = false;
        }
    }

    public void ResetSprintFlag()
    {
        isSprinting = false;
        sprintTimeoutDelta = SprintTimeout;
    }

    // 点击游戏模式的窗口时，可以隐藏鼠标
    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
