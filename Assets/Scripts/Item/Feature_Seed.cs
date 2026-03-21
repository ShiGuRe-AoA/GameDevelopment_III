using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game/Feature/Seed")]
public class Feature_Seed : ItemFeature, IHoldInteract
{
    public List<TileBase> SeedTiles;
    public ItemBase_SO Product;
    public EntityRuntimeKind CropRuntimeKind;
    
    public void OnHoldInteract(HoldInteractContext context)
    {
        Vector3Int interactGrid = context.InteractGrid;
        int itemCount = context.backpackContainer.Items[context.containerIndex].count;
        if(itemCount <= 0) { return; }

        WorldState.Instance.GetCell(interactGrid, out bool hasDetail, out DetailedCellData detailedData);
        if(!hasDetail || detailedData.EntityID == 0)
        {
            return;
        }
        int entityID = detailedData.EntityID;
        EntityRuntime entityRuntime = WorldState.Instance.GetEntity(entityID);
        if (entityRuntime is Farmland_Entity farmLand)
        {
            if (!farmLand.CanPlant(parent))
            {
                return;
            }
            else
            {
                farmLand.Plant(parent);
                SlotController.Instance.SetItemCount(context.backpackContainer, context.containerIndex, itemCount - 1);
            }
        }

    }
}
