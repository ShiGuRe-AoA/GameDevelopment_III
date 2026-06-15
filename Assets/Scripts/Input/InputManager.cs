using System;
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
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public readonly PlayerInputContext Context = new();

    public InputActionMain inputActions;

    private const int HotbarSize = 10;

    // ================================================================================
    // 事件（由外部消费者订阅，取代直接方法调用）
    // ================================================================================
    public static event Action<Vector2> OnMoveInput;
    public static event Action OnInteract;
    public static event Action OnEntityInteract;
    public static event Action<IHoverTarget> OnHoverChanged;
    public static event Action OnToggleBackpack;
    public static event Action OnTogglePause;
    public static event Action<int> OnHotbarSlotSelected; // 参数：0-based 快捷栏索引
    public static event Action<int> OnHotbarScroll;       // 参数：+1 = 上滚, -1 = 下滚
    public static event Action OnItemInteract;             // 使用手持物品（替换旧 Input.GetMouseButtonUp）


    // ================================================================================
    // 生命周期
    // ================================================================================
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
            inputActions.Main.EntityInteract.performed += OnEntityInteractPerformed;
            inputActions.Main.Pause.performed += OnPausePerformed;
            inputActions.Main.ItemInteract.performed += OnItemInteractPerformed;

            inputActions.Main.CompositeOperation.started += OnCompositeOperationStarted;
            inputActions.Main.CompositeOperation.canceled += OnCompositeOperationCanceled;
        }

        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        Context.MouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        DetectHoverChanged();

        // --- 快捷栏数字键 ---
        int idx = GetHotbarNumberKeyDown(HotbarSize);
        if (idx != -1)
            OnHotbarSlotSelected?.Invoke(idx);

        // --- 滚轮切换 ---
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
            OnHotbarScroll?.Invoke(-1);
        else if (scroll < 0f)
            OnHotbarScroll?.Invoke(1);
    }

    // ================================================================================
    // 玩家移动 → 事件
    // ================================================================================
    private void OnPlayerMoveCanceled(InputAction.CallbackContext obj)
    {
        Context.MoveInput = Vector2.zero;
        OnMoveInput?.Invoke(Vector2.zero);
    }

    private void OnPlayerMovePerformed(InputAction.CallbackContext obj)
    {
        Context.MoveInput = obj.ReadValue<Vector2>();
        OnMoveInput?.Invoke(Context.MoveInput);
    }

    // ================================================================================
    // 交互 → 事件
    // ================================================================================
    private void OnInteractPerformed(InputAction.CallbackContext obj)
    {
        OnInteract?.Invoke();
    }

    private void OnOpenBackpackPerformed(InputAction.CallbackContext obj)
    {
        OnToggleBackpack?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext obj)
    {
        OnTogglePause?.Invoke();
    }

    private void OnItemInteractPerformed(InputAction.CallbackContext obj)
    {
        OnItemInteract?.Invoke();
    }

    private void OnEntityInteractPerformed(InputAction.CallbackContext obj)
    {
        OnEntityInteract?.Invoke();
    }

    IHoverTarget currentHoverTarget;
    private void DetectHoverChanged()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(Context.MouseWorldPos);
        IHoverTarget newTarget = null;

        foreach(Collider2D hit in hits)
        {
            newTarget = hit.GetComponentInParent<IHoverTarget>();

            if (newTarget != null)
                break;
        }

        if (newTarget == currentHoverTarget)
            return;
        currentHoverTarget = newTarget;

        OnHoverChanged?.Invoke(currentHoverTarget);
    }

    // ================================================================================
    // 复合操作
    // ================================================================================
    private void OnCompositeOperationStarted(InputAction.CallbackContext obj)
    {
        Context.CompositeOperation = true;
    }

    private void OnCompositeOperationCanceled(InputAction.CallbackContext obj)
    {
        Context.CompositeOperation = false;
    }

    // ================================================================================
    // 工具方法
    // ================================================================================
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
