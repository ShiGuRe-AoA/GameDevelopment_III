using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public int pickupRange { get; private set; } = 5;   

    public ItemContainer backpackContainer { get; private set; }
    private SpriteRenderer spriteRenderer;

    private ItemStack itemStack;
    public void Init(ItemStack itemStack)
    {
        this.itemStack = itemStack;
        spriteRenderer = GetComponent<SpriteRenderer>();

        var def = ItemRegistry.Get(itemStack.itemId);
        spriteRenderer.sprite = def.ItemSprite;
        backpackContainer = WorldState.Instance.backpackContainer;

    }
    private void Start()
    {
        InvokeRepeating(nameof(TryPlayerPickup), 1f, 1f);
    }
    public void TryPlayerPickup()
    {
        if (WorldState.Instance.PlayerDist(transform.position) < pickupRange)
        {
            if(SlotController.Instance.TryAddItem(itemStack, backpackContainer))
            {
                transform.DOMove(WorldState.Instance.PlayerPos(), 0.5f).OnComplete(() => { Destroy(gameObject); });
            }
        }
    }
}
