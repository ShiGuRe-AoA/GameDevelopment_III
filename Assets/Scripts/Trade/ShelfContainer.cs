using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfContainer : ItemContainer_Base
{
    [Header("Shelf Panel")]
    public Transform ShelfPanel;

    [SerializeField] private List<ItemSlotUI> saleSlots = new();    // sale - can be displayed to customer
    public int saleSlotCount;           
    public int enableInteractCount;     // enableInteract - saleSlots which can be used

    public bool IsOpen { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SlotController.Instance.RefreshAll(container);
        Utils.CollectComponentsInChildren<ItemSlotUI>(collectedParent, saleSlots, saleSlotCount);
    }

    private void Start()
    {
        // test
        // 测试
        SlotController.Instance.TryAddItem(10100, 36, container);
        Debug.Log($"DebugItemAddComplete");
        SlotController.Instance.TryAddItem(10100, 12, container);
        SlotController.Instance.TryAddItem(10100, 4, container);
        SlotController.Instance.TryAddItem(10101, 36, container);
        SlotController.Instance.TryAddItem(10102, 5, container);
        SlotController.Instance.TryAddItem(14001, 32, container);
        SlotController.Instance.TryAddItem("Hoe_1", 1, container);
        SlotController.Instance.TryAddItem("WateringCan_1", 1, container);

    }

    // 之后这些似乎需要到InputManager实现交互
    // 或者Input.GetButtonDown然后执行OpenShelf,再次就CloseShelf
    public void OpenShelf()
    {
        ShelfPanel.gameObject.SetActive(true);
        IsOpen = true;
    }

    public void CloseShelf()
    {
        ShelfPanel.gameObject.SetActive(false);
        IsOpen = false;
    }

    // 还得有个对外的UI显示, 大概类似于手持虚影那种
    // 就是在saleSlotCount内的为卖品, 对外展示

}
