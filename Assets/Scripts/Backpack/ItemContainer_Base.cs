using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainer_Base : MonoBehaviour
{
    // Data
    protected List<ItemContainer> containers = new List<ItemContainer>();
    protected ItemContainer container => (containers.Count > 0) ? containers[0] : null;
    // Views
    protected List<ContainerView> views = new List<ContainerView>();
    protected ContainerView view => (views.Count > 0) ? views[0] : null;


    [SerializeField] protected List<List<ItemSlotUI>> UISlotsList = new();
    protected List<ItemSlotUI> UISlots => (UISlotsList.Count > 0) ? UISlotsList[0] : null;

    [SerializeField] protected List<Transform> collectedParents = new List<Transform>();
    protected Transform collectedParent => (collectedParents.Count > 0) ? collectedParents[0] : null;

    [SerializeField] protected List<bool> enableInteract = new List<bool>();

    protected virtual void Awake()
    {
        if (InitContainer())
        {
            for (int i = 0; i < containers.Count; i++) 
            {
                Refresh(i);
            }
        }
    }

    private bool InitContainer(bool force = false)
    {
        int num = collectedParents.Count;
        for (int i = 0; i < num; i++)
        {
            List<ItemSlotUI> thisUISlots = new List<ItemSlotUI>();
            UISlotsList.Add(thisUISlots);

            if (force) { thisUISlots.Clear(); }

            if(thisUISlots.Count <= 0)
            {
                if (collectedParents[i] == null)
                {
                    return false;
                }
                Utils.CollectComponentsInChildren<ItemSlotUI>(collectedParents[i], thisUISlots);
            }

            if (thisUISlots.Count <= 0)
            {
                return false;
            }

            ItemContainer newContainer = new ItemContainer(thisUISlots.Count, "backpackContainer");
            containers.Add(newContainer);

            ContainerView newView = new ContainerView(newContainer, thisUISlots);
            views.Add(newView);

        //Debug.Log($"Container Contains {num} Childs");
            for (int uiIndex = 0; uiIndex < newView.UISlotCount; uiIndex++)
            {
                newView.UISlots[uiIndex].Bind(newContainer, newView, uiIndex, this);
            }
            newContainer.RegistryView(view);
            newContainer.enableInteract = (i >= enableInteract.Count) ? false : enableInteract[i];

        }
        return true;
    }
    
    public void OnLeftClick(ItemContainer container, int containerIndex)
    {
        if (!container.enableInteract) { return; }
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
        if (!container.enableInteract) { return; }
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
        if (!container.enableInteract) { return; }
        if (!SlotController.Instance.holdingItem) { return; }
        SlotController.Instance.CollectAll(container);
    }

    public void SetColletParent(Transform collectParent, int containerID = 0)
    {
        collectedParents[containerID] = collectParent;
        if (InitContainer(true))
        {
            Refresh();
        }
    }



    public ItemContainer GetContainer(int containerID = 0)
    {
        return containers[containerID];
    }

    public int GetSlotCount(int containerID = 0)
    {
        if (containers[containerID] == null) { return 0; }
        return containers[containerID].SlotCount;
    }

    public void Refresh(int containerID = 0)
    {
        if (containers[containerID] == null) 
        {
            Debug.LogError($"Invalid ContainerID Detected");
            return; 
        }
        if (SlotController.Instance == null) { return; }
        SlotController.Instance.RefreshAll(containers[containerID]);
    }
}

