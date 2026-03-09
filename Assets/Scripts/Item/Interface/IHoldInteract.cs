using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct HoldInteractContext
{
    public Vector3 MousePos;
    public bool isValid;
    public ItemContainer backpackContainer;
    public int ItemID;
    public int containerIndex;
}
public interface IHoldInteract
{
    public void OnHoldInteract(HoldInteractContext context);
}
