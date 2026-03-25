using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public int pickupRange { get; private set; } = 5;   

    public ItemContainer backpackContainer { get; private set; }

    private ItemStack itemStack;
    public void Init(ItemStack itemStack)
    {
        this.itemStack = itemStack;
    }
    public void TryPlayerPickup()
    {
        if (WorldState.Instance.PlayerDist(transform.position) < pickupRange)
        {
            SlotController.Instance.TryAddItem(itemStack,backpackContainer);
            Destroy(gameObject);
        }
    }
}
