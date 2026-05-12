using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feature_Fertilizer : ItemFeature, IHoldInteract
{

    public void OnHoldInteract(HoldInteractContext context)
    {
        Vector3Int interactGrid = context.InteractGrid;
        WorldState.Instance.GetCell(interactGrid, out bool hasDetail, out DetailedCellData detailedData);
        if (!hasDetail || detailedData.CheckEmpty()) { return; }

        if (WorldState.Instance.TryGetEntityOnCell(interactGrid, out Farmland_Entity farmland))
        {
            farmland.ApplyFertilizer(1);
            WorldState.Instance.RefreshDetailedState(interactGrid);
            int itemCount = context.backpackContainer.Items[context.containerIndex].count;
            if (itemCount > 0)
            {
                SlotController.Instance.SetItemCount(context.backpackContainer, context.containerIndex, itemCount - 1);
            }
        }
    }
}

