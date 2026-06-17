using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemContainerEvents
{
    public static event Action<ItemContainer, int> OnContainer2OutsideChanged;
    public static event Action<ItemContainer> OnOutside2ContainerChanged;

    public static void ContainerChanged(ItemContainer container, int index)
    {
        OnContainer2OutsideChanged?.Invoke(container, index);
    }
    public static void OutsideChanged(ItemContainer container)
    {
        OnOutside2ContainerChanged?.Invoke(container);
    }
}
