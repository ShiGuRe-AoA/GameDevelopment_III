using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaleItem : MonoBehaviour
{

    public SpriteRenderer saleItemSprite;

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
        saleItemSprite = GetComponent<SpriteRenderer>();
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
