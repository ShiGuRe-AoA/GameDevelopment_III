using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct EnterSelectContext
{
    public GameObject placementPrefab;
    public GameObject cellPrefab;
}
public interface IEnterSelect
{
    public void EnterSelect(EnterSelectContext context, out GameObject prefabInstance, out List<GameObject> cellInstance);
}
