using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public InputActionMain inputActions;
    public PlayerController playerController;
    public BackpackContainer backpackContainer;

    public int hotbarSize = 10; // 1~0 = 10 格（0 通常映射第10格）
    public int selectedIndex = 0;
    public InventoryContainer inventoryContainer;

    public bool CompositeOperation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 如果你希望切场景后也保留，再取消注释
        // DontDestroyOnLoad(gameObject);

        if (inputActions == null)
        {
            inputActions = new InputActionMain();

            inputActions.Main.PlayerMove.performed += PlayerMove_performed;
            inputActions.Main.PlayerMove.canceled += PlayerMove_canceled;

            inputActions.Main.OpenBackpack.performed += OpenBackpack_performed;
            inputActions.Main.Interact.performed += Interact_performed;
            inputActions.Main.Pause.performed += Pause_performed;

            inputActions.Main.CompositeOperation.started += CompositeOperation_started;
            inputActions.Main.CompositeOperation.canceled += CompositeOperation_canceled;
        }

        inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    public void Init()
    {
        CompositeOperation = false;
    }

    //============================================================================================
    // 玩家移动
    //============================================================================================
    private void PlayerMove_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (playerController != null)
            playerController.SetMoveInput(Vector2.zero);
    }

    private void PlayerMove_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (playerController != null)
            playerController.SetMoveInput(obj.ReadValue<Vector2>());
    }

    //============================================================================================
    // 背包交互
    //============================================================================================
    private void OpenBackpack_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Opening Backpack");

        if (backpackContainer == null) return;

        if (backpackContainer.IsOpen) { backpackContainer.CloseBackpack(); }
        else { backpackContainer.OpenBackpack(); }
    }

    //============================================================================================
    // 鼠标点击交互
    //============================================================================================
    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Player Interact");
    }

    //============================================================================================
    // 暂停
    //============================================================================================
    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("游戏暂停");

        if (TimeManager.Instance == null) return;

        if (TimeManager.Instance.IsPause) { TimeManager.Instance.StartGame(); }
        else { TimeManager.Instance.PauseGame(); }
    }

    //============================================================================================
    // 复合操作
    //============================================================================================
    private void CompositeOperation_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        CompositeOperation = true;
    }

    private void CompositeOperation_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        CompositeOperation = false;
    }

    private void Update()
    {
        if (inventoryContainer == null) return;

        int idx = GetHotbarNumberKeyDown(hotbarSize);
        if (idx != -1)
        {
            Debug.Log($"InputDetected:{idx}");
            inventoryContainer.SetCurrentSlot(idx);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
        {
            inventoryContainer.PreviousSlot();
        }
        else if (scroll < 0f)
        {
            inventoryContainer.NextSlot();
        }
    }

    // 返回 0..hotbarSize-1；没按返回 -1
    private static int GetHotbarNumberKeyDown(int hotbarSize)
    {
        int max9 = Mathf.Min(9, hotbarSize);
        for (int n = 1; n <= max9; n++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + n)))
                return n - 1;
        }

        if (hotbarSize >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
            return 9;

        return -1;
    }
}