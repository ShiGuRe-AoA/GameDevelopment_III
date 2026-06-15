using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 摊位上放物品的格子
public class SaleItemSlot : MonoBehaviour, IInteractable, IEntityRuntime
{
    #region IEntityRuntime
    public int EntityId { get; private set; }
    public Vector3Int PivotPos { get; private set; }
    public List<GameObject> RelativeObj { get; private set; }
    public void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }

    public void OnAwake() { }
    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
    }

    #endregion

    [SerializeField] private SpriteRenderer _sr;
    public SpriteRenderer Sr { get => _sr; }

    private ItemContainer sourceContainer;
    private int sourceIndex = -1;

    public void Bind(ItemContainer container, int index)
    {
        sourceContainer = container;
        sourceIndex = index;

        RefreshFromSource();
    }


    private void Awake()
    {
        if(_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            
            if(_sr == null)
                Debug.LogError("Sprite Renderer on Sale Item not found", this);
        }
    }

    private void OnEnable()
    {
        ItemContainerEvents.OnSlotChanged += HandleSlotChanged;
    }

    private void OnDisable()
    {
        ItemContainerEvents.OnSlotChanged -= HandleSlotChanged;
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3Int pivot = WorldState.Instance.WorldToCell(transform.position);
        WorldState.Instance.PlaceEntity(pivot, this as IEntityRuntime, 1, 1);
        RuntimeRegisterUtility.RegisterAll(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void HandleSlotChanged(ItemContainer changedContainer, int changedIndex)
    {
        if (changedContainer != sourceContainer)
            return;

        if (changedIndex != sourceIndex)
            return;

        RefreshFromSource();
    }

    private void RefreshFromSource()
    {
        if (sourceContainer == null ||
            sourceIndex < 0 ||
            sourceIndex >= sourceContainer.SlotCount)
        {
            _sr.sprite = null;
            return;
        }

        ItemStack stack = sourceContainer.Items[sourceIndex];

        if (stack.IsEmpty)
        {
            _sr.sprite = null;
            return;
        }

        _sr.sprite = stack.GetSprite();
    }

    public void OnInteract()
    {
        Debug.Log("Sale Slot Interact");
        sourceContainer.Items[sourceIndex].count--;
        RefreshFromSource();
    }

    public InteractPhase OnInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }

    public void SetSprite(Sprite sprite)
    {
        _sr.sprite = sprite;
    }
}
