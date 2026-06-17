using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerContext
{
    public CustomerController CustomerController;
    // 可被吸引的目标
    public PlayerStore_Entity StoreEntity;
    // 真正要去的目标
    public PlayerStore_Entity TargetEntity;
    public Vector2 QueueTargetPos;
    public bool HasQueueTarget;
    public ItemStack BuyItem;
    public int Price;
    public int Count;
}
