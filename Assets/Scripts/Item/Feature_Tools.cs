using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    Hoe,
    Axe,
    WateringCan
}

[CreateAssetMenu(menuName = "Game/Feature/Tool")]
[SerializeField]
public class Feature_Tools : ItemFeature, IHoldInteract
{
    public List<ToolType> ToolTypes = new();
    public void OnHoldInteract(HoldInteractContext context)
    {
        WorldState.Instance.ItemInteract(context.InteractGrid, ToolTypes);
    }
}
