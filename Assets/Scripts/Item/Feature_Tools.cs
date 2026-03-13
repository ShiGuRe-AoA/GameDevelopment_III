using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    Hoe,
    Axe
}
[CreateAssetMenu(menuName = "Game/Feature/Tool")]
[SerializeField]
public class Feature_Tools : ItemFeature, IHoldInteract
{
    public void OnHoldInteract(HoldInteractContext context)
    {
        
    }
}
