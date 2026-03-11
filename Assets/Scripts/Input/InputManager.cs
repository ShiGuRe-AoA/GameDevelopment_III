using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public InputActionMain inputActions;


    public int hotbarSize = 10; // 1~0 = 10 格（0 通常映射第10格）
    public int selectedIndex = 0;
    public InventoryContainer inventoryContainer;

    public Vector2 playerMoveInput;

    private void Awake()
    {
        if(inputActions == null)
        {
            inputActions = new InputActionMain();
            inputActions.Main.PlayerMove.performed += PlayerMove_performed;
            inputActions.Main.PlayerMove.canceled += PlayerMove_canceled;

            inputActions.Main.OpenBackpack.performed += OpenBackpack_performed;

            inputActions.Main.Interact.performed += Interact_performed;

            inputActions.Main.Pause.performed += Pause_performed;
        }
    }


    public void Init()
    {
        playerMoveInput = Vector2.zero;
    }


    //============================================================================================
    //玩家移动
    //============================================================================================
    private void PlayerMove_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        playerMoveInput = Vector2.zero;
    }

    private void PlayerMove_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        playerMoveInput = obj.ReadValue<Vector2>();
    }

    //============================================================================================
    //背包交互
    //============================================================================================
    private void OpenBackpack_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Opening Backpack");
    }

    //============================================================================================
    //鼠标点击交互
    //============================================================================================
    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("Player Interact");
    }

    //============================================================================================
    //暂停
    //============================================================================================
    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Debug.Log("游戏暂停");
    }

    void Update()
    {
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
    static int GetHotbarNumberKeyDown(int hotbarSize)
    {
        // 1..9 -> 0..8
        int max9 = Mathf.Min(9, hotbarSize);
        for (int n = 1; n <= max9; n++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + n)))
                return n - 1;
        }

        // 0 -> 第10格（索引 9）
        if (hotbarSize >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
            return 9;

        return -1;
    }
}
