using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 摊位上放物品的格子
public class SaleItemSlot : MonoBehaviour, IInteractable, IWorldObject
{
    #region IWorldObject
    public int ObjectId { get; private set; }
    public Vector3 WorldPos { get; private set; }

    public void ObjectInit(int objectId, Vector3 worldPos, WorldState worldState)
    {
        ObjectId = objectId;
        WorldPos = worldPos;
    }

    public void OnAwake() { }
    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
    }

    #endregion

    public event Action<int> OnSaleSlotInteracted;

    [SerializeField] private SpriteRenderer _sr;
    public SpriteRenderer Sr { get => _sr; }

    private ItemContainer sourceContainer;
    private int sourceIndex = -1;

    // 对应 saleContainer 第几格
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
        ItemContainerEvents.OnContainer2OutsideChanged += RefreshSaleSlot;
    }

    private void OnDisable()
    {
        ItemContainerEvents.OnContainer2OutsideChanged -= RefreshSaleSlot;
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 world = transform.position;
        RuntimeRegisterUtility.RegisterAll(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void RefreshSaleSlot(ItemContainer changedContainer, int changedIndex)
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
        
        ref var sourceStack = ref sourceContainer.Items[sourceIndex];

        if (sourceStack.IsEmpty) return;

        //else
        //{
        //    sourceStack.count--;
        //    ItemContainerEvents.OutsideChanged(sourceContainer);
        //} 

        OnSaleSlotInteracted?.Invoke(sourceIndex);

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
