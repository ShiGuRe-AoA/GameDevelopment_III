using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public int pickupRange { get; private set; } = 5;   

    private ItemBase_SO itemData;
    private int count;
    public void Init(ItemBase_SO itemData, int num)
    {
        this.itemData = itemData;
    }
    public void TryPlayerPickup()
    {
        if (WorldState.Instance.PlayerDist(transform.position) < pickupRange)
        {

        }
    }
}
