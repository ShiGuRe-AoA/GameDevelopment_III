using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementPhantom : MonoBehaviour
{
    private ItemBase_SO itemData;
    private Feature_Placement placementFeature;
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
    public void Init(int itemID)
    {
        itemData = ItemRegistry.Get(itemID);
        placementFeature = itemData.GetFeature<Feature_Placement>();
        if (placementFeature == null) return;
    }
}
