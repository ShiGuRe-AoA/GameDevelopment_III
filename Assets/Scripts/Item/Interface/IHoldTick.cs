using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct HoldTickContext
{
    public Vector3 MousePos;
    public GameObject PlacementInstance;
    public List<GameObject> CellInstance;
    public Material PhantomMat_G;
    public Material PhantomMat_R;
}
public interface IHoldTick
{
    public void OnHoldTick(HoldTickContext context, out bool isValid);
}
