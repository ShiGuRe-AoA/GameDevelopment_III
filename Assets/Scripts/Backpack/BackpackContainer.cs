using System;
using System.Collections.Generic;
using UnityEngine;




public class BackpackContainer : MonoBehaviour
{
    [Header("Backpack UI Slots")]
    public Transform SlotsParent;
    private List<ItemSlotUI> BackpackUISlots = new();

    [Header("Backpack Panel")]
    public Transform BackpackPanel;
    // Data
    private ItemContainer backpackContainer;
    // Views
    private ContainerView backpackView;

    [Header("Inventory Slots")]
    public InventoryContainer inventoryContainer;


    private void Awake()
    {
        LoadItemSlots(SlotsParent);
        InitializeBackpack();
        inventoryContainer.Init(backpackContainer);
        SlotController.Instance.RefreshAll(backpackContainer);
    }

    private void Start()
    {
        // ²âÊÔ
        SlotController.Instance.TryAddItem(10100, 36, backpackContainer);
        SlotController.Instance.TryAddItem(10100, 12, backpackContainer);
        SlotController.Instance.TryAddItem(10100, 4, backpackContainer);
        SlotController.Instance.TryAddItem(10101, 36, backpackContainer);
        SlotController.Instance.TryAddItem(10102, 5, backpackContainer);
    }

    public void InitializeBackpack()
    {
        backpackContainer = new ItemContainer(BackpackUISlots.Count,"backpackContainer");

        backpackView = new ContainerView(backpackContainer, BackpackUISlots);

        for (int uiIndex = 0; uiIndex < backpackView.UISlotCount; uiIndex++)
        {
            backpackView.UISlots[uiIndex].Bind(backpackContainer, backpackView, uiIndex, this);
        }
        backpackContainer.RegistryView(backpackView);
    }
    

    public void LoadItemSlots(Transform parent)
    {
        foreach(Transform child in parent)
        {
            BackpackUISlots.Add(child.GetComponentInChildren<ItemSlotUI>());
        }
    }

    public void OpenBackpackUI() => BackpackPanel.gameObject.SetActive(true);
    public void CloseBackpackUI() => BackpackPanel.gameObject.SetActive(false);

    public void OnLeftClick(ItemContainer container, int containerIndex)
    {
        if (!SlotController.Instance.holdingItem)
        {
            SlotController.Instance.PickUpAll(container, containerIndex);
        }
        else 
        { 
            SlotController.Instance.PlaceAllOrSwap(container, containerIndex); 
        }
    }

    public void OnRightClick(ItemContainer container, int containerIndex)
    {
        if (!SlotController.Instance.holdingItem)
        {
            SlotController.Instance.PickUpHalf(container, containerIndex);
        }
        else
        {
            SlotController.Instance.PlaceOne(container, containerIndex);
        }
    }

    public void OnDoubleLeftClick(ItemContainer container, int containerIndex)
    {
        if (!SlotController.Instance.holdingItem) { return; }
        Debug.Log("¼ì²âµ½Ë«»÷");
        SlotController.Instance.CollectAll(container);
    }
}