using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaleItem : MonoBehaviour
{
    public SpriteRenderer itemSr;
    private Sprite oldItemSprite;   // 存未更改时的sprite

    //public ItemContainer saleContainer;

    public int currentSlot;

    //public ItemStack CurrentStack => saleContainer.Items[currentSlot];
    //public ItemBase_SO CurrentItem
    //{
    //    get
    //    {
    //        var stack = CurrentStack;
    //        return ItemRegistry.Get(stack.itemId);
    //    }
    //}

    private void Awake()
    {
        itemSr = GetComponent<SpriteRenderer>();
        if(itemSr == null)
        {
            Debug.LogError("Sprite Renderer on Sale Item not found", this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
