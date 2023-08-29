using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    protected int maintenance;
    protected int damage;
    protected int battery;

    private bool interact;
    private bool isInteracting;

    protected PlayerInputManager playerInputManager;

    void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
    }

    void Start()
    {
        maintenance = 100;
        damage = 10;
        battery = 100;

        interact = false;
    }

    void Update()
    {
        GetAllInputs();
        InteractTest();
    }

    void GetAllInputs()
    {
        interact = playerInputManager.interact;
    }

    void InteractTest()
    {
        if (interact)
        {
            Debug.Log("Interact.");
        }
    }

    public void SetMaintenance(int maintenanceDelta)
    {
        maintenance += maintenanceDelta;
    }
}
