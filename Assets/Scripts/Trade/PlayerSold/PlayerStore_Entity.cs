using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 单列
[System.Serializable]
public class QueueSlot
{
    public Transform frontPoint;
    public Vector2 backDirection = Vector2.down;
    public float spacing = 1.2f;
    public List<CustomerController> customers = new();
}

public class PlayerStore_Entity : ItemContainer_Base, IEntityRuntime, IEntityInteractable, ISaveableEntity
{
    public ItemContainer shelfContainer;
    public ItemContainer saleContainer;

    public Transform storePanel;

    public int EntityId { get; private set; }
    public Vector3Int PivotPos { get; private set; }
    public List<GameObject> RelativeObj { get; private set; }
    public void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;

        RefreshActiveSlots();
        RefreshAllQueueTargets();
    }


    [Header("摊位数据")]
    [SerializeField] private int level = 1;

    [Header("排队点")]
    [SerializeField] private List<QueueSlot> allQueueSlots = new();
    private readonly List<QueueSlot> activeQueueSlots = new();

    [Header("展示位数据")]
    [SerializeField] private SaleItemSlot[] saleItemSlots;

    protected override void Awake()
    {
        base.Awake();

        shelfContainer = containers[0];
        saleContainer = containers[1];

        Refresh(shelfContainer);
        Refresh(saleContainer);

        InitSaleSlots();

        // 订阅玩家移动事件, 移动时关闭UI
        InputManager.OnMoveInput += HandleMove;
    }

    public void OnAwake() { }

    public void OnDestroy()
    {
        DisableSaleSlots();

        InputManager.OnMoveInput -= HandleMove;

        RuntimeRegisterUtility.UnregisterAll(this);
    }

    public void Start()
    {
        Vector3Int pivot = WorldState.Instance.WorldToCell(transform.position);
        WorldState.Instance.PlaceEntity(pivot, this as IEntityRuntime, 8, 2);
        RuntimeRegisterUtility.RegisterAll(this);
    }

    public void OnEntityInteract()
    {
        // 如果未摆摊时打开则打开 货架UI(背包往货架放东西)
        if (!storePanel.gameObject.activeInHierarchy)
            OpenStorePanel();
        else CloseStorePanel();
        
        // 如果摆摊时打开则只显示货架UI不显示背包
        // 可以暂时不加

    }

    public InteractPhase OnEntityInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }

    public void JoinQueue(CustomerController customer)
    {
        Debug.Log("Customer Joined");
        if (customer == null) return;
        if (IsInAnyQueue(customer)) return;

        QueueSlot slot = GetBestSlot();
        if (slot == null) return;

        slot.customers.Add(customer);

        RefreshQueueTargets(slot);
    }


    public void LeaveQueue(CustomerController customer)
    {
        foreach (var slot in activeQueueSlots)
        {
            if (slot.customers.Remove(customer))
            {
                RefreshQueueTargets(slot);
                return;
            }
        }
    }

    public int GetQueueIndex(CustomerController customer)
    {
        foreach (var slot in activeQueueSlots)
        {
            int index = slot.customers.IndexOf(customer);
            if (index >= 0)
                return index;
        }

        return -1;
    }


    // 摊位升级
    public void Upgrade()
    {
        level++;
        RefreshActiveSlots();
        RefreshAllQueueTargets();
    }

    // 加入最短的队
    private QueueSlot GetBestSlot()
    {
        if (activeQueueSlots.Count == 0)
        {
            Debug.LogError("No active queue slots in PlayerStore_Entity.");
            return null;
        }

        QueueSlot best = activeQueueSlots[0];

        // 目前每次找最短都会遍历一遍
        for(int i = 1; i < activeQueueSlots.Count; i++)
        {
            if (activeQueueSlots[i].customers.Count < best.customers.Count)
                best = activeQueueSlots[i];
        }

        return best;
    }
    private void RefreshActiveSlots()
    {
        activeQueueSlots.Clear();

        int count = GetSlotCountByLevel(level);

        for (int i = 0; i < count && i < allQueueSlots.Count; i++)
        {
            activeQueueSlots.Add(allQueueSlots[i]);
        }
    }
    private bool IsInAnyQueue(CustomerController customer)
    {
        foreach (var slot in activeQueueSlots)
        {
            if (slot.customers.Contains(customer))
                return true;
        }

        return false;
    }
    private void RefreshQueueTargets(QueueSlot slot)
    {
        for (int i = 0; i < slot.customers.Count; i++)
        {
            Vector2 targetPos =
                (Vector2)slot.frontPoint.position +
                slot.backDirection.normalized * slot.spacing * i;

            Debug.Log(targetPos);
            slot.customers[i].SetQueueTarget(targetPos);
        }
    }

    private void RefreshAllQueueTargets()
    {
        foreach (var slot in activeQueueSlots)
        {
            RefreshQueueTargets(slot);
        }
    }

    public bool IsQueueFront(CustomerController customer)
    {
        foreach (var slot in activeQueueSlots)
        {
            if (slot.customers.Count > 0 && slot.customers[0] == customer)
            {
                //float dist = Vector2.Distance(slot.frontPoint.position, customer.transform.position);
                //if (dist <= 0.05f) return true;

                return true;
            }
        }

        return false;
    }

    // 摊位等级对应摊位展示位数
    private int GetSlotCountByLevel(int level)
    {
        return level switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => allQueueSlots.Count
        };
    }

    // 初始化摊位展示位
    private void InitSaleSlots()
    {
        for(int i = 0; i < saleItemSlots.Length; i++)
        {
            if (i >= saleContainer.SlotCount)
                break;

            if (saleItemSlots[i] == null)
                continue;

            saleItemSlots[i].Bind(saleContainer, i);

            saleItemSlots[i].OnSaleSlotInteracted -= HandleSaleSlotInteracted;
            saleItemSlots[i].OnSaleSlotInteracted += HandleSaleSlotInteracted;
        }
    }

    // 摊位展示位取消订阅事件
    private void DisableSaleSlots()
    {
        if (saleItemSlots == null)
            return;

        for (int i = 0; i < saleItemSlots.Length; i++)
        {
            if (saleItemSlots[i] == null)
                continue;

            saleItemSlots[i].OnSaleSlotInteracted -= HandleSaleSlotInteracted;
        }
    }

    private void HandleSaleSlotInteracted(int slotIndex)
    {
        if (slotIndex < 0)
            return;

        if (slotIndex >= saleContainer.SlotCount)
            return;

        if (slotIndex >= activeQueueSlots.Count)
            return;

        QueueSlot queueSlot = activeQueueSlots[slotIndex];

        if (queueSlot == null || queueSlot.customers == null || queueSlot.customers.Count == 0)
            return;

        CustomerController customer = queueSlot.customers[0];

        if (customer == null)
            return;

        ref var sourceStack = ref saleContainer.Items[slotIndex];

        if (sourceStack.IsEmpty)
            return;

        if (!customer.TryConsumeBuyItem(sourceStack))
            return;

        sourceStack.count--;

        ItemContainerEvents.OutsideChanged(saleContainer);
    }

    private void HandleMove(Vector2 vector2)
    {
        CloseStorePanel();
    }

    public void Load(EntitySaveData data)
    {
        throw new System.NotImplementedException();
    }
    public EntitySaveData Save()
    {
        throw new System.NotImplementedException();
    }

    // 在摊位互动时打开
    public void OpenStorePanel()
    {
        RefreshStorePanel();
        storePanel.gameObject.SetActive(true);

        PlayerInteractionMode.SetContainerPanelOpen(true);
    }

    public void RefreshStorePanel()
    {
        Refresh(shelfContainer);
        Refresh(saleContainer);
    }

    public void CloseStorePanel()
    {
        storePanel.gameObject.SetActive(false);

        PlayerInteractionMode.SetContainerPanelOpen(false);
    }
}
