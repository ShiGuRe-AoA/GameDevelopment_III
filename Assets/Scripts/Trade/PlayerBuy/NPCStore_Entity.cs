using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCStore_Entity : ItemContainer_Base, IEntityRuntime, IInteractable, ISaveableEntity
{
    public Transform storePanel;

    public int EntityId { get; private set; }
    public Vector3Int PivotPos { get; private set; }
    public List<GameObject> RelativeObj {  get; private set; }

    public void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void OnAwake() { }

    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
    }

    public void OnInteract()
    {
        if (!storePanel.gameObject.activeInHierarchy)
            OpenStorePanel();
        else CloseStorePanel();
    }

    public InteractPhase OnInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }

    public void Load(EntitySaveData data)
    {
        throw new System.NotImplementedException();
    }

    public EntitySaveData Save()
    {
        throw new System.NotImplementedException();
    }

    public void OpenStorePanel()
    {
        storePanel.gameObject.SetActive(true);
        for (int i = 0; i < containers.Count; i++)
            Refresh(i);
    }

    public void CloseStorePanel()
    {
        storePanel.gameObject.SetActive(false); 
    }
}
