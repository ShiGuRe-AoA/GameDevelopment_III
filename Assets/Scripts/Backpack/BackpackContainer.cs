using System;
using System.Collections.Generic;
using UnityEngine;




public class BackpackContainer : ItemContainer_Base
{
    [Header("Backpack Panel")]
    public Transform BackpackPanel;

    [Header("Inventory Slots")]
    public InventoryContainer inventoryContainer;

    public bool IsOpen {  get; private set; }

    protected override void Awake()
    {
        base.Awake();
        inventoryContainer.Init(container);
        SlotController.Instance.RefreshAll(container);
    }

    private void Start()
    {
        // ≤‚ ‘
        SlotController.Instance.TryAddItem(10100, 36, container);
        SlotController.Instance.TryAddItem(10100, 12, container);
        SlotController.Instance.TryAddItem(10100, 4, container);
        SlotController.Instance.TryAddItem(10101, 36, container);
        SlotController.Instance.TryAddItem(10102, 5, container);
    }
    public void OpenBackpack()
    {
        BackpackPanel.gameObject.SetActive(true);
        IsOpen = true;
    }  
    public void CloseBackpack() 
    {
        BackpackPanel.gameObject.SetActive(false);
        IsOpen = false;
    }
}