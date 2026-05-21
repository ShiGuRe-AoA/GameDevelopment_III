using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree_Entity : EntityRuntimeBase
{
    private const int maxHealth = 10;
    private const int spawnID = 10102;
    private const int spawnAmount = 15;

    public int curHealth = 10;
    public void Logging()
    {
        curHealth -= 1;

        if (curHealth <= 0)
        {
            FallingDown();
        }
    }

    public InteractPhase OnInteractDetected()
    {
        return InteractPhase.Logging;
    }

    public override void OnAwake()
    {
        base.OnAwake();
        curHealth = maxHealth;
    }

    public void FallingDown()
    {
        WorldState.Instance.SpawnItem(PivotPos, spawnID, spawnAmount, 1.5f);
        WorldState.Instance.DestroyEntity(EntityId);
    }
}
