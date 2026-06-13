using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputContext
{
    public Vector2 MoveInput;
    public bool InteractPressed;
    public bool OpenBackpackPressed;
    public bool PausePressed;
    public bool CompositeOperation;

    /// <summary>当前帧鼠标世界坐标（由 InputManager 每帧更新）</summary>
    public Vector3 MouseWorldPos;

    /// <summary>当前帧鼠标左键松开（由 InputManager 每帧更新）</summary>
    public bool HoldInteractUpThisFrame;
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public readonly PlayerInputContext Context = new();

    public InputActionMain inputActions;
    public PlayerController playerController;
    public BackpackContainer backpackContainer;
    public InventoryContainer inventoryContainer;

    private const int HotbarSize = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (inputActions == null)
        {
            inputActions = new InputActionMain();

            inputActions.Main.PlayerMove.performed += OnPlayerMovePerformed;
            inputActions.Main.PlayerMove.canceled += OnPlayerMoveCanceled;

            inputActions.Main.OpenBackpack.performed += OnOpenBackpackPerformed;
            inputActions.Main.Interact.performed += OnInteractPerformed;
            inputActions.Main.Pause.performed += OnPausePerformed;

            inputActions.Main.CompositeOperation.started += OnCompositeOperationStarted;
            inputActions.Main.CompositeOperation.canceled += OnCompositeOperationCanceled;
        }

        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    //============================================================================================
    // 玩家移动
    //============================================================================================
    private void OnPlayerMoveCanceled(InputAction.CallbackContext obj)
    {
        Context.MoveInput = Vector2.zero;
        playerController.SetInputInfo(Vector2.zero);
    }

    private void OnPlayerMovePerformed(InputAction.CallbackContext obj)
    {
        Context.MoveInput = obj.ReadValue<Vector2>();
        playerController.SetInputInfo(Context.MoveInput);
    }

    //============================================================================================
    // 背包交互
    //============================================================================================
    private void OnOpenBackpackPerformed(InputAction.CallbackContext obj)
    {
        if (backpackContainer == null) return;

        if (backpackContainer.IsOpen)
            backpackContainer.CloseBackpack();
        else
            backpackContainer.OpenBackpack();
    }

    //============================================================================================
    // 鼠标点击交互
    //============================================================================================
    private void OnInteractPerformed(InputAction.CallbackContext obj)
    {
        playerController.SimpleInteract();
    }

    //============================================================================================
    // 暂停
    //============================================================================================
    private void OnPausePerformed(InputAction.CallbackContext obj)
    {
        if (TimeManager.Instance == null) return;

        if (TimeManager.Instance.IsPause)
            TimeManager.Instance.StartGame();
        else
            TimeManager.Instance.PauseGame();
    }

    //============================================================================================
    // 复合操作
    //============================================================================================
    private void OnCompositeOperationStarted(InputAction.CallbackContext obj)
    {
        Context.CompositeOperation = true;
    }

    private void OnCompositeOperationCanceled(InputAction.CallbackContext obj)
    {
        Context.CompositeOperation = false;
    }

    private void Update()
    {
        // --- 鼠标输入采集（所有模块统一从此读取） ---
        Context.MouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Context.HoldInteractUpThisFrame = Input.GetMouseButtonUp(0);

        if (inventoryContainer == null) return;

        int idx = GetHotbarNumberKeyDown(HotbarSize);
        if (idx != -1)
        {
            inventoryContainer.SetCurrentSlot(idx);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
            inventoryContainer.PreviousSlot();
        else if (scroll < 0f)
            inventoryContainer.NextSlot();
    }

    /// <summary>返回 0..hotbarSize-1；没按返回 -1</summary>
    private static int GetHotbarNumberKeyDown(int hotbarSize)
    {
        int max = Mathf.Min(9, hotbarSize);
        for (int n = 1; n <= max; n++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + n)))
                return n - 1;
        }

        if (hotbarSize >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
            return 9;

        return -1;
    }
}
