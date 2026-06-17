using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店实体：玩家与之交互时打开商店面板。
/// 挂载在商店预制体上，需配置 ShopInventorySO、面板引用以及 Collider2D（用于悬停检测）。
///
/// 交互方式：
///   鼠标右击  → 经过悬停检测 → IWorldObject → State_Interact → 打开/关闭面板
///   F键       → 若同一格有实体 → State_EntityInteract → 打开/关闭面板
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopEntity : MonoBehaviour, IEntityRuntime, IWorldObject, IHoverTarget, IEntityInteractable
{
    [Header("商店配置")]
    [SerializeField] private ShopInventorySO shopInventory;

    [Header("UI 面板")]
    [SerializeField] private GameObject shopPanel;

    // --------------------------------------------------------------------------------
    // IEntityRuntime
    // --------------------------------------------------------------------------------
    public int EntityId { get; private set; }
    public Vector3Int PivotPos { get; private set; }
    public List<GameObject> RelativeObj { get; private set; }

    // --------------------------------------------------------------------------------
    // IWorldObject
    // --------------------------------------------------------------------------------
    public int ObjectId { get; private set; }
    public Vector3 WorldPos => transform.position;

    public void ObjectInit(int objectId, Vector3 worldPos, WorldState worldState)
    {
        ObjectId = objectId;
    }

    // --------------------------------------------------------------------------------
    // Unity 生命周期
    // --------------------------------------------------------------------------------
    private void Awake()
    {
        // 确保 Collider2D 为 Trigger（用于悬停检测）
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        // 注册到 RuntimeRegister 和 WorldObject 系统
        RuntimeRegisterUtility.RegisterAll(this);
        WorldState.Instance?.RegisterWorldObject(this);

        if (EntityId == 0 && WorldState.Instance != null)
        {
            // 同时注册为格子实体（若放置在 Tilemap 上）
            Vector3Int cellPos = WorldState.Instance.WorldToCell(transform.position);
            WorldState.Instance.PlaceEntity(cellPos, this, 1, 1);
        }

        if (shopInventory == null)
            Debug.LogWarning($"[ShopEntity] {gameObject.name} 未配置 ShopInventorySO", this);
        if (shopPanel == null)
            Debug.LogWarning($"[ShopEntity] {gameObject.name} 未配置 shopPanel", this);
    }

    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
        WorldState.Instance?.UnRegisterWorldObject(this);
        // 关闭面板避免残留
        if (shopPanel != null && shopPanel.TryGetComponent<ShopPanelController>(out var ctrl))
            ctrl.Close();
    }

    // --------------------------------------------------------------------------------
    // IEntityRuntime
    // --------------------------------------------------------------------------------
    public void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }

    public void OnAwake()
    {
        // WorldState 注册实体时回调（若已通过 Awake 注册则跳过）
        if (EntityId == 0) return;
        RuntimeRegisterUtility.RegisterAll(this);
    }

    // --------------------------------------------------------------------------------
    // IEntityInteractable — F 键 / 右击 → 打开面板
    // --------------------------------------------------------------------------------
    public void OnEntityInteract()
    {
        TogglePanel();
    }

    public InteractPhase OnEntityInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }

    // --------------------------------------------------------------------------------
    // IHoverTarget — 悬停检测
    // --------------------------------------------------------------------------------
    public void OnHoverEnter() { }
    public void OnHoverExit() { }

    // --------------------------------------------------------------------------------
    // 面板开关
    // --------------------------------------------------------------------------------
    private void TogglePanel()
    {
        if (shopInventory == null)
        {
            Debug.LogError($"[ShopEntity] {gameObject.name} 无法打开：未配置 ShopInventorySO");
            return;
        }
        if (shopPanel == null)
        {
            Debug.LogError($"[ShopEntity] {gameObject.name} 无法打开：未配置 shopPanel");
            return;
        }

        ShopPanelController controller = shopPanel.GetComponent<ShopPanelController>();
        if (controller == null)
        {
            Debug.LogError($"[ShopEntity] {gameObject.name} shopPanel 上未找到 ShopPanelController", shopPanel);
            return;
        }

        if (!shopPanel.activeSelf)
            controller.Open(shopInventory);
        else
            controller.Close();
    }
}
