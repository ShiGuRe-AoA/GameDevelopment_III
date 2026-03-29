using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfContainer : ItemContainer_Base
{
    [Header("Shelf Panel")]
    public Transform ShelfPanel;

    // Display to customer
    // will be deleted after debug
    // [SerializeField] private List<ItemSlotUI> _saleSlots = new(); 
    // public IReadOnlyList<ItemSlotUI> saleSlots { get; private set; }
    
    // 售卖槽位占货仓槽位的数量
    // 分为已解锁和未被解锁
    public int saleSlotCount;
    // 已解锁售卖槽位在升级时可被增加
    // 可能需要外部引用商店等级之类的
    public int enableInteractCount;


    public bool IsOpen { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SlotController.Instance.RefreshAll(container);
        //saleSlots = Utils.ReadOnly<ItemSlotUI>(UISlots, () => saleSlotCount);
        //RefreshSaleSlotsDebug();
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

    // Debug看saleSlots用，之后删
    //private void RefreshSaleSlotsDebug()
    //{
    //    _saleSlots.Clear();

    //    for (int i = 0; i < saleSlots.Count; i++)
    //    {
    //        _saleSlots.Add(saleSlots[i]);
    //    }
    //}

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
    
    // EnableSaleSlot感觉得每次打开的时候刷新一下看看是不是升级了
    public void EnableSaleSlot()
    {
        for(int i = 0; i < saleSlotCount; i++)
        {
            // 这里如果ItemSlotUI有更方便的函数需要改
            if (i < enableInteractCount)
                UISlots[i].Interactable = true;
            else
                UISlots[i].Interactable = false;
        }
    }

}
