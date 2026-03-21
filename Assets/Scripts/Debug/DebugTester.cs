using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTester : MonoBehaviour
{
    public Vector3Int Spawnpoint;
    void Start()
    {
        //WorldState.Instance.PlaceEntity(Spawnpoint, 10102);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;   
        Gizmos.DrawWireSphere(WorldState.Instance.CellToWorld(Spawnpoint),0.5f);
    }
}
