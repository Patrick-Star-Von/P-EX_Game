using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerAttacker : PlayerManager
{
    public bool lightAttack;
    public bool heavyAttack;
    public bool testButton;

    [Tooltip("防止玩家一直按着攻击键后，人物一直攻击")]
    private bool attackLock = false;
    [Tooltip("只要玩家没按着攻击键，就解锁攻击锁")]
    private bool unlockAttack = false;
    [Tooltip("决定玩家是否有折叠臂刃")]
    private bool hasDualArmBlade = true;
    [Tooltip("玩家是否已伸出臂刃")]
    public bool dualArmBladeDrawn = true;
    public float DrawBladeTimeout = 1.0f;

    private float drawBladeTimeoutDelta;
    private float deltaTime;

    Weapon currentWeapon;

    [Space(10)]
    [Header("Weapon Model")]
    public GameObject DualArmBladeLeft;
    public GameObject DualArmBladeRight;

    [Header("Weapon Position")]
    public Transform DualArmBladeLeftDraw;
    public Transform DualArmBladeRightDraw;
    public Transform DualArmBladeLeftRest;
    public Transform DualArmBladeRightRest;

    void Start()
    {
        drawBladeTimeoutDelta = DrawBladeTimeout;
    }

    void Update()
    {
        GetAllInputs();

        deltaTime = Time.deltaTime;
        if (drawBladeTimeoutDelta > 0.0f) drawBladeTimeoutDelta -= deltaTime;

        if ((drawBladeTimeoutDelta <= 0.0f) && testButton)
        {
            DrawDualArmBlade();
        }

        // 进行攻击，点击攻击键开始攻击时，锁上攻击，避免玩家一直按着攻击键进行攻击
        if (!attackLock && lightAttack)
        {
            attackLock = true;
            LightAttack();
        }
        else if (!attackLock && heavyAttack)
        {
            attackLock = true;
            HeavyAttack();
        }
        else if (attackLock && unlockAttack)
        {
            attackLock = false;
        }
    }

    void GetAllInputs()
    {
        lightAttack = playerInputManager.lightAttack;
        heavyAttack = playerInputManager.heavyAttack;
        testButton = playerInputManager.testButton;
    }

    void LightAttack()
    {
        if (currentWeapon == Weapon.BareHand)
        {
            
        }
    }

    void HeavyAttack()
    {

    }

    void DrawDualArmBlade()
    {
        if (!dualArmBladeDrawn)
        {
            DualArmBladeLeft.transform.localPosition = DualArmBladeLeftDraw.localPosition;
            DualArmBladeRight.transform.localPosition = DualArmBladeRightDraw.localPosition;
            drawBladeTimeoutDelta = DrawBladeTimeout;
            dualArmBladeDrawn = true;
        }
        else if (dualArmBladeDrawn)
        {
            DualArmBladeLeft.transform.localPosition = DualArmBladeLeftRest.localPosition;
            DualArmBladeRight.transform.localPosition = DualArmBladeRightRest.localPosition;
            drawBladeTimeoutDelta = DrawBladeTimeout;
            dualArmBladeDrawn = false;
        }
    }

    // 用于某些场景，比如过场动画时，强制收回臂刃
    void UnequipDualArmBlade()
    {
        DualArmBladeLeft.transform.localPosition = DualArmBladeLeftRest.localPosition;
        DualArmBladeRight.transform.localPosition = DualArmBladeRightRest.localPosition;
    }

    enum Weapon
    {
        BareHand,
        DualArmBlade,
        ElectroMagneticRifle,
    }
}
