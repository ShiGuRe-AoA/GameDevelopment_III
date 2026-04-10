using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainer_Base : MonoBehaviour
{
    // Data
    protected ItemContainer container;
    // Views
    protected ContainerView view;

    [SerializeField] protected List<ItemSlotUI> UISlots = new();
    [SerializeField] protected Transform collectedParent;

    [SerializeField] protected bool enableInteract = true;

    protected virtual void Awake()
    {
        InitContainer();
        Refresh();
    }

    private void InitContainer(bool force = false)
    {
        if (force) { UISlots.Clear(); }
        if(UISlots.Count <= 0)
        {
            if (collectedParent == null)
            {
                return;
            }
            Utils.CollectComponentsInChildren<ItemSlotUI>(collectedParent, UISlots);
        }
        container = new ItemContainer(UISlots.Count, "backpackContainer");

        view = new ContainerView(container, UISlots);

        for (int uiIndex = 0; uiIndex < view.UISlotCount; uiIndex++)
        {
            view.UISlots[uiIndex].Bind(container, view, uiIndex, this);
        }
        container.RegistryView(view);
    }
    
    public void OnLeftClick(ItemContainer container, int containerIndex)
    {
        if (!enableInteract) { return; }
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
        if (!enableInteract) { return; }
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
        if (!enableInteract) { return; }
        if (!SlotController.Instance.holdingItem) { return; }
        SlotController.Instance.CollectAll(container);
    }

    public void SetColletParent(Transform collectParent)
    {
        this.collectedParent = collectParent;
        InitContainer(true);
    }

    public ItemContainer GetContainer()
    {
        return container;
    }

    public int GetSlotCount()
    {
        return container.SlotCount;
    }

    public void Refresh()
    {
        SlotController.Instance.RefreshAll(container);
    }
}
