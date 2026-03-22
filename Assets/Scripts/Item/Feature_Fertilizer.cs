using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feature_Fertilizer : ItemFeature, IHoldInteract
{

    public void OnHoldInteract(HoldInteractContext context)
    {
        Vector3Int interactGrid = context.InteractGrid;
        WorldState.Instance.GetCell(interactGrid, out bool hasDetail, out DetailedCellData detailedData);
        if (!hasDetail) { return; }

        int entityID = detailedData.EntityID;
        if (entityID == 0) { return; }

        if (WorldState.Instance.GetEntity(entityID) is Farmland_Entity farmland)
        {
            farmland.ApplyFertilizer(1);
            //TODO:尚未处理肥料等级和持续时间的逻辑
            int itemCount = context.backpackContainer.Items[context.containerIndex].count;
            if (itemCount > 0)
            {
                SlotController.Instance.SetItemCount(context.backpackContainer, context.containerIndex, itemCount - 1);
            }
        }

    }
}
