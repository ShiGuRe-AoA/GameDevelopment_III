using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Game/Feature/Placement")]
[SerializeField]
public class Feature_Placement : ItemFeature, IHoldTick,IHoldInteract,IEnterSelect,IExitSelect
{
    public int Length;   //放置长度
    public int Height;  //放置宽度
    public GameObject prefabObj;

    public void EnterSelect(EnterSelectContext context, out GameObject prefabInstance, out List<GameObject> cellInstance)
    {
        prefabInstance = Instantiate(context.placementPrefab);
        cellInstance = new List<GameObject>();
        //按照长宽生成范围指示物
        for (int x = 0; x < Length; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                GameObject newCell = Instantiate(context.cellPrefab);
                //设置cell位置
                Vector3Int pivotPos = GetPivotGridPos(prefabInstance.transform.position);
                Vector3 pos = WorldState.Instance.CellToWorld(pivotPos + new Vector3Int(x, y, 0));
                newCell.transform.position = pos;
                newCell.transform.SetParent(prefabInstance.transform);
                cellInstance.Add(newCell);
            }
        }
    }


    public void ExitSelect(ExitSelectContext context, ref GameObject prefabInstance, ref List<GameObject> cellInstance)
    {
        //销毁预制体和范围指示物
        if (prefabInstance != null) { Destroy(prefabInstance); }
        if (cellInstance != null)
            foreach (var cell in cellInstance)
        {
            if (cell != null) { Destroy(cell); }
        }
         prefabInstance = null;
         if(cellInstance != null) cellInstance.Clear();
    }
    public void OnHoldTick(HoldTickContext context, out bool isValid)
    {
        Vector3Int pivotGridPos = GetPivotGridPos(context.MousePos);
        Vector3 centerWorld = GetCenterWorldFromPivot(pivotGridPos);

        context.PlacementInstance.transform.position = centerWorld;

        bool _isValid = true;
        //检测虚影位置合法性
        foreach(var cell in context.CellInstance)
        {
            Vector3Int cellGrid = WorldState.Instance.WorldToCell(cell.transform.position);
            SpriteRenderer cellRenderer = cell.GetComponent<SpriteRenderer>();
            if (!WorldState.Instance.CheckEmpty(cellGrid))
            {
                cellRenderer.material = context.PhantomMat_R;
                _isValid = false;
            }
            else
            {
                cellRenderer.material = context.PhantomMat_G;
            }
        }
        SpriteRenderer prefabRenderer = context.PlacementInstance.GetComponent<SpriteRenderer>();
        //修改虚影材质
        if (!_isValid)
        {
            prefabRenderer.material = context.PhantomMat_R;
        }
        else
        {
            prefabRenderer.material = context.PhantomMat_G;
        }
        isValid = _isValid;
    }
    public void OnHoldInteract(HoldInteractContext context)
    {
        if (!context.isValid) { return; }
        int itemCount = context.backpackContainer.Items[context.containerIndex].count;
        Debug.Log("HoldInteract");
        if (itemCount <= 0) { return; }

        Vector3Int placeCell = GetPivotGridPos(context.MousePos);
        WorldState.Instance.PlaceEntity(placeCell, context.ItemID);

        SlotController.Instance.SetItemCount(context.backpackContainer, context.containerIndex, itemCount - 1);

    }

    public void UpdateMapData()
    {
        throw new System.NotImplementedException("TODO：考虑地图机器存储方案");
    }
    public Vector3Int GetPivotGridPos(Vector3 centerPos)
    {
        float cellX = WorldState.Instance.cellSize.x;
        float cellY = WorldState.Instance.cellSize.y;
        Tilemap mainTile = WorldState.Instance.MainTile;
        Vector3 pivotPos = centerPos - new Vector3(Length * 0.5f * cellX, Height * 0.5f * cellY);
        Vector3Int pivotGridPos = mainTile.WorldToCell(pivotPos);
        return pivotGridPos;
    }
    public Vector3 GetCenterWorldFromPivot(Vector3Int pivotGridPos)
    {
        Tilemap mainTile = WorldState.Instance.MainTile;

        float cellX = WorldState.Instance.cellSize.x;
        float cellY = WorldState.Instance.cellSize.y;

        // pivot格子的中心世界坐标
        Vector3 pivotWorld = mainTile.GetCellCenterWorld(pivotGridPos);

        // 加回半个物体尺寸
        Vector3 centerWorld = pivotWorld + new Vector3(
            Length * 0.5f * cellX,
            Height * 0.5f * cellY
        );

        return centerWorld;
    }

}
