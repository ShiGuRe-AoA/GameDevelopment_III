using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct HoldInteractContext
{
    public Vector3 MousePos;
    public Vector3Int InteractGrid;
    public bool isValid;
    public ItemContainer backpackContainer;
    public PlayerController playerController;
    public int ItemID;
    public int containerIndex;
}
public interface IHoldInteract
{
    public void OnHoldInteract(HoldInteractContext context);
}
