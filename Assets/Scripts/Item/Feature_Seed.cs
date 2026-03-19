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
    private ItemBase_SO parent;//TODO: 目前设计上Feature没有指向父Item的引用，如果需要访问父Item数据，可以考虑在使用时传入父Item或者在Feature中添加一个初始化方法来设置父Item引用
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
