using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemContainerEvents
{
    public static event Action<ItemContainer, int> OnSlotChanged;

    public static void SlotChanged(ItemContainer container, int index)
    {
        OnSlotChanged?.Invoke(container, index);
    }
}
