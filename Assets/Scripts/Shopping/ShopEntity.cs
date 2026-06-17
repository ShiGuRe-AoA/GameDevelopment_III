using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店实体：玩家与之交互时打开商店面板。
/// 挂载在商店预制体上，需配置 ShopInventorySO 与面板引用。
/// </summary>
public class ShopEntity : MonoBehaviour, IEntityRuntime, IEntityInteractable
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
    // Unity 生命周期
    // --------------------------------------------------------------------------------
    private void Awake()
    {
        // 直接放在场景中的情况：自行注册
        if (EntityId == 0)
            RuntimeRegisterUtility.RegisterAll(this);

        //OnInteract();
        if (shopInventory == null)
            Debug.LogWarning($"[ShopEntity] {gameObject.name} 未配置 ShopInventorySO", this);
        if (shopPanel == null)
            Debug.LogWarning($"[ShopEntity] {gameObject.name} 未配置 shopPanel", this);
    }

    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
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
    // IInteractable
    // --------------------------------------------------------------------------------
    public void OnEntityInteract()
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

    public InteractPhase OnEntityInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }
}